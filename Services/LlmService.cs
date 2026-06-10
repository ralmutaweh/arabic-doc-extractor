using Microsoft.SemanticKernel;


namespace ArabicPdfReader.Services
{
    public class LlmService
    {
        private readonly Kernel kernel;

        public LlmService(Kernel kernel)
        {
            this.kernel = kernel;
        }

        public async Task<string> ExtractData(string text)
        {
            string prompt = BuildPrompt(text);

            try
            {
                var response = await kernel.InvokePromptAsync(prompt);

                return response.ToString();
            }
            catch (Exception e)
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