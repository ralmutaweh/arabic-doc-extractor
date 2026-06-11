using System.ComponentModel;
using System.Threading.Tasks;
using ArabicPdfReader.Services;
using Microsoft.SemanticKernel;

namespace ArabicPdfReader.Plugins
{
    public class DocumentPlugin
    {
        private PdfService pdfService;
        private DocxService docxService;
        public DocumentPlugin(PdfService pdfService, DocxService docxService)
        {
            this.pdfService = pdfService;
            this.docxService = docxService;
        }

        [KernelFunction]
        [Description("Extracts raw text from a PDF document provided as bytes. Document is expected to be Arabic but may contain mixed content including English characters and numbers.")]
        public string handlePdf(byte[] fileBytes)
        {
            using var memoryStream = new MemoryStream(fileBytes);
            return pdfService.ExtractText(memoryStream);
        }

        [KernelFunction]
        [Description("Extracts raw text from a Word DOCX document provided as bytes. Document is expected to be Arabic but may contain mixed content including English characters and numbers.")]
        public string handleDocx(byte[] fileBytes)
        {
            using var memoryStream = new MemoryStream(fileBytes);
            return docxService.ExtractText(memoryStream);
        }
    }
}