using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rehttp.Mocks
{
    public static class Mocks
    {
        [FunctionName("OkPathRequest")]
        public static IActionResult OkPathRequest(
            [HttpTrigger(AuthorizationLevel.Function,
                "DELETE", "GET", "HEAD", "OPTIONS", "POST", "PUT", "TRACE",
                Route = "ok/{*path}")] HttpRequestMessage request,
            string path,
            [Queue(queueName: "{path}", Connection = "InvocationQueue")] IAsyncCollector<Invocation> invocations,
            ILogger log)
        {
            log.LogInformation($"Received {nameof(OkPathRequest)} request");

            invocations.AddAsync(new Invocation()
            {
                Method = request.Method,
                Content = request.Content,
            });

            return new OkResult();
        }

        [FunctionName("Slow")]
        public static async Task<IActionResult> SlowAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "slow")] HttpRequest req,
            TraceWriter log)
        {
            log.Info($"Received {nameof(SlowAsync)} request");

            await Task.Delay(TimeSpan.FromSeconds(5));

            return new OkResult();
        }

        [FunctionName("InternalError")]
        public static IActionResult InternalError(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "error")] HttpRequest req,
            TraceWriter log)
        {
            log.Info($"Received {nameof(InternalError)} request");

            throw new IOException();
        }
    }
}
