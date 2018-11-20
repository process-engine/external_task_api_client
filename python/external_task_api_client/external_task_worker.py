import asyncio
import threading
import uuid


class ExternalTaskWorker:
    """ Implements the Business Logic for proper handling of External Tasks. """

    def __init__(self, external_task_api):
        self.__lock_duration = 30000
        self.__external_task_api = external_task_api
        self.__extend_lock_timer = None
        self.worker_id = str(uuid.uuid4())


    async def wait_for_handle(self, identity, topic, max_tasks, long_polling_timeout, handle_action):
        """
        Wait for an External Task to appear for the given topic;
        after an External Task has been locked, this will call the action specified in handle_action.

        You can simply call this and cause the Worker to wait forever on the given Topic.

        :identity: The identity, we want to execute the action in handle_action; the identity is verified by the IAM-System of the ProcessEngine.
        :topic: Specifies the name of the External Task to react to; in the diagram, you will find the specification for the topic.
        :max_tasks: the maximum amount of tasks we want to fetch at once.
        :long_polling_timeout: This specifies the timeout to wait before abandon a request and ask again.
        :handle_action: The callback to ask to do the actual work.

        :returns: Nothing, as it runs for ever.
        """
        while True:
            external_tasks = await self.__fetch_and_lock_external_tasks(
                    identity,
                    topic,
                    max_tasks,
                    long_polling_timeout
            )

            self.__start_extend_lock_timer(
                identity,
                external_tasks,
                (self.__lock_duration - 5000) / 1000
            )

            try:
                tasks = []

                for external_task in external_tasks:
                    task = self.__execute_external_task(
                            identity,
                            external_task,
                            handle_action
                    )

                    tasks.append(task)

                if len(tasks) > 0:
                    await asyncio.wait(tasks)

            finally:
                self.__extend_lock_timer.cancel()


    async def __fetch_and_lock_external_tasks(self, identity, topic_name, max_tasks, long_polling_timeout):
        """
        :return: The result of the fetch and lock call to the ProcessEngine.
        """
        try:
            return await self.__external_task_api.fetch_and_lock_external_tasks(
                identity,
                self.worker_id,
                topic_name,
                max_tasks,
                long_polling_timeout,
                self.__lock_duration)

        except Exception as exception:
            print('There was an exception as we tried to fetch an lock: "{}"'.format(exception))

            await asyncio.sleep(1)

            # restart the fetch and lock
            return await self.__fetch_and_lock_external_tasks(
                identity,
                topic_name,
                max_tasks,
                long_polling_timeout)


    def __extend_locks(self, identity, external_tasks):
        """ Uses the External Task API to extend a lock; use this if you need more time to handle the work. """
        for external_task in external_tasks:
            asyncio.run(
                self.__external_task_api.extend_lock(
                    identity,
                    self.worker_id,
                    external_task["id"],
                    self.__lock_duration
                )
            )


    def __start_extend_lock_timer(self, identity, external_tasks, interval):
        """
        """
        self.__extend_lock_timer = threading.Timer(
            interval,
            self.__extend_locks,
            args=[identity, external_tasks]
        )
        self.__extend_lock_timer.start()

        return


    async def __execute_external_task(self, identity, external_task, handle_action):
        """ Call the handle_action callback we should use for this External Task. """
        try:
            handle_action_result = await handle_action(external_task)

            # Use the handle_action result to answer to the ProcessEngine.
            await handle_action_result .send_to_external_task_api(
                self.__external_task_api,
                identity,
                self.worker_id
            )

        except Exception as exception:
            print('The External Task handle Action caused an Exception: "{}"'.format(exception))
            print('We will answer with a service error to the ProcessEngine.')

            await self.__external_task_api .handle_service_error(
                identity,
                self.worker_id,
                external_task["id"],
                exception,
                ""
            )
