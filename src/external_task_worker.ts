import {
  ExternalTask,
  IExternalTaskWorker,
  IExternalTaskApi,
} from '@process-engine/external_task_api_contracts';
import {IIdentity} from '@essential-projects/iam_contracts';

export class ExternalTaskWorker implements IExternalTaskWorker {
  private readonly _workerId: string = 'TestWorker';
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
    handleAction: (externalTask: ExternalTask<TPayload>) => Promise<TResult>): Promise<void> {

    const externalTasks: Array<ExternalTask<TPayload>> =
      await this._externalTaskApi.fetchAndLockExternalTasks<TPayload>(
        identity,
        this._workerId,
        topic,
        maxTasks,
        longpollingTimeout,
        this._lockDuration
      );

    for (const externalTask of externalTasks) {
      const result = await handleAction(externalTask);
      await this._externalTaskApi.finishExternalTask(identity, this._workerId, externalTask.id, result);
    }

    await this.waitForAndHandle(identity, topic, maxTasks, longpollingTimeout, handleAction);
  }
}
