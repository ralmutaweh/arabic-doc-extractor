namespace ArabicPdfReader.Models
{
    public class ComparisonLogEntry
    {
        public string ExtractionId { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public bool NamesMatch { get; set; }
        public bool EntitiesMatch { get; set; }
        public bool CountriesMatch { get; set; }
        public string OverallScore { get; set; } = string.Empty;
    }
}