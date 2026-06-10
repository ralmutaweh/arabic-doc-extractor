using ArabicPdfReader.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

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
        private LlmService llmService;
        private PdfService pdfService;
        private DocxService docxService;

        public ExtractionController(LlmService llmService, PdfService pdfService, DocxService docxService)
        {
            this.llmService = llmService;
            this.pdfService = pdfService;
            this.docxService = docxService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null) return BadRequest("File is null");

            string fileType = file.ContentType;
            string result = string.Empty;

            if (fileType == "application/pdf")
            {
                result = pdfService.ExtractText(file.OpenReadStream());
            }
            else if (fileType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                result = docxService.ExtractText(file.OpenReadStream());
            }
            else
            {
                return BadRequest("Unsupported file type");
            }

            string llmResponse = await llmService.ExtractData(result);
            return Ok(llmResponse);
        }
    }
}