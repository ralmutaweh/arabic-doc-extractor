using ArabicPdfReader.Services;
using ArabicPdfReader.Utilities;
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
        private readonly IConfiguration configuration;
        private readonly PerformanceMonitor performanceMonitor;

        public ExtractionController(LlmService llmService, GlinerService glinerService, PdfService pdfService, DocxService docxService, ILogger<ExtractionController> logger, IConfiguration configuration, PerformanceMonitor performanceMonitor)
        {
            this.llmService = llmService;
            this.glinerService = glinerService;
            this.pdfService = pdfService;
            this.docxService = docxService;
            this.logger = logger;
            this.configuration = configuration;
            this.performanceMonitor = performanceMonitor;
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

            Guid extraction_id = Guid.Empty;
            string modelResponse = string.Empty;

            long? promptTokens = null, completionTokens = null, totalDurationMs = null, evalDurationMs = null;
            string? doneReason = null;

            try
            {
                if (model == "gliner")
                {
                    (extraction_id, modelResponse) = await glinerService.ExtractData(extractedText);
                }
                else
                {
                    (extraction_id, modelResponse, promptTokens, completionTokens, totalDurationMs, evalDurationMs, doneReason) = await llmService.ExtractData(extractedText, model);
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

            var csvLine = string.Join(",",
                extraction_id,
                DateTime.UtcNow.ToString("o"),
                file.FileName,
                fileType,
                fileBytes.Length,
                model,
                model == "gliner" ? null : promptTokens,
                model == "gliner" ? null : completionTokens,
                model == "gliner" ? null : totalDurationMs,
                model == "gliner" ? null : evalDurationMs,
                model == "gliner" ? "stop" : doneReason
            );

            var csvPath = configuration["CsvExtractionLogPath"] ?? "/app/logs/extraction_log.csv";
            Directory.CreateDirectory(Path.GetDirectoryName(csvPath)!);

            if (!System.IO.File.Exists(csvPath))
                await System.IO.File.AppendAllTextAsync(csvPath, "extraction_id,timestamp,file_name,file_type,file_size_bytes,model,prompt_tokens,completion_tokens,total_duration_ms,eval_duration_ms,done_reason\n");

            await System.IO.File.AppendAllTextAsync(csvPath, csvLine + "\n");

            await performanceMonitor.UpdateAfterExtractionAsync(stopwatch.ElapsedMilliseconds);

            return Ok(new
            {
                extractionId = extraction_id,
                result = modelResponse
            });
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> Feedback(Guid extraction_id, string feedback)
        {
            try
            {
                var csvLine = string.Join(
                    ",",
                    extraction_id,
                    DateTime.UtcNow.ToString("o"),
                    $"\"{feedback}\""
                );

                var csvPath = configuration["CsvFeedbackLogPath"] ?? "/app/logs/feedback_log.csv";
                Directory.CreateDirectory(Path.GetDirectoryName(csvPath)!);

                if (!System.IO.File.Exists(csvPath))
                    await System.IO.File.AppendAllTextAsync(csvPath, "extraction_id,timestamp,feedback\n");

                await System.IO.File.AppendAllTextAsync(csvPath, csvLine + "\n");

                await performanceMonitor.UpdateAfterFeedbackAsync(feedback);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write feedback to CSV. ExtractionId: {ExtractionId}", extraction_id);
                return StatusCode(500, "Failed to save feedback.");
            }

            return Ok("Feedback received. Thank you!");
        }

        [HttpGet("report")]
        public async Task<IActionResult> Report()
        {
            await performanceMonitor.GenerateReport();
            return Ok("Report Generated. Check Docker Service logs.");
        }

        private string DetectFileType(Stream stream)
        {
            string fileType = string.Empty;
            byte[] header = new byte[4];
            stream.ReadExactly(header, 0, 4);

            if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
                fileType = "pdf";
            else if (header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04)
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