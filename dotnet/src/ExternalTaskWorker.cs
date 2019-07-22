namespace ProcessEngine.ExternalTaskAPI.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ProcessEngine.ExternalTaskAPI.Contracts;

    /// <summary>
    /// Periodically fetches, locks and processes ExternalTasks for a given topic.
    /// </summary>
    public class ExternalTaskWorker : IExternalTaskWorker
    {
        private const int LockDurationInMilliseconds = 30000;

        private readonly IExternalTaskAPI externalTaskAPI;

        /// <summary>
        /// Creates an new instance of ExternalTaskWorker
        /// </summary>
        public ExternalTaskWorker(IExternalTaskAPI externalTaskAPI)
        {
            this.externalTaskAPI = externalTaskAPI;
        }

        /// <summary>
        /// The ID of the worker
        /// </summary>
        public string WorkerId { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Periodically fetches, locks and processes available ExternalTasks with a given topic,
        /// using the given callback as a processing function.
        /// </summary>
        /// <param name="identity">
        /// The identity to use for fetching and processing ExternalTasks.
        /// </param>
        /// <param name="topic">
        /// The topic by which to look for and process ExternalTasks.
        /// </param>
        /// <param name="maxTasks">
        /// max. ExternalTasks to fetch.
        /// </param>
        /// <param name="longpollingTimeout">
        /// Longpolling Timeout in ms.
        /// </param>
        /// <param name="handleAction">
        /// The function for processing the ExternalTasks.
        /// </param>
        public async Task WaitForHandle<TPayload>(
            IIdentity identity,
            string topic,
            int maxTasks,
            int longpollingTimeout,
            HandleExternalTaskAction<TPayload> handleAction
        )
            where TPayload : new()
        {
            const bool keepPolling = true;
            while (keepPolling)
            {
                var externalTasks = await this.FetchAndLockExternalTasks<TPayload>(
                    identity,
                    topic,
                    maxTasks,
                    longpollingTimeout
                );

                if (externalTasks.Count() == 0)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                var tasks = new List<Task>();

                foreach (var externalTask in externalTasks)
                {
                    tasks.Add(this.ExecuteExternalTask<TPayload>(identity, externalTask, handleAction));
                }

                await Task.WhenAll(tasks);
            }
        }

        private async Task<IEnumerable<ExternalTask<TPayload>>> FetchAndLockExternalTasks<TPayload>(
            IIdentity identity,
            string topic,
            int maxTasks,
            int longpollingTimeout
        )
        where TPayload : new()
        {
            try
            {
                return await this.externalTaskAPI.FetchAndLockExternalTasks<TPayload>(
                  identity,
                  WorkerId,
                  topic,
                  maxTasks,
                  longpollingTimeout,
                  LockDurationInMilliseconds
                );
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);

                // Returning an empty Array here, since "waitForAndHandle" already implements a timeout, in case no tasks are available for processing.
                // No need to do that twice.
                return new List<ExternalTask<TPayload>>();
            }
        }

        private async Task ExecuteExternalTask<TPayload>(
          IIdentity identity,
          ExternalTask<TPayload> externalTask,
          HandleExternalTaskAction<TPayload> handleAction
        )
          where TPayload : new()
        {
            const int lockExtensionBuffer = 5000;
            const int lockRefreshIntervalInMs = LockDurationInMilliseconds - lockExtensionBuffer;

            var lockRefreshTimer = this.StartExtendLockTimer(identity, externalTask, lockRefreshIntervalInMs);

            try
            {
                var result = await handleAction(externalTask);

                lockRefreshTimer.Stop();
                await result.SendToExternalTaskApi(this.externalTaskAPI, identity, WorkerId);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                lockRefreshTimer.Stop();
                await this.externalTaskAPI.HandleServiceError(identity, this.WorkerId, externalTask.Id, exception.Message, exception.StackTrace);
            }
        }

        private System.Timers.Timer StartExtendLockTimer<TPayload>(IIdentity identity, ExternalTask<TPayload> externalTask, int intervall)
        {
            var timer = new System.Timers.Timer(intervall);
            timer.Elapsed += async (sender, e) => await ExtendLock<TPayload>(identity, externalTask);
            timer.Start();

            return timer;
        }

        private async Task ExtendLock<TPayload>(IIdentity identity, ExternalTask<TPayload> externalTask)
        {
            try
            {
                await this.externalTaskAPI.ExtendLock(identity, this.WorkerId, externalTask.Id, LockDurationInMilliseconds);
            }
            catch (Exception error)
            {
                // This can happen, if the lock-extension was performed after the task was already finished.
                // Since this isn't really an error, a warning suffices here.
                Console.WriteLine($"An error occured while trying to extend the lock for ExternalTask ${externalTask.Id}", error.Message, error.StackTrace);
            }
        }
    }
}
