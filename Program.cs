using ArabicPdfReader.Services;
using Microsoft.SemanticKernel;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<LlmService>();
        builder.Services.AddSingleton<PdfService>();
        builder.Services.AddSingleton<DocxService>();
        builder.Services.AddControllers();

        var ollamaHost = builder.Configuration["OLLAMA_HOST"] ?? "http://localhost:11434"; // fallback for local dev without Docker

        builder.Services.AddOllamaChatCompletion(
          modelId: "qwen3.5:9b",
          endpoint: new Uri(ollamaHost) // Where Ollama instance is listening
        );

        builder.Services.AddTransient<Kernel>(); // Opposite of singleton instance, each kernel request gets its own clean kernel instance with no shared state
        var app = builder.Build();

        app.MapGet("/", () => "Arabic Doc Extractor API is running!");
        app.MapControllers();

        app.Run();
    }
}