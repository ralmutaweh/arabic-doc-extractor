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

        builder.Services.AddOllamaChatCompletion(
          modelId: "qwen3.5:9b",
          endpoint: new Uri("http://192.168.100.194:11434") // Where Ollama instance is listening
        );

        builder.Services.AddTransient<Kernel>(); // Opposite of singleton instance, each kernel request gets its own clean kernel instance with no shared state
        var app = builder.Build();

        app.MapGet("/", () => "Arabic Doc Extractor API is running!");
        app.MapControllers();

        app.Run();
    }
}