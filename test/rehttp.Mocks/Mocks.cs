using Indigo.Functions.Redis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rehttp.Mocks
{
    public static class Mocks
    {
        [FunctionName("UniversalMock")]
        public static async Task<IActionResult> UniversalMockAsync(
            [HttpTrigger(AuthorizationLevel.Function,
                "DELETE", "GET", "HEAD", "OPTIONS", "POST", "PUT", "TRACE",
                Route = "universal/{*path}")] HttpRequestMessage request,
            string path,
            [Redis] IDatabase database,
            ILogger log)
        {
            log.LogInformation($"Received {nameof(OkPathRequestAsync)} request");

            var invocation = new Invocation()
            {
                Content = await request.Content.ReadAsStringAsync(),
                Method = request.Method,
                TargetUri = request.RequestUri,
            };
            await database.ListRightPushAsync($"response/{path}", JsonConvert.SerializeObject(invocation));

            var recordedResponse = await database.ListLeftPopAsync(path);
            if (string.IsNullOrEmpty(recordedResponse))
            {
                throw new IOException($"No prerecorded responses left for key: {path}");
            }

            var response = JsonConvert.DeserializeObject<Response>(recordedResponse);
            if (response.DelayInMilliseconds.HasValue)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(response.DelayInMilliseconds.Value));
            }

            return new ObjectResult(recordedResponse)
            {
                StatusCode = (int) response.StatusCode
            };
        }

        [FunctionName("OkPathRequest")]
        public static async Task<IActionResult> OkPathRequestAsync(
            [HttpTrigger(AuthorizationLevel.Function,
                "DELETE", "GET", "HEAD", "OPTIONS", "POST", "PUT", "TRACE",
                Route = "ok/{*path}")] HttpRequestMessage request,
            string path,
            [Queue(queueName: "{path}", Connection = "InvocationQueue")] IAsyncCollector<Invocation> invocations,
            ILogger log)
        {
            log.LogInformation($"Received {nameof(OkPathRequestAsync)} request");

            await invocations.AddAsync(new Invocation()
                    {
                        Content = await request.Content.ReadAsStringAsync(),
                        Method = request.Method,
                        TargetUri = request.RequestUri,
                    })
                .ConfigureAwait(false);

            return new OkResult();
        }

        [FunctionName("OkLongPathRequest")]
        public static async Task<IActionResult> OkLongPathRequest(
            [HttpTrigger(AuthorizationLevel.Function,
                "DELETE", "GET", "HEAD", "OPTIONS", "POST", "PUT", "TRACE",
                Route = "ok/long/{*path}")] HttpRequestMessage request,
            string path,
            [Queue(queueName: "mockpaths", Connection = "InvocationQueue")] IAsyncCollector<Invocation> invocations,
            ILogger log)
        {
            log.LogInformation($"Received {nameof(OkPathRequestAsync)} request");

            await invocations.AddAsync(new Invocation()
                    {
                        Content = await request.Content.ReadAsStringAsync(),
                        Method = request.Method,
                        TargetUri = request.RequestUri,
                    })
                .ConfigureAwait(false);

            return new OkResult();
        }

        [FunctionName("Slow")]
        public static async Task<IActionResult> SlowAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "slow")] HttpRequest request,
            TraceWriter log)
        {
            log.Info($"Received {nameof(SlowAsync)} request");

            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

            return new OkResult();
        }

        [FunctionName("InternalError")]
        public static IActionResult InternalError(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "error")] HttpRequestMessage request,
            TraceWriter log)
        {
            log.Info($"Received {nameof(InternalError)} request");

            throw new IOException();
        }
    }
}
