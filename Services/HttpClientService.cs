using System.Net.Http;
namespace ArabicPdfReader.Services
{
    public class HttpClientService
    {
        private readonly IHttpClientFactory clientFactory;

        public HttpClientService(IHttpClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        public HttpClient GetClient()
        {
            var client = clientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5); // Set a longer timeout for long-running
            return client;
        }
    }
}