using ArabicPdfReader.Services;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<HttpClientService>();
        services.AddSingleton<LlmService>();
        services.AddSingleton<PdfService>();
        services.AddSingleton<DocxService>();

        var serviceProvider = services.BuildServiceProvider();

        var llmService = serviceProvider.GetRequiredService<LlmService>();
        var pdfService = serviceProvider.GetRequiredService<PdfService>();
        var docxService = serviceProvider.GetRequiredService<DocxService>();

        var pdfPath = Path.Combine(
          Directory.GetCurrentDirectory(),
          "Content",
          "01-contact-form-simple.pdf"
        );

        var docxPath = Path.Combine(
          Directory.GetCurrentDirectory(),
          "Content",
          "07-company-profile.docx"
        );

        string rawTextFromPdf = pdfService.ExtractText(pdfPath);

        string rawTextFromDocx = docxService.ExtractText(docxPath);

        Console.WriteLine("=== RAW TEXT FROM WORD ===");
        Console.WriteLine(rawTextFromDocx);

        Console.WriteLine("=== RAW TEXT FROM PDF ===");
        Console.WriteLine(rawTextFromPdf);

        string responsePdf = await llmService.ExtractData(rawTextFromPdf);
        Console.WriteLine("=== LLM RESPONSE PDF ===");
        Console.WriteLine(responsePdf);


        string responseDocx = await llmService.ExtractData(rawTextFromDocx);
        Console.WriteLine("=== LLM RESPONSE Docx ===");
        Console.WriteLine(responseDocx);
    }
}