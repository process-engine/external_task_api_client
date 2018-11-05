class ExternalTaskBpmnError:
    def __init__(self, externalTaskId, errorMessage, errorDetails):
        self.__externalTaskId = externalTaskId
        self.__errorMessage = errorMessage
        self.__errorDetails = errorDetails

    async def sendToExternalTaskApi(self, externalTaskApi, identity, workerId):
        await externalTaskApi.handleBpmnError(identity, workerId, self.__externalTaskId, self.__errorMessage, self.__errorDetails)
