import {IHttpClient, IRequestOptions, IResponse} from '@essential-projects/http_contracts';
import {IIdentity} from '@essential-projects/iam_contracts';

import {
  ExtendLockRequestPayload,
  ExternalTask,
  FetchAndLockRequestPayload,
  FinishExternalTaskRequestPayload,
  HandleBpmnErrorRequestPayload,
  HandleServiceErrorRequestPayload,
  IExternalTaskApi,
  restSettings,
} from '@process-engine/external_task_api_contracts';

export class ExternalTaskApiExternalAccessor implements IExternalTaskApi {

  private baseUrl: string = 'api/external_task/v1';

  private _httpClient: IHttpClient = undefined;

  public config: any;

  constructor(httpClient: IHttpClient) {
    this._httpClient = httpClient;
  }

  public async fetchAndLockExternalTasks(identity: IIdentity,
                                         workerId: string,
                                         topicName: string,
                                         maxTasks: number,
                                         longPollingTimeout: number,
                                         lockDuration: number): Promise<Array<ExternalTask>> {

    const requestAuthHeaders: IRequestOptions = this._createRequestAuthHeaders(identity);

    let url: string = restSettings.paths.fetchAndLockExternalTasks
      .replace(restSettings.params.workerId, workerId);

    url = this._applyBaseUrl(url);

    const payload: FetchAndLockRequestPayload =
      new FetchAndLockRequestPayload(topicName, maxTasks, longPollingTimeout, lockDuration);

    const httpResponse: IResponse<Array<ExternalTask>> =
      await this._httpClient.post<FetchAndLockRequestPayload, Array<ExternalTask>>(url, payload, requestAuthHeaders);

    return httpResponse.result;
  }

  public async extendLock(identity: IIdentity, workerId: string, externalTaskId: string, additionalDuration: number): Promise<void> {

    const requestAuthHeaders: IRequestOptions = this._createRequestAuthHeaders(identity);

    let url: string = restSettings.paths.extendLock
      .replace(restSettings.params.workerId, workerId)
      .replace(restSettings.params.externalTaskId, externalTaskId);

    url = this._applyBaseUrl(url);

    const payload: ExtendLockRequestPayload = new ExtendLockRequestPayload(additionalDuration);

    await this._httpClient.post<ExtendLockRequestPayload, any>(url, payload, requestAuthHeaders);
  }

  public async handleBpmnError(identity: IIdentity, workerId: string, externalTaskId: string, errorCode: string): Promise<void> {

    const requestAuthHeaders: IRequestOptions = this._createRequestAuthHeaders(identity);

    let url: string = restSettings.paths.handleBpmnError
      .replace(restSettings.params.workerId, workerId)
      .replace(restSettings.params.externalTaskId, externalTaskId);

    url = this._applyBaseUrl(url);

    const payload: HandleBpmnErrorRequestPayload = new HandleBpmnErrorRequestPayload(errorCode);

    await this._httpClient.post<HandleBpmnErrorRequestPayload, any>(url, payload, requestAuthHeaders);
  }

  public async handleServiceError(identity: IIdentity,
                                  workerId: string,
                                  externalTaskId: string,
                                  errorMessage: string,
                                  errorDetails: string): Promise<void> {

    const requestAuthHeaders: IRequestOptions = this._createRequestAuthHeaders(identity);

    let url: string = restSettings.paths.handleServiceError
      .replace(restSettings.params.workerId, workerId)
      .replace(restSettings.params.externalTaskId, externalTaskId);

    url = this._applyBaseUrl(url);

    const payload: HandleServiceErrorRequestPayload = new HandleServiceErrorRequestPayload(errorMessage, errorDetails);

    await this._httpClient.post<HandleServiceErrorRequestPayload, any>(url, payload, requestAuthHeaders);
  }

  public async finishExternalTask(identity: IIdentity, workerId: string, externalTaskId: string, results: any): Promise<void> {

    const requestAuthHeaders: IRequestOptions = this._createRequestAuthHeaders(identity);

    let url: string = restSettings.paths.finishExternalTask
      .replace(restSettings.params.workerId, workerId)
      .replace(restSettings.params.externalTaskId, externalTaskId);

    url = this._applyBaseUrl(url);

    const payload: FinishExternalTaskRequestPayload = new FinishExternalTaskRequestPayload(results);

    await this._httpClient.post<FinishExternalTaskRequestPayload, any>(url, payload, requestAuthHeaders);
  }

  private _createRequestAuthHeaders(identity: IIdentity): IRequestOptions {

    const noAuthTokenProvided: boolean = !identity || typeof identity.token !== 'string';
    if (noAuthTokenProvided) {
      return {};
    }

    const requestAuthHeaders: IRequestOptions = {
      headers: {
        Authorization: `Bearer ${identity.token}`,
      },
    };

    return requestAuthHeaders;
  }

  private _applyBaseUrl(url: string): string {
    return `${this.baseUrl}${url}`;
  }
}
