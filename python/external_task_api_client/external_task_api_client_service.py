import aiohttp
import asyncio
import json


class ExternalTaskApiClientService:

    def __init__(self, baseUrl):
        self.__baseUrl = baseUrl

    async def extendLock(self, identity, workerId, externalTaskId, additionalDuration):
        uri = "task/" + externalTaskId + "/extend_lock"
        request = {
            "workerId": workerId,
            "additionalDuration": additionalDuration
        }

        return await self.__sendPostToExternalTaskApi(identity, uri, request)

    async def fetchAndLockExternalTasks(self, identity, workerId, topicName, maxTasks, longPollingTimeout, lockDuration):
        uri = "fetch_and_lock"
        request = {
            "workerId": workerId,
            "topicName": topicName,
            "maxTasks": maxTasks,
            "longPollingTimeout": longPollingTimeout,
            "lockDuration": lockDuration
        }

        return await self.__sendPostToExternalTaskApi(identity, uri, request)

    async def finishExternalTask(self, identity, workerId, externalTaskId, payload):
        uri = "task/" + externalTaskId + "/finish"
        request = {
            "workerId": workerId,
            "result": payload
        }

        await self.__sendPostToExternalTaskApi(identity, uri, request)

    async def handleBpmnError(self, identity, workerId, externalTaskId, errorCode):
        uri = "task/" + externalTaskId + "/handle_bpmn_error"
        request = {
            "workerId": workerId,
            "errorCode": errorCode
        }

        await self.__sendPostToExternalTaskApi(identity, uri, request)

    async def handleServiceError(self, identity, workerId, externalTaskId, errorMessage, errorDetails):
        uri = "task/" + externalTaskId + "/handle_service_error"
        request = {
            "workerId": workerId,
            "errorMessage": errorMessage,
            "errorDetails": errorDetails
        }

        await self.__sendPostToExternalTaskApi(identity, uri, request)

    async def __sendPostToExternalTaskApi(self, identity, uri, request):
        headers = {"Authorization": "Bearer " + identity["token"]}
        url = self.__combineWithBaseUrl(uri)

        async with aiohttp.ClientSession(headers=headers) as session:
            async with session.post(url, json=request) as response:
                response.raise_for_status()
                if response.status == 200:
                    return await response.json()

    def __combineWithBaseUrl(self, uri):
        return self.__baseUrl + "/api/external_task/v1/" + uri
