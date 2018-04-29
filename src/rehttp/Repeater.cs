using Indigo.Functions.Configuration;
using Indigo.Functions.Injection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rehttp
{
    public static class Repeater
    {
        [FunctionName("Repeater")]
        public static async Task RunAsync(
            [QueueTrigger("requests", Connection = "RequestsQueueConnection")] RequestMessage request,
            [Inject] Client client,
            [Config("RequestTimeout")] TimeSpan timeout,
            [Queue("requests", Connection = "RequestsQueueConnection")] CloudQueue queue,
            [Config("MaxRetryDelay")] TimeSpan maxDelay,
            ILogger logger)
        {
            var httpRequest = new HttpRequestMessage(
                new HttpMethod(request.Method), request.Destination)
            {
                Content = new ByteArrayContent(request.Content)
            };

            var requestResult = await client.SendAsync(httpRequest, timeout);
            switch (requestResult)
            {
                case RequestResult.Ok:
                    logger.LogInformation($"Succeeded to replay to {request.Destination}");
                    return;
                case RequestResult.Invalid:
                    logger.LogInformation($"Abandoning invalid request to {request.Destination}");
                    return;
            }

            var nextDelay = request.DelayInSeconds * 2;
            if (nextDelay >= maxDelay.TotalSeconds)
            {
                logger.LogInformation($"Reached max retry delay of {nextDelay}s for {request.Destination}");
                return;
            }

            request.DelayInSeconds = nextDelay;
            var serializedRequest = JsonConvert.SerializeObject(request);
            var nextMessage = new CloudQueueMessage(serializedRequest);

            await queue.AddMessageAsync(nextMessage,
                    timeToLive: maxDelay,
                    initialVisibilityDelay: TimeSpan.FromSeconds(nextDelay),
                    options: queue.ServiceClient.DefaultRequestOptions,
                    operationContext: null)
                .ConfigureAwait(false);

            logger.LogInformation($"Postponed request for {nextDelay}s to {request.Destination}");
        }
    }
}
