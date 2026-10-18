namespace Qingtu.Services;

public static class ImagePaths
{
    private static readonly HashSet<string> Exts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp"
    };

    public static bool IsImageFile(string path)
    {
        var ext = Path.GetExtension(path);
        return Exts.Contains(ext);
    }

    public static string DefaultOutputDir(string inputFile)
    {
        var dir = Path.GetDirectoryName(inputFile);
        return dir is null ? "compressed" : Path.Combine(dir, "compressed");
    }
}
