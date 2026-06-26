using System.Text;
using System.Text.Json;

namespace ArabicPdfReader.Services
{
    public class GlinerService
    {
        private readonly ILogger<GlinerService> logger;
        private readonly IConfiguration configuration;
        private readonly HttpClient httpClient;

        public GlinerService(ILogger<GlinerService> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.httpClient = new HttpClient();
        }

        public async Task<(Guid extraction_id, string resultText)> ExtractData(string extractedText)
        {
            try
            {
                Guid extraction_id = Guid.NewGuid();
                var glinerHost = configuration["GLINER_HOST"] ?? "http://localhost:8001";

                var body = new StringContent(
                    JsonSerializer.Serialize(new { text = extractedText }),
                    Encoding.UTF8,
                    "application/json"
                );

                logger.LogInformation("Sending request to GLiNER service");

                var response = await httpClient.PostAsync($"{glinerHost}/extract", body);
                response.EnsureSuccessStatusCode();

                var resultText = await response.Content.ReadAsStringAsync();
                return (extraction_id, resultText);
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "GLiNER request timed out.");
                throw new TimeoutException("GLiNER did not respond in time.", ex);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to reach GLiNER service.");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during GLiNER extraction.");
                throw new InvalidOperationException("Unexpected error during GLiNER extraction.", ex);
            }
        }
    }
}