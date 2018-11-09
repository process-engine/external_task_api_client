class ExternalTaskFinished:
    def __init__(self, external_task_id, result):
        self.__external_task_id = external_task_id
        self.__result = result

    async def send_to_external_task_api(self, external_task_api, identity, worker_id):
        await external_task_api.finish_external_task(
                identity,
                worker_id,
                self.__external_task_id,
                self.__result
            )
