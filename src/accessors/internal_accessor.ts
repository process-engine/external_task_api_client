import {UnauthorizedError} from '@essential-projects/errors_ts';
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

    this._ensureIsAuthorized(identity);

    return this._externalApiService.fetchAndLockExternalTasks(identity, workerId, topicName, maxTasks, longPollingTimeout, lockDuration);
  }

  public async extendLock(identity: IIdentity, workerId: string, externalTaskId: string, additionalDuration: number): Promise<void> {

    this._ensureIsAuthorized(identity);

    return this._externalApiService.extendLock(identity, workerId, externalTaskId, additionalDuration);
  }

  public async handleBpmnError(identity: IIdentity, workerId: string, externalTaskId: string, errorCode: string): Promise<void> {

    this._ensureIsAuthorized(identity);

    return this._externalApiService.handleBpmnError(identity, workerId, externalTaskId, errorCode);
  }

  public async handleServiceError(identity: IIdentity,
                                  workerId: string,
                                  externalTaskId: string,
                                  errorMessage: string,
                                  errorDetails: string): Promise<void> {

    this._ensureIsAuthorized(identity);

    return this._externalApiService.handleServiceError(identity, workerId, externalTaskId, errorMessage, errorDetails);
  }

  public async finishExternalTask(identity: IIdentity, workerId: string, externalTaskId: string, payload: any): Promise<void> {

    this._ensureIsAuthorized(identity);

    return this._externalApiService.finishExternalTask(identity, workerId, externalTaskId, payload);
  }

  private _ensureIsAuthorized(identity: IIdentity): void {

    // Note: When using an external accessor, this check is performed by the ConsumerApiHttp module.
    // Since that component is bypassed by the internal accessor, we need to perform this check here.
    const authTokenNotProvided: boolean = !identity || typeof identity.token !== 'string';
    if (authTokenNotProvided) {
      throw new UnauthorizedError('No auth token provided!');
    }
  }
}
