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
            [QueueTrigger("requests", Connection = "RequestsQueueConnection")] Request message,
            [Inject] Client client,
            [Config("RequestTimeout")] TimeSpan timeout,
            [Queue("requests", Connection = "RequestsQueueConnection")] CloudQueue queue,
            [Config("MaxRetryDelay")] TimeSpan maxDelay,
            ILogger logger)
        {
            var httpRequest = new HttpRequestMessage(
                new HttpMethod(message.Method), message.Destination)
            {
                Content = new ByteArrayContent(message.Content)
            };

            var requestResult = await client.SendAsync(httpRequest, timeout);
            switch (requestResult)
            {
                case RequestResult.Ok:
                    logger.LogInformation($"Succeeded to replay to {message.Destination}");
                    return;
                case RequestResult.Invalid:
                    logger.LogInformation($"Abandoning invalid request to {message.Destination}");
                    return;
            }

            var nextDelay = message.DelayInSeconds * 2;
            if (nextDelay >= maxDelay.TotalSeconds)
            {
                logger.LogInformation($"Reached max retry delay of {nextDelay}s for {message.Destination}");
                return;
            }

            message.DelayInSeconds = nextDelay;
            var serializedRequest = JsonConvert.SerializeObject(message);
            var nextMessage = new CloudQueueMessage(serializedRequest);

            await queue.AddMessageAsync(nextMessage,
                    timeToLive: maxDelay,
                    initialVisibilityDelay: TimeSpan.FromSeconds(nextDelay),
                    options: queue.ServiceClient.DefaultRequestOptions,
                    operationContext: null)
                .ConfigureAwait(false);

            logger.LogInformation($"Postponed request for {nextDelay}s to {message.Destination}");
        }
    }
}
