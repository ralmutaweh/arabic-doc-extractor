using ArabicPdfReader.Middleware;
using ArabicPdfReader.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<LlmService>();
        builder.Services.AddSingleton<PdfService>();
        builder.Services.AddSingleton<DocxService>();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMiddleware<RequestLoggingMiddleware>();
        app.MapGet("/", () => "Arabic Doc Extractor API is running!");
        app.MapControllers();

        app.Run();
    }
}