namespace Qingtu.Models;

public sealed class JobOptions
{
    public int Quality { get; init; } = 80;
    /// <summary>0 = 不缩放</summary>
    public int MaxSide { get; init; }
    public OutputFormat Format { get; init; } = OutputFormat.KeepOriginal;
    public string? OutputDir { get; init; }
}
