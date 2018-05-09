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
            var path = $"{nameof(RunAsync_TargetIsAvailable_ExactlyOneRequestMadeAsync)}/{Path.GetRandomFileName()}";

            await Database.ListRightPushAsync(path, JsonConvert.SerializeObject(
                new Response()
                {
                    StatusCode = HttpStatusCode.Accepted
                }));

            // Act
            await Client.SendAsync(
                new HttpRequestMessage(method,
                    $"http://localhost:7072/r/http://localhost:7073/test/{path}")
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
            var path = $"{nameof(RunAsync_TargetIsAvailable_CorrectUriPathSentAsync)}/{Guid.NewGuid()}/{Guid.NewGuid()}/{Guid.NewGuid()}";

            await Database.ListRightPushAsync(path, JsonConvert.SerializeObject(
                new Response()
                {
                    StatusCode = HttpStatusCode.Accepted
                }));

            // Act
            await Client.SendAsync(
                new HttpRequestMessage(method,
                    $"http://localhost:7072/r/http://localhost:7073/test/{path}")
            );

            // Assert
            var request = JsonConvert.DeserializeObject<Request>(await Database.ListLeftPopAsync($"response/{path}"));
            Assert.Equal($"http://localhost:7073/test/{path}", request.TargetUri.ToString());
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
            var path = $"{nameof(RunAsync_TargetIsAvailable_CorrectUriQuerySentAsync)}/{Path.GetRandomFileName()}";
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
                    $"http://localhost:7072/r/http://localhost:7073/test/{path}?param1={param1}&param2={param2}")
            );

            // Assert
            var request = JsonConvert.DeserializeObject<Request>(await Database.ListLeftPopAsync($"response/{path}"));
            Assert.Equal($"http://localhost:7073/test/{path}?param1={param1}&param2={param2}", request.TargetUri.ToString());
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
            var path = $"{nameof(RunAsync_TargetIsAvailable_CorrectMethodSentAsync)}/{Path.GetRandomFileName()}";

            await Database.ListRightPushAsync(path, JsonConvert.SerializeObject(
                new Response()
                {
                    StatusCode = HttpStatusCode.Accepted
                }));

            // Act
            await Client.SendAsync(
                new HttpRequestMessage(method,
                    $"http://localhost:7072/r/http://localhost:7073/test/{path}")
            );

            // Assert
            var request = JsonConvert.DeserializeObject<Request>(await Database.ListLeftPopAsync($"response/{path}"));
            Assert.Equal(method, request.Method);
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_TargetIsAvailable_CorrectContentSentAsync(string httpMethod)
        {
            // Arrange
            var method = new HttpMethod(httpMethod);
            var path = $"{nameof(RunAsync_TargetIsAvailable_CorrectContentSentAsync)}/{Path.GetRandomFileName()}";
            var content = Guid.NewGuid().ToString();

            await Database.ListRightPushAsync(path, JsonConvert.SerializeObject(
                new Response()
                {
                    StatusCode = HttpStatusCode.Accepted
                }));

            // Act
            await Client.SendAsync(
                new HttpRequestMessage(method,
                    $"http://localhost:7072/r/http://localhost:7073/test/{path}")
                {
                    Content = new StringContent(content),
                }
            );

            // Assert
            var request = JsonConvert.DeserializeObject<Request>(await Database.ListLeftPopAsync($"response/{path}"));
            Assert.Equal(content, request.Content);
        }
    }
}
