using System.Text.RegularExpressions;

namespace TestUtils;

public static partial class OutputLogParser
{
    public static List<string> ParseLogSegments(string log, out (string duration, string maxMemoryUsed, string initDuration) report)
    {
        var segments = new List<string>();
        var regex = ParseLogRegex();
        var matches = regex.Matches(log);
        report = ("N/A", "N/A", "N/A");

        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            if (index == matches.Count - 1)
            {
                report = ExtractReportMetrics(match.Value);
                continue;
            }

            segments.Add(match.Value);
        }

        return segments;
    }

    public static (string duration, string maxMemoryUsed, string initDuration) ExtractReportMetrics(string report)
    {
        var regex = ExtractMetricsRegex();
        var match = regex.Match(report);

        if (!match.Success) return ("N/A", "N/A", "N/A");
        
        var duration = match.Groups["duration"].Value;
        var maxMemoryUsed = match.Groups["maxMemory"].Value;
        var initDuration = match.Groups["initDuration"].Success ? match.Groups["initDuration"].Value : "N/A";

        return (duration, maxMemoryUsed, initDuration);
    }

    [GeneratedRegex(@"Duration: (?<duration>\d+\.\d+) ms.*?Max Memory Used: (?<maxMemory>\d+) MB(?:.*?Init Duration: (?<initDuration>\d+\.\d+) ms)?", RegexOptions.Singleline)]
    private static partial Regex ExtractMetricsRegex();
    [GeneratedRegex(@"(\{(?:[^{}]|(?<open>\{)|(?<-open>\}))*\}(?(open)(?!)))|(REPORT RequestId:.*?)(?=START RequestId:|\z)", RegexOptions.Singleline)]
    private static partial Regex ParseLogRegex();
}