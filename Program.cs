using ArabicPdfReader.Middleware;
using ArabicPdfReader.Plugins;
using ArabicPdfReader.Services;
using ArabicPdfReader.Observability;
using Microsoft.SemanticKernel;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<LlmService>();
        builder.Services.AddSingleton<PdfService>();
        builder.Services.AddSingleton<DocxService>();
        builder.Services.AddSingleton<IFunctionInvocationFilter, TracingFunctionInvocationFilter>();
        builder.Services.AddControllers();

        var ollamaHost = builder.Configuration["OLLAMA_HOST"] ?? "http://localhost:11434"; // fallback for local dev without Docker

        builder.Services.AddOllamaChatCompletion(
          modelId: "qwen3.5:9b",
          endpoint: new Uri(ollamaHost) // Where Ollama instance is listening
        );

        // Factory lambda — ASP.NET Core passes the service provider (serviceProvider) automatically
        // Each request gets a fresh Kernel with DocumentPlugin registered (Transient)
        builder.Services.AddTransient<Kernel>(serviceProvider =>
        {
            var kernel = new Kernel(serviceProvider);
            var pdf = serviceProvider.GetRequiredService<PdfService>();
            var docx = serviceProvider.GetRequiredService<DocxService>();
            kernel.Plugins.AddFromObject(new DocumentPlugin(pdf, docx));
            kernel.FunctionInvocationFilters.Add(serviceProvider.GetRequiredService<IFunctionInvocationFilter>());
            return kernel;
        });

        var app = builder.Build();

        app.UseMiddleware<RequestLoggingMiddleware>();
        app.MapGet("/", () => "Arabic Doc Extractor API is running!");
        app.MapControllers();

        app.Run();
    }
}