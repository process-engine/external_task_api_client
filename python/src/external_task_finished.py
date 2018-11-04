class ExternalTaskFinished:
    def __init__(self, externalTaskId, result):
        self.__externalTaskId = externalTaskId
        self.__result = result

    async def sendToExternalTaskApi(self, externalTaskApi, identity, workerId):
        await externalTaskApi.finishExternalTask(identity, workerId, self.__externalTaskId, self.__result)
