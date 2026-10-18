using Qingtu.Models;
using SkiaSharp;

namespace Qingtu.Services;

public static class ImageCompressService
{
    public static IReadOnlyList<JobResult> RunJobs(IEnumerable<string> paths, JobOptions opts)
    {
        return paths.Select(p => CompressOne(p, opts)).ToList();
    }

    public static JobResult CompressOne(string input, JobOptions opts)
    {
        try
        {
            if (!File.Exists(input))
                return Fail(input, "不是有效文件");

            var bytesBefore = new FileInfo(input).Length;
            var fmt = TargetFormat(input, opts.Format);
            var outDir = string.IsNullOrWhiteSpace(opts.OutputDir)
                ? ImagePaths.DefaultOutputDir(input)
                : opts.OutputDir!;
            Directory.CreateDirectory(outDir);

            using var bitmap = LoadAndResize(input, opts.MaxSide);
            var outPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(input)}.{ExtFor(fmt)}");
            Save(bitmap, outPath, fmt, opts.Quality);

            var bytesAfter = new FileInfo(outPath).Length;
            return new JobResult
            {
                InputPath = input,
                OutputPath = outPath,
                BytesBefore = bytesBefore,
                BytesAfter = bytesAfter,
                Message = "完成"
            };
        }
        catch (Exception ex)
        {
            return Fail(input, ex.Message);
        }
    }

    private static JobResult Fail(string input, string message) => new()
    {
        InputPath = input,
        Message = message
    };

    private static SKBitmap LoadAndResize(string path, int maxSide)
    {
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream);
        var info = codec.Info;
        using var src = SKBitmap.Decode(codec);
        if (src is null)
            throw new InvalidOperationException("无法解码图片");

        if (maxSide <= 0)
            return CloneBitmap(src);

        var w = src.Width;
        var h = src.Height;
        var longest = Math.Max(w, h);
        if (longest <= maxSide)
            return CloneBitmap(src);

        var scale = maxSide / (double)longest;
        var nw = Math.Max(1, (int)Math.Round(w * scale));
        var nh = Math.Max(1, (int)Math.Round(h * scale));
        var resized = src.Resize(new SKImageInfo(nw, nh), SKFilterQuality.High);
        if (resized is null)
            throw new InvalidOperationException("缩放失败");
        return resized;
    }

    private static SKBitmap CloneBitmap(SKBitmap src)
    {
        var copy = new SKBitmap(src.Width, src.Height, src.ColorType, src.AlphaType);
        if (!src.CopyTo(copy))
            throw new InvalidOperationException("复制位图失败");
        return copy;
    }

    private static void Save(SKBitmap bitmap, string path, SKEncodedImageFormat fmt, int quality)
    {
        var q = Math.Clamp(quality, 1, 100);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = fmt switch
        {
            SKEncodedImageFormat.Jpeg => image.Encode(SKEncodedImageFormat.Jpeg, q),
            SKEncodedImageFormat.Png => image.Encode(SKEncodedImageFormat.Png, PngQuality(q)),
            SKEncodedImageFormat.Webp => image.Encode(SKEncodedImageFormat.Webp, q),
            _ => image.Encode(fmt, q)
        };
        if (data is null)
            throw new InvalidOperationException("编码失败");
        using var fs = File.Create(path);
        data.SaveTo(fs);
    }

    private static int PngQuality(int q) => q >= 85 ? 100 : q >= 50 ? 80 : 60;

    private static SKEncodedImageFormat TargetFormat(string path, OutputFormat choice)
    {
        return choice switch
        {
            OutputFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            OutputFormat.Png => SKEncodedImageFormat.Png,
            OutputFormat.WebP => SKEncodedImageFormat.Webp,
            _ => DetectFormat(path)
        };
    }

    private static SKEncodedImageFormat DetectFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".webp" => SKEncodedImageFormat.Webp,
            ".gif" => SKEncodedImageFormat.Gif,
            ".bmp" => SKEncodedImageFormat.Bmp,
            _ => SKEncodedImageFormat.Png
        };
    }

    private static string ExtFor(SKEncodedImageFormat fmt) => fmt switch
    {
        SKEncodedImageFormat.Jpeg => "jpg",
        SKEncodedImageFormat.Webp => "webp",
        SKEncodedImageFormat.Gif => "gif",
        SKEncodedImageFormat.Bmp => "bmp",
        _ => "png"
    };
}
