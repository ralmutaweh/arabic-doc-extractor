using Microsoft.SemanticKernel;

namespace ArabicPdfReader.Services
{
    public class LlmService
    {
        private readonly Kernel kernel;
        private readonly ILogger<LlmService> logger;

        private const string promptTemplate = @"
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
            {{ $extractedText }}
        ";

        public LlmService(Kernel kernel, ILogger<LlmService> logger)
        {
            this.kernel = kernel;
            this.logger = logger;
        }

        public async Task<string> ExtractData(byte[] fileBytes, string fileType)
        {
            try
            {
                // Invoke the correct plugin method based on file type
                string extractedText = fileType == "pdf"
                    ? kernel.Plugins["DocumentPlugin"]["HandlePdf"].ToString()!
                    : kernel.Plugins["DocumentPlugin"]["HandleDocx"].ToString()!;

                // Use SK plugin to extract text from bytes
                var pluginFunction = fileType == "pdf"
                    ? kernel.Plugins["DocumentPlugin"]["HandlePdf"]
                    : kernel.Plugins["DocumentPlugin"]["HandleDocx"];

                var extractionResult = await kernel.InvokeAsync(pluginFunction,
                    new KernelArguments { ["fileBytes"] = fileBytes });

                extractedText = extractionResult.ToString();

                // Pass extracted text to LLM prompt
                var function = kernel.CreateFunctionFromPrompt(promptTemplate);
                var response = await kernel.InvokeAsync(function,
                    new KernelArguments { ["extractedText"] = extractedText });

                return response.ToString();
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "Ollama request timed out. The model may be overloaded or unreachable.");
                throw new TimeoutException("Ollama did not respond in time.", ex);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to reach Ollama. Verify the host is running and OLLAMA_HOST is correctly configured.");
                throw new InvalidOperationException("Could not reach Ollama.", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during LLM extraction. File bytes length: {Length}.", fileBytes.Length);
                throw new InvalidOperationException("Unexpected error during extraction.", ex);
            }
        }
    }
}