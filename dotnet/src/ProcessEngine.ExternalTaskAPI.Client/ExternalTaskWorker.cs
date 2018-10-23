namespace ProcessEngine.ExternalTaskAPI.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;
    using Foundation.IAM.Contracts;
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
        /// Id of worker
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
            while (true)
            {
                var externalTasks = await this.FetchAndLockExternalTasks<TPayload>(identity, topic, maxTasks, longpollingTimeout);

                Timer timer = this.StartExtendLockTimer(identity, externalTasks, LockDurationInMilliseconds - 5000);

                IList<Task> tasks = new List<Task>();

                foreach (ExternalTask<TPayload> externalTask in externalTasks)
                {
                    tasks.Add(this.ExecuteExternalTask<TPayload>(identity, externalTask, handleAction));
                }

                await Task.WhenAll(tasks);

                timer.Stop();
            }
        }

        private Timer StartExtendLockTimer<TPayload>(IIdentity identity, IEnumerable<ExternalTask<TPayload>> externalTasks, int intervall)
        {
            Timer timer = new Timer(intervall);
            timer.Elapsed += async (sender, e) => await ExtendLocks<TPayload>(identity, externalTasks.ToList());
            timer.Start();

            return timer;
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
                await Task.Delay(1000);
                return await this.FetchAndLockExternalTasks<TPayload>(identity, topic, maxTasks, longpollingTimeout);
            }
        }

        private async Task ExecuteExternalTask<TPayload>(
          IIdentity identity,
          ExternalTask<TPayload> externalTask,
          HandleExternalTaskAction<TPayload> handleAction
        )
          where TPayload : new()
        {
            try
            {
                IExternalTaskResult result = await handleAction(externalTask);
                await result.SendToExternalTaskApi(this.externalTaskAPI, identity, WorkerId);

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                await this.externalTaskAPI.HandleServiceError(identity, WorkerId, externalTask.Id, exception.Message, exception.StackTrace);
            }
        }

        private async Task ExtendLocks<TPayload>(IIdentity identity, IList<ExternalTask<TPayload>> externalTasks)
        {
            foreach (ExternalTask<TPayload> externalTask in externalTasks)
            {
                await this.externalTaskAPI.ExtendLock(identity, WorkerId, externalTask.Id, LockDurationInMilliseconds);
            }
        }
    }
}