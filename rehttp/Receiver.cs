using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rehttp
{
    public static class Receiver
    {
        private readonly static HttpClient _httpClient = new HttpClient();

        [FunctionName("Receiver")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "{*path}")] HttpRequestMessage request,
            string path,
            [Queue(queueName: "requests", Connection = "RequestsQueueConnection")] IAsyncCollector<QueuedRequest> queuedRequests,
            TraceWriter log)
        {
            log.Info($"Received request for {path}");

            if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                return new BadRequestObjectResult($"{path} is not valid absolute Uri");
            }

            string message = null;
            try
            {
                using (var response = await _httpClient.GetAsync(uri))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        log.Info($"Received response: {await response.Content.ReadAsStringAsync()}");

                        return new OkObjectResult($"Received {response.StatusCode} from {uri}");
                    }

                    message = $"Received {response.StatusCode} from {uri}";
                }
            }
            catch (ArgumentException ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
            catch (HttpRequestException ex)
            {
                log.Info($"Request exception: {ex}");
                message = $"Received {ex.Message} from {uri}";
            }

            var queueMessage = new QueuedRequest()
            {
                Destination = path,
                Content = request.Content
            };
            await queuedRequests.AddAsync(queueMessage);
            
            return new OkObjectResult(message);
        }
    }
}
