namespace ArabicPdfReader.Utilities
{
    public static class CsvTailReader
    {
        public static async Task<List<String>> ReadLastLinesAsync(string path, int count)
        {
            var lines = new List<string>();

            if (!File.Exists(path)) return lines; // Returns an empty array to be handled by the dashboard JS file

            // This opens a file for reeading while safely allowing other programs 
            // to read from or write to the file at the same time
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Starting at the end of the file
            long positon = stream.Length;

            // Temporarily holds the bytes of the line currently being assembled
            var buffer = new List<byte>();
            int newLinesFound = 0;

            // Walking backwards one byte at a time until the required number of lines is found
            // or reached the beggining of the file
            while (positon > 0 && newLinesFound <= count)
            {
                positon--;
                stream.Seek(positon, SeekOrigin.Begin); // Moving the read pointer back one byte
                int b = stream.ReadByte();

                if (b == '\n')
                {
                    if (buffer.Count > 0) // To avoid reading empty lines
                    {
                        // Bytes are collected in reverse order (reading the sentence backwards)
                        // so now they will be flipped and added as a line
                        buffer.Reverse();
                        // Converting from byte to string
                        string line = System.Text.Encoding.UTF8.GetString(buffer.ToArray());
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            lines.Add(line);
                            newLinesFound++;
                        }
                        buffer.Clear();
                    }
                }
                else
                {
                    buffer.Add((byte)b);
                }

                if (newLinesFound >= count) break;
            }

            // Handling edge case:
            // When reach the header line before fetching 10 rows of data
            // We will capture what is left and drop the header 
            if (positon == 0 && buffer.Count > 0 && newLinesFound < count)
            {
                buffer.Reverse();
                string line = System.Text.Encoding.UTF8.GetString(buffer.ToArray());
                bool looksLikeHeader = line.StartsWith("extraction_id,");
                if (!string.IsNullOrWhiteSpace(line) && !looksLikeHeader)
                    lines.Add(line);
            }

            return lines;
        }
    }
}