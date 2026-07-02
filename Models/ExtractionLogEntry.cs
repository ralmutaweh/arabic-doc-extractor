namespace ArabicPdfReader.Models
{
    public class ExtractionLogEntry
    {
        public string ExtractionId { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public long LatencyMs { get; set; }
        public string Model { get; set; } = string.Empty;
        public string? PromptTokens { get; set; }
        public string? CompletionTokens { get; set; }
        public string? TotalDurationMs { get; set; }
        public string? EvalDurationMs { get; set; }
        public string? DoneReason { get; set; }
    }
}