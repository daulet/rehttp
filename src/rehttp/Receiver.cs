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
                Route = "{*path}")] HttpRequestMessage request,
            string path,
            [Inject] Client client,
            [Config("InitialRequestTimeout")] TimeSpan initialRequestTimeout,
            [Queue(queueName: "requests", Connection = "RequestsQueueConnection")] CloudQueue queue,
            [Config("InitialRetryDelay")] TimeSpan initialRetryDelay,
            [Config("MaxRetryDelay")] TimeSpan maxRetryDelay,
            ILogger log)
        {
            log.LogInformation($"Received request {request.RequestUri}");

            // remove "/r/" from the path and query part of URL
            var targetUri = request.RequestUri.PathAndQuery.Substring(3);
            if (!Uri.TryCreate(targetUri, UriKind.Absolute, out var uri))
            {
                return new BadRequestObjectResult($"{targetUri} is not valid absolute Uri");
            }

            var requestMessage = new HttpRequestMessage(request.Method, uri);
            if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
            {
                requestMessage.Content = request.Content;
            };

            var requestResult = await client.SendAsync(requestMessage, initialRequestTimeout)
                .ConfigureAwait(false);
            switch (requestResult)
            {
                case RequestResult.Ok:
                    return new OkResult();
                case RequestResult.Invalid:
                    return new BadRequestResult();
            }

            var serializableRequest = new Request()
            {
                Destination = targetUri,
                Method = request.Method.Method,
                DelayInSeconds = initialRetryDelay.TotalSeconds,
            };
            if (request.Content != null)
            {
                serializableRequest.Content = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }

            var serializedRequest = JsonConvert.SerializeObject(serializableRequest);

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
            
            return new OkResult();
        }
    }
}
