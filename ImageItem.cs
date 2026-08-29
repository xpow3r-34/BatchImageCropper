using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BatchImageCropper
{
    public class ImageItem : INotifyPropertyChanged
    {
        private const double MaxDisplayHeight = 640;

        private string _path;
        private BitmapImage _thumbnail;
        private double _cropNormX;
        private double _cropNormY;
        private double _cropNormW;
        private double _cropNormH;
        private double _displayWidth = 300;
        private double _displayHeight = 225;
        private double _originalWidth;
        private double _originalHeight;
        private DateTime _creationTime;
        private DateTime _lastModified;
        private long _fileSize;
        private Brush _borderColor = Brushes.Gray;
        private System.Windows.Thickness _borderThickness = new System.Windows.Thickness(1);

        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                LoadFileMetadata();
                OnPropertyChanged(nameof(Path));
            }
        }

        public BitmapImage Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                if (_thumbnail != null)
                {
                    _originalWidth = _thumbnail.PixelWidth;
                    _originalHeight = _thumbnail.PixelHeight;
                    FitToWidth(_displayWidth);
                }
                OnPropertyChanged(nameof(Thumbnail));
            }
        }

        public double OriginalWidth => _originalWidth;
        public double OriginalHeight => _originalHeight;

        public double DisplayWidth => _displayWidth;
        public double DisplayHeight => _displayHeight;

        /// <summary>
        /// Verilen genişliğe en-boy oranını koruyarak sığdırır.
        /// Aşırı uzun görseller maksimum yüksekliği aşacaksa genişliği buna göre daraltır.
        /// Kırpma koordinatları normalize saklandığı için boyut değişse de korunur.
        /// </summary>
        public void FitToWidth(double width)
        {
            if (width <= 0) return;

            _displayWidth = width;

            if (_originalWidth > 0 && _originalHeight > 0)
            {
                var height = _displayWidth * _originalHeight / _originalWidth;
                if (height > MaxDisplayHeight)
                {
                    height = MaxDisplayHeight;
                    _displayWidth = height * _originalWidth / _originalHeight;
                }
                _displayHeight = height;
            }

            NotifySizeChanged();
        }

        /// <summary>
        /// Görüntüyü hem genişliğe hem yüksekliğe sığacak şekilde ölçeklendirir (detay görünümü için).
        /// </summary>
        public void FitToViewport(double availWidth, double availHeight)
        {
            if (availWidth <= 0 || availHeight <= 0) return;
            if (_originalWidth <= 0 || _originalHeight <= 0) return;

            var w = availWidth;
            var h = w * _originalHeight / _originalWidth;
            if (h > availHeight)
            {
                h = availHeight;
                w = h * _originalWidth / _originalHeight;
            }

            if (w < 4) w = 4;
            if (h < 4) h = 4;

            _displayWidth = w;
            _displayHeight = h;
            NotifySizeChanged();
        }

        /// <summary>
        /// Önizleme boyutu: orijinal boyutun verilen yüzdesi (varsayılan %25).
        /// Tek tek görsellerin küçük kalmasını önlemek için en az bir kolon genişliğinde
        /// ve en fazla görünüm genişliğine sığacak şekilde sınırlanır.
        /// </summary>
        public void FitToLargePreview(double minColumnWidth, double maxWidth, double percent = 25)
        {
            if (_originalWidth <= 0 || _originalHeight <= 0)
            {
                FitToWidth(minColumnWidth);
                return;
            }

            var fraction = percent / 100.0;
            var target = Math.Max(minColumnWidth, _originalWidth * fraction);
            target = Math.Max(80, Math.Min(target, maxWidth));
            FitToWidth(target);
        }

        private void NotifySizeChanged()
        {
            OnPropertyChanged(nameof(DisplayWidth));
            OnPropertyChanged(nameof(DisplayHeight));
            OnCropPropertiesChanged();
        }

        // Normalize (0..1) kırpma koordinatları. Görüntü boyutu değişse de geçerlidir.
        public double CropNormX
        {
            get => _cropNormX;
            set
            {
                _cropNormX = Clamp01(value);
                OnPropertyChanged(nameof(CropNormX));
                OnPropertyChanged(nameof(CropX));
            }
        }

        public double CropNormY
        {
            get => _cropNormY;
            set
            {
                _cropNormY = Clamp01(value);
                OnPropertyChanged(nameof(CropNormY));
                OnPropertyChanged(nameof(CropY));
            }
        }

        public double CropNormWidth
        {
            get => _cropNormW;
            set
            {
                _cropNormW = Clamp01(value);
                OnPropertyChanged(nameof(CropNormWidth));
                OnPropertyChanged(nameof(CropWidth));
            }
        }

        public double CropNormHeight
        {
            get => _cropNormH;
            set
            {
                _cropNormH = Clamp01(value);
                OnPropertyChanged(nameof(CropNormHeight));
                OnPropertyChanged(nameof(CropHeight));
            }
        }

        // Ekrandaki (görüntü alanındaki piksel) koordinatlar: normalize * display boyutu
        public double CropX
        {
            get => _cropNormX * _displayWidth;
            set
            {
                _cropNormX = _displayWidth > 0 ? Clamp01(value / _displayWidth) : 0;
                OnPropertyChanged(nameof(CropX));
                OnPropertyChanged(nameof(CropNormX));
            }
        }

        public double CropY
        {
            get => _cropNormY * _displayHeight;
            set
            {
                _cropNormY = _displayHeight > 0 ? Clamp01(value / _displayHeight) : 0;
                OnPropertyChanged(nameof(CropY));
                OnPropertyChanged(nameof(CropNormY));
            }
        }

        public double CropWidth
        {
            get => _cropNormW * _displayWidth;
            set
            {
                _cropNormW = _displayWidth > 0 ? Clamp01(value / _displayWidth) : 0;
                OnPropertyChanged(nameof(CropWidth));
                OnPropertyChanged(nameof(CropNormWidth));
            }
        }

        public double CropHeight
        {
            get => _cropNormH * _displayHeight;
            set
            {
                _cropNormH = _displayHeight > 0 ? Clamp01(value / _displayHeight) : 0;
                OnPropertyChanged(nameof(CropHeight));
                OnPropertyChanged(nameof(CropNormHeight));
            }
        }

        public void ClearCrop()
        {
            _cropNormX = 0;
            _cropNormY = 0;
            _cropNormW = 0;
            _cropNormH = 0;
            OnCropPropertiesChanged();
        }

        public void CopyCropFrom(ImageItem source)
        {
            _cropNormX = source._cropNormX;
            _cropNormY = source._cropNormY;
            _cropNormW = source._cropNormW;
            _cropNormH = source._cropNormH;
            OnCropPropertiesChanged();
        }

        public bool HasCrop => _cropNormW > 0 && _cropNormH > 0;

        private static double Clamp01(double value)
        {
            return Math.Max(0, Math.Min(1, value));
        }

        public Brush BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                OnPropertyChanged(nameof(BorderColor));
            }
        }

        public System.Windows.Thickness BorderThickness
        {
            get => _borderThickness;
            set
            {
                _borderThickness = value;
                OnPropertyChanged(nameof(BorderThickness));
            }
        }

        public DateTime CreationTime
        {
            get => _creationTime;
            set
            {
                _creationTime = value;
                OnPropertyChanged(nameof(CreationTime));
            }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set
            {
                _lastModified = value;
                OnPropertyChanged(nameof(LastModified));
            }
        }

        public long FileSize
        {
            get => _fileSize;
            set
            {
                _fileSize = value;
                OnPropertyChanged(nameof(FileSize));
            }
        }

        public string FileName => System.IO.Path.GetFileNameWithoutExtension(_path);
        public string FileExtension => System.IO.Path.GetExtension(_path).ToLowerInvariant();
        public string FormattedFileSize => FormatFileSize(_fileSize);
        public string FormattedCreationTime => _creationTime.ToString("dd.MM.yyyy HH:mm");

        private void LoadFileMetadata()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var fileInfo = new FileInfo(_path);
                    CreationTime = fileInfo.CreationTime;
                    LastModified = fileInfo.LastWriteTime;
                    FileSize = fileInfo.Length;
                }
            }
            catch (Exception)
            {
                CreationTime = DateTime.Now;
                LastModified = DateTime.Now;
                FileSize = 0;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCropPropertiesChanged()
        {
            OnPropertyChanged(nameof(CropX));
            OnPropertyChanged(nameof(CropY));
            OnPropertyChanged(nameof(CropWidth));
            OnPropertyChanged(nameof(CropHeight));
            OnPropertyChanged(nameof(CropNormX));
            OnPropertyChanged(nameof(CropNormY));
            OnPropertyChanged(nameof(CropNormWidth));
            OnPropertyChanged(nameof(CropNormHeight));
        }

        public void UpdateAllProperties()
        {
            OnPropertyChanged(nameof(DisplayWidth));
            OnPropertyChanged(nameof(DisplayHeight));
            OnCropPropertiesChanged();
        }
    }
}