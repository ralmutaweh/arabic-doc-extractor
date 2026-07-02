using System.Text.Json;

namespace ArabicPdfReader.Utilities
{
    public class PerformanceMonitor
    {
        private ILogger<PerformanceMonitor> logger;
        private IConfiguration configuration;

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public async Task<PerformanceSummary> ReadSummaryAsync()
        {
            var path = configuration["PerformanceSummaryPath"] ?? "/app/logs/performance_summary.json";

            if (!System.IO.File.Exists(path))
                return new PerformanceSummary();

            var json = await System.IO.File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<PerformanceSummary>(json) ?? new PerformanceSummary();
        }

        public async Task WriteSummaryAsync(PerformanceSummary summary) 
        {
            var path = configuration["PerformanceSummaryPath"] ?? "/app/logs/performance_summary.json";
            var json = JsonSerializer.Serialize(summary);
            await System.IO.File.WriteAllTextAsync(path, json);
        }

        public async Task UpdateAfterExtractionAsync(long? latencyMs)
        {
           var summary = await ReadSummaryAsync();

           summary.TotalExtractions++;

           var today = DateTime.UtcNow.Date;
           var lastUpdatedDate = DateTime.TryParse(summary.LastUpdated, out var parsed) ? parsed.Date : DateTime.MinValue.Date;

           if (lastUpdatedDate < today)
                summary.ExtractionsToday = 1;
           else
                summary.ExtractionsToday++;

           summary.LastUpdated = DateTime.UtcNow.ToString();

           if (latencyMs.HasValue)
                summary.AverageLatencyMs = ((summary.AverageLatencyMs * (summary.TotalExtractions - 1)) + latencyMs.Value) / summary.TotalExtractions;

           await WriteSummaryAsync(summary);
        }

        public async Task UpdateAfterComparisonAsync(bool fullMatch, int changedCount)
        {
            var summary = await ReadSummaryAsync();

            summary.TotalComparisons++;

            if (fullMatch) summary.FullMatchCount++;

            summary.TotalFieldsChanged += changedCount;

            summary.LastUpdated = DateTime.UtcNow.ToString();

            await WriteSummaryAsync(summary);
        }
    }

    public class PerformanceSummary
    {
        public int TotalExtractions { get; set; }
        public int TotalComparisons { get; set; } // This will be useful when the comparison is toggled off
        public int FullMatchCount { get; set;}
        public int TotalFieldsChanged {get; set; } // Not a final full match 
        public long AverageLatencyMs { get; set; }
        public int ExtractionsToday { get; set; }
        public string LastUpdated { get; set; } = "No logs yet — will update on first extraction.";
    }
}