using Microsoft.AspNetCore.Http;

namespace Rehttp
{
    public class QueuedRequest
    {
        public string Destination { get; set; }

        public HttpRequest Request { get; set; }
    }
}
