using Indigo.Functions.Configuration;
using Indigo.Functions.Injection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Rehttp
{
    public static class Receiver
    {
        public const long KB = 1024;

        [FunctionName("Receiver")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous,
                "DELETE", "GET", "HEAD", "OPTIONS", "POST", "PUT", "TRACE",
                Route = "{*path}")] HttpRequestMessage httpRequest,
            [Inject] Client client,
            [Config("InitialRequestTimeout")] TimeSpan initialRequestTimeout,
            [Queue(queueName: "requests", Connection = "RequestsQueueConnection")] CloudQueue queue,
            [Config("InitialRetryDelay")] TimeSpan initialRetryDelay,
            [Config("MaxRetryDelay")] TimeSpan maxRetryDelay,
            ILogger logger)
        {
            logger.LogInformation($"Received request {httpRequest.RequestUri}");

            // remove "/r/" from the path and query part of URL
            var targetUri = httpRequest.RequestUri.PathAndQuery.Substring(3);
            if (!Uri.TryCreate(targetUri, UriKind.Absolute, out var uri))
            {
                logger.LogInformation($"Rejecting request to {targetUri} because it is not valid absolute Uri");
                return new BadRequestObjectResult($"Unsupported request to {uri}");
            }

            HttpRequestMessage requestMessage;
            try
            {
                requestMessage = new HttpRequestMessage(httpRequest.Method, uri);
            }
            catch (ArgumentException ex)
            {
                logger.LogInformation($"Rejecting request to {targetUri} due to {ex.Message}");
                return new BadRequestObjectResult($"Unsupported request to {uri}");
            }

            if (httpRequest.Method != HttpMethod.Get && httpRequest.Method != HttpMethod.Head)
            {
                requestMessage.Content = httpRequest.Content;
            };

            var requestResult = await client.SendAsync(requestMessage, initialRequestTimeout)
                .ConfigureAwait(false);
            switch (requestResult)
            {
                case RequestResult.Ok:
                    logger.LogInformation($"Succeeded to relay to {uri}");
                    return new OkResult();
                case RequestResult.Invalid:
                    logger.LogInformation($"Rejecting invalid request to {uri}");
                    return new BadRequestResult();
            }

            var request = new RequestMessage()
            {
                Destination = targetUri,
                Method = httpRequest.Method.Method,
                DelayInSeconds = initialRetryDelay.TotalSeconds,
            };
            if (httpRequest.Content != null)
            {
                request.Content = await httpRequest.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }

            var serializedRequest = JsonConvert.SerializeObject(request);

            // 48KB is a limit for byte array queue messages
            // https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-azure-and-service-bus-queues-compared-contrasted#capacity-and-quotas
            // reserving 1KB for other properties of the message, like TTL
            if (Encoding.Unicode.GetByteCount(serializedRequest) > 47 * KB)
            {

            }

            var message = new CloudQueueMessage(serializedRequest);
            await queue.AddMessageAsync(message,
                    timeToLive: maxRetryDelay,
                    initialVisibilityDelay: initialRetryDelay,
                    options: queue.ServiceClient.DefaultRequestOptions,
                    operationContext: null)
                .ConfigureAwait(false);

            logger.LogInformation($"Postponed request for {initialRetryDelay.TotalSeconds}s to {uri}");
            return new OkResult();
        }
    }
}
