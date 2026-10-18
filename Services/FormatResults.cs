using Qingtu.Models;

namespace Qingtu.Services;

public static class FormatResults
{
    public static string FormatLog(IEnumerable<JobResult> results)
    {
        return string.Join(Environment.NewLine, results.Select(FormatLine));
    }

    private static string FormatLine(JobResult r)
    {
        var name = Path.GetFileName(r.InputPath);
        if (string.IsNullOrEmpty(r.OutputPath))
            return $"{name} → {r.Message}";
        var outName = Path.GetFileName(r.OutputPath);
        return $"{name} → {outName} ({HumanBytes(r.BytesBefore)} → {HumanBytes(r.BytesAfter)})";
    }

    private static string HumanBytes(long n)
    {
        if (n < 1024) return $"{n} B";
        if (n < 1024 * 1024) return $"{n / 1024.0:F1} KB";
        return $"{n / 1024.0 / 1024.0:F1} MB";
    }
}
