import {UnauthorizedError} from '@essential-projects/errors_ts';
import {IIdentity} from '@essential-projects/iam_contracts';

import {
  APIs,
  DataModels,
} from '@process-engine/consumer_api_contracts';

export class ExternalTaskApiInternalAccessor implements APIs.IExternalTaskConsumerApi {

  private readonly externalApiService: APIs.IExternalTaskConsumerApi = undefined;

  constructor(externalApiService: APIs.IExternalTaskConsumerApi) {
    this.externalApiService = externalApiService;
  }

  public async fetchAndLockExternalTasks<TPayloadType>(
    identity: IIdentity,
    workerId: string,
    topicName: string,
    maxTasks: number,
    longPollingTimeout: number,
    lockDuration: number,
  ): Promise<Array<DataModels.ExternalTask.ExternalTask<TPayloadType>>> {

    this.ensureIsAuthorized(identity);

    return this
      .externalApiService
      .fetchAndLockExternalTasks<TPayloadType>(identity, workerId, topicName, maxTasks, longPollingTimeout, lockDuration);
  }

  public async extendLock(identity: IIdentity, workerId: string, externalTaskId: string, additionalDuration: number): Promise<void> {

    this.ensureIsAuthorized(identity);

    return this.externalApiService.extendLock(identity, workerId, externalTaskId, additionalDuration);
  }

  public async handleBpmnError(
    identity: IIdentity,
    workerId: string,
    externalTaskId: string,
    errorCode: string,
    errorMessage?: string,
  ): Promise<void> {
    this.ensureIsAuthorized(identity);
    return this.externalApiService.handleBpmnError(identity, workerId, externalTaskId, errorCode, errorMessage);
  }

  public async handleServiceError(
    identity: IIdentity,
    workerId: string,
    externalTaskId: string,
    errorMessage: string,
    errorDetails: string,
    errorCode?: string,
  ): Promise<void> {
    this.ensureIsAuthorized(identity);
    return this.externalApiService.handleServiceError(identity, workerId, externalTaskId, errorMessage, errorDetails, errorCode);
  }

  public async finishExternalTask<TResultType>(
    identity: IIdentity,
    workerId: string,
    externalTaskId: string,
    payload: TResultType,
  ): Promise<void> {

    this.ensureIsAuthorized(identity);

    return this.externalApiService.finishExternalTask(identity, workerId, externalTaskId, payload);
  }

  private ensureIsAuthorized(identity: IIdentity): void {

    // Note: When using an external accessor, this check is performed by the ConsumerApiHttp module.
    // Since that component is bypassed by the internal accessor, we need to perform this check here.
    const authTokenNotProvided: boolean = !identity || typeof identity.token !== 'string';
    if (authTokenNotProvided) {
      throw new UnauthorizedError('No auth token provided!');
    }
  }

}
