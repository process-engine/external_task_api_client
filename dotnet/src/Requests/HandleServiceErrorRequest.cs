namespace ProcessEngine.ExternalTaskAPI.Client.Requests
{
    internal class HandleServiceErrorRequest
    {
        public HandleServiceErrorRequest(string workerId, string errorMessage, string errorDetails)
        {
            this.WorkerId = workerId;
            this.ErrorMessage = errorMessage;
            this.ErrorDetails = errorDetails;
        }

        public string WorkerId { get; }
        public string ErrorMessage { get; }
        public string ErrorDetails { get; }
    }
}
