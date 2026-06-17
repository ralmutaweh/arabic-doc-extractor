using ArabicPdfReader.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ArabicPdfReader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    // http://localhost:5000 — default ASP.NET Core local URL
    // then "api" keyword is added from the route attribute
    // then "extraction" keyword is added from the class name. "Controller" is dropped
    // "upload" comes from the HttpPost("upload")
    // Finally the Api Url is: http://localhost:5000/api/extraction/upload
    public class ExtractionController : ControllerBase
    {
        // Set to true to enforce the file size limit defined below.
        // Future maintainers: flip this flag and set _maxFileSizeBytes to activate.
        private readonly bool _enforceFileSizeLimit = false;
        private readonly long _maxFileSizeBytes = 20 * 1024 * 1024; // 20 MB, adjust as needed
        private readonly ILogger<ExtractionController> logger;
        private readonly LlmService llmService;
        private string? modelUsed; // This var is kept for testing purposes. It eases the switch of models in testing phase.

        public ExtractionController(LlmService llmService, ILogger<ExtractionController> logger)
        {
            this.llmService = llmService;
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

            // Convert uploaded file to bytes — passed to DocumentPlugin via LlmService
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            byte[] fileBytes = memoryStream.ToArray();

            var stopwatch = Stopwatch.StartNew();

            string llmResponse = string.Empty;
            try
            {
                llmResponse = await llmService.ExtractData(fileBytes, fileType, model);
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
                file.FileName, stopwatch.ElapsedMilliseconds, llmResponse);

            return Ok(llmResponse);
        }

        // Rather than trusting file.ContentType, which is client-supplied and can be spoofed,
        // raw magic bytes are inspected from the stream directly. This prevents a malicious actor
        // from disguising a harmful file (e.g. an .exe) as a valid PDF or DOCX.
        // Kept in place even though this service runs in a closed local environment for good security practice regardless.
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

            // Reset the stream position to the beginning
            stream.Position = 0;

            return fileType;
        }
    }
}