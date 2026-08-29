using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BatchImageCropper;

public partial class DetailWindow : Window
{
    private const double MaxBaseWidth = 4096;

    private readonly ImageItem _item;
    private bool _isTurkish;
    private double _baseWidth;
    private double _baseHeight;
    private double _zoom = 1.0;
    private bool _isPanning;
    private bool _isDragging;
    private bool _isMovingCrop;
    private System.Windows.Point _startPoint;
    private System.Windows.Point _cropStartPoint;
    private System.Windows.Point _panStart;
    private double _panStartH;
    private double _panStartV;

    public Action<ImageItem> CropMoved;
    public Action<ImageItem> CropEnded;

    public DetailWindow(ImageItem item, double? aspectRatio, bool isTurkish)
    {
        InitializeComponent();
        _item = item;
        _isTurkish = isTurkish;
        AspectRatio = aspectRatio;
        DataContext = item;

        var sourceW = item.OriginalWidth > 0 ? item.OriginalWidth : item.DisplayWidth;
        var sourceH = item.OriginalHeight > 0 ? item.OriginalHeight : item.DisplayHeight;
        if (sourceW <= 0 || sourceH <= 0)
        {
            sourceW = 800;
            sourceH = 600;
        }

        var scale = sourceW > MaxBaseWidth ? MaxBaseWidth / sourceW : 1.0;
        _baseWidth = sourceW * scale;
        _baseHeight = sourceH * scale;

        DetailContent.Width = _baseWidth;
        DetailContent.Height = _baseHeight;
        DetailImage.Width = _baseWidth;
        DetailImage.Height = _baseHeight;
        DetailCanvas.Width = _baseWidth;
        DetailCanvas.Height = _baseHeight;

        DetailFileNameLabel.Text = item.FileName + item.FileExtension;
        UpdateCropRect();
        UpdateLanguage();

        _item.PropertyChanged += OnItemPropertyChanged;
        Closed += (_, _) => _item.PropertyChanged -= OnItemPropertyChanged;
        Loaded += (_, _) =>
        {
            DetailScrollViewer.ScrollToHome();
        };
    }

    public double? AspectRatio { get; set; }

    public bool IsTurkish
    {
        set
        {
            _isTurkish = value;
            UpdateLanguage();
        }
    }

