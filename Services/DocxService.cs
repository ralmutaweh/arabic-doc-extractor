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

                            // DOCX stores Arabic in correct logical order already —
                            // no BidiReshape needed. NFKC kept as a safety net for
                            // any stray presentation-form characters.
                            text = text.Normalize(NormalizationForm.FormKC);

                            stringBuilder.AppendLine(text);
                        }

                        if (element is Table table)
                        {
                            var rows = table.Elements<TableRow>().ToList();
                            if (rows.Count == 0) continue;

                            // The first row will contain the column headers
                            var headerCells = rows[0].Elements<TableCell>()
                                .Select(cell => cell.InnerText.Normalize(NormalizationForm.FormKC))
                                .ToList();

                            // Each cell, in the remaining rows, will pair with its column header as "header: value"
                            foreach (var row in rows.Skip(1))
                            {
                                var cells = row.Elements<TableCell>().ToList();

                                var lines = cells.Select((cell, index) =>
                                {
                                    string text = cell.InnerText.Normalize(NormalizationForm.FormKC);
                                    string header = headerCells[index];

                                    return $"{header}: {text}";
                                });

                                // One key: value pair per line — mirrors the format that worked in manual GLiNER testing.
                                stringBuilder.AppendLine(string.Join('\n', lines));
                                stringBuilder.AppendLine(); // blank line between rows/records
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