namespace ProcessEngine.ExternalTaskAPI.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using EssentialProjects.IAM.Contracts;

    using ProcessEngine.ConsumerAPI.Contracts;
    using ProcessEngine.ConsumerAPI.Contracts.DataModel;

    /// <summary>
    /// Periodically fetches, locks and processes ExternalTasks for a given topic.
    /// </summary>
    public class ExternalTaskWorker<TExternalTaskPayload, TResultPayload> : IExternalTaskWorker
            where TExternalTaskPayload : new()
    {
        private const int LockDuration = 30000;
        private readonly string ProcessEngineUrl;
        private readonly string Topic;
        private readonly int MaxTasks;
        private readonly int LongpollingTimeout;
        private readonly HandleExternalTaskAction<TExternalTaskPayload, TResultPayload> ProcessingFunction;

        private ExternalTaskApiClientService ExternalTaskClient;

        /// <summary>
        /// Creates an new instance of ExternalTaskWorker
        /// </summary>
        public ExternalTaskWorker(
            string processEngineUrl,
            IIdentity identity,
            string topic,
            int maxTasks,
            int longpollingTimeout,
            HandleExternalTaskAction<TExternalTaskPayload, TResultPayload> processingFunction
        )
        {
            this.ProcessEngineUrl = processEngineUrl;
            this.Identity = identity;
            this.Topic = topic;
            this.MaxTasks = maxTasks;
            this.LongpollingTimeout = longpollingTimeout;
            this.ProcessingFunction = processingFunction;

            this.Initialize();
        }

        /// <summary>
        /// The identity the worker should use to poll for ExternalTasks.
        /// </summary>
        /// <value></value>
        public IIdentity Identity { get; set;}

        /// <summary>
        /// Indicates, if the worker is currently polling for ExternalTasks.
        /// </summary>
        public bool PollingActive { get; private set;} = false;

        /// <summary>
        /// The ID of the worker
        /// </summary>
        public string WorkerId { get; } = Guid.NewGuid().ToString();

        public void Start() {
            this.PollingActive = true;
            this.ProcessExternalTasks();
        }

        public void Stop() {
            this.PollingActive = false;
        }

        private void Initialize() {
          var httpClient = new HttpClient();
          httpClient.BaseAddress = new Uri(ProcessEngineUrl);

          this.ExternalTaskClient = new ExternalTaskApiClientService(httpClient);
        }

        /// <summary>
        /// Periodically fetches, locks and processes available ExternalTasks with a given topic,
        /// using the given callback as a processing function.
        /// </summary>
        private async Task ProcessExternalTasks()
        {
            while (this.PollingActive)
            {
                var externalTasks = await this.FetchAndLockExternalTasks(
                    this.Identity,
                    this.Topic,
                    this.MaxTasks,
                    this.LongpollingTimeout
                );

                if (externalTasks.Count() == 0)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                var tasks = new List<Task>();

                foreach (var externalTask in externalTasks)
                {
                    tasks.Add(this.ExecuteExternalTask(Identity, externalTask));
                }

                await Task.WhenAll(tasks);
            }
        }

        private async Task<IEnumerable<ExternalTask<TExternalTaskPayload>>> FetchAndLockExternalTasks(
            IIdentity identity,
            string topic,
            int maxTasks,
            int longpollingTimeout
        )
        {
            try
            {
                return await this.ExternalTaskClient.FetchAndLockExternalTasks<TExternalTaskPayload>(
                  this.Identity,
                  this.WorkerId,
                  this.Topic,
                  this.MaxTasks,
                  this.LongpollingTimeout,
                  LockDuration
                );
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);

                // Returning an empty Array here, since "waitForAndHandle" already implements a timeout, in case no tasks are available for processing.
                // No need to do that twice.
                return new List<ExternalTask<TExternalTaskPayload>>();
            }
        }

        private async Task ExecuteExternalTask(
          IIdentity identity,
          ExternalTask<TExternalTaskPayload> externalTask
        )
        {
            const int lockExtensionBuffer = 5000;
            const int lockRefreshIntervalInMs = LockDuration - lockExtensionBuffer;

            var lockRefreshTimer = this.StartExtendLockTimer(identity, externalTask, lockRefreshIntervalInMs);

            try
            {
                var result = await this.ProcessingFunction(externalTask);

                lockRefreshTimer.Stop();

                await this.ProcessResult(identity, result, externalTask.Id);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                lockRefreshTimer.Stop();
                await this.ExternalTaskClient.HandleServiceError(identity, this.WorkerId, externalTask.Id, exception.Message, exception.StackTrace);
            }
        }

        private System.Timers.Timer StartExtendLockTimer(IIdentity identity, ExternalTask<TExternalTaskPayload> externalTask, int intervall)
        {
            var timer = new System.Timers.Timer(intervall);
            timer.Elapsed += async (sender, e) => await ExtendLock(identity, externalTask);
            timer.Start();

            return timer;
        }

        private async Task ExtendLock(IIdentity identity, ExternalTask<TExternalTaskPayload> externalTask)
        {
            try
            {
                await this.ExternalTaskClient.ExtendLock(identity, this.WorkerId, externalTask.Id, LockDuration);
            }
            catch (Exception error)
            {
                // This can happen, if the lock-extension was performed after the task was already finished.
                // Since this isn't really an error, a warning suffices here.
                Console.WriteLine($"An error occured while trying to extend the lock for ExternalTask ${externalTask.Id}", error.Message, error.StackTrace);
            }
        }

        private async Task ProcessResult(IIdentity identity, ExternalTaskResultBase result, string externalTaskId) {

            if (result is ExternalTaskBpmnError) {

                var bpmnError = result as ExternalTaskBpmnError;
                await this.ExternalTaskClient.HandleBpmnError(identity, this.WorkerId, externalTaskId, bpmnError.ErrorCode);

            }
            else if (result is ExternalTaskServiceError<object>)
            {

                var serviceError = result as ExternalTaskServiceError<object>;
                await this
                    .ExternalTaskClient
                    .HandleServiceError(identity, this.WorkerId, externalTaskId, serviceError.ErrorMessage, serviceError.ErrorDetails as string);

            }
            else
            {
                await this.ExternalTaskClient.FinishExternalTask(identity, this.WorkerId, externalTaskId, (result as ExternalTaskSuccessResult<object>).Result);
            }
        }
    }
}
