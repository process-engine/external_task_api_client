import {Logger} from 'loggerhythm';
import * as uuid from 'uuid';

import {IIdentity} from '@essential-projects/iam_contracts';
import {
  ExternalTask,
  HandleExternalTaskAction,
  IExternalTaskApi,
  IExternalTaskResult,
  IExternalTaskWorker,
} from '@process-engine/external_task_api_contracts';

const logger: Logger = Logger.createLogger('pprocessengine:external_task:worker');

export class ExternalTaskWorker implements IExternalTaskWorker {
  private readonly _workerId: string = uuid.v4();
  private readonly _lockDuration: number = 30000;
  private readonly _externalTaskApi: IExternalTaskApi = undefined;

  constructor(externalTaskApi: IExternalTaskApi) {
    this._externalTaskApi = externalTaskApi;
  }

  public get workerId(): string {
    return this._workerId;
  }

  public async waitForAndHandle<TPayload>(identity: IIdentity,
                                          topic: string,
                                          maxTasks: number,
                                          longpollingTimeout: number,
                                          handleAction: HandleExternalTaskAction<TPayload>,
                                         ): Promise<void> {

    const keepPolling: boolean = true;
    while (keepPolling) {

      const externalTasks: Array<ExternalTask<TPayload>> =
        await this._fetchAndLockExternalTasks<TPayload>(
          identity,
          topic,
          maxTasks,
          longpollingTimeout,
        );

      // tslint:disable-next-line:no-magic-numbers
      const interval: NodeJS.Timeout = setInterval(async() => await this._extendLocks(identity, externalTasks), this._lockDuration - 5000);

      const executeTaskPromises: Array<Promise<void>> = [];

      for (const externalTask of externalTasks) {
        executeTaskPromises.push(this._executeExternalTask(identity, externalTask, handleAction));
      }

      await Promise.all(executeTaskPromises);

      clearInterval(interval);
    }
  }

  private async _fetchAndLockExternalTasks<TPayload>(identity: IIdentity,
                                                     topic: string,
                                                     maxTasks: number,
                                                     longpollingTimeout: number,
                                                   ): Promise<Array<ExternalTask<TPayload>>> {

    try {
      return await this._externalTaskApi.fetchAndLockExternalTasks<TPayload>(
        identity,
        this._workerId,
        topic,
        maxTasks,
        longpollingTimeout,
        this._lockDuration,
      );
    } catch (error) {

      logger.error(error);
      // tslint:disable-next-line:no-magic-numbers
      await this._sleep(1000);

      return await this._fetchAndLockExternalTasks<TPayload>(identity, topic, maxTasks, longpollingTimeout);
    }
  }

  private async _executeExternalTask<TPayload>(identity: IIdentity,
                                               externalTask: ExternalTask<TPayload>,
                                               handleAction: HandleExternalTaskAction<TPayload>,
                                              ): Promise<void> {

    try {
      const result: IExternalTaskResult = await handleAction(externalTask);
      await result.sendToExternalTaskApi(this._externalTaskApi, identity, this._workerId);
    } catch (error) {
      logger.error(error);
      await this._externalTaskApi.handleServiceError(identity, this._workerId, externalTask.id, error.message, '');
    }
  }

  private async _extendLocks(identity: IIdentity, externalTasks: Array<ExternalTask<any>>): Promise<void> {
    for (const externalTask of externalTasks) {
      await this._externalTaskApi.extendLock(identity, this._workerId, externalTask.id, this._lockDuration);
    }
  }

  private async _sleep(milliseconds: number): Promise<void> {
    return new Promise<void>((resolve: Function): void => {
      setTimeout(() => resolve(), milliseconds);
    });
  }
}
