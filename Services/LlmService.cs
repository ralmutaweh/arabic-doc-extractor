using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace ArabicPdfReader.Services
{
    public class LlmService
    {
        private readonly ILogger<LlmService> logger;
        private readonly IConfiguration configuration;
        private readonly string promptTemplate;

        public LlmService(ILogger<LlmService> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
            promptTemplate = File.ReadAllText("Prompts/extraction_prompt.txt", System.Text.Encoding.UTF8);
        }

        public async Task<(Guid extraction_id, string resultText, long? PromptEvalCount, long? EvalCount, long? TotalDuration, long? EvalDuration, string? DoneReason)> 
        ExtractData(string extractedText, string model)
        {
            try
            {
                Guid extraction_id = Guid.NewGuid();
                string renderedPrompt = promptTemplate.Replace("{{extractedText}}", extractedText);

                var ollamaHost = configuration["OLLAMA_HOST"] ?? "http://localhost:11434";
                var ollamaClient = new OllamaApiClient(new Uri(ollamaHost), model);

                var chatRequest = new ChatRequest
                {
                    Messages = new[]
                    {
                        new Message { Role = ChatRole.System, Content = "You are a structured data extraction engine for Arabic documents. Return only raw JSON." },
                        new Message { Role = ChatRole.User, Content = renderedPrompt }
                    },
                    Think = false,
                    Stream = false,
                    Options = new OllamaSharp.Models.RequestOptions { Temperature = 0 }
                };

                string resultText = string.Empty;
                ChatDoneResponseStream? lastChunk = null;

                await foreach (var chunk in ollamaClient.ChatAsync(chatRequest))
                {
                    if (chunk?.Message?.Content is { Length: > 0 } content)
                        resultText += content;
                    if (chunk is ChatDoneResponseStream done) 
                        lastChunk = done;
                }

                return (extraction_id, resultText,
                    lastChunk?.PromptEvalCount,
                    lastChunk?.EvalCount,
                    lastChunk?.TotalDuration / 1_000_000,
                    lastChunk?.EvalDuration / 1_000_000,
                    lastChunk?.DoneReason);
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "Ollama request timed out.");
                throw new TimeoutException("Ollama did not respond in time.", ex);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to reach Ollama.");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during LLM extraction.");
                throw new InvalidOperationException("Unexpected error during extraction.", ex);
            }
        }
    }
}