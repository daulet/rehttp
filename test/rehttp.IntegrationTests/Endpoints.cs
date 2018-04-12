
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace rehttp.IntegrationTests
{
    public static class Endpoints
    {
        [FunctionName("Ok")]
        public static IActionResult Ok(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ok")] HttpRequest req,
            TraceWriter log)
        {
            log.Info($"Received {nameof(Ok)} request");

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
