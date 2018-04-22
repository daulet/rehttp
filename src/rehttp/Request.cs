using System.Net.Http;

namespace Rehttp
{
    public class Request
    {
        public string Destination { get; set; }

        public HttpContent Content { get; set; }
    }
}
