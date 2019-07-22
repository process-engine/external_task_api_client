namespace ProcessEngine.ExternalTaskAPI.Client.Requests
{
    internal class FetchAndLockRequest
    {
        public FetchAndLockRequest(string workerId, string topicName, int maxTasks, int longPollingTimeout, int lockDuration)
        {
            this.WorkerId = workerId;
            this.TopicName = topicName;
            this.MaxTasks = maxTasks;
            this.LongPollingTimeout = longPollingTimeout;
            this.LockDuration = lockDuration;
        }

        public string WorkerId { get; }
        public string TopicName { get; }
        public int MaxTasks { get; }
        public int LongPollingTimeout { get; }
        public int LockDuration { get; }
    }
}
