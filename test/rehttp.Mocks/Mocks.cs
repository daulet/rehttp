using Indigo.Functions.Redis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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
        [FunctionName("MockFunction")]
        public static async Task<IActionResult> MockFunctionAsync(
            [HttpTrigger(AuthorizationLevel.Function,
                "DELETE", "GET", "HEAD", "OPTIONS", "POST", "PUT", "TRACE",
                Route = "{*path}")] HttpRequestMessage request,
            string path,
            [Redis] IDatabase database,
            ILogger log)
        {
            log.LogInformation($"{request.RequestUri}: received");

            var recordedResponse = await database.ListLeftPopAsync(path);
            if (string.IsNullOrEmpty(recordedResponse))
            {
                log.LogError($"{request.RequestUri}: no prerecorded responses left for {path} key");
                throw new IOException($"No prerecorded responses left for key: {path}, request: {request.RequestUri}");
            }

            var invocation = new Request()
            {
                Content = await request.Content.ReadAsStringAsync(),
                Method = request.Method,
                TargetUri = request.RequestUri,
            };
            await database.ListRightPushAsync($"response/{path}", JsonConvert.SerializeObject(invocation));

            var response = JsonConvert.DeserializeObject<Response>(recordedResponse);
            if (response.DelayInMilliseconds.HasValue)
            {
                log.LogInformation($"{request.RequestUri}: slowing down by {response.DelayInMilliseconds.Value}ms");
                await Task.Delay(TimeSpan.FromMilliseconds(response.DelayInMilliseconds.Value));
            }

            log.LogInformation($"{request.RequestUri}: {response.StatusCode}");
            return new ObjectResult(recordedResponse)
            {
                StatusCode = (int) response.StatusCode
            };
        }
    }
}
