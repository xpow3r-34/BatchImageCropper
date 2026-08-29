using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualBasic.FileIO;

namespace BatchImageCropper;

public partial class MainWindow : Window
{
    private ObservableCollection<ImageItem> Images { get; set; }
    private ImageItem _currentImage;
    private ImageItem _activeImage;
    private ImageItem _previouslyActiveImage;
    private System.Windows.Point _startPoint;
    private System.Windows.Point _cropStartPoint;
    private bool _isDragging;
    private bool _isMovingCrop = false;
    private bool _isTurkish = true;
    private bool _syncEnabled = false;
    private double? _aspectRatio;
    private int _previewSizePercent = 25;

    private DetailWindow _detailWindow;

    private bool _isRelaying;
    private bool _relayoutPending;

    private ExportOptions Options { get; set; }
    private CancellationTokenSource _exportCts;

    private static readonly System.Windows.Media.Brush ActiveBorderBrush = System.Windows.Media.Brushes.Red;
    private static readonly System.Windows.Media.Brush InactiveBorderBrush = System.Windows.Media.Brushes.Gray;
    private static readonly System.Windows.Thickness ActiveBorderThickness = new System.Windows.Thickness(3);
    private static readonly System.Windows.Thickness InactiveBorderThickness = new System.Windows.Thickness(1);

