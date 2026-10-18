using SkiaSharp;

namespace Qingtu.Services;

public static class ImageStitchService
{
    public static void StitchHorizontal(IReadOnlyList<string> inputs, string outputPath)
    {
        if (inputs.Count < 2)
            throw new InvalidOperationException("拼接至少需要 2 张图片");

        var loaded = new List<SKBitmap>();
        var pieces = new List<SKBitmap>();
        try
        {
            foreach (var p in inputs)
            {
                using var fs = File.OpenRead(p);
                var bmp = SKBitmap.Decode(fs);
                if (bmp is null)
                    throw new InvalidOperationException($"无法打开: {Path.GetFileName(p)}");
                loaded.Add(bmp);
            }

            var targetH = loaded.Min(b => b.Height);
            if (targetH <= 0)
                throw new InvalidOperationException("图片高度无效");

            foreach (var bmp in loaded)
            {
                if (bmp.Height == targetH)
                {
                    var copy = new SKBitmap(bmp.Width, bmp.Height, bmp.ColorType, bmp.AlphaType);
                    if (!bmp.CopyTo(copy))
                        throw new InvalidOperationException("复制位图失败");
                    pieces.Add(copy);
                    continue;
                }

                var w = Math.Max(1, (int)Math.Round(bmp.Width * (targetH / (double)bmp.Height)));
                var s = bmp.Resize(new SKImageInfo(w, targetH), SKFilterQuality.High);
                if (s is null)
                    throw new InvalidOperationException("缩放失败");
                pieces.Add(s);
            }

            var totalW = pieces.Sum(b => b.Width);
            using var canvasBmp = new SKBitmap(totalW, targetH);
            using var canvas = new SKCanvas(canvasBmp);
            canvas.Clear(SKColors.Transparent);
            var x = 0;
            foreach (var s in pieces)
            {
                canvas.DrawBitmap(s, x, 0);
                x += s.Width;
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var image = SKImage.FromBitmap(canvasBmp);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            if (data is null)
                throw new InvalidOperationException("导出 PNG 失败");
            using var outFs = File.Create(outputPath);
            data.SaveTo(outFs);
        }
        finally
        {
            foreach (var s in pieces)
                s.Dispose();
            foreach (var b in loaded)
                b.Dispose();
        }
    }
}
