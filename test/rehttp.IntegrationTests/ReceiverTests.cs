using Newtonsoft.Json;
using Rehttp.Mocks;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Rehttp.IntegrationTests
{
    public class ReceiverTests
    {
        private static readonly HttpClient Client = new HttpClient();

        // @TODO create these
        // RunAsync_TargetIsAvailable_NoQueuedRequests(string httpMethod)

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_TargetIsAvailable_CorrectRequestAsync(string httpMethod)
        {
            using (var uniqueQueue = new UniqueQueue())
            {
                var method = new HttpMethod(httpMethod);
                var queueName = uniqueQueue.Queue.Name;

                await Client.SendAsync(
                    new HttpRequestMessage(method,
                        $"http://localhost:7072/r/http://localhost:7073/test/ok/{queueName}")
                );

                var message = await uniqueQueue.Queue.GetMessageAsync();
                var invocation = JsonConvert.DeserializeObject<Invocation>(message.AsString);

                Assert.Equal(method, invocation.Method);
                Assert.Equal(string.Empty, invocation.Content);
            }
        }
    }
}
