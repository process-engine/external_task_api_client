// tslint:disable:typedef
const params = {
  externalTaskId: ':external_task_id',
  externalTaskTopic: ':external_task_topic',
};

const queryParams = {
};

const paths = {
  externalTasks: `/external_tasks/${params.externalTaskTopic}`,
  finishExternalTask: `/external_tasks/${params.externalTaskId}/finish`,
};

/**
 * Contains the endpoints and various rest parameters used by the management api.
 */
export const restSettings = {
  /**
   * A collection of all url parameters employed by the management api.
   */
  params: params,
  /**
   * A collection of all query parameters employed by the management api.
   */
  queryParams: queryParams,
  /**
   * A collection of all urls employed by the management api.
   */
  paths: paths,
};
