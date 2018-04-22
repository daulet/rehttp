using System.Net.Http;

namespace Rehttp.Mocks
{
    /// <summary>
    /// @TODO add path and query pieces
    /// </summary>
    public class Invocation
    {
        public HttpMethod Method { get; set; }

        public string Content { get; set; }
    }
}
