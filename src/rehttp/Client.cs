using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rehttp
{
    public class Client
    {
        private readonly HttpClient _httpClient;

        public Client(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
                            return RequestResult.Ok;
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
                return RequestResult.Invalid;
            }
            catch (HttpRequestException)
            { }

            return RequestResult.Retry;
        }
    }
}
