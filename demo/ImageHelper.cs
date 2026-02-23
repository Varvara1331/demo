using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace demo
{
    public static class ImageHelper
    {
        private static readonly string ImagesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Images");
        private static readonly string StubPath = Path.Combine(ImagesDirectory, "stub.jpg");

        public static BitmapImage LoadImage(string photoPath)
        {
            try
            {
                string fullPath = GetFullPath(photoPath);

                if (string.IsNullOrEmpty(photoPath) || !File.Exists(fullPath))
                    return LoadStubImage();

                byte[] imageData = File.ReadAllBytes(fullPath);
                var bitmap = new BitmapImage();

                using (var stream = new MemoryStream(imageData))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }

                return bitmap;
            }
            catch
            {
                return LoadStubImage();
            }
        }

        public static BitmapImage LoadStubImage()
        {
            try
            {
                if (File.Exists(StubPath))
                {
                    byte[] imageData = File.ReadAllBytes(StubPath);
                    var bitmap = new BitmapImage();

                    using (var stream = new MemoryStream(imageData))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }

                    return bitmap;
                }
            }
            catch { }

            return null;
        }

        public static string SaveImage(string sourceFilePath)
        {
            if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
                return null;

            try
            {
                string fileName = $"{Guid.NewGuid():N}.jpg";
                string destPath = Path.Combine(ImagesDirectory, fileName);

                File.Copy(sourceFilePath, destPath);
                return fileName;
            }
            catch
            {
                return null;
            }
        }

        public static void DeleteImage(string photoPath)
        {
            if (string.IsNullOrEmpty(photoPath))
                return;

            try
            {
                string fullPath = Path.Combine(ImagesDirectory, photoPath);
                if (File.Exists(fullPath) && fullPath != StubPath)
                {
                    File.Delete(fullPath);
                }
            }
            catch { }
        }

        private static string GetFullPath(string photoPath)
        {
            if (string.IsNullOrEmpty(photoPath))
                return StubPath;

            return Path.Combine(ImagesDirectory, photoPath);
        }
    }
}