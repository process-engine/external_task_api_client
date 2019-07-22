namespace ProcessEngine.ExternalTaskAPI.Client.Requests
{
    internal class FinishExternalTaskRequest<TResult>
    {
        public FinishExternalTaskRequest(string workerId, TResult result)
        {
            this.WorkerId = workerId;
            this.Result = result;
        }

        public string WorkerId { get; }
        public TResult Result { get; }
    }
}
