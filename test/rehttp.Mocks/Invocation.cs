using System.Net.Http;

namespace Rehttp.Mocks
{
    public class Invocation
    {
        public HttpMethod Method { get; set; }

        public HttpContent Content { get; set; }
    }
}
