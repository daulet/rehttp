using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Rehttp.Mocks;
using StackExchange.Redis;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Rehttp.IntegrationTests
{
    public class ReceiverTests
    {
        private static readonly HttpClient Client = new HttpClient();
        private static readonly IDatabase Database = ConnectionMultiplexer.Connect("localhost").GetDatabase();
        private static readonly CloudStorageAccount StorageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
        private const string REQUESTS_QUEUE = "requests";

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_InvalidAbsoluteUri_RequestRejected(string httpMethod)
        {
            var method = new HttpMethod(httpMethod);
            var response = await Client.SendAsync(
                new HttpRequestMessage(method,
                    $"http://localhost:7072/r/domain")
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_UnsupportedProtocol_RequestRejected(string httpMethod)
        {
            var method = new HttpMethod(httpMethod);
            var response = await Client.SendAsync(
                new HttpRequestMessage(method,
                    $"http://localhost:7072/r/ftp://test.com")
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_TargetIsAvailable_ExactlyOneRequestMadeAsync(string httpMethod)
        {
            // Arrange
            var method = new HttpMethod(httpMethod);
            var path = Path.GetRandomFileName();

            await Database.ListRightPushAsync(path, JsonConvert.SerializeObject(
                new Response()
                {
                    StatusCode = HttpStatusCode.Accepted
                }));

            // Act
            await Client.SendAsync(
                new HttpRequestMessage(method,
                    $"http://localhost:7072/r/http://localhost:7073/test/universal/{path}")
            );

            // Assert
            var requests = await Database.ListLengthAsync($"response/{path}");
            Assert.Equal(1, requests);
        }

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
            // Arrange
            var method = new HttpMethod(httpMethod);
            var path = $"{Guid.NewGuid()}/{Guid.NewGuid()}/{Guid.NewGuid()}";

            await Database.ListRightPushAsync(path, JsonConvert.SerializeObject(
                new Response()
                {
                    StatusCode = HttpStatusCode.Accepted
                }));

            // Act
            await Client.SendAsync(
                new HttpRequestMessage(method,
                    $"http://localhost:7072/r/http://localhost:7073/test/universal/{path}")
            );

            // Assert
            var invocation = JsonConvert.DeserializeObject<Invocation>(await Database.ListLeftPopAsync($"response/{path}"));
            Assert.Equal($"http://localhost:7073/test/universal/{path}", invocation.TargetUri.ToString());
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
            // Arrange
            var method = new HttpMethod(httpMethod);
            var path = Path.GetRandomFileName();
            var param1 = Guid.NewGuid();
            var param2 = Guid.NewGuid();

            await Database.ListRightPushAsync(path, JsonConvert.SerializeObject(
                new Response()
                {
                    StatusCode = HttpStatusCode.Accepted
                }));

            // Act
            await Client.SendAsync(
                new HttpRequestMessage(method,
                    $"http://localhost:7072/r/http://localhost:7073/test/universal/{path}?param1={param1}&param2={param2}")
            );

            // Assert
            var invocation = JsonConvert.DeserializeObject<Invocation>(await Database.ListLeftPopAsync($"response/{path}"));
            Assert.Equal($"http://localhost:7073/test/universal/{path}?param1={param1}&param2={param2}", invocation.TargetUri.ToString());
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
            // Arrange
            var method = new HttpMethod(httpMethod);
            var path = Path.GetRandomFileName();

            await Database.ListRightPushAsync(path, JsonConvert.SerializeObject(
                new Response()
                {
                    StatusCode = HttpStatusCode.Accepted
                }));

            // Act
            await Client.SendAsync(
                new HttpRequestMessage(method,
                    $"http://localhost:7072/r/http://localhost:7073/test/universal/{path}")
            );

            // Assert
            var invocation = JsonConvert.DeserializeObject<Invocation>(await Database.ListLeftPopAsync($"response/{path}"));
            Assert.Equal(method, invocation.Method);
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

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_TargetIsSlow_RequestIsQueued(string httpMethod)
        {
            var method = new HttpMethod(httpMethod);

            await Client.SendAsync(
                new HttpRequestMessage(method,
                    $"http://localhost:7072/r/http://localhost:7073/test/slow")
            );

            var message = await StorageAccount
                .CreateCloudQueueClient()
                .GetQueueReference(REQUESTS_QUEUE)
                .GetMessageAsync()
                .ConfigureAwait(false);
            Assert.NotNull(message);
        }
    }
}