    private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImageItem.CropX) || e.PropertyName == nameof(ImageItem.CropY) ||
            e.PropertyName == nameof(ImageItem.CropWidth) || e.PropertyName == nameof(ImageItem.CropHeight) ||
            e.PropertyName == nameof(ImageItem.CropNormX) || e.PropertyName == nameof(ImageItem.CropNormY) ||
            e.PropertyName == nameof(ImageItem.CropNormWidth) || e.PropertyName == nameof(ImageItem.CropNormHeight) ||
            e.PropertyName == nameof(ImageItem.Thumbnail))
        {
            UpdateCropRect();
        }
    }

    private void UpdateCropRect()
    {
        if (DetailCropRect == null || _item == null) return;

        DetailCropRect.SetValue(Canvas.LeftProperty, _item.CropNormX * _baseWidth);
        DetailCropRect.SetValue(Canvas.TopProperty, _item.CropNormY * _baseHeight);
        DetailCropRect.Width = _item.CropNormWidth * _baseWidth;
        DetailCropRect.Height = _item.CropNormHeight * _baseHeight;
    }

    private System.Windows.Rect CropRectInBase
    {
        get
        {
            if (_item == null) return System.Windows.Rect.Empty;
            return new System.Windows.Rect(
                _item.CropNormX * _baseWidth,
                _item.CropNormY * _baseHeight,
                _item.CropNormWidth * _baseWidth,
                _item.CropNormHeight * _baseHeight);
        }
    }

    private void UpdateLanguage()
    {
        if (DetailBackButton == null) return;

        DetailBackButton.Content = _isTurkish ? "← Geri" : "← Back";
        DetailFitButton.Content = _isTurkish ? "Sığdır" : "Fit";
        DetailHintLabel.Text = _isTurkish
            ? "Ctrl+Tekerlek: zoom • Sağ-sürükle: kaydır • Çift tık: sıfırla • Sol-sürükle: kırp"
            : "Ctrl+Wheel: zoom • Right-drag: pan • Double-click: reset • Left-drag: crop";
        Title = _isTurkish ? "Detay Görünümü" : "Detail View";
    }

    private void Detail_Back_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Detail_Fit_Click(object sender, RoutedEventArgs e)
    {
        FitToViewport();
    }

    private void FitToViewport()
    {
        if (_baseWidth <= 0 || _baseHeight <= 0) return;

        var w = DetailScrollViewer.ViewportWidth;
        var h = DetailScrollViewer.ViewportHeight;
        if (w <= 0) w = DetailScrollViewer.ActualWidth;
        if (h <= 0) h = DetailScrollViewer.ActualHeight;
        if (w <= 0 || h <= 0) return;

        var fit = Math.Min(w / _baseWidth, h / _baseHeight);
        _zoom = Math.Max(0.05, Math.Min(8.0, fit));
        UpdateTransform();
        DetailScrollViewer.ScrollToHome();
    }

    private void UpdateTransform()
    {
        DetailScale.ScaleX = _zoom;
        DetailScale.ScaleY = _zoom;
        DetailZoomLabel.Text = $"{_zoom * 100:0}%";
    }

    private void Detail_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            var factor = e.Delta > 0 ? 1.1 : 1 / 1.1;
            var point = e.GetPosition(DetailContent);
            ZoomAt(_zoom * factor, point);
            e.Handled = true;
        }
    }

    private void ZoomAt(double targetZoom, System.Windows.Point contentPoint)
    {
        targetZoom = Math.Max(0.05, Math.Min(8.0, targetZoom));

        var sv = DetailScrollViewer;
        var currentZoom = Math.Max(0.05, _zoom);

        var scaledW = _baseWidth * currentZoom;
        var scaledH = _baseHeight * currentZoom;

        var absX = sv.HorizontalOffset + contentPoint.X * currentZoom;
        var absY = sv.VerticalOffset + contentPoint.Y * currentZoom;
        var relX = scaledW > 0 ? absX / scaledW : 0;
        var relY = scaledH > 0 ? absY / scaledH : 0;

        _zoom = targetZoom;
        UpdateTransform();

        var newAbsX = relX * _baseWidth * _zoom - contentPoint.X * _zoom;
        var newAbsY = relY * _baseHeight * _zoom - contentPoint.Y * _zoom;

        Dispatcher.BeginInvoke(() =>
        {
            sv.ScrollToHorizontalOffset(newAbsX);
            sv.ScrollToVerticalOffset(newAbsY);
        });
    }

    private void Detail_PanStart(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _panStart = e.GetPosition(this);
        _panStartH = DetailScrollViewer.HorizontalOffset;
        _panStartV = DetailScrollViewer.VerticalOffset;

        (sender as Canvas)?.CaptureMouse();
        e.Handled = true;
    }

    private void Detail_PanEnd(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning) return;

        _isPanning = false;
        (sender as Canvas)?.ReleaseMouseCapture();
    }

    private void Crop_Start(object sender, MouseButtonEventArgs e)
    {
        var canvas = sender as Canvas;
        if (canvas == null || _item == null) return;

        if (e.ClickCount == 2)
        {
            _zoom = 1.0;
            UpdateTransform();
            DetailScrollViewer.ScrollToHome();
            _isDragging = false;
            _isMovingCrop = false;
            canvas.ReleaseMouseCapture();
            return;
        }

        _isDragging = true;
        _startPoint = e.GetPosition(canvas);

        var cropRect = CropRectInBase;
        if (cropRect != System.Windows.Rect.Empty && cropRect.Contains(_startPoint))
        {
            _isMovingCrop = true;
            _cropStartPoint = new System.Windows.Point(cropRect.X, cropRect.Y);
            canvas.CaptureMouse();
            return;
        }

        _isMovingCrop = false;
        SetCrop(_startPoint.X, _startPoint.Y, 0, 0);
        canvas.CaptureMouse();
    }

    private void Crop_Move(object sender, MouseEventArgs e)
    {
        var canvas = sender as Canvas;
        if (canvas == null || _item == null) return;

        if (_isPanning)
        {
            var p = e.GetPosition(this);
            DetailScrollViewer.ScrollToHorizontalOffset(_panStartH + (_panStart.X - p.X));
            DetailScrollViewer.ScrollToVerticalOffset(_panStartV + (_panStart.Y - p.Y));
            return;
        }

        if (!_isDragging) return;

        var currentPoint = e.GetPosition(canvas);
        var maxWidth = _baseWidth;
        var maxHeight = _baseHeight;

        if (_isMovingCrop)
        {
            var deltaX = currentPoint.X - _startPoint.X;
            var deltaY = currentPoint.Y - _startPoint.Y;

            var crop = CropRectInBase;
            var newX = Math.Max(0, Math.Min(_cropStartPoint.X + deltaX, maxWidth - crop.Width));
            var newY = Math.Max(0, Math.Min(_cropStartPoint.Y + deltaY, maxHeight - crop.Height));

            _item.CropNormX = Clamp01(newX / _baseWidth);
            _item.CropNormY = Clamp01(newY / _baseHeight);
        }
        else if (AspectRatio.HasValue)
        {
            ComputeRatioRect(_startPoint, currentPoint, AspectRatio.Value, maxWidth, maxHeight,
                out var x, out var y, out var w, out var h);
            SetCrop(x, y, w, h);
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

            SetCrop(rectX, rectY, rectWidth, rectHeight);
        }

        CropMoved?.Invoke(_item);
    }

    private void SetCrop(double x, double y, double w, double h)
    {
        _item.CropNormX = Clamp01(x / _baseWidth);
        _item.CropNormY = Clamp01(y / _baseHeight);
        _item.CropNormWidth = Clamp01(w / _baseWidth);
        _item.CropNormHeight = Clamp01(h / _baseHeight);
    }

    private void Crop_End(object sender, MouseButtonEventArgs e)
    {
        (sender as Canvas)?.ReleaseMouseCapture();
        _isDragging = false;
        _isMovingCrop = false;
        CropEnded?.Invoke(_item);
    }

    private static double Clamp01(double value)
    {
        return Math.Max(0, Math.Min(1, value));
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
}