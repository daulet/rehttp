using Microsoft.Extensions.Logging;
using Moq;
using Rehttp;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace rehttp.UnitTests
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
            var targetUrl = "http://endpoint.io/path/to/media";

            var mockHttp = new MockHttpMessageHandler();
            var method = new HttpMethod(httpMethod);
            mockHttp.Expect(method, targetUrl)
                    .Respond(HttpStatusCode.OK, new StringContent(string.Empty));
            Receiver.SharedHttpClient = mockHttp.ToHttpClient();

            await Receiver.RunAsync(
                new HttpRequestMessage(method, $"https://rehttp.me/r/{targetUrl}"),
                targetUrl,
                null,
                Mock.Of<ILogger>());

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
