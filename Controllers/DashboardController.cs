using ArabicPdfReader.Models;
using ArabicPdfReader.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace ArabicPdfReader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        PerformanceMonitor performanceMonitor;
        IConfiguration configuration;
        public DashboardController(PerformanceMonitor performanceMonitor, IConfiguration configuration)
        {
            this.performanceMonitor = performanceMonitor;
            this.configuration = configuration;
        }

        [HttpGet("report-json")]
        public async Task<IActionResult> ReportJson()
        {
            var summary = await performanceMonitor.ReadSummaryAsync();

            return Ok(summary);
        }

        [HttpGet("recent-logs")]
        public async Task<IActionResult> RecentLogs()
        {
            var csvPath = configuration["CsvExtractionLogPath"] ?? "/app/logs/extraction_log.csv";

            var logs = await CsvTailReader.ReadLastLinesAsync(csvPath, 10);

            var entries = new List<ExtractionLogEntry>();

            foreach (var line in logs)
            {
                string[] fields = line.Split(',');

                var entry = new ExtractionLogEntry
                {
                    ExtractionId = fields[0],
                    Timestamp = fields[1],
                    FileName = fields[2],
                    FileType = fields[3],
                    FileSizeBytes = long.Parse(fields[4]),
                    LatencyMs = long.Parse(fields[5]),
                    Model = fields[6],
                    PromptTokens = fields[7],
                    CompletionTokens = fields[8],
                    TotalDurationMs = fields[9],
                    EvalDurationMs = fields[10],
                    DoneReason = fields[11],
                };

                entries.Add(entry);
            }

            return Ok(entries);
        }

        [HttpGet("recent-comparisons")]
        public async Task<IActionResult> RecentComparisons()
        {
            var csvPath = configuration["CsvComparisonLogPath"] ?? "/app/logs/feedback_log.csv";

            var comparisons = await CsvTailReader.ReadLastLinesAsync(csvPath, 10);

            var entries = new List<ComparisonLogEntry>();

            foreach (var line in comparisons)
            {
                string[] fields = line.Split(',');

                var entry = new ComparisonLogEntry
                {
                    ExtractionId = fields[0],
                    Timestamp = fields[1],
                    NamesMatch = bool.Parse(fields[2]),
                    EntitiesMatch = bool.Parse(fields[3]),
                    CountriesMatch = bool.Parse(fields[4]),
                    OverallScore = fields[5],
                };

                entries.Add(entry);
            }

            return Ok(entries);
        }

        [HttpGet("latency-trend")]
        public async Task<IActionResult> LatencyTrend()
        {
            var csvPath = configuration["CsvExtractionLogPath"] ?? "/app/logs/extraction_log.csv";
            var lines = await CsvTailReader.ReadLastLinesAsync(csvPath, 200);

            var entries = lines.Select(line => {
                var f = line.Split(',');
                return new { Timestamp = f[1], LatencyMs = long.Parse(f[5]) };
            }).ToList();

            entries.Reverse();

            return Ok(entries);
        }
    }
}