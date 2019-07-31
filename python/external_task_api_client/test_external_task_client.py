#!/usr/bin/env python
# -*- coding: utf-8 -*-
import asyncio
import unittest
import json

from external_task_api_client.external_task_api_client_service import ExternalTaskApiClientService
from external_task_api_client.external_task_worker import ExternalTaskWorker
from external_task_api_client.external_task_results.finished import ExternalTaskFinished


class TestHandleExternalTask(unittest.TestCase):
    def setUp(self):
        self.loop = asyncio.new_event_loop()
        asyncio.set_event_loop(None)

    async def __handle_external_task(self, task):
        print(json.dumps(task, sort_keys=True, indent=2))

        task_result = {"testprop": "Hallo"}

        return ExternalTaskFinished(task["id"], task_result)

    def test_handle_external_task(self):
        process_engine_location = 'http://localhost:54317'

        print('Using ProcessEngine at "{}"'.format(process_engine_location))

        external_task_client = ExternalTaskApiClientService(
            process_engine_location)

        worker = ExternalTaskWorker(external_task_client)

        @asyncio.coroutine
        def main_loop(worker):
            return worker.wait_for_handle(
                identity={"token": "ZHVtbXlfdG9rZW4="},
                topic="TestTopic",
                max_tasks=10,
                long_polling_timeout=10_000,
                handle_action=self.__handle_external_task
            )

        print("WARNING: This test has to be terminated manually.")
        self.loop.run_until_complete(main_loop(worker))


if __name__ == '__main__':
    unittest.main()
