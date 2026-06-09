using ArabicPdfReader.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main(string[] args)
    {
        
        var builder = WebApplication.CreateBuilder(args);
        
        // Register services
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<HttpClientService>(); // Abstraction layer for Microsoft HttpClient
        builder.Services.AddSingleton<LlmService>();
        builder.Services.AddSingleton<PdfService>();
        builder.Services.AddSingleton<DocxService>();
        
        var app = builder.Build();   

        app.MapGet("/", () => "Arabic Doc Extractor API is running!");  

        app.Run();
      }
}