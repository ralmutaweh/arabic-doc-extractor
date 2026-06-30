namespace ArabicPdfReader.Models
{
    public class ExtractionVerification
    {
        public Guid ExtractionId { get; set; }

        public List<string> ExtractedNames { get; set; } = new();
        public List<string> ExtractedEntities { get; set; } = new();
        public List<string> ExtractedCountries { get; set; } = new();

        public List<string> UserFinalUploadedNames { get; set; } = new();
        public List<string> UserFinalUploadedEntities { get; set; } = new();
        public List<string> UserFinalUploadedCountries { get; set; } = new();
    }
}