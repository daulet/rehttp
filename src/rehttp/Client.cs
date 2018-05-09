using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rehttp
{
    public class Client
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public Client(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<RequestResult> SendAsync(HttpRequestMessage requestMessage, TimeSpan timeout)
        {
            try
            {
                var requestTask = _httpClient.SendAsync(requestMessage);
                var timeoutTask = Task.Delay(timeout);
                if (await Task.WhenAny(requestTask, timeoutTask).ConfigureAwait(false) == requestTask)
                {
                    using (var response = await requestTask.ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            _logger.LogInformation($"Received response: {content}");

                            return RequestResult.Ok;
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
                return RequestResult.Invalid;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogInformation($"Request exception: {ex}");
            }

            return RequestResult.Retry;
        }
    }
}
