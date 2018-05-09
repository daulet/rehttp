using System;
using System.Net.Http;

namespace Rehttp.Mocks
{
    public class Request
    {
        public string Content { get; set; }

        public HttpMethod Method { get; set; }

        public Uri TargetUri { get; set; }
    }
}
