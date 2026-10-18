namespace Qingtu.Models;

public sealed class JobResult
{
    public required string InputPath { get; init; }
    public string OutputPath { get; init; } = "";
    public long BytesBefore { get; init; }
    public long BytesAfter { get; init; }
    public string Message { get; init; } = "";
}