    public MainWindow()
    {
        try
        {
            Logger.Information("Uygulama başlatılıyor...");
            InitializeComponent();
            Images = new ObservableCollection<ImageItem>();
            ImageGrid.ItemsSource = Images;

            Options = new ExportOptions();
            PreserveMetadataCheckBox.IsChecked = Options.PreserveMetadata;

            ImageScrollViewer.SizeChanged += (_, _) =>
            {
                if (ImageScrollViewer.Visibility == Visibility.Visible)
                {
                    ScheduleRelayout();
                }
            };

            DropZone.SizeChanged += (_, _) => UpdateDropZoneWatermark();

            if (Dispatcher != null)
            {
                Dispatcher.BeginInvoke(UpdateLanguage);
            }

            UpdateUI();
            Logger.Information("Uygulama başarıyla başlatıldı");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Uygulama başlatılamadı");
            MessageBox.Show("Uygulama başlatılamadı. Logları kontrol edin.", "Kritik Hata",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region Sürükle & Bırak Olayları

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                DropZone.Background = new SolidColorBrush(Color.FromRgb(230, 240, 250));
                DropZone.BorderBrush = new SolidColorBrush(Color.FromRgb(100, 150, 200));
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Sürükleme işleyicisinde hata");
            e.Effects = DragDropEffects.None;
        }
    }

    private void Window_DragLeave(object sender, DragEventArgs e)
    {
        DropZone.Background = new SolidColorBrush(Color.FromRgb(248, 248, 248));
        DropZone.BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221));
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    _ = ResimleriYukle(files);
                }
            }

            DropZone.Background = new SolidColorBrush(Color.FromRgb(248, 248, 248));
            DropZone.BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Bırakma işleyicisinde kritik hata");
            MessageBox.Show("Bırakılan dosyalar işlenirken hata oluştu. Logları kontrol edin.",
                          "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ResimleriYukle(string[] filePaths)
    {
        try
        {
            if (filePaths == null) return;

            var supportedFormats = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var imageFiles = filePaths
                .Where(file =>
                {
                    try
                    {
                        var ext = Path.GetExtension(file)?.ToLowerInvariant();
                        return !string.IsNullOrEmpty(ext) && supportedFormats.Contains(ext) && File.Exists(file);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("Dosya kontrolünde hata: {File} - {Error}", file, ex.Message);
                        return false;
                    }
                })
                .ToArray();

            var totalFiles = imageFiles.Length;
            if (totalFiles == 0)
            {
                ShowWarning(_isTurkish ? "Desteklenen resim dosyası bulunamadı."
                                       : "No supported image files found.");
                return;
            }

            var processedCount = 0;

            foreach (var filePath in imageFiles)
            {
                try
                {
                    processedCount++;

                    if (processedCount % 10 == 0 || processedCount == 1 || processedCount == totalFiles)
                    {
                        StatusText.Text = $"{processedCount}/{totalFiles} " +
                            (_isTurkish ? "resim yükleniyor..." : "images loading...");
                    }

                    var thumbnail = await Task.Run(() =>
                    {
                        return ImageProcessor.LoadImage(filePath, out _, out _);
                    });

                    // ImageItem ve BitmapImage UI thread'inde oluşturulur;
                    // yalnızca ağır görüntü yükleme işi arka planda çalışır.
                    // (WPF: DependencyObject'ler kendi thread'inde oluşturulmak zorundadır.)
                    var imageItem = new ImageItem
                    {
                        Path = filePath,
                        Thumbnail = thumbnail
                    };

                    Images.Add(imageItem);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Resim yüklenemedi: {File}", filePath);
                    ShowWarning($"{Path.GetFileName(filePath)} yüklenirken hata: {ex.Message}");
                }
            }

            UpdateUI();

            if (Images.Count <= LargePreviewThreshold)
            {
                FitAllToLargePreview();
            }
            else
            {
                RelayoutImages();
            }

            StatusText.Text = _isTurkish
                ? $"{Images.Count} resim yüklendi"
                : $"{Images.Count} images loaded";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Resim yüklemede kritik hata");
            ShowError(_isTurkish ? "Resimler yüklenirken hata oluştu. Logları kontrol edin."
                                 : "An error occurred while loading images. Check the logs.");
        }
    }

    #endregion

    #region Kırpma Olayları

    private void Crop_Start(object sender, MouseButtonEventArgs e)
    {
        var canvas = sender as Canvas;
        if (canvas == null) return;

        _currentImage = canvas.DataContext as ImageItem;
        if (_currentImage == null) return;

        SetActiveImage(_currentImage);

        if (e.ClickCount == 2)
        {
            OpenDetailImage(_currentImage);

            _isDragging = false;
            _isMovingCrop = false;
            canvas.ReleaseMouseCapture();
            return;
        }

        _isDragging = true;
        _startPoint = e.GetPosition(canvas);

        if (_currentImage.HasCrop)
        {
            var cropRect = new System.Windows.Rect(
                _currentImage.CropX,
                _currentImage.CropY,
                _currentImage.CropWidth,
                _currentImage.CropHeight);

            if (cropRect.Contains(_startPoint))
            {
                _isMovingCrop = true;
                _cropStartPoint = new System.Windows.Point(_currentImage.CropX, _currentImage.CropY);
                canvas.CaptureMouse();
                return;
            }
        }

        _isMovingCrop = false;
        _currentImage.CropX = _startPoint.X;
        _currentImage.CropY = _startPoint.Y;
        _currentImage.CropWidth = 0;
        _currentImage.CropHeight = 0;
        canvas.CaptureMouse();
    }

    private void Crop_Move(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _currentImage == null) return;

        var canvas = sender as Canvas;
        if (canvas == null) return;

        var currentPoint = e.GetPosition(canvas);
        var maxWidth = _currentImage.DisplayWidth;
        var maxHeight = _currentImage.DisplayHeight;

        if (_isMovingCrop)
        {
            var deltaX = currentPoint.X - _startPoint.X;
            var deltaY = currentPoint.Y - _startPoint.Y;

            var newX = Math.Max(0, Math.Min(_cropStartPoint.X + deltaX, maxWidth - _currentImage.CropWidth));
            var newY = Math.Max(0, Math.Min(_cropStartPoint.Y + deltaY, maxHeight - _currentImage.CropHeight));

            _currentImage.CropX = newX;
            _currentImage.CropY = newY;
        }
        else
        {
            if (_aspectRatio.HasValue)
            {
                ComputeRatioRect(_startPoint, currentPoint, _aspectRatio.Value, maxWidth, maxHeight,
                    out var ratX, out var ratY, out var ratW, out var ratH);

                _currentImage.CropX = ratX;
                _currentImage.CropY = ratY;
                _currentImage.CropWidth = ratW;
                _currentImage.CropHeight = ratH;
            }
            else
            {
                var rectX = Math.Min(_startPoint.X, currentPoint.X);
                var rectY = Math.Min(_startPoint.Y, currentPoint.Y);
                var rectWidth = Math.Abs(currentPoint.X - _startPoint.X);
                var rectHeight = Math.Abs(currentPoint.Y - _startPoint.Y);

                rectX = Math.Max(0, Math.Min(rectX, maxWidth));
                rectY = Math.Max(0, Math.Min(rectY, maxHeight));
                rectWidth = Math.Max(0, Math.Min(rectWidth, maxWidth - rectX));
                rectHeight = Math.Max(0, Math.Min(rectHeight, maxHeight - rectY));

                _currentImage.CropX = rectX;
                _currentImage.CropY = rectY;
                _currentImage.CropWidth = rectWidth;
                _currentImage.CropHeight = rectHeight;
            }
        }

        var widthText = _currentImage.CropWidth.ToString("0");
        var heightText = _currentImage.CropHeight.ToString("0");
        StatusText.Text = _isTurkish
            ? $"Seçim: {widthText} x {heightText} px"
            : $"Selection: {widthText} x {heightText} px";

        if (_syncEnabled)
        {
            ApplyCropToAll(_currentImage);
        }
    }

    private void Crop_End(object sender, MouseButtonEventArgs e)
    {
        var canvas = sender as Canvas;
        canvas?.ReleaseMouseCapture();

        _isDragging = false;
        _isMovingCrop = false;
        _currentImage = null;

        StatusText.Text = _activeImage != null && _activeImage.HasCrop
            ? (_isTurkish ? "Kırpma alanı hazır" : "Crop area ready")
            : (_isTurkish ? "Kırpma alanı çizmek için sürükleyin" : "Drag to draw a crop area");
    }

    #endregion

    #region Buton Olayları

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var exportable = Images.Where(i => i.HasCrop).ToList();
        if (exportable.Count == 0)
        {
            ShowInfo(_isTurkish
                ? "Kırpma alanı seçilmiş resim yok."
                : "No images with a crop area selected.");
            return;
        }

        var plan = BuildExportPlan(exportable);
        if (plan.Count == 0)
        {
            ShowWarning(_isTurkish
                ? "Geçerli çıktı yolu oluşturulamadı. Ayarları kontrol edin."
                : "Could not build valid output paths. Check the settings.");
            return;
        }

        SetExportUiBusy(true, plan.Count);
        _exportCts = new CancellationTokenSource();

        var errors = new List<string>();
        var exported = 0;
        var canceled = false;
        var exportedItems = new List<ImageItem>();
        var deletedOriginals = 0;

        try
        {
            for (var i = 0; i < plan.Count; i++)
            {
                if (_exportCts.Token.IsCancellationRequested)
                {
                    canceled = true;
                    break;
                }

                var (item, outputPath, format) = plan[i];
                try
                {
                    await Task.Run(() => ImageProcessor.CropImage(
                        item.Path,
                        item.CropNormX,
                        item.CropNormY,
                        item.CropNormWidth,
                        item.CropNormHeight,
                        outputPath,
                        format,
                        Options.Quality,
                        Options.PreserveMetadata));

                    exported++;
                    exportedItems.Add(item);
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(item.Path)}: {ex.Message}");
                    Logger.Error(ex, "Dışa aktarılamadı: {File}", item.Path);
                }

                ExportProgress.Value = i + 1;
                StatusText.Text = _isTurkish
                    ? $"Dışa aktarılıyor: {i + 1}/{plan.Count}..."
                    : $"Exporting: {i + 1}/{plan.Count}...";
            }
        }
        finally
        {
            SetExportUiBusy(false, 0);
            _exportCts?.Dispose();
            _exportCts = null;
        }

        if (!canceled && exportedItems.Count > 0 && DeleteOriginalsCheckBox?.IsChecked == true)
        {
            deletedOriginals = DeleteExportedOriginals(exportedItems, errors);
        }

        ShowExportResult(exported, errors, canceled, deletedOriginals);
    }

    private int DeleteExportedOriginals(List<ImageItem> exportedItems, List<string> errors)
    {
        var deleted = 0;
        var toRemove = new List<ImageItem>();

        foreach (var item in exportedItems)
        {
            try
            {
                FileSystem.DeleteFile(item.Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                deleted++;
                toRemove.Add(item);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Orijinal geri dönüşüm kutusuna atılamadı: {File}", item.Path);
                errors.Add($"{Path.GetFileName(item.Path)} ({(_isTurkish ? "orijinal silinemedi" : "original could not be deleted")}): {ex.Message}");
            }
        }

        if (toRemove.Count > 0)
        {
            foreach (var item in toRemove)
            {
                Images.Remove(item);
            }
        }

        return deleted;
    }

    private List<(ImageItem Item, string OutputPath, ImageFormat Format)> BuildExportPlan(List<ImageItem> exportable)
    {
        var plan = new List<(ImageItem, string, ImageFormat)>();
        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in exportable)
        {
            var directory = GetOutputDirectory(item);
            if (string.IsNullOrEmpty(directory)) continue;

            var baseName = Path.GetFileNameWithoutExtension(item.Path) + Options.Suffix;
            var extension = ImageProcessor.GetOutputExtension(Options.Format, item.FileExtension);
            var path = GetUniquePath(Path.Combine(directory, baseName + extension), usedPaths);

            plan.Add((item, path, ImageProcessor.ResolveFormat(Options.Format, item.FileExtension)));
        }

        return plan;
    }

    private string GetOutputDirectory(ImageItem item)
    {
        if (!Options.UseSourceFolder && !string.IsNullOrWhiteSpace(Options.OutputFolder))
        {
            return Options.OutputFolder;
        }

        var sourceDir = Path.GetDirectoryName(item.Path);
        if (!string.IsNullOrEmpty(sourceDir))
        {
            Directory.CreateDirectory(sourceDir);
            return sourceDir;
        }

        return null;
    }

    private static string GetUniquePath(string basePath, HashSet<string> usedPaths)
    {
        var directory = Path.GetDirectoryName(basePath);
        var name = Path.GetFileNameWithoutExtension(basePath);
        var ext = Path.GetExtension(basePath);

        var candidate = basePath;
        var counter = 1;
        while (usedPaths.Contains(candidate) || File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{name} ({counter}){ext}");
            counter++;
        }

        usedPaths.Add(candidate);
        return candidate;
    }

    private void SetExportUiBusy(bool busy, int planCount)
    {
        ExportButton.IsEnabled = !busy;
        OptionsButton.IsEnabled = !busy;
        CancelExportButton.IsEnabled = busy;
        CancelExportButton.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        ExportProgress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;

        if (busy)
        {
            ExportProgress.Maximum = planCount;
            ExportProgress.Value = 0;
        }
    }

    private void ShowExportResult(int exported, List<string> errors, bool canceled, int deletedOriginals = 0)
    {
        if (canceled)
        {
            StatusText.Text = _isTurkish ? "Dışa aktarım iptal edildi" : "Export canceled";
            ShowInfo(_isTurkish
                ? $"{exported} resim dışa aktarıldı, işlem iptal edildi."
                : $"{exported} images exported, operation canceled.");
            return;
        }

        StatusText.Text = _isTurkish
            ? $"{exported} resim dışa aktarıldı"
            : $"{exported} images exported";

        var message = exported > 0
            ? (_isTurkish
                ? $"{exported} resim başarıyla dışa aktarıldı!"
                : $"{exported} images exported successfully!")
            : (_isTurkish
                ? "Hiç resim dışa aktarılmadı."
                : "No images were exported.");

        if (deletedOriginals > 0)
        {
            message += $"\n\n{(_isTurkish
                ? $"{deletedOriginals} orijinal geri dönüşüm kutusuna atıldı."
                : $"{deletedOriginals} original image(s) moved to the Recycle Bin.")}";
        }

        if (errors.Any())
        {
            message += $"\n\n{(_isTurkish ? "Hatalar:" : "Errors:")}\n{string.Join("\n", errors)}";
        }

        MessageBox.Show(message,
            _isTurkish ? "Dışa Aktarım Tamamlandı" : "Export Complete",
            MessageBoxButton.OK,
            exported > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void CancelExport_Click(object sender, RoutedEventArgs e)
    {
        _exportCts?.Cancel();
        StatusText.Text = _isTurkish ? "İptal ediliyor..." : "Canceling...";
        CancelExportButton.IsEnabled = false;
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (_detailWindow != null)
        {
            _detailWindow.Close();
            _detailWindow = null;
        }

        SetActiveImage(null);
        Images.Clear();
        UpdateUI();
        Logger.Information("Tüm resimler temizlendi");
    }

    private void ClearCrop_Click(object sender, RoutedEventArgs e)
    {
        foreach (var image in Images)
        {
            image.ClearCrop();
        }

        StatusText.Text = _isTurkish ? "Tüm seçimler temizlendi" : "All selections cleared";
        Logger.Information("Tüm kırpma seçimleri temizlendi");
    }

    private void Options_Click(object sender, RoutedEventArgs e)
    {
        var window = new ExportOptionsWindow(Options, _isTurkish)
        {
            Owner = this
        };

        if (window.ShowDialog() == true && window.Result != null)
        {
            Options = window.Result;
            PreserveMetadataCheckBox.IsChecked = Options.PreserveMetadata;
            Logger.Information("Dışa aktarma ayarları güncellendi: {Format}, {Quality}, {Suffix}",
                Options.Format, Options.Quality, Options.Suffix);
        }
    }

    #endregion

    #region Düzen

    private const double ItemLanePadding = 16;
    private const double FixedColumnWidth = 320;
    private const int LargePreviewThreshold = 4;

    private double GetTargetDisplayWidth()
    {
        var available = ImageScrollViewer?.ViewportWidth ?? 0;
        if (available <= 0) available = ImageScrollViewer?.ActualWidth ?? 0;
        if (available <= 0) available = ActualWidth;
        if (available <= 0) available = 800;

        // Pencere genişliğine göre otomatik dizilim: her görsel en fazla sabit genişlikte,
        // görünümden dar olsa pencereyi taşmayacak şekilde boyutlanır (WrapPanel sütunları sarlar).
        return Math.Max(80, Math.Min(FixedColumnWidth, available - ItemLanePadding));
    }

    private void ScheduleRelayout()
    {
        if (_relayoutPending) return;
        _relayoutPending = true;
        Dispatcher.BeginInvoke(() =>
        {
            _relayoutPending = false;
            RelayoutImages();
        });
    }

    private void RelayoutImages()
    {
        if (_isRelaying) return;
        if (Images == null || Images.Count == 0) return;

        var target = GetTargetDisplayWidth();
        if (target <= 0) return;

        _isRelaying = true;
        try
        {
            foreach (var image in Images)
            {
                if (Math.Abs(image.DisplayWidth - target) >= 0.5)
                {
                    image.FitToWidth(target);
                }
            }
        }
        finally
        {
            _isRelaying = false;
        }
    }

    private void FitAllToLargePreview()
    {
        if (Images == null || Images.Count == 0) return;

        var lane = GetTargetDisplayWidth();
        var viewport = ImageScrollViewer?.ViewportWidth > 0
            ? ImageScrollViewer.ViewportWidth
            : lane + ItemLanePadding;

        foreach (var image in Images)
        {
            image.FitToLargePreview(lane, viewport, _previewSizePercent);
        }
    }

    private void PreviewSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsInitialized) return;

        _previewSizePercent = (int)e.NewValue;

        if (PreviewSizeLabel != null)
        {
            PreviewSizeLabel.Text = $"{_previewSizePercent}%";
        }

        if (Images != null && Images.Count > 0 && Images.Count <= LargePreviewThreshold)
        {
            FitAllToLargePreview();
        }
    }

    private void UpdateDropZoneWatermark()
    {
        if (DropZone == null || DropZoneText == null) return;

        var height = DropZone.ActualHeight;
        if (height <= 0) return;

        DropZoneText.FontSize = Math.Max(18, Math.Min(44, height * 0.10));
    }

    private void AspectRatio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized) return;

        var tag = (AspectRatioCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (double.TryParse(tag, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
        {
            _aspectRatio = parsed;
        }
        else
        {
            _aspectRatio = null;
        }

        if (_detailWindow != null)
        {
            _detailWindow.AspectRatio = _aspectRatio;
        }

        if (_activeImage != null && _aspectRatio.HasValue)
        {
            FitCropToAspectRatio(_activeImage, _aspectRatio.Value);

            if (_syncEnabled)
            {
                ApplyCropToAll(_activeImage);
            }

            StatusText.Text = _isTurkish
                ? $"Oran uygulandı: {FormatRatio(_aspectRatio.Value)}"
                : $"Ratio applied: {FormatRatio(_aspectRatio.Value)}";
        }
    }

    private static string FormatRatio(double ratio)
    {
        var w = Math.Round(ratio * 100) / 100;
        var h = Math.Round(100 / ratio) / 100;
        return $"{w:0.##}:{h:0.##}";
    }

    /// <summary>
    /// Başlangıç noktasını köşe kabul ederek, imleç yönünde ve görüntü sınırları içinde
    /// verilen en-boy oranına tam uyan dikdörtgeni hesaplar.
    /// </summary>
    private static void ComputeRatioRect(System.Windows.Point start, System.Windows.Point current,
        double ratio, double maxWidth, double maxHeight,
        out double x, out double y, out double w, out double h)
    {
        var dirX = current.X >= start.X ? 1 : -1;
        var dirY = current.Y >= start.Y ? 1 : -1;

        var bankW = dirX > 0 ? maxWidth - start.X : start.X;
        var bankH = dirY > 0 ? maxHeight - start.Y : start.Y;

        var wantedW = Math.Abs(current.X - start.X);
        var wantedH = Math.Abs(current.Y - start.Y);

        if (ratio > 0 && wantedW / ratio <= bankH && wantedW <= bankW)
        {
            w = wantedW;
            h = w / ratio;
        }
        else
        {
            h = wantedH;
            w = h * ratio;
            if (w > bankW) { w = bankW; h = w / ratio; }
        }

        if (h > bankH) { h = bankH; w = h * ratio; }
        if (w > bankW) { w = bankW; h = w / ratio; }
        if (w <= 0) w = 0;
        if (h <= 0) h = 0;

        x = dirX > 0 ? start.X : start.X - w;
        y = dirY > 0 ? start.Y : start.Y - h;

        x = Math.Max(0, Math.Min(x, maxWidth - w));
        y = Math.Max(0, Math.Min(y, maxHeight - h));
    }

    /// <summary>
    /// Mevcut kırpma alanının merkezini koruyarak, alanını hedef en-boy oranına oturtur.
    /// </summary>
    private static void FitCropToAspectRatio(ImageItem image, double ratio)
    {
        if (!image.HasCrop || ratio <= 0) return;

        var maxWidth = image.DisplayWidth;
        var maxHeight = image.DisplayHeight;
        if (maxWidth <= 0 || maxHeight <= 0) return;

        var currentW = image.CropWidth;
        var currentH = image.CropHeight;
        var centerX = image.CropX + currentW / 2;
        var centerY = image.CropY + currentH / 2;

        var area = currentW * currentH;
        var newWidth = Math.Sqrt(area * ratio);
        var newHeight = newWidth / ratio;

        if (newWidth > maxWidth) { newWidth = maxWidth; newHeight = newWidth / ratio; }
        if (newHeight > maxHeight) { newHeight = maxHeight; newWidth = newHeight * ratio; }
        if (newWidth > maxWidth || newHeight > maxHeight)
        {
            newWidth = Math.Min(maxWidth, maxHeight * ratio);
            newHeight = newWidth / ratio;
        }

        var newX = Math.Max(0, Math.Min(centerX - newWidth / 2, maxWidth - newWidth));
        var newY = Math.Max(0, Math.Min(centerY - newHeight / 2, maxHeight - newHeight));

        image.CropX = newX;
        image.CropY = newY;
        image.CropWidth = newWidth;
        image.CropHeight = newHeight;
    }

    #endregion

    #region Detay Görünümü

    private void OpenDetailImage(ImageItem item)
    {
        if (item == null) return;

        if (_detailWindow != null && _detailWindow.IsVisible)
        {
            _detailWindow.Activate();
            return;
        }

        SetActiveImage(item);
        _isDragging = false;
        _isMovingCrop = false;
        _currentImage = null;
        Mouse.Capture(null);

        _detailWindow = new DetailWindow(item, _aspectRatio, _isTurkish)
        {
            Owner = this
        };
        _detailWindow.CropMoved = OnDetailCropMoved;
        _detailWindow.CropEnded = OnDetailCropEnded;
        _detailWindow.Closed += (_, _) =>
        {
            _detailWindow = null;
        };

        _detailWindow.Show();
        _detailWindow.Activate();
        Logger.Information("Detay penceresi açıldı: {File}", item.Path);
    }

    private void OnDetailCropMoved(ImageItem item)
    {
        if (item != null && item.HasCrop)
        {
            var widthText = item.CropWidth.ToString("0");
            var heightText = item.CropHeight.ToString("0");
            StatusText.Text = _isTurkish
                ? $"Seçim: {widthText} x {heightText} px"
                : $"Selection: {widthText} x {heightText} px";
        }

        if (_syncEnabled && item != null)
        {
            ApplyCropToAll(item);
        }
    }

    private void OnDetailCropEnded(ImageItem item)
    {
        StatusText.Text = item != null && item.HasCrop
            ? (_isTurkish ? "Kırpma alanı hazır" : "Crop area ready")
            : (_isTurkish ? "Kırpma alanı çizmek için sürükleyin" : "Drag to draw a crop area");
    }

    #endregion

    #region Arayüz Güncellemeleri

    /// <summary>
    /// Senkron açıksa, kaynak görseldeki kırpmayı diğer tüm görsellere uygular.
    /// Tüm hesaplamalar normalize (0-1 oran) uzayında yapılır; böylece görüntü boyutu
    /// (önizleme, grid veya detay ölçeği) ne olursa olsun sonuç aynı kalır.
    /// </summary>
    private void ApplyCropToAll(ImageItem source)
    {
        if (!_syncEnabled || source == null || Images == null) return;

        foreach (var image in Images)
        {
            if (image != source)
            {
                ApplySyncCrop(source, image, _aspectRatio);
            }
        }
    }

    /// <summary>
    /// Kaynak kırpmayı hedef görsele uyarlar:
    /// - Serbest kırpmada kaynağın normalize çerçevesi (konum + boyut oranı) birebir kopyalanır;
    ///   her görselde aynı göreli bölge seçilir.
    /// - En-boy oranı kilitliyken kırpmanın PİKSEL oranı korunur: alan, kaynağın oransal merkez
    ///   konumunda ve hedef görsele sığacak şekilde yeniden ölçeklenir; görünürdeki şekil bozulmaz.
    /// </summary>
    private static void ApplySyncCrop(ImageItem source, ImageItem target, double? ratio)
    {
        if (source.CropNormWidth <= 0 || source.CropNormHeight <= 0)
        {
            target.ClearCrop();
            return;
        }

        if (ratio.HasValue && ratio.Value > 0)
        {
            ApplySyncRatio(source, target, ratio.Value);
        }
        else
        {
            target.CropNormX = source.CropNormX;
            target.CropNormY = source.CropNormY;
            target.CropNormWidth = source.CropNormWidth;
            target.CropNormHeight = source.CropNormHeight;
        }
    }

    /// <summary>
    /// En-boy oranı kilitli senkron: hedef görselde aynı piksel oranına sahip, kaynağın oransal
    /// merkez konumunda, hedef sınırları içinde kalan kırpma alanını hesaplar.
    /// Farklı en-boy oranına sahip dosyalarda dahi kırpma şekli korunur.
    /// </summary>
    private static void ApplySyncRatio(ImageItem source, ImageItem target, double ratio)
    {
        var tW = target.OriginalWidth;
        var tH = target.OriginalHeight;
        if (tW <= 0 || tH <= 0)
        {
            target.CopyCropFrom(source);
            return;
        }

        var centerXFrac = source.CropNormX + source.CropNormWidth / 2;
        var centerYFrac = source.CropNormY + source.CropNormHeight / 2;

        // Kaynakla aynı genişlik oranıyla başla, rati'yi pikselde koru.
        var cw = source.CropNormWidth * tW;
        var ch = cw / ratio;
        if (ch > tH)
        {
            ch = tH;
            cw = ch * ratio;
        }
        if (cw > tW)
        {
            cw = tW;
            ch = cw / ratio;
        }
        if (cw < 2) cw = 2;
        if (ch < 2) ch = 2;

        var nw = cw / tW;
        var nh = ch / tH;
        var nx = Math.Max(0, Math.Min(centerXFrac - nw / 2, 1 - nw));
        var ny = Math.Max(0, Math.Min(centerYFrac - nh / 2, 1 - nh));

        target.CropNormX = nx;
        target.CropNormY = ny;
        target.CropNormWidth = nw;
        target.CropNormHeight = nh;
    }

    private void SetActiveImage(ImageItem item)
    {
        if (_previouslyActiveImage != null && _previouslyActiveImage != item)
        {
            _previouslyActiveImage.BorderColor = InactiveBorderBrush;
            _previouslyActiveImage.BorderThickness = InactiveBorderThickness;
            _previouslyActiveImage.UpdateAllProperties();
        }

        if (item != null)
        {
            item.BorderColor = ActiveBorderBrush;
            item.BorderThickness = ActiveBorderThickness;
            item.UpdateAllProperties();
        }

        _activeImage = item;
        _previouslyActiveImage = item;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && _activeImage != null)
        {
            Images.Remove(_activeImage);
            SetActiveImage(null);
            UpdateUI();
            e.Handled = true;
        }
    }

    #region Dil Yönetimi

    private void TurkishLanguage_Checked(object sender, RoutedEventArgs e)
    {
        _isTurkish = true;
        UpdateLanguage();
    }

    private void EnglishLanguage_Checked(object sender, RoutedEventArgs e)
    {
        _isTurkish = false;
        UpdateLanguage();
    }

    private void UpdateLanguage()
    {
        if (ExportButton != null)
            ExportButton.Content = _isTurkish ? "Seçili Alanları Kırp ve Dışarı Aktar" : "Crop and Export Selected Areas";

        if (ClearButton != null)
            ClearButton.Content = _isTurkish ? "Fotoğrafları Kaldır" : "Remove Photos";

        if (ClearCropButton != null)
            ClearCropButton.Content = _isTurkish ? "Seçimi Temizle" : "Clear Selection";

        if (DeleteOriginalsCheckBox != null)
            DeleteOriginalsCheckBox.Content = _isTurkish ? "Orijinali Sil" : "Delete Originals";

        if (OptionsButton != null)
            OptionsButton.Content = _isTurkish ? "Ayarlar" : "Settings";

        if (PreserveMetadataCheckBox != null)
            PreserveMetadataCheckBox.Content = _isTurkish ? "Meta Veriyi Koru" : "Preserve Metadata";

        if (SyncCheckBox != null)
            SyncCheckBox.Content = _isTurkish ? "Senkronize Et" : "Sync";

        if (LanguageHeadingText != null)
            LanguageHeadingText.Text = _isTurkish ? "Dil: " : "Language: ";

        if (RatioHeadingText != null)
            RatioHeadingText.Text = _isTurkish ? "Oran: " : "Ratio: ";

        if (PreviewHeadingText != null)
            PreviewHeadingText.Text = _isTurkish ? "Önizleme: " : "Preview: ";

        if (PreviewSizeLabel != null)
            PreviewSizeLabel.Text = $"{_previewSizePercent}%";

        if (AspectRatioCombo != null && AspectRatioCombo.Items.Count > 0)
            ((ComboBoxItem)AspectRatioCombo.Items[0]).Content = _isTurkish ? "Serbest" : "Free";

        if (AboutButton != null)
            AboutButton.Content = _isTurkish ? "Hakkında" : "About";

        if (CancelExportButton != null)
            CancelExportButton.Content = _isTurkish ? "İptal" : "Cancel";

        if (DropZoneText != null)
            DropZoneText.Text = _isTurkish ? "📁 Resimleri buraya sürükleyin" : "📁 Drag and drop images here";

        if (StatusText != null && !_isDragging && _currentImage == null)
        {
            StatusText.Text = Images != null && Images.Count > 0
                ? (_isTurkish ? $"{Images.Count} resim yüklendi" : $"{Images.Count} images loaded")
                : (_isTurkish ? "Resimleri sürükleyip buraya bırakın" : "Drag and drop images here");
        }

        if (_detailWindow != null)
        {
            _detailWindow.IsTurkish = _isTurkish;
        }

        Title = _isTurkish ? "Toplu Fotoğraf Kırpıcı" : "Batch Image Cropper";
    }

    private void Sync_Checked(object sender, RoutedEventArgs e)
    {
        _syncEnabled = true;
        Logger.Information("Kırpma senkronizasyonu aktif");
    }

    private void Sync_Unchecked(object sender, RoutedEventArgs e)
    {
        _syncEnabled = false;
        Logger.Information("Kırpma senkronizasyonu pasif");
    }

    private void PreserveMetadata_Checked(object sender, RoutedEventArgs e)
    {
        if (Options != null) Options.PreserveMetadata = true;
    }

    private void PreserveMetadata_Unchecked(object sender, RoutedEventArgs e)
    {
        if (Options != null) Options.PreserveMetadata = false;
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        string title = _isTurkish ? "Hakkında" : "About";
        string message = "by xpow3r 2026\nv: 1.4.9";
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    private void UpdateUI()
    {
        if (Dispatcher.CheckAccess())
        {
            UpdateUIInternal();
        }
        else
        {
            Dispatcher.Invoke(UpdateUIInternal);
        }
    }

    #endregion

    private void UpdateUIInternal()
    {
        if (DropZone != null && ImageScrollViewer != null && StatusText != null)
        {
            if (Images.Count > 0)
            {
                DropZone.Visibility = Visibility.Collapsed;
                ImageScrollViewer.Visibility = Visibility.Visible;
                StatusText.Text = _isTurkish ? $"{Images.Count} resim yüklendi" : $"{Images.Count} images loaded";
            }
            else
            {
                DropZone.Visibility = Visibility.Visible;
                ImageScrollViewer.Visibility = Visibility.Collapsed;
                StatusText.Text = _isTurkish ? "Resimleri sürükleyip buraya bırakın" : "Drag and drop images here";
            }
        }
    }

    private void ShowWarning(string message)
    {
        Dispatcher.BeginInvoke(() =>
            MessageBox.Show(message, _isTurkish ? "Uyarı" : "Warning", MessageBoxButton.OK, MessageBoxImage.Warning));
    }

    private void ShowInfo(string message)
    {
        Dispatcher.BeginInvoke(() =>
            MessageBox.Show(message, _isTurkish ? "Bilgi" : "Info", MessageBoxButton.OK, MessageBoxImage.Information));
    }

    private void ShowError(string message)
    {
        Dispatcher.BeginInvoke(() =>
            MessageBox.Show(message, _isTurkish ? "Hata" : "Error", MessageBoxButton.OK, MessageBoxImage.Error));
    }
}