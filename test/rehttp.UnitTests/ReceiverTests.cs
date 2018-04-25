using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Moq;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using System;
using System.IO;
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
                queueMock.Object,
                mockHttp.ToHttpClient(),
                Mock.Of<ILogger>());

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
                queueMock.Object,
                mockHttp.ToHttpClient(),
                Mock.Of<ILogger>());

            mockHttp.VerifyNoOutstandingExpectation();
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
                queueMock.Object,
                mockHttp.ToHttpClient(),
                Mock.Of<ILogger>());

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
        public async Task RunAsync_FailureStatusCode_CorrectRequestQueued(string httpMethod)
        {
            var content = Path.GetRandomFileName();
            var method = new HttpMethod(httpMethod);
            var targetUrl = "http://endpoint.io/path/to/media";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(method, targetUrl)
                    .Respond(HttpStatusCode.InternalServerError);

            var queueMock = new Mock<CloudQueue>(MockBehavior.Loose, new Uri("http://localhost"));

            await Receiver.RunAsync(
                new HttpRequestMessage(method, $"https://rehttp.me/r/{targetUrl}")
                {
                    Content = new StringContent(content),
                },
                targetUrl,
                queueMock.Object,
                mockHttp.ToHttpClient(),
                Mock.Of<ILogger>());

            mockHttp.VerifyNoOutstandingExpectation();
            queueMock.Verify(x => x.AddMessageAsync(
                It.Is<CloudQueueMessage>(m =>
                    m.AsString == JsonConvert.SerializeObject(
                        new Request()
                        {
                            Content = new StringContent(content),
                            Destination = targetUrl,
                        })),
                It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>()));
        }
    }
}
