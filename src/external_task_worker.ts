import * as uuid from 'uuid';

import {
  ExternalTask,
  HandleExternalTaskAction,
  IExternalTaskWorker,
  IExternalTaskApi,
  IHandleExternalTaskResult
} from '@process-engine/external_task_api_contracts';
import {IIdentity} from '@essential-projects/iam_contracts';

export class ExternalTaskWorker implements IExternalTaskWorker {
  private readonly _workerId: string = uuid.v4();
  private readonly _lockDuration: number = 30000;
  private readonly _externalTaskApi: IExternalTaskApi = undefined;

  constructor(externalTaskApi: IExternalTaskApi) {
    this._externalTaskApi = externalTaskApi;
  }

  public async waitForAndHandle<TPayload, TResult>(
    identity: IIdentity,
    topic: string,
    maxTasks: number,
    longpollingTimeout: number,
    handleAction: HandleExternalTaskAction<TPayload>): Promise<void> {

    const externalTasks: Array<ExternalTask<TPayload>> =
      await this._fetchAndLockExternalTasks<TPayload>(
        identity,
        topic,
        maxTasks,
        longpollingTimeout
      );

    for (const externalTask of externalTasks) {
      try {

        const result: IHandleExternalTaskResult = await handleAction(externalTask);
        await result.applyTo(this._externalTaskApi, identity, this._workerId);

      } catch (exception) {
        await this._externalTaskApi.handleServiceError(identity, this._workerId, externalTask.id, exception.message, '');
      }
    }

    await this.waitForAndHandle(identity, topic, maxTasks, longpollingTimeout, handleAction);
  }

  private async _fetchAndLockExternalTasks<TPayload>(
    identity: IIdentity,
    topic: string,
    maxTasks: number,
    longpollingTimeout: number): Promise<Array<ExternalTask<TPayload>>> {

    try {
      return await this._externalTaskApi.fetchAndLockExternalTasks<TPayload>(
        identity,
        this._workerId,
        topic,
        maxTasks,
        longpollingTimeout,
        this._lockDuration
      );
    } catch (exception) {
      await this._sleep(1000);
      return await this._fetchAndLockExternalTasks<TPayload>(identity, topic, maxTasks, longpollingTimeout);
    }
  }

  private async _sleep(milliseconds: number): Promise<void> {
    return new Promise<void>((resolve) => {
      setTimeout(() => resolve(), milliseconds);
    });
  }
}
