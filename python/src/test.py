import asyncio
import json
import external_task_api_client_service as etacs
import external_task_finished as etf
import external_task_worker as etw


async def handle(externalTask):
    print(externalTask)
    return etf.ExternalTaskFinished(externalTask["id"], {"testprop": "Hallo"})


async def main():
    identity = {"token": "ZHVtbXlfdG9rZW4="}
    topicName = "TestTopic"

    externalTaskApi = etacs.ExternalTaskApiClientService(
        "http://localhost:8000")

    worker = etw.ExternalTaskWorker(externalTaskApi)

    await worker.waitForHandle(identity, topicName, 10, 10000, handle)

asyncio.run(main())
