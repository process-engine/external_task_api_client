import {IIdentity} from '@essential-projects/iam_contracts';

import {
  ExternalTask,
  IExternalTaskApi,
} from '@process-engine/external_task_api_contracts';

export class ExternalTaskApiInternalAccessor implements IExternalTaskApi {

  private readonly _externalApiService: IExternalTaskApi = undefined;

  constructor(externalApiService: IExternalTaskApi) {
    this._externalApiService = externalApiService;
  }

  public async fetchAndLockExternalTasks(identity: IIdentity,
                                         workerId: string,
                                         topicName: string,
                                         maxTasks: number,
                                         longPollingTimeout: number,
                                         lockDuration: number): Promise<Array<ExternalTask>> {
    return this._externalApiService.fetchAndLockExternalTasks(identity, workerId, topicName, maxTasks, longPollingTimeout, lockDuration);
  }

  public async extendLock(identity: IIdentity, workerId: string, externalTaskId: string, additionalDuration: number): Promise<void> {
    return this._externalApiService.extendLock(identity, workerId, externalTaskId, additionalDuration);
  }

  public async handleBpmnError(identity: IIdentity, workerId: string, externalTaskId: string, errorCode: string): Promise<void> {
    return this._externalApiService.handleBpmnError(identity, workerId, externalTaskId, errorCode);
  }

  public async handleServiceError(identity: IIdentity,
                                  workerId: string,
                                  externalTaskId: string,
                                  errorMessage: string,
                                  errorDetails: string): Promise<void> {
    return this._externalApiService.handleServiceError(identity, workerId, externalTaskId, errorMessage, errorDetails);
  }

  public async finishExternalTask(identity: IIdentity, workerId: string, externalTaskId: string, payload: any): Promise<void> {
    return this._externalApiService.finishExternalTask(identity, workerId, externalTaskId, payload);
  }
}
