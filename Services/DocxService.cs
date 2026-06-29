using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ArabicPdfReader.Services
{
    public class DocxService
    {
        private readonly ILogger<DocxService> logger;

        public DocxService(ILogger<DocxService> logger)
        {
            this.logger = logger;
        }

        public string ExtractText(Stream stream)
        {
            var stringBuilder = new StringBuilder();

            try
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(stream, false))
                {
                    if (wordDoc.MainDocumentPart == null) return string.Empty;
                    var body = wordDoc.MainDocumentPart?.Document?.Body;
                    if (body == null) return string.Empty;

                    foreach (var element in body.Elements())
                    {
                        if (element is Paragraph paragraph)
                        {
                            string text = paragraph.InnerText;
                            text = text.Normalize(NormalizationForm.FormKC);
                            stringBuilder.AppendLine(text);
                        }

                        if (element is Table table)
                        {
                            var rows = table.Elements<TableRow>().ToList();
                            if (rows.Count == 0) continue;

                            var headerCells = rows[0].Elements<TableCell>()
                                .Select(cell => cell.InnerText.Normalize(NormalizationForm.FormKC))
                                .ToList();

                            foreach (var row in rows.Skip(1))
                            {
                                var cells = row.Elements<TableCell>().ToList();
                                var lines = cells.Select((cell, index) =>
                                {
                                    string text = cell.InnerText.Normalize(NormalizationForm.FormKC);
                                    string header = headerCells[index];
                                    return $"{header}: {text}";
                                });

                                stringBuilder.AppendLine(string.Join('\n', lines));
                                stringBuilder.AppendLine();
                            }
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "Failed to read DOCX file.");
                throw new InvalidOperationException("Failed to read DOCX file.", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while processing DOCX file.");
                throw new InvalidOperationException("Unexpected error while processing DOCX.", ex);
            }

            return stringBuilder.ToString();
        }
    }
}