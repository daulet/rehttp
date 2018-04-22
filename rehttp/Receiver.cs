using Indigo.Functions.Injection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rehttp
{
    public static class Receiver
    {
        [FunctionName("Receiver")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function,
                "DELETE", "GET", "HEAD", "OPTIONS", "POST", "PUT", "TRACE",
                Route = "{*path}")] HttpRequestMessage request,
            string path,
            [Queue(queueName: "requests", Connection = "RequestsQueueConnection")] IAsyncCollector<Request> queuedRequests,
            [Inject] HttpClient httpClient,
            ILogger log)
        {
            log.LogInformation($"Received request for {path}");

            if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                return new BadRequestObjectResult($"{path} is not valid absolute Uri");
            }

            string message = null;
            try
            {
                var requestMessage = new HttpRequestMessage(request.Method, uri);
                using (var response = await httpClient.SendAsync(requestMessage))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        log.LogInformation($"Received response: {await response.Content.ReadAsStringAsync()}");

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
                log.LogInformation($"Request exception: {ex}");
                message = $"Received {ex.Message} from {uri}";
            }

            var queueMessage = new Request()
            {
                Destination = path,
                Content = request.Content
            };
            await queuedRequests.AddAsync(queueMessage);
            
            return new OkObjectResult(message);
        }
    }
}
