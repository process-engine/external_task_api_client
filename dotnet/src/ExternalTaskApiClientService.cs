namespace ProcessEngine.ExternalTaskAPI.Client
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    using EssentialProjects.IAM.Contracts;

    using ProcessEngine.ExternalTaskAPI.Contracts;

    public class ExternalTaskApiClientService : IExternalTaskAPI
    {
        private readonly HttpClient httpClient;

        public ExternalTaskApiClientService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            this.ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            this.httpClient.DefaultRequestHeaders.Accept.Clear();
            this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task ExtendLock(IIdentity identity, string workerId, string externalTaskId, int additionalDuration)
        {
            var uri = $"task/{externalTaskId}/extend_lock";
            var request = new ExtendLockRequest
            (
                workerId,
                additionalDuration
            );

            await this.SendPostToExternalTaskApi(identity, uri, request);
        }

        public async Task<IEnumerable<ExternalTask<TPayload>>> FetchAndLockExternalTasks<TPayload>(IIdentity identity, string workerId, string topicName, int maxTasks, int longPollingTimeout, int lockDuration) where TPayload : new()
        {
            var uri = "fetch_and_lock";
            var request = new FetchAndLockRequest
            (
                workerId,
                topicName,
                maxTasks,
                longPollingTimeout,
                lockDuration
            );

            var response = await this.SendPostToExternalTaskApi<FetchAndLockRequest, IEnumerable<ExternalTask<TPayload>>>
            (
                identity,
                uri,
                request
            );
            return response;
        }

        public async Task FinishExternalTask<TPayload>(IIdentity identity, string workerId, string externalTaskId, TPayload payload)
        {
            var uri = $"task/{externalTaskId}/finish";

            var request = new FinishExternalTaskRequest<TPayload>
            (
                workerId,
                payload
            );

            await this.SendPostToExternalTaskApi(identity, uri, request);
        }

        public async Task HandleBpmnError(IIdentity identity, string workerId, string externalTaskId, string errorCode)
        {
            var uri = $"task/{externalTaskId}/handle_bpmn_error";

            var request = new HandleBpmnErrorRequest
            (
                workerId,
                errorCode
            );

            await this.SendPostToExternalTaskApi(identity, uri, request);
        }

        public async Task HandleServiceError(IIdentity identity, string workerId, string externalTaskId, string errorMessage, string errorDetails)
        {
            var uri = $"task/{externalTaskId}/handle_service_error";

            var request = new HandleServiceErrorRequest
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

        private async Task SendPostToExternalTaskApi<TRequest>(IIdentity identity, string uri, TRequest request)
        {
            await this.SendPostRequest(identity, uri, request);
        }

        private async Task<TResponse> SendPostToExternalTaskApi<TRequest, TResponse>(IIdentity identity, string uri, TRequest request)
        {
            var response = await this.SendPostRequest(identity, uri, request);

            return await DeserializeResposne<TResponse>(response);
        }

        private async Task<HttpResponseMessage> SendPostRequest<TRequest>(IIdentity identity, string uri, TRequest request)
        {
            SetAuthenticationHeader(identity);

            var content = this.SerializeRequest(request);
            var url = this.ApplyBaseUrl(uri);

            var response = await this.httpClient.PostAsync(url, content);

            response.EnsureSuccessStatusCode();

            return response;
        }

        private void SetAuthenticationHeader(IIdentity identity)
        {
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", identity.Token);
        }

        private StringContent SerializeRequest<TRequest>(TRequest request)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var serializedRequest = JsonConvert.SerializeObject(request, settings);
            var content = new StringContent(serializedRequest, Encoding.UTF8, "application/json");
            return content;
        }

        private async Task<TResponse> DeserializeResposne<TResponse>(HttpResponseMessage response)
        {
            var serializedResponse = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TResponse>(serializedResponse);
        }

        private string ApplyBaseUrl(string uri)
        {
            const string BaseRoute = "api/external_task/v1";
            return $"{BaseRoute}/{uri}";
        }
    }
}
