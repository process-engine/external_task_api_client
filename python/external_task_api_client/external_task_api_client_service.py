import aiohttp
import asyncio
import json


class ExternalTaskApiClientService:

    def __init__(self, base_url):
        self.__base_url = base_url
        self.__extend_lock_uri = 'task/{}/extend_lock'
        self.__fetch_and_lock_uri = 'fetch_and_lock'
        self.__finish_external_task_uri = 'task/{}/finish'
        self.__handle_bpmn_error_uri = 'task/{}/handle_bpmn_error'
        self.__handle_service_error_uri = 'task/{}/handle_service_error'
        self.__combined_url = '{}/api/external_task/v1/{}'

    async def extend_lock(self, identity, worker_id, external_task_id, additional_duration):
        uri = self.__extend_lock_uri.format(external_task_id)
        request = {
            "workerId": worker_id,
            "additionalDuration": additional_duration
        }

        return await self.__send_post_to_external_task_api(identity, uri, request)

    async def fetch_and_lock_external_tasks(self, identity, worker_id, topic_name, max_tasks, long_polling_timeout, lock_duration):
        uri = self.__fetch_and_lock_uri
        request = {
            "workerId": worker_id,
            "topicName": topic_name,
            "maxTasks": max_tasks,
            "longPollingTimeout": long_polling_timeout,
            "lockDuration": lock_duration
        }

        return await self.__send_post_to_external_task_api(identity, uri, request)

    async def finish_external_task(self, identity, worker_id, external_task_id, payload):
        uri = self.__finish_external_task_uri.format(external_task_id)
        request = {
            "workerId": worker_id,
            "result": payload
        }

        await self.__send_post_to_external_task_api(identity, uri, request)

    async def handle_bpmn_error(self, identity, worker_id, external_task_id, error_code):
        uri = self.__handle_bpmn_error_uri.format(external_task_id)
        request = {
            "workerId": worker_id,
            "errorCode": error_code
        }

        await self.__send_post_to_external_task_api(identity, uri, request)

    async def handle_service_error(self, identity, worker_id, external_task_id, error_message, error_details):
        uri = self.__handle_service_error_uri.format(external_task_id)
        request = {
            "workerId": worker_id,
            "errorMessage": error_message,
            "errorDetails": error_details
        }

        await self.__send_post_to_external_task_api(identity, uri, request)

    async def __send_post_to_external_task_api(self, identity, uri, request):
        headers = self.__get_authorization_header(identity["token"])
        url = self.__combine_with_base_url(uri)

        async with aiohttp.ClientSession(headers=headers) as session:
            async with session.post(url, json=request) as response:
                response.raise_for_status()
                if response.status == 200:
                    return await response.json()


    def __get_authorization_header(self, token):
        """Builds the HTTP header line for authorization; uses the BEARER token format"""
        return {'Authorization': 'Bearer {}'.format(token)}


    def __combine_with_base_url(self, uri):
        return self.__combined_url.format(self.__base_url, uri)
