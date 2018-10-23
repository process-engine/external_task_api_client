namespace ProcessEngine.ExternalTaskAPI.Client.Requests
{
    internal class HandleBpmnErrorRequest
    {
        public HandleBpmnErrorRequest(string workerId, string errorCode)
        {
            this.WorkerId = workerId;
            this.ErrorCode = errorCode;
        }

        public string WorkerId { get; }
        public string ErrorCode { get; }
    }
}
