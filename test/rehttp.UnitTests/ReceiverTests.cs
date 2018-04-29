using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Moq;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Rehttp.UnitTests
{
    public class ReceiverTests
    {
        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_SuccessStatusCode_NoRequestQueued(string httpMethod)
        {
            var method = new HttpMethod(httpMethod);
            var targetUrl = "http://endpoint.io/path/to/media";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(method, targetUrl)
                    .Respond(HttpStatusCode.OK, new StringContent(string.Empty));

            var queueMock = new Mock<CloudQueue>(MockBehavior.Strict, new Uri("http://localhost"));

            await Receiver.RunAsync(
                    new HttpRequestMessage(method, $"https://rehttp.me/r/{targetUrl}"),
                    targetUrl,
                    new Client(mockHttp.ToHttpClient()),
                    TimeSpan.FromMilliseconds(1),
                    queueMock.Object,
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromSeconds(1),
                    Mock.Of<ILogger>())
                .ConfigureAwait(false);

            mockHttp.VerifyNoOutstandingExpectation();
            queueMock.VerifyAll();
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_FailureStatusCode_SomeRequestQueued(string httpMethod)
        {
            var method = new HttpMethod(httpMethod);
            var targetUrl = "http://endpoint.io/path/to/media";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(method, targetUrl)
                    .Respond(HttpStatusCode.InternalServerError);

            var queueMock = new Mock<CloudQueue>(MockBehavior.Strict, new Uri("http://localhost"));
            queueMock
                .Setup(x => x.AddMessageAsync(
                    It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>()))
                .Returns(Task.CompletedTask);

            await Receiver.RunAsync(
                    new HttpRequestMessage(method, $"https://rehttp.me/r/{targetUrl}"),
                    targetUrl,
                    new Client(mockHttp.ToHttpClient()),
                    TimeSpan.FromMilliseconds(1),
                    queueMock.Object,
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromSeconds(1),
                    Mock.Of<ILogger>())
                .ConfigureAwait(false);

            mockHttp.VerifyNoOutstandingExpectation();
            queueMock.VerifyAll();
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_FailureStatusCode_ExactlyOneRequestQueued(string httpMethod)
        {
            var method = new HttpMethod(httpMethod);
            var targetUrl = "http://endpoint.io/path/to/media";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(method, targetUrl)
                    .Respond(HttpStatusCode.InternalServerError);

            var queueMock = new Mock<CloudQueue>(MockBehavior.Loose, new Uri("http://localhost"));

            await Receiver.RunAsync(
                    new HttpRequestMessage(method, $"https://rehttp.me/r/{targetUrl}"),
                    targetUrl,
                    new Client(mockHttp.ToHttpClient()),
                    TimeSpan.FromMilliseconds(1),
                    queueMock.Object,
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromSeconds(1),
                    Mock.Of<ILogger>())
                .ConfigureAwait(false);

            mockHttp.VerifyNoOutstandingExpectation();
            queueMock.Verify(
                x => x.AddMessageAsync(
                    It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>()),
                Times.Once);
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_FailureStatusCode_CorrectDestinationQueued(string httpMethod)
        {
            var method = new HttpMethod(httpMethod);
            var targetUrl = "http://endpoint.io/path/to/media?p1=v1&p2=v2";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(method, targetUrl)
                    .Respond(HttpStatusCode.InternalServerError);

            var queueMock = new Mock<CloudQueue>(MockBehavior.Loose, new Uri("http://localhost"));

            await Receiver.RunAsync(
                    new HttpRequestMessage(method, $"https://rehttp.me/r/{targetUrl}"),
                    "http://endpoint.io/path/to/media",
                    new Client(mockHttp.ToHttpClient()),
                    TimeSpan.FromMilliseconds(1),
                    queueMock.Object,
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromSeconds(1),
                    Mock.Of<ILogger>())
                .ConfigureAwait(false);

            mockHttp.VerifyNoOutstandingExpectation();
            queueMock.Verify(x => x.AddMessageAsync(
                It.Is<CloudQueueMessage>(m =>
                    JsonConvert.DeserializeObject<Request>(m.AsString).Destination == targetUrl),
                It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>()));
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_FailureStatusCode_CorrectMethodQueued(string httpMethod)
        {
            var method = new HttpMethod(httpMethod);
            var targetUrl = "http://endpoint.io/path/to/media";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(method, targetUrl)
                    .Respond(HttpStatusCode.InternalServerError);

            var queueMock = new Mock<CloudQueue>(MockBehavior.Loose, new Uri("http://localhost"));

            await Receiver.RunAsync(
                    new HttpRequestMessage(method, $"https://rehttp.me/r/{targetUrl}"),
                    targetUrl,
                    new Client(mockHttp.ToHttpClient()),
                    TimeSpan.FromMilliseconds(1),
                    queueMock.Object,
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromSeconds(1),
                    Mock.Of<ILogger>())
                .ConfigureAwait(false);

            mockHttp.VerifyNoOutstandingExpectation();
            queueMock.Verify(x => x.AddMessageAsync(
                It.Is<CloudQueueMessage>(m =>
                    JsonConvert.DeserializeObject<Request>(m.AsString).Method == httpMethod),
                It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>()));
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_FailureStatusCode_CorrectContentQueued(string httpMethod)
        {
            var randomizer = new Random();
            var bytes = new Byte[100];
            randomizer.NextBytes(bytes);
            var method = new HttpMethod(httpMethod);
            var targetUrl = "http://endpoint.io/path/to/media";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(method, targetUrl)
                    .Respond(HttpStatusCode.InternalServerError);

            var queueMock = new Mock<CloudQueue>(MockBehavior.Loose, new Uri("http://localhost"));

            await Receiver.RunAsync(
                    new HttpRequestMessage(method, $"https://rehttp.me/r/{targetUrl}")
                    {
                        Content = new ByteArrayContent(bytes),
                    },
                    targetUrl,
                    new Client(mockHttp.ToHttpClient()),
                    TimeSpan.FromMilliseconds(1),
                    queueMock.Object,
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromSeconds(1),
                    Mock.Of<ILogger>())
                .ConfigureAwait(false);

            mockHttp.VerifyNoOutstandingExpectation();
            queueMock.Verify(x => x.AddMessageAsync(
                It.Is<CloudQueueMessage>(m =>
                    JsonConvert.DeserializeObject<Request>(m.AsString).Content.SequenceEqual(bytes)),
                It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>()));
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("TRACE")]
        public async Task RunAsync_FailureStatusCode_CorrectDelayQueued(string httpMethod)
        {
            var method = new HttpMethod(httpMethod);
            var targetUrl = "http://endpoint.io/path/to/media";
            var delayInSeconds = 123;

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(method, targetUrl)
                    .Respond(HttpStatusCode.InternalServerError);

            var queueMock = new Mock<CloudQueue>(MockBehavior.Loose, new Uri("http://localhost"));

            await Receiver.RunAsync(
                    new HttpRequestMessage(method, $"https://rehttp.me/r/{targetUrl}"),
                    targetUrl,
                    new Client(mockHttp.ToHttpClient()),
                    TimeSpan.FromMilliseconds(1),
                    queueMock.Object,
                    TimeSpan.FromSeconds(delayInSeconds),
                    TimeSpan.FromSeconds(1),
                    Mock.Of<ILogger>())
                .ConfigureAwait(false);

            mockHttp.VerifyNoOutstandingExpectation();
            queueMock.Verify(x => x.AddMessageAsync(
                It.Is<CloudQueueMessage>(m =>
                    JsonConvert.DeserializeObject<Request>(m.AsString).DelayInSeconds == delayInSeconds),
                It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>()));
        }
    }
}
