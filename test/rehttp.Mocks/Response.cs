using System.Net;

namespace Rehttp.Mocks
{
    public class Response
    {
        public int? DelayInMilliseconds { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }
}
