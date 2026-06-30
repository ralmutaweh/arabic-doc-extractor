using ArabicPdfReader.Middleware;
using ArabicPdfReader.Services;
using ArabicPdfReader.Utilities;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<LlmService>();
        builder.Services.AddSingleton<GlinerService>();
        builder.Services.AddSingleton<PdfService>();
        builder.Services.AddSingleton<DocxService>();
        builder.Services.AddSingleton<PerformanceMonitor>();
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseStaticFiles(); // Allows ASP.NET to servie files from wwwroot/ automatically
        app.MapGet("/", () => "Arabic Doc Extractor API is running!");
        app.MapControllers();

        app.Run();
    }
}