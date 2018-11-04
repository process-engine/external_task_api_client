import asyncio
import uuid


class ExternalTaskWorker:

    def __init__(self, externalTaskApi):
        self.__lockDuration = 30000
        self.__externalTaskApi = externalTaskApi
        self.workerId = str(uuid.uuid4())

    async def waitForHandle(self, identity, topic, maxTasks, longPollingTimeout, handleAction):
        while True:
            externalTasks = await self.__fetchAndLockExternalTasks(identity, topic, maxTasks, longPollingTimeout)

            if len(externalTasks) > 0:
                tasks = []

                for externalTask in externalTasks:
                    tasks.append(self.__executeExternalTask(
                        identity, externalTask, handleAction))

                if len(tasks) > 0:
                    await asyncio.wait(tasks)

    async def __fetchAndLockExternalTasks(self, identity, topicName, maxTasks, longPollingTimeout):
        try:
            return await self.__externalTaskApi.fetchAndLockExternalTasks(
                identity, self.workerId, topicName, maxTasks, longPollingTimeout, self.__lockDuration)
        except Exception as exception:
            print(exception)

            await asyncio.sleep(1000)
            return await self.__fetchAndLockExternalTasks(
                identity, topicName, maxTasks, longPollingTimeout)

    async def __executeExternalTask(self, identity, externalTask, handleAction):
        try:
            result = await handleAction(externalTask)

            await result.sendToExternalTaskApi(
                self.__externalTaskApi, identity, self.workerId)
        except Exception as exception:
            print(exception)
            await self.__externalTaskApi.handleServiceError(identity, self.workerId, externalTask["id"], exception, "")
