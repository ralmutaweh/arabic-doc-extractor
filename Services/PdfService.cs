using System.Text;
using BidiReshapeSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ArabicPdfReader.Services
{
    public class PdfService
    {
        private readonly ILogger<PdfService> logger;
        public PdfService(ILogger<PdfService> logger)
        {
            this.logger = logger;
        }
        public string ExtractText(Stream stream)
        {
            var stringBuilder = new StringBuilder();

            try
            {
                using (PdfDocument document = PdfDocument.Open(stream))
                {
                    foreach (Page page in document.GetPages())
                    {
                        // This can be used for non table documents -- deprecated use in this source code
                        // string text = ContentOrderTextExtractor.GetText(page);

                        var words = page.GetWords().OrderByDescending(word => word.BoundingBox.Bottom).ToList();

                        if (words.Count == 0) continue; // Skip empty pages


                        double averageTextHeight = words.Average(word => word.BoundingBox.Height);
                        double tolerance = averageTextHeight * 0.3; // 30% of average word height

                        var rows = new List<List<Word>>();
                        List<Word>? currentRow = null;
                        double? currentRowY = null;

                        foreach (var word in words)
                        {
                            if (currentRow == null || Math.Abs(word.BoundingBox.Bottom - currentRowY!.Value) > tolerance)
                            {
                                currentRow = new List<Word>();
                                rows.Add(currentRow);
                                currentRowY = word.BoundingBox.Bottom;
                            }
                            currentRow.Add(word);
                        }

                        foreach (var row in rows)
                        {
                            var orderedWords = row.OrderByDescending(word => word.BoundingBox.Left);
                            string line = string.Join(" ", orderedWords.Select(word => word.Text));
                            string processedLine;
                            try
                            {
                                processedLine = BidiReshape.ProcessString(line);    
                            } catch
                            {
                                processedLine = line;    
                            }
                            
                            processedLine = processedLine.Normalize(NormalizationForm.FormKC);
                            stringBuilder.AppendLine(processedLine);
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "Failed to read PDF file.");
                throw new InvalidOperationException("Failed to read PDF file.", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while processing PDF file.");
                throw new InvalidOperationException("Unexpected error while processing PDF.", ex);
            }

            return stringBuilder.ToString();
        }
    }
}