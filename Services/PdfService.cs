using System.Text;
using BidiReshapeSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ArabicPdfReader.Services
{
    public class PdfService
    {
        public string ExtractText(Stream stream)
        {
            var stringBuilder = new StringBuilder();

            try
            {
                using (PdfDocument document = PdfDocument.Open(stream))
                {
                    foreach (Page page in document.GetPages())
                    {
                        string text = ContentOrderTextExtractor.GetText(page);

                        // Reshape the text to correct the order of Arabic characters
                        var lines = text.Split('\n');
                        var processedLines = lines.Select(line =>
                        {
                            try
                            {
                                return BidiReshape.ProcessString(line);
                            }
                            catch
                            {
                                // If reshaping fails, return the original line
                                return line;
                            }
                        });
                        text = string.Join('\n', processedLines);

                        stringBuilder.AppendLine(text);
                    }
                }
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("Failed to read PDF.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error while processing PDF.", ex);
            }

            return stringBuilder.ToString();
        }
    }
}