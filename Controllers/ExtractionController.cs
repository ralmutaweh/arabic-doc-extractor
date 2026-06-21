using ArabicPdfReader.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ArabicPdfReader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExtractionController : ControllerBase
    {
        private readonly bool _enforceFileSizeLimit = false;
        private readonly long _maxFileSizeBytes = 20 * 1024 * 1024; // 20 MB
        private readonly ILogger<ExtractionController> logger;
        private readonly LlmService llmService;
        private readonly GlinerService glinerService;
        private readonly PdfService pdfService;
        private readonly DocxService docxService;

        public ExtractionController(LlmService llmService, GlinerService glinerService, PdfService pdfService, DocxService docxService, ILogger<ExtractionController> logger)
        {
            this.llmService = llmService;
            this.glinerService = glinerService;
            this.pdfService = pdfService;
            this.docxService = docxService;
            this.logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file, string model = "qwen3.5:9b")
        {
            if (file == null) return BadRequest("File is null");

            if (_enforceFileSizeLimit && file.Length > _maxFileSizeBytes)
                return StatusCode(413, "File exceeds the maximum allowed size.");

            using Stream stream = file.OpenReadStream();
            string fileType = DetectFileType(stream);

            if (fileType == "unknown")
                return BadRequest("Unsupported file type. Only PDF and DOCX are accepted.");

            logger.LogInformation("Extraction request received. File: {FileName}, Size: {Size} bytes, Type: {Type}",
                file.FileName, file.Length, fileType);

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            byte[] fileBytes = memoryStream.ToArray();

            string extractedText = ExtractText(fileBytes, fileType);
            
            logger.LogInformation("Extracted text for {FileName}:\n{ExtractedText}", file.FileName, extractedText);
            
            var stopwatch = Stopwatch.StartNew();

            string modelResponse = string.Empty;
            try
            {
                if (model == "gliner")
                {
                    Console.WriteLine(extractedText);
                    modelResponse = await glinerService.ExtractData(extractedText, fileType, fileBytes.Length, file.FileName);
                } else {
                    modelResponse = await llmService.ExtractData(extractedText, fileType, fileBytes.Length, file.FileName, model);
                }
            }
            catch (TimeoutException ex)
            {
                logger.LogError(ex, "Ollama timed out processing file {FileName}.", file.FileName);
                return StatusCode(504, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Could not reach Ollama while processing file {FileName}.", file.FileName);
                return StatusCode(503, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing file {FileName}.", file.FileName);
                return StatusCode(500, "An internal error occurred.");
            }

            stopwatch.Stop();
            logger.LogInformation("Extraction completed. File: {FileName}, Elapsed: {ElapsedMs}ms, Result: {Result}",
                file.FileName, stopwatch.ElapsedMilliseconds, modelResponse);

            return Ok(modelResponse);
        }

        private string DetectFileType(Stream stream)
        {
            string fileType = string.Empty;
            byte[] header = new byte[4];
            stream.ReadExactly(header, 0, 4);

            if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46) // PDF ASCII
                fileType = "pdf";
            else if (header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04) // DOCX ASCII
                fileType = "docx";
            else
                fileType = "unknown";

            stream.Position = 0;

            return fileType;
        }

        private string ExtractText(byte[] fileBytes, string fileType)
        {
            using var memoryStream = new MemoryStream(fileBytes);
            return fileType == "pdf" ?
            pdfService.ExtractText(memoryStream) :
            docxService.ExtractText(memoryStream);
        }
    }
}