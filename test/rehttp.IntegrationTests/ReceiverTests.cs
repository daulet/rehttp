using Newtonsoft.Json;
using Rehttp.Mocks;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Rehttp.IntegrationTests
{
    public class ReceiverTests
    {
        private static readonly HttpClient Client = new HttpClient();

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_TargetIsAvailable_CorrectUriPathSentAsync(string httpMethod)
        {
            using (var uniqueQueue = new UniqueQueue("mockpaths"))
            {
                var method = new HttpMethod(httpMethod);
                var path1 = Guid.NewGuid();
                var path2 = Guid.NewGuid();
                var path3 = Guid.NewGuid();

                await Client.SendAsync(
                    new HttpRequestMessage(method,
                        $"http://localhost:7072/r/http://localhost:7073/test/ok/long/{path1}/{path2}/{path3}")
                );

                var message = await uniqueQueue.Queue.GetMessageAsync();
                Assert.NotNull(message);

                var invocation = JsonConvert.DeserializeObject<Invocation>(message.AsString);
                Assert.Equal($"http://localhost:7073/test/ok/long/{path1}/{path2}/{path3}", invocation.TargetUri.ToString());
            }
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_TargetIsAvailable_CorrectUriQuerySentAsync(string httpMethod)
        {
            using (var uniqueQueue = new UniqueQueue())
            {
                var method = new HttpMethod(httpMethod);
                var queueName = uniqueQueue.Queue.Name;
                var param1 = Guid.NewGuid();
                var param2 = Guid.NewGuid();

                await Client.SendAsync(
                    new HttpRequestMessage(method,
                        $"http://localhost:7072/r/http://localhost:7073/test/ok/{queueName}?param1={param1}&param2={param2}")
                );

                var message = await uniqueQueue.Queue.GetMessageAsync();
                Assert.NotNull(message);

                var invocation = JsonConvert.DeserializeObject<Invocation>(message.AsString);
                Assert.Equal($"http://localhost:7073/test/ok/{queueName}?param1={param1}&param2={param2}", invocation.TargetUri.ToString());
            }
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_TargetIsAvailable_CorrectMethodSentAsync(string httpMethod)
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
                Assert.NotNull(message);

                var invocation = JsonConvert.DeserializeObject<Invocation>(message.AsString);
                Assert.Equal(method, invocation.Method);
            }
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_TargetIsAvailable_CorrectContentSentAsync(string httpMethod)
        {
            using (var uniqueQueue = new UniqueQueue())
            {
                var method = new HttpMethod(httpMethod);
                var content = Guid.NewGuid().ToString();
                var queueName = uniqueQueue.Queue.Name;

                await Client.SendAsync(
                    new HttpRequestMessage(method,
                        $"http://localhost:7072/r/http://localhost:7073/test/ok/{queueName}")
                    {
                        Content = new StringContent(content),
                    }
                );

                var message = await uniqueQueue.Queue.GetMessageAsync();
                Assert.NotNull(message);

                var invocation = JsonConvert.DeserializeObject<Invocation>(message.AsString);
                Assert.Equal(content, invocation.Content);
            }
        }
    }
}
