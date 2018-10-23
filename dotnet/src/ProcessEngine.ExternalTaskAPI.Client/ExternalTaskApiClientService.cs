namespace ProcessEngine.ExternalTaskAPI.Client
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

    using Foundation.IAM.Contracts;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using ProcessEngine.ExternalTaskAPI.Client.Requests;
    using ProcessEngine.ExternalTaskAPI.Contracts;

    public class ExternalTaskApiClientService : IExternalTaskAPI
    {
        private const string BaseRoute = "api/external_task/v1";

        private readonly HttpClient httpClient;


        public ExternalTaskApiClientService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task ExtendLock(IIdentity identity, string workerId, string externalTaskId, int additionalDuration)
        {
            string uri = $"task/{externalTaskId}/extend_lock";
            ExtendLockRequest request = new ExtendLockRequest
            (
                workerId,
                additionalDuration
            );

            await this.SendPostToExternalTaskApi(identity, uri, request);
        }

        public async Task<IEnumerable<ExternalTask<TPayload>>> FetchAndLockExternalTasks<TPayload>(IIdentity identity, string workerId, string topicName, int maxTasks, int longPollingTimeout, int lockDuration) where TPayload : new()
        {
            string uri = "fetch_and_lock";
            FetchAndLockRequest request = new FetchAndLockRequest
            (
                workerId,
                topicName,
                maxTasks,
                longPollingTimeout,
                lockDuration
            );

            IEnumerable<ExternalTask<TPayload>> response = await this.SendPostToExternalTaskApi<FetchAndLockRequest, IEnumerable<ExternalTask<TPayload>>>
            (
                identity,
                uri,
                request
            );
            return response;
        }

        public async Task FinishExternalTask<TPayload>(IIdentity identity, string workerId, string externalTaskId, TPayload payload)
        {
            string uri = $"task/{externalTaskId}/finish";

            FinishExternalTaskRequest<TPayload> request = new FinishExternalTaskRequest<TPayload>
            (
                workerId,
                payload
            );

            await this.SendPostToExternalTaskApi(identity, uri, request);
        }

        public async Task HandleBpmnError(IIdentity identity, string workerId, string externalTaskId, string errorCode)
        {
            string uri = $"task/{externalTaskId}/handle_bpmn_error";

            HandleBpmnErrorRequest request = new HandleBpmnErrorRequest
            (
                workerId,
                errorCode
            );

            await this.SendPostToExternalTaskApi(identity, uri, request);
        }

        public async Task HandleServiceError(IIdentity identity, string workerId, string externalTaskId, string errorMessage, string errorDetails)
        {
            string uri = $"task/{externalTaskId}/handle_service_error";

            HandleServiceErrorRequest request = new HandleServiceErrorRequest
            (
                workerId,
                errorMessage,
                errorDetails
            );

            await this.SendPostToExternalTaskApi(identity, uri, request);
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        private async Task<TResponse> SendPostToExternalTaskApi<TRequest, TResponse>(IIdentity identity, string uri, TRequest request)
        {
            SetAuthenticationHeader(identity);

            StringContent content = GetRequestAsStringContent(request);

            var response = await this.httpClient.PostAsync(CombineBaseRouteWith(uri), content);

            response.EnsureSuccessStatusCode();

            return await DeserializeResposne<TResponse>(response);
        }

        private async Task SendPostToExternalTaskApi<TRequest>(IIdentity identity, string uri, TRequest request)
        {
            SetAuthenticationHeader(identity);

            StringContent content = GetRequestAsStringContent(request);

            var response = await this.httpClient.PostAsync(CombineBaseRouteWith(uri), content);

            response.EnsureSuccessStatusCode();
        }

        private void SetAuthenticationHeader(IIdentity identity)
        {
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", identity.ToString());
        }

        private StringContent GetRequestAsStringContent<TRequest>(TRequest request)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            string serializedRequest = JsonConvert.SerializeObject(request, settings);
            StringContent content = new StringContent(serializedRequest, Encoding.UTF8, "application/json");
            return content;
        }

        private async Task<TResponse> DeserializeResposne<TResponse>(HttpResponseMessage response)
        {
            string serializedResponse = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TResponse>(serializedResponse);
        }

        private string CombineBaseRouteWith(string uri)
        {
            return $"{BaseRoute}/{uri}";
        }
    }
}
