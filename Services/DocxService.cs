using System.Text;
using BidiReshapeSharp;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ArabicPdfReader.Services
{
    public class DocxService
    {
        public string ExtractText(string path)
        {
            var stringBuilder = new StringBuilder();

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(path, false))
            {
                if (wordDoc.MainDocumentPart == null) return string.Empty;

                var body = wordDoc.MainDocumentPart?.Document?.Body;

                if (body == null) return string.Empty;

                foreach (Paragraph paragraph in body.Elements<Paragraph>())
                {
                    string text = paragraph.InnerText;

                    var lines = text.Split('\n');
                    var processedLines = lines.Select(line =>
                    {
                        try
                        {
                            return BidiReshape.ProcessString(line);
                        } catch
                        {
                            return line;
                        }
                    });

                    text = string.Join('\n', processedLines);

                    stringBuilder.AppendLine(text);

                }
            }

            return stringBuilder.ToString();
        }
    }
}