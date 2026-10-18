using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Qingtu.Models;
using Qingtu.Services;

namespace Qingtu;

public partial class MainWindow : Window
{
    private readonly List<string> _files = [];
    private bool _isCompressTab = true;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        SliderQuality.ValueChanged += (_, _) => TxtQuality.Text = ((int)SliderQuality.Value).ToString();
        UpdateTabUi();
        UpdateRunLabel();
    }

    private void OnTabCompress(object sender, RoutedEventArgs e)
    {
        _isCompressTab = true;
        UpdateTabUi();
    }

    private void OnTabStitch(object sender, RoutedEventArgs e)
    {
        _isCompressTab = false;
        UpdateTabUi();
    }

    private void UpdateTabUi()
    {
        PanelCompress.Visibility = _isCompressTab ? Visibility.Visible : Visibility.Collapsed;
        TxtStitchHint.Visibility = _isCompressTab ? Visibility.Collapsed : Visibility.Visible;
        StyleTab(BtnTabCompress, _isCompressTab);
        StyleTab(BtnTabStitch, !_isCompressTab);
        UpdateRunLabel();
    }

    private static void StyleTab(Button btn, bool active)
    {
        btn.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
        btn.Foreground = active
            ? (Brush)btn.FindResource("TealBrush")
            : (Brush)btn.FindResource("MutedBrush");
        btn.Background = active
            ? (Brush)btn.FindResource("InputBrush")
            : (Brush)btn.FindResource("CardBrush");
        btn.BorderBrush = active
            ? (Brush)btn.FindResource("TealBrush")
            : (Brush)btn.FindResource("BorderBrush");
    }

    private void UpdateRunLabel()
    {
        BtnRun.Content = _isCompressTab ? $"开始压缩 ({_files.Count})" : $"开始拼接 ({_files.Count})";
    }

    private void OnPickFiles(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "图片|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp|所有文件|*.*",
            Multiselect = true
        };
        if (dlg.ShowDialog() == true)
            AddFiles(dlg.FileNames);
    }

    private void OnPickOutDir(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() == true)
        {
            TxtOutDir.Text = dlg.FolderName;
            ChkCustomOut.IsChecked = true;
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            AddFiles(paths);
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        foreach (var p in paths)
        {
            if (!ImagePaths.IsImageFile(p) || _files.Contains(p))
                continue;
            _files.Add(p);
            FileList.Items.Add(System.IO.Path.GetFileName(p));
        }

        TxtDropHint.Text = _files.Count > 0 ? $"已选 {_files.Count} 个文件" : "拖拽图片到这里";
        TxtDropHint.Foreground = _files.Count > 0
            ? (Brush)FindResource("TealBrush")
            : (Brush)FindResource("MutedBrush");
        UpdateRunLabel();
    }

    private JobOptions BuildOptions()
    {
        _ = int.TryParse(TxtMaxSide.Text.Trim(), out var maxSide);
        if (maxSide < 0) maxSide = 0;
        return new JobOptions
        {
            Quality = (int)SliderQuality.Value,
            MaxSide = maxSide,
            Format = GetFormat(),
            OutputDir = ChkCustomOut.IsChecked == true && !string.IsNullOrWhiteSpace(TxtOutDir.Text)
                ? TxtOutDir.Text.Trim()
                : null
        };
    }

    private OutputFormat GetFormat()
    {
        if (FmtJpeg.IsChecked == true) return OutputFormat.Jpeg;
        if (FmtPng.IsChecked == true) return OutputFormat.Png;
        if (FmtWebp.IsChecked == true) return OutputFormat.WebP;
        return OutputFormat.KeepOriginal;
    }

    private async void OnRun(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        if (_files.Count == 0)
        {
            TxtLog.Text = "请先添加图片";
            return;
        }

        _busy = true;
        BtnRun.IsEnabled = false;
        try
        {
            if (_isCompressTab)
            {
                var opts = BuildOptions();
                var results = await Task.Run(() => ImageCompressService.RunJobs(_files, opts));
                TxtLog.Text = FormatResults.FormatLog(results);
            }
            else
            {
                if (_files.Count < 2)
                {
                    TxtLog.Text = "拼接需要至少 2 张图";
                    return;
                }

                var outPath = await Task.Run(() =>
                {
                    var opts = BuildOptions();
                    string path;
                    if (!string.IsNullOrWhiteSpace(opts.OutputDir))
                        path = System.IO.Path.Combine(opts.OutputDir!, "stitched.png");
                    else
                        path = System.IO.Path.Combine(ImagePaths.DefaultOutputDir(_files[0]), "stitched.png");
                    ImageStitchService.StitchHorizontal(_files, path);
                    return path;
                });
                TxtLog.Text = $"拼接完成 → {outPath}";
            }
        }
        catch (Exception ex)
        {
            TxtLog.Text = ex.Message;
        }
        finally
        {
            _busy = false;
            BtnRun.IsEnabled = true;
        }
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        _files.Clear();
        FileList.Items.Clear();
        TxtLog.Clear();
        TxtDropHint.Text = "拖拽图片到这里";
        TxtDropHint.Foreground = (Brush)FindResource("MutedBrush");
        UpdateRunLabel();
    }
}
