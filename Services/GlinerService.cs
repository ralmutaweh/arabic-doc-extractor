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

        public async Task<string> ExtractData(string extractedText, string fileType, long fileSize, string fileName)
        {
            try
            {
                var glinerHost = configuration["GLINER_HOST"] ?? "http://localhost:8001";

                var body = new StringContent(
                    JsonSerializer.Serialize(new { text = extractedText }),
                    Encoding.UTF8,
                    "application/json"
                );

                logger.LogInformation("Sending request to GLiNER service. FileType: {FileType}, Size: {Size} bytes.", fileType, fileSize);

                var response = await httpClient.PostAsync($"{glinerHost}/extract", body);
                response.EnsureSuccessStatusCode();

                var resultText = await response.Content.ReadAsStringAsync();

                // CSV logging
                var csvLine = string.Join(
                    ",",
                    DateTime.UtcNow.ToString("o"),
                    fileName,
                    fileType,
                    fileSize,
                    "gliner",
                    null, // no prompt tokens
                    null, // no completion tokens
                    null, // no total duration
                    null, // no eval duration
                    "stop"
                );

                var csvPath = "/app/logs/extraction_log.csv";
                Directory.CreateDirectory(Path.GetDirectoryName(csvPath)!);

                if (!File.Exists(csvPath))
                    await File.AppendAllTextAsync(csvPath, "timestamp,file_name,file_type,file_size_bytes,model,prompt_tokens,completion_tokens,total_duration_ms,eval_duration_ms,done_reason\n");

                await File.AppendAllTextAsync(csvPath, csvLine + "\n");

                return resultText;
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "GLiNER request timed out.");
                throw new TimeoutException("GLiNER did not respond in time.", ex);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to reach GLiNER service. Verify GLINER_HOST is correctly configured.");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during GLiNER extraction. File size: {Size}.", fileSize);
                throw new InvalidOperationException("Unexpected error during GLiNER extraction.", ex);
            }
        }
    }
}