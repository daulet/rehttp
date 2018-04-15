using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace Rehttp
{
    public class QueuedRequest
    {
        public string Destination { get; set; }

        public HttpContent Content { get; set; }
    }
}
