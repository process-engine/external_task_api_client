import {Logger} from 'loggerhythm';
import * as uuid from 'uuid';

import {IIdentity} from '@essential-projects/iam_contracts';
import {
  ExternalTask,
  HandleExternalTaskAction,
  IExternalTaskApi,
  IExternalTaskWorker,
} from '@process-engine/external_task_api_contracts';

const logger: Logger = Logger.createLogger('pprocessengine:external_task:worker');

export class ExternalTaskWorker implements IExternalTaskWorker {

  // eslint-disable-next-line @typescript-eslint/member-naming
  private readonly _workerId = uuid.v4();
  private readonly lockDuration = 30000;
  private readonly externalTaskApi: IExternalTaskApi = undefined;

  constructor(externalTaskApi: IExternalTaskApi) {
    this.externalTaskApi = externalTaskApi;
  }

  public get workerId(): string {
    return this._workerId;
  }

  public async waitForAndHandle<TPayload>(
    identity: IIdentity,
    topic: string,
    maxTasks: number,
    longpollingTimeout: number,
    handleAction: HandleExternalTaskAction<TPayload>,
  ): Promise<void> {

    const keepPolling = true;
    while (keepPolling) {

      const externalTasks = await this.fetchAndLockExternalTasks<TPayload>(
        identity,
        topic,
        maxTasks,
        longpollingTimeout,
      );

      if (externalTasks.length === 0) {
        await this.sleep(1000);
        continue;
      }

      const executeTaskPromises: Array<Promise<void>> = [];

      for (const externalTask of externalTasks) {
        executeTaskPromises.push(this.executeExternalTask(identity, externalTask, handleAction));
      }

      await Promise.all(executeTaskPromises);
    }
  }

  private async fetchAndLockExternalTasks<TPayload>(
    identity: IIdentity,
    topic: string,
    maxTasks: number,
    longpollingTimeout: number,
  ): Promise<Array<ExternalTask<TPayload>>> {

    try {
      return await this
        .externalTaskApi
        .fetchAndLockExternalTasks<TPayload>(identity, this.workerId, topic, maxTasks, longpollingTimeout, this.lockDuration);
    } catch (error) {

      logger.error(
        'An error occured during fetchAndLock!',
        'This can happen, if the tasks were already locked by another worker, before this worker could apply its own lock.',
        error);

      // Returning an empty Array here, since "waitForAndHandle" already implements a timeout, in case no tasks are available for processing.
      // No need to do that twice.
      return [];
    }
  }

  private async executeExternalTask<TPayload>(
    identity: IIdentity,
    externalTask: ExternalTask<TPayload>,
    handleAction: HandleExternalTaskAction<TPayload>,
  ): Promise<void> {

    try {
      const lockExtensionBuffer = 5000;
      const interval = setInterval(async (): Promise<void> => this.extendLocks<TPayload>(identity, externalTask), this.lockDuration - lockExtensionBuffer);
      const result = await handleAction(externalTask);
      clearInterval(interval);

      await result.sendToExternalTaskApi(this.externalTaskApi, identity, this.workerId);
    } catch (error) {
      logger.error('Failed to execute ExternalTask!', error);
      await this.externalTaskApi.handleServiceError(identity, this.workerId, externalTask.id, error.message, '');
    }
  }

  private async extendLocks<TPayload>(identity: IIdentity, externalTask: ExternalTask<TPayload>): Promise<void> {
    try {
      await this.externalTaskApi.extendLock(identity, this.workerId, externalTask.id, this.lockDuration);
    } catch (error) {
      // This can happen, if the lock-extension was performed after the task was already finished.
      // Since this isn't really an error, a warning suffices here.
      logger.warn(`An error occured while trying to extend the lock for ExternalTask ${externalTask.id}`, error);
    }
  }

  private async sleep(milliseconds: number): Promise<void> {
    return new Promise<void>((resolve: Function): void => {
      setTimeout((): void => { resolve(); }, milliseconds);
    });
  }

}
