using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace ArabicPdfReader.Services
{
    public class LlmService
    {
        private const string ApiUrl = "http://192.168.100.194:11434/api/chat";

        private readonly HttpClient client;

        public LlmService(HttpClientService client)
        {
            this.client = client.GetClient();
        }

        public async Task<string> ExtractData(string text)
        {
            string prompt = BuildPrompt(text);

            var requestBody = new
            {
                model = "qwen3.5:9b",
                messages = new[]
                {
                    new { role = "system", content = "You are an Arabic information extraction engine. You extract field values exactly as they appear in Arabic text. You never translate Arabic to English. You never use markdown. You return only raw JSON." },
                    new { role = "user", content = prompt }
                },
                stream = false,
                think = false,
                options = new { temperature = 0 }
            };

            string json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // The response is the HTTP response object 
                var response = await client.PostAsync(ApiUrl, content);

                // Result is the HTTP body as a stream, which is converted into a string
                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return $"Error: {response.StatusCode} - {result}";

                // Parse the Ollama JSON envelope and extract the inner content string 
                using JsonDocument parseResult = JsonDocument.Parse(result);
                JsonElement root = parseResult.RootElement;

                string parsedResult = root.GetProperty("message").GetProperty("content").GetString() ?? "Error: content was null";

                return parsedResult;
            }
            catch (HttpRequestException e)
            {
                return e.Message;
            }
        }

        private static string BuildPrompt(string text)
        {
            return $@"
            You are an information extraction engine.

            Extract the following fields from the PROVIDED text and return ONLY valid JSON.

            Fields:
            - Full Name
            - Location
            - Phone Number
            - Fax Number
            - Email Address
            - Company Name
            - CR Number
            - Address
            - Role / Title
            - Organisation
            - Source / Informant
            - Date

            Rules:
            - Return ONLY JSON
            - No explanation
            - No markdown
            - No extra information
            - If a field is missing, return null
            - Extract text exactly as it appears in the document, in Arabic
            - Do not transliterate Arabic to English
            - Do not romanize Arabic names or words
            - Return ONLY raw JSON, no markdown, no code blocks, no backticks
            - Full Name is the person's complete Arabic name
            - Phone and Fax are separate fields, do not combine them
            - The field labels in the document are in Arabic — extract the VALUE after the colon, not the label itself
            - You MUST NOT use backticks or code blocks under any circumstances
            - NEVER translate any Arabic text to English, return it exactly as extracted
            - Copy the Arabic value character by character, do not modify, translate, or guess
            - If you are not 100% certain a value exists in the text, return null
            - Do not guess, infer, or complete partial values

            Text:
            {text}
            ";
        }
    }
}