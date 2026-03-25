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
        public static int ThumbnailPercentage { get; set; } = 25; // 1-100 percentage

        public static BitmapImage LoadImage(string imagePath, out double originalWidth, out double originalHeight)
        {
            try
            {
                using var originalImage = Image.FromFile(imagePath);
                originalWidth = originalImage.Width;
                originalHeight = originalImage.Height;

                // Load full image - sizing will be handled by WPF Image control
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

        public static void CropImage(string imagePath, double cropX, double cropY, double cropWidth, double cropHeight, 
                                   double displayWidth, double displayHeight, double originalWidth, double originalHeight,
                                   string outputPath, bool preserveMetadata = true)
        {
            try
            {
                using var originalImage = Image.FromFile(imagePath);

                // Convert display coordinates to original image coordinates
                // Display coordinates are relative to display size, convert to original image coordinates
                double displayToOriginalX = originalImage.Width / displayWidth;
                double displayToOriginalY = originalImage.Height / displayHeight;
                
                int originalX = (int)(cropX * displayToOriginalX);
                int originalY = (int)(cropY * displayToOriginalY);
                int originalCropWidth = (int)(cropWidth * displayToOriginalX);
                int originalCropHeight = (int)(cropHeight * displayToOriginalY);

                // Ensure coordinates are within image bounds
                originalX = Math.Max(0, Math.Min(originalX, originalImage.Width - 1));
                originalY = Math.Max(0, Math.Min(originalY, originalImage.Height - 1));
                originalCropWidth = Math.Max(1, Math.Min(originalCropWidth, originalImage.Width - originalX));
                originalCropHeight = Math.Max(1, Math.Min(originalCropHeight, originalImage.Height - originalY));

                var cropRect = new Rectangle(originalX, originalY, originalCropWidth, originalCropHeight);
                
                // Create cropped image
                var croppedImage = new Bitmap(cropRect.Width, cropRect.Height);
                using (var graphics = Graphics.FromImage(croppedImage))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(originalImage, 0, 0, cropRect, GraphicsUnit.Pixel);
                }

                // Save with metadata preservation
                if (preserveMetadata)
                {
                    SaveWithMetadata(croppedImage, outputPath, GetImageFormat(imagePath));
                    // Copy file timestamps from source to output
                    CopyFileTimestamps(imagePath, outputPath);
                }
                else
                {
                    croppedImage.Save(outputPath, ImageFormat.Jpeg);
                }

                croppedImage.Dispose();
            }
            catch (Exception ex)
            {
                throw new Exception($"Resim kırpılamadı: {imagePath} - {ex.Message}", ex);
            }
        }

        private static void CopyFileTimestamps(string sourcePath, string destPath)
        {
            try
            {
                var sourceInfo = new FileInfo(sourcePath);
                var destInfo = new FileInfo(destPath);
                
                // Copy creation, last write, and last access times
                destInfo.CreationTime = sourceInfo.CreationTime;
                destInfo.LastWriteTime = sourceInfo.LastWriteTime;
                destInfo.LastAccessTime = sourceInfo.LastAccessTime;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Dosya zaman damgaları kopyalanamadı: {ex.Message}");
            }
        }

        private static void SaveWithMetadata(Bitmap image, string outputPath, ImageFormat format)
        {
            try
            {
                // For JPEG, use quality encoder
                if (format == ImageFormat.Jpeg)
                {
                    var jpegEncoder = GetJpegEncoder();
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 95L);
                    image.Save(outputPath, jpegEncoder, encoderParams);
                }
                else
                {
                    // For other formats, save normally
                    image.Save(outputPath, format);
                }
            }
            catch (Exception)
            {
                // Fallback to normal save if metadata preservation fails
                image.Save(outputPath, ImageFormat.Jpeg);
            }
        }

        private static ImageFormat GetImageFormat(string imagePath)
        {
            var extension = Path.GetExtension(imagePath).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                ".png" => ImageFormat.Png,
                ".bmp" => ImageFormat.Bmp,
                ".gif" => ImageFormat.Gif,
                _ => ImageFormat.Jpeg
            };
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
