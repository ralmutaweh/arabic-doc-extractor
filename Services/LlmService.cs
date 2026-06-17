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

        public async Task<string> ExtractData(string extractedText, string fileType, long fileSize, string fileName, string model)
        {
            try
            {
                string renderedPrompt = promptTemplate.Replace("{{extractedText}}", extractedText);

                var ollamaHost = configuration["OLLAMA_HOST"] ?? "http://localhost:11434";
                var ollamaClient = new OllamaApiClient(new Uri(ollamaHost), model);

                var chatRequest = new ChatRequest
                {
                    Messages = new[]
                    {
                        new Message { 
                            Role = ChatRole.System, 
                            Content = "You are a structured data extraction engine for Arabic documents. Return only raw JSON."  
                        },
                        new Message
                        {
                            Role = ChatRole.User,
                            Content = renderedPrompt
                        }
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

                var csvLine = string.Join(
                    ",",
                    DateTime.UtcNow.ToString("o"), // ISO 8601 foramt
                    fileName,
                    fileType,
                    fileSize,
                    model,
                    lastChunk?.PromptEvalCount,
                    lastChunk?.EvalCount,
                    lastChunk?.TotalDuration / 1_000_000,
                    lastChunk?.EvalDuration / 1_000_000,
                    lastChunk?.DoneReason
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
                logger.LogError(ex, "Ollama request timed out. The model may be overloaded or unreachable.");
                throw new TimeoutException("Ollama did not respond in time.", ex);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to reach Ollama. Verify the host is running and OLLAMA_HOST is correctly configured.");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during LLM extraction. File bytes length: {Length}.", fileSize);
                throw new InvalidOperationException("Unexpected error during extraction.", ex);
            }
        }
    }
}