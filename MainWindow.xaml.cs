using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BatchImageCropper;

public partial class MainWindow : Window
{
    private ObservableCollection<ImageItem> Images { get; set; }
    private ImageItem _currentImage;
    private ImageItem _activeImage;
    private System.Windows.Point _startPoint;
    private System.Windows.Point _cropStartPoint;
    private bool _isDragging;
    private bool _isMovingCrop = false;
    private bool _isTurkish = true;
    private bool _syncEnabled = false;

    public MainWindow()
    {
        try
        {
            Logger.Information("Uygulama başlatılıyor...");
            InitializeComponent();
            Images = new ObservableCollection<ImageItem>();
            ImageGrid.ItemsSource = Images;
            
            // UI elemanları oluşturulduktan sonra dil ayarlarını yap
            if (Dispatcher != null)
            {
                Dispatcher.BeginInvoke(() => UpdateLanguage());
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
                Logger.Debug("Sürükleme algılandı");
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
            Logger.Information("Bırakma olayı tetiklendi");
            
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Logger.Information("Bırakılan dosyalar: {Count}", files?.Length ?? 0);
                
                if (files != null && files.Length > 0)
                {
                    ResimleriYukle(files);
                }
            }
            else
            {
                Logger.Warning("Bırakma olayında dosya verisi yok");
            }

            // Bırakma alanı görünümünü sıfırla
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

    private async void ResimleriYukle(string[] filePaths)
    {
        try
        {
            Logger.Information("{Count} resim yüklenmeye başlanıyor", filePaths?.Length ?? 0);
            
            if (filePaths == null)
            {
                Logger.Warning("Dosya yolları dizisi boş");
                return;
            }

            var supportedFormats = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
            var imageFiles = filePaths.Where(file => 
            {
                try
                {
                    var ext = Path.GetExtension(file)?.ToLower();
                    var isSupported = !string.IsNullOrEmpty(ext) && supportedFormats.Contains(ext);
                    if (!isSupported)
                    {
                        Logger.Warning("Desteklenmeyen dosya formatı: {File}", file);
                    }
                    return isSupported && File.Exists(file);
                }
                catch (Exception ex)
                {
                    Logger.Warning("Dosya kontrolünde hata: {File} - {Error}", file, ex.Message);
                    return false;
                }
            }).ToArray();

            Logger.Information("{Count} geçerli resim dosyası bulundu", imageFiles.Length);
            
            // Update status to show loading progress
            var totalFiles = imageFiles.Length;
            var processedCount = 0;

            foreach (var filePath in imageFiles)
            {
                try
                {
                    processedCount++;
                    
                    // Status güncellemesini daha az sık yap (performans için)
                    if (processedCount % 10 == 0 || processedCount == 1 || processedCount == totalFiles)
                    {
                        await Dispatcher.InvokeAsync(() => 
                        {
                            StatusText.Text = $"{processedCount}/{totalFiles} resim yükleniyor...";
                        });
                    }
                    
                    Logger.Debug("Resim yükleniyor: {File}", filePath);
                    
                    // Load image asynchronously
                    var sliderValue = ThumbnailSizeSlider != null ? (int)ThumbnailSizeSlider.Value : 25;
                    var imageItem = await Task.Run(() =>
                    {
                        var item = new ImageItem { Path = filePath };
                        
                        // Load full image - WPF will handle sizing
                        var image = ImageProcessor.LoadImage(
                            filePath, 
                            out double originalWidth, 
                            out double originalHeight);

                        item.Thumbnail = image;
                        item.ThumbnailScale = sliderValue;
                        
                        return item;
                    });

                    await Dispatcher.InvokeAsync(() =>
                    {
                        Images.Add(imageItem);
                        
                        // Her resim eklendiğinde UI'ı zorla güncelle
                    });
                    
                    Logger.Debug("Resim başarıyla yüklendi: {File}", Path.GetFileName(filePath));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Resim yüklenemedi: {File}", filePath);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"{Path.GetFileName(filePath)} yüklenirken hata: {ex.Message}", 
                                      "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
            }

            await Dispatcher.InvokeAsync(() =>
            {
                UpdateUI();
            });
            
            Logger.Information("Resim yükleme tamamlandı. Toplam resim: {Count}", Images.Count);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Resim yüklemede kritik hata");
            await Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show("Resimler yüklenirken hata oluştu. Logları kontrol edin.", 
                              "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            });
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

        _isDragging = true;
        _startPoint = e.GetPosition(canvas);

        // Mevcut kırpma alanının içinde mi diye kontrol et
        if (_currentImage.CropWidth > 0 && _currentImage.CropHeight > 0)
        {
            var cropRect = new System.Windows.Rect(
                _currentImage.CropX, 
                _currentImage.CropY, 
                _currentImage.CropWidth, 
                _currentImage.CropHeight);
            
            if (cropRect.Contains(_startPoint))
            {
                // Kırpma alanını taşıma modu
                _isMovingCrop = true;
                _cropStartPoint = new System.Windows.Point(_currentImage.CropX, _currentImage.CropY);
                Logger.Debug("Kırpma alanı taşıma modu başlatıldı");
            }
            else
            {
                // Yeni kırpma alanı oluştur
                _isMovingCrop = false;
                _currentImage.CropX = _startPoint.X;
                _currentImage.CropY = _startPoint.Y;
                _currentImage.CropWidth = 0;
                _currentImage.CropHeight = 0;
            }
        }
        else
        {
            // Yeni kırpma alanı oluştur
            _isMovingCrop = false;
            _currentImage.CropX = _startPoint.X;
            _currentImage.CropY = _startPoint.Y;
            _currentImage.CropWidth = 0;
            _currentImage.CropHeight = 0;
        }
        
        _currentImage.UpdateAllProperties();
        canvas.CaptureMouse();
    }

    private void Crop_Move(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _currentImage == null) return;

        var canvas = sender as Canvas;
        if (canvas == null) return;

        var currentPoint = e.GetPosition(canvas);

        // Aktif resmi güncelle
        _activeImage = _currentImage;

        // Kırpma alanını taşıma modu
        if (_isMovingCrop)
        {
            // Kırpma alanını taşı
            var deltaX = currentPoint.X - _startPoint.X;
            var deltaY = currentPoint.Y - _startPoint.Y;
            
            var newX = _cropStartPoint.X + deltaX;
            var newY = _cropStartPoint.Y + deltaY;
            
            // Görüntü alanına göre sınırla
            var maxDisplayWidth = _currentImage.DisplayWidth > 0 ? _currentImage.DisplayWidth : _currentImage.ThumbnailWidth;
            var maxDisplayHeight = _currentImage.DisplayHeight > 0 ? _currentImage.DisplayHeight : _currentImage.ThumbnailHeight;
            
            newX = Math.Max(0, Math.Min(newX, maxDisplayWidth - _currentImage.CropWidth));
            newY = Math.Max(0, Math.Min(newY, maxDisplayHeight - _currentImage.CropHeight));
            
            _currentImage.CropX = newX;
            _currentImage.CropY = newY;
        }
        else
        {
            // YENİ SEÇİM ALANI OLUŞTURMA - BAŞTAN YAZILDI
            var maxDisplayWidth = _currentImage.DisplayWidth > 0 ? _currentImage.DisplayWidth : _currentImage.ThumbnailWidth;
            var maxDisplayHeight = _currentImage.DisplayHeight > 0 ? _currentImage.DisplayHeight : _currentImage.ThumbnailHeight;
            
            // Dikdörtgenin sol üst köşesi ve boyutlarını hesapla
            var rectX = Math.Min(_startPoint.X, currentPoint.X);
            var rectY = Math.Min(_startPoint.Y, currentPoint.Y);
            var rectWidth = Math.Abs(currentPoint.X - _startPoint.X);
            var rectHeight = Math.Abs(currentPoint.Y - _startPoint.Y);
            
            // Sınırları kontrol et
            rectX = Math.Max(0, Math.Min(rectX, maxDisplayWidth));
            rectY = Math.Max(0, Math.Min(rectY, maxDisplayHeight));
            rectWidth = Math.Max(0, Math.Min(rectWidth, maxDisplayWidth - rectX));
            rectHeight = Math.Max(0, Math.Min(rectHeight, maxDisplayHeight - rectY));
            
            // Koordinatları ayarla
            _currentImage.CropX = rectX;
            _currentImage.CropY = rectY;
            _currentImage.CropWidth = rectWidth;
            _currentImage.CropHeight = rectHeight;
            
            // Boyutları her zaman göster (sıfırdan büyükse)
            if (rectWidth > 0 && rectHeight > 0)
            {
                Dispatcher.BeginInvoke(() => {
                    if (StatusText != null)
                        StatusText.Text = $"Seçim: {rectWidth:0} x {rectHeight:0} px";
                });
            }
        }
        
        _currentImage.UpdateAllProperties();

        // Senkronizasyon aktifse diğer resimlere de uygula
        if (_syncEnabled)
        {
            SyncCropToAllImages(_currentImage);
        }
        UpdateAllBorders();
    }

    private void Crop_End(object sender, MouseButtonEventArgs e)
    {
        var canvas = sender as Canvas;
        if (canvas != null)
        {
            canvas.ReleaseMouseCapture();
        }

        _isDragging = false;
        _isMovingCrop = false;
        _currentImage = null;
        _activeImage = null;
        
        // Çerçeveleri güncelle (aktif resmi sıfırla)
        UpdateAllBorders();
        
        // Status'u normale döndür
        Dispatcher.BeginInvoke(() => {
            UpdateUI();
        });
    }

    #endregion

    #region Buton Olayları

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (Images.Count == 0)
        {
            MessageBox.Show("Dışa aktarılacak resim yok!", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var exportedCount = 0;
        var errors = new List<string>();
        var preserveMetadata = PreserveMetadataCheckBox?.IsChecked == true;

        foreach (var imageItem in Images)
        {
            try
            {
                // Kırpma alanı seçilmediyse atla
                if (imageItem.CropWidth <= 0 || imageItem.CropHeight <= 0)
                {
                    continue;
                }

                var outputPath = Path.Combine(
                    Path.GetDirectoryName(imageItem.Path),
                    Path.GetFileNameWithoutExtension(imageItem.Path) + "_kirpilmis.jpg"
                );

                ImageProcessor.CropImage(
                    imageItem.Path,
                    imageItem.CropX,
                    imageItem.CropY,
                    imageItem.CropWidth,
                    imageItem.CropHeight,
                    imageItem.DisplayWidth > 0 ? imageItem.DisplayWidth : imageItem.ThumbnailWidth,
                    imageItem.DisplayHeight > 0 ? imageItem.DisplayHeight : imageItem.ThumbnailHeight,
                    imageItem.OriginalWidth,
                    imageItem.OriginalHeight,
                    outputPath,
                    preserveMetadata
                );

                exportedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(imageItem.Path)}: {ex.Message}");
            }
        }

        // Sonuçları göster
        var message = exportedCount > 0 
            ? $"{exportedCount} resim başarıyla dışa aktarıldı!" 
            : "Hiç resim dışa aktarılmadı (kırpma alanı seçilmedi).";

        if (errors.Any())
        {
            message += $"\n\nHatalar:\n{string.Join("\n", errors)}";
        }

        MessageBox.Show(message, "Dışa Aktarım Tamamlandı", MessageBoxButton.OK, 
                       exportedCount > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        // Tüm resimleri temizle
        Images.Clear();
        
        // UI'ı tamamen sıfırla ve zorla güncelle
        Dispatcher.BeginInvoke(() => {
            UpdateUI();
            
            // Thumbnail slider'ı da sıfırla (varsayılan değere)
            if (ThumbnailSizeSlider != null)
            {
                ThumbnailSizeSlider.Value = 25;
            }
            
            // Grid'i yenile
            if (ImageGrid != null)
            {
                ImageGrid.Items.Refresh();
            }
        });
        
        Logger.Information("Tüm resimler temizlendi");
    }

    private void ClearCrop_Click(object sender, RoutedEventArgs e)
    {
        // Tüm resimlerin kırpma alanlarını temizle
        foreach (var image in Images)
        {
            image.CropX = 0;
            image.CropY = 0;
            image.CropWidth = 0;
            image.CropHeight = 0;
            image.UpdateAllProperties();
        }
        
        Logger.Information("Tüm kırpma seçimleri temizlendi");
    }

    #endregion

    #region Arayüz Güncellemeleri

    private void SyncCropToAllImages(ImageItem sourceImage)
    {
        // Kaynak resmin kırpma alanını diğer tüm resimlere uygula
        foreach (var image in Images)
        {
            if (image != sourceImage)
            {
                image.CropX = sourceImage.CropX;
                image.CropY = sourceImage.CropY;
                image.CropWidth = sourceImage.CropWidth;
                image.CropHeight = sourceImage.CropHeight;
                image.UpdateAllProperties();
            }
        }
    }

    private void UpdateAllBorders()
    {
        // Tüm resimlerin çerçevesini güncelle
        foreach (var image in Images)
        {
            if (image == _activeImage)
            {
                // Aktif resim: kırmızı çerçeve
                image.BorderColor = new SolidColorBrush(Colors.Red);
                image.BorderThickness = new System.Windows.Thickness(3);
            }
            else
            {
                // Pasif resimler: gri çerçeve
                image.BorderColor = new SolidColorBrush(Colors.Gray);
                image.BorderThickness = new System.Windows.Thickness(1);
            }
            image.UpdateAllProperties();
        }
    }

    private void ThumbnailSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Skip if application is still initializing
        if (!IsInitialized) return;
        
        var newScale = (int)e.NewValue;
        var oldScale = (int)e.OldValue;
        
        if (ThumbnailSizeLabel != null)
        {
            ThumbnailSizeLabel.Text = $"{newScale}%";
        }
        
        // Update all loaded images' scale and crop areas
        if (Images != null)
        {
            foreach (var image in Images)
            {
                // Mevcut kırpma alanını yeni ölçeğe göre orantıla
                if (image.CropWidth > 0 && image.CropHeight > 0 && oldScale > 0)
                {
                    var scaleRatio = (double)newScale / oldScale;
                    
                    image.CropX = image.CropX * scaleRatio;
                    image.CropY = image.CropY * scaleRatio;
                    image.CropWidth = image.CropWidth * scaleRatio;
                    image.CropHeight = image.CropHeight * scaleRatio;
                }
                
                image.ThumbnailScale = newScale;
                image.UpdateAllProperties();
            }
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
        
        if (PreserveMetadataCheckBox != null)
            PreserveMetadataCheckBox.Content = _isTurkish ? "Meta Veriyi Koru" : "Preserve Metadata";
        
        if (SyncCheckBox != null)
            SyncCheckBox.Content = _isTurkish ? "Senkronize Et" : "Sync";
        
        if (AboutButton != null)
            AboutButton.Content = _isTurkish ? "Hakkında" : "About";
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

    private void About_Click(object sender, RoutedEventArgs e)
    {
        string title = _isTurkish ? "Hakkında" : "About";
        string message = "by xpow3r 2026\nv: 1.0.0";
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

    #endregion
}