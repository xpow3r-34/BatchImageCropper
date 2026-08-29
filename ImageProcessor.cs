using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace BatchImageCropper
{
    public static class ImageProcessor
    {
        public static BitmapImage LoadImage(string imagePath, out double originalWidth, out double originalHeight)
        {
            try
            {
                using var originalImage = Image.FromFile(imagePath);
                originalWidth = originalImage.Width;
                originalHeight = originalImage.Height;

                using var memory = new MemoryStream();
                originalImage.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
            catch (Exception ex)
            {
                originalWidth = 0;
                originalHeight = 0;
                throw new Exception($"Resim yüklenemedi: {imagePath} - {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Kırpma koordinatları normalize (0..1) olarak verilir; orijinal piksele çevrilir.
        /// </summary>
        public static void CropImage(string imagePath, double cropNormX, double cropNormY,
            double cropNormW, double cropNormH, string outputPath, ImageFormat format,
            int quality, bool preserveMetadata = true)
        {
            try
            {
                using var originalImage = Image.FromFile(imagePath);

                if (originalImage.Width <= 0 || originalImage.Height <= 0)
                {
                    throw new InvalidOperationException("Kaynak görüntü boyutları geçersiz");
                }

                int originalX = (int)Math.Round(Math.Max(0, Math.Min(1, cropNormX)) * originalImage.Width);
                int originalY = (int)Math.Round(Math.Max(0, Math.Min(1, cropNormY)) * originalImage.Height);
                int originalCropWidth = (int)Math.Round(Math.Max(0, Math.Min(1, cropNormW)) * originalImage.Width);
                int originalCropHeight = (int)Math.Round(Math.Max(0, Math.Min(1, cropNormH)) * originalImage.Height);

                originalX = Math.Max(0, Math.Min(originalX, originalImage.Width - 1));
                originalY = Math.Max(0, Math.Min(originalY, originalImage.Height - 1));
                originalCropWidth = Math.Max(1, Math.Min(originalCropWidth, originalImage.Width - originalX));
                originalCropHeight = Math.Max(1, Math.Min(originalCropHeight, originalImage.Height - originalY));

                var cropRect = new Rectangle(originalX, originalY, originalCropWidth, originalCropHeight);

                var croppedImage = new Bitmap(cropRect.Width, cropRect.Height);
                using (var graphics = Graphics.FromImage(croppedImage))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    graphics.DrawImage(originalImage, 0, 0, cropRect, GraphicsUnit.Pixel);
                }

                SaveImage(croppedImage, outputPath, format, quality);

                if (preserveMetadata)
                {
                    CopyFileTimestamps(imagePath, outputPath);
                }

                croppedImage.Dispose();
            }
            catch (Exception ex)
            {
                throw new Exception($"Resim kırpılamadı: {imagePath} - {ex.Message}", ex);
            }
        }

        private static void SaveImage(Bitmap image, string outputPath, ImageFormat format, int quality)
        {
            if (format == ImageFormat.Jpeg)
            {
                var jpegEncoder = GetJpegEncoder();
                var encoderParams = new EncoderParameters(1);
                var q = Math.Max(0, Math.Min(100, quality));
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)q);
                image.Save(outputPath, jpegEncoder, encoderParams);
            }
            else
            {
                image.Save(outputPath, format);
            }
        }

        public static ImageFormat ResolveFormat(string formatName, string sourceExtension)
        {
            var key = string.Equals(formatName, "source", StringComparison.OrdinalIgnoreCase)
                ? sourceExtension?.TrimStart('.')
                : formatName;

            return (key ?? "jpg").ToLowerInvariant() switch
            {
                "jpg" or "jpeg" => ImageFormat.Jpeg,
                "png" => ImageFormat.Png,
                "bmp" => ImageFormat.Bmp,
                "gif" => ImageFormat.Gif,
                _ => ImageFormat.Jpeg
            };
        }

        public static string GetOutputExtension(string formatName, string sourceExtension)
        {
            if (string.Equals(formatName, "source", StringComparison.OrdinalIgnoreCase))
            {
                var ext = string.IsNullOrWhiteSpace(sourceExtension) ? ".jpg" : sourceExtension;
                return ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : ext;
            }

            return "." + (formatName ?? "jpg").TrimStart('.').ToLowerInvariant();
        }

        private static void CopyFileTimestamps(string sourcePath, string destPath)
        {
            try
            {
                var sourceInfo = new FileInfo(sourcePath);
                var destInfo = new FileInfo(destPath);

                destInfo.CreationTime = sourceInfo.CreationTime;
                destInfo.LastWriteTime = sourceInfo.LastWriteTime;
                destInfo.LastAccessTime = sourceInfo.LastAccessTime;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Dosya zaman damgaları kopyalanamadı: {ex.Message}");
            }
        }

        private static ImageCodecInfo GetJpegEncoder()
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            var jpegCodec = codecs.FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

            if (jpegCodec == null)
            {
                throw new NotSupportedException("JPEG encoder bulunamadı");
            }

            return jpegCodec;
        }
    }
}