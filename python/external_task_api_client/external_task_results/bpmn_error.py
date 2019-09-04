class ExternalTaskBpmnError:
    def __init__(self, external_task_id, error_code):
        self.__external_task_id = external_task_id
        self.__error_code = error_code

    async def send_to_external_task_api(self, external_task_api, identity, worker_id):
        await external_task_api.handle_bpmn_error(
            identity,
            worker_id,
            self.__external_task_id,
            self.__error_code
        )
