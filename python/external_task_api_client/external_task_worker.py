import asyncio
import threading
import uuid


class ExternalTaskWorker:

    def __init__(self, external_task_api):
        self.__lock_duration = 30000
        self.__external_task_api = external_task_api
        self.worker_id = str(uuid.uuid4())

    async def wait_for_handle(self, identity, topic, max_tasks, long_polling_timeout, handle_action):
        while True:
            external_tasks = await self.__fetch_and_lock_external_tasks(identity, topic, max_tasks, long_polling_timeout)

            timer = self.__start_extend_lock_timer(
                identity, external_tasks, (self.__lock_duration - 5000) / 1000)

            try:
                tasks = []

                for external_task in external_tasks:
                    tasks.append(self.__execute_external_task(
                        identity, external_task, handle_action))

                if len(tasks) > 0:
                    await asyncio.wait(tasks)
            finally:
                timer.cancel()

    async def __fetch_and_lock_external_tasks(self, identity, topic_name, max_tasks, long_polling_timeout):
        try:
            return await self.__external_task_api.fetch_and_lock_external_tasks(
                identity, self.worker_id, topic_name, max_tasks, long_polling_timeout, self.__lock_duration)
        except Exception as exception:
            print(exception)

            await asyncio.sleep(1)
            return await self.__fetch_and_lock_external_tasks(
                identity, topic_name, max_tasks, long_polling_timeout)

    def __extend_locks(self, identity, external_tasks):
        for external_task in external_tasks:
            asyncio.run(self.__external_task_api.extend_lock(
                identity, self.worker_id, external_task["id"], self.__lock_duration))

    def __start_extend_lock_timer(self, identity, external_tasks, interval):
        timer = threading.Timer(
            interval, self.__extend_locks, args=[identity, external_tasks])
        timer.start()

        return timer

    async def __execute_external_task(self, identity, external_task, handle_action):
        try:
            result = await handle_action(external_task)

            await result.send_to_external_task_api(
                self.__external_task_api, identity, self.worker_id)
        except Exception as exception:
            print(exception)
            await self.__external_task_api.handle_service_error(identity, self.worker_id, external_task["id"], exception, "")
