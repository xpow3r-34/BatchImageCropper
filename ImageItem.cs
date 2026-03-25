using System.ComponentModel;
using System;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;

namespace BatchImageCropper
{
    public class ImageItem : INotifyPropertyChanged
    {
        private string _path;
        private BitmapImage _thumbnail;
        private double _cropX;
        private double _cropY;
        private double _cropWidth;
        private double _cropHeight;
        private double _thumbnailScale = 25; // Default 25%
        private double _originalWidth;
        private double _originalHeight;
        private double _scaleX;
        private double _scaleY;
        private bool _preserveMetadata = true;
        private DateTime _creationTime;
        private DateTime _lastModified;
        private long _fileSize;
        private double _thumbnailWidth;
        private double _thumbnailHeight;
        private Brush _borderColor = new SolidColorBrush(Colors.Gray);
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
                }
                OnPropertyChanged(nameof(Thumbnail));
                OnPropertyChanged(nameof(DisplayWidth));
                OnPropertyChanged(nameof(DisplayHeight));
            }
        }

        public double ThumbnailScale
        {
            get => _thumbnailScale;
            set
            {
                _thumbnailScale = Math.Max(1, Math.Min(100, value));
                OnPropertyChanged(nameof(ThumbnailScale));
                OnPropertyChanged(nameof(DisplayWidth));
                OnPropertyChanged(nameof(DisplayHeight));
            }
        }

        public double DisplayWidth => _originalWidth * _thumbnailScale / 100.0;
        public double DisplayHeight => _originalHeight * _thumbnailScale / 100.0;
        public double OriginalWidth => _originalWidth;
        public double OriginalHeight => _originalHeight;

        public double CropX
        {
            get => _cropX;
            set
            {
                _cropX = value;
                OnPropertyChanged(nameof(CropX));
            }
        }

        public double CropY
        {
            get => _cropY;
            set
            {
                _cropY = value;
                OnPropertyChanged(nameof(CropY));
            }
        }

        public double CropWidth
        {
            get => _cropWidth;
            set
            {
                _cropWidth = value;
                OnPropertyChanged(nameof(CropWidth));
            }
        }

        public double CropHeight
        {
            get => _cropHeight;
            set
            {
                _cropHeight = value;
                OnPropertyChanged(nameof(CropHeight));
            }
        }

        public double ThumbnailWidth
        {
            get => _thumbnailWidth;
            set
            {
                _thumbnailWidth = value;
                OnPropertyChanged(nameof(ThumbnailWidth));
            }
        }

        public double ThumbnailHeight
        {
            get => _thumbnailHeight;
            set
            {
                _thumbnailHeight = value;
                OnPropertyChanged(nameof(ThumbnailHeight));
            }
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

        // DisplayWidth and DisplayHeight are calculated from ThumbnailScale

        public double ScaleX
        {
            get => _scaleX;
            set
            {
                _scaleX = value;
                OnPropertyChanged(nameof(ScaleX));
            }
        }

        public double ScaleY
        {
            get => _scaleY;
            set
            {
                _scaleY = value;
                OnPropertyChanged(nameof(ScaleY));
            }
        }

        public bool PreserveMetadata
        {
            get => _preserveMetadata;
            set
            {
                _preserveMetadata = value;
                OnPropertyChanged(nameof(PreserveMetadata));
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
        public string FileExtension => System.IO.Path.GetExtension(_path).ToLower();
        public string FormattedFileSize => FormatFileSize(_fileSize);
        public string FormattedCreationTime => _creationTime.ToString("dd.MM.yyyy HH:mm");
        public string FormattedLastModified => _lastModified.ToString("dd.MM.yyyy HH:mm");

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
                // If metadata loading fails, use defaults
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

        public void UpdateAllProperties()
        {
            OnPropertyChanged(nameof(CropX));
            OnPropertyChanged(nameof(CropY));
            OnPropertyChanged(nameof(CropWidth));
            OnPropertyChanged(nameof(CropHeight));
        }
    }
}
