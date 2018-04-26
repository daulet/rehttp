using Google.Protobuf;
using Indigo.Functions.Injection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rehttp
{
    public static class Receiver
    {
        public const long KB = 1024;

        [FunctionName("Receiver")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function,
                "DELETE", "GET", "HEAD", "OPTIONS", "POST", "PUT", "TRACE",
                Route = "{*path}")] HttpRequestMessage request,
            string path,
            [Queue(queueName: "requests", Connection = "RequestsQueueConnection")] CloudQueue queue,
            [Inject] HttpClient httpClient,
            ILogger log)
        {
            log.LogInformation($"Received request {request.RequestUri}");

            // remove "/r/" from the path and query part of URL
            var targetUri = request.RequestUri.PathAndQuery.Substring(3);
            if (!Uri.TryCreate(targetUri, UriKind.Absolute, out var uri))
            {
                return new BadRequestObjectResult($"{targetUri} is not valid absolute Uri");
            }

            string responseMessage = null;
            try
            {
                var requestMessage = new HttpRequestMessage(request.Method, uri);
                if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
                {
                    requestMessage.Content = request.Content;
                };

                using (var response = await httpClient.SendAsync(requestMessage))
                {
                    responseMessage = $"Received {response.StatusCode} from {uri}";

                    if (response.IsSuccessStatusCode)
                    {
                        log.LogInformation($"Received response: {await response.Content.ReadAsStringAsync()}");

                        return new OkObjectResult(responseMessage);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
            catch (HttpRequestException ex)
            {
                log.LogInformation($"Request exception: {ex}");
                responseMessage = $"Received {ex.Message} from {uri}";
            }

            var serializableRequest = new Request()
            {
                Destination = targetUri,
                Method = request.Method.Method,
            };

            if (request.Content != null)
            {
                serializableRequest.Content = ByteString.CopyFrom(await request.Content.ReadAsByteArrayAsync());
            }

            // 48KB is a limit for byte array queue messages
            // https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-azure-and-service-bus-queues-compared-contrasted#capacity-and-quotas
            // reserving 1KB for other properties of the message, like TTL
            if (serializableRequest.CalculateSize() > 47 * KB)
            {

            }

            // TODO: upgrade to Microsoft.Azure.Storage.Common and replace with new CloudQueueMessage(requestAsBytes)
            // or at least bump version of Microsoft.WindowsAzure.Storage
            var message = new CloudQueueMessage(null);
            message.SetMessageContent(serializableRequest.ToByteArray());

            await queue.AddMessageAsync(message,
                timeToLive: TimeSpan.FromDays(2),
                initialVisibilityDelay: TimeSpan.FromMinutes(5),
                options: queue.ServiceClient.DefaultRequestOptions,
                operationContext: null);
            
            return new OkObjectResult(responseMessage);
        }
    }
}
