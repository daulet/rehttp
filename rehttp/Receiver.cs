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
        [FunctionName("Receiver")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "{*path}")] HttpRequest request,
            string path,
            TraceWriter log)
        {
            log.Info($"Received request for {path}");

            if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                return new BadRequestObjectResult($"{path} is not valid absolute Uri");
            }

            using (var client = new HttpClient())
            {
                try
                {
                    using (var response = await client.GetAsync(uri))
                    {
                        log.Info($"Received response: {await response.Content.ReadAsStringAsync()}");

                        return new OkObjectResult($"Received {response.StatusCode} from {uri}");
                    }
                }
                catch (ArgumentException ex)
                {
                    return new BadRequestObjectResult(ex.Message);
                }
                catch (HttpRequestException ex)
                {
                    log.Info($"Request exception: {ex}");

                    return new OkObjectResult($"Received {ex.Message} from {uri}");
                }
            }
        }
    }
}
