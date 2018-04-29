namespace Rehttp
{
    public class Request
    {
        public string Destination { get; set; }

        public string Method { get; set; }

        public byte[] Content { get; set; }
    }
}
