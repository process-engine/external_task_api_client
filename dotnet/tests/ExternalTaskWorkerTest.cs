namespace ProcessEngine.ExternalTaskAPI.Client.Tests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Xunit;

    using EssentialProjects.IAM.Contracts;
    using ProcessEngine.ConsumerAPI.Contracts.DataModel;

    public class ExternalTaskWorkerTest
    {
        private ExternalTaskWorker<TestPayload, SampleResult> testObject;
        [Fact]
        public async void HandleExternalTask()
        {
            var identity = new TestIdentity();
            var client = new HttpClient();
            var url = "http://localhost:8000";

            testObject = new ExternalTaskWorker<TestPayload, SampleResult>(
                url,
                identity,
                "TestTopic",
                10,
                10000,
                async (externalTask) =>
                {
                    return await this.Callback(externalTask);
                });

            testObject.Start();
        }

        private Task<ExternalTaskSuccessResult<SampleResult>> Callback(ExternalTask<TestPayload> externalTask)
        {
            this.testObject.Stop();

            var samplePayload = new SampleResult();

            return Task.Run<ExternalTaskSuccessResult<SampleResult>>(() => {
              return new ExternalTaskSuccessResult<SampleResult>(externalTask.Id, samplePayload);
            });
        }

        private class SampleResult
        {
            public string Bla {get; set;} = "Bla";
        }

        private class TestIdentity : IIdentity
        {
            public string Token { get; } = "ZHVtbXlfdG9rZW4=";
            public string UserId { get; } = "dummy_token";
        }

        private class TestPayload
        {
            public string Prop { get; set; }
        }

        private class TestResult
        {
            public string TestProperty { get; } = "Fertig";
        }
    }
}
