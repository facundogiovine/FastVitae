using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FastVitae.Storage;

// Profile picture loading and saving
public static class PictureFile
{
    // Largest saved height
    private const int MaxSide = 600;

    // Load without locking the file
    public static BitmapSource Load(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(path);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        image.EndInit();
        image.Freeze();
        return image;
    }

    // Portrait shape, 4 by 5
    private const double WidthOverHeight = 0.8;

    // Rotate, crop, shrink, save JPEG
    public static void SaveResized(string sourcePath, string targetPath)
    {
        BitmapSource picture = Load(sourcePath);
        picture = new TransformedBitmap(picture, new RotateTransform(RotationOf(sourcePath)));

        int width = picture.PixelWidth;
        int height = picture.PixelHeight;
        if ((double)width / height > WidthOverHeight)
            width = (int)Math.Round(height * WidthOverHeight);   // too wide: cut the sides
        else
            height = (int)Math.Round(width / WidthOverHeight);   // too tall: cut the ends
        var middle = new System.Windows.Int32Rect(
            (picture.PixelWidth - width) / 2, (picture.PixelHeight - height) / 2, width, height);
        picture = new CroppedBitmap(picture, middle);

        double scale = Math.Min(1.0, (double)MaxSide / height);
        picture = new TransformedBitmap(picture, new ScaleTransform(scale, scale));

        var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
        encoder.Frames.Add(BitmapFrame.Create(picture));
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        using var stream = File.Create(targetPath);
        encoder.Save(stream);
    }

    // Read the EXIF rotation
    private static double RotationOf(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            object? orientation = (frame.Metadata as BitmapMetadata)?.GetQuery("/app1/ifd/{ushort=274}");
            return orientation is ushort value
                ? value switch { 3 => 180, 6 => 90, 8 => 270, _ => 0 }
                : 0;
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or ArgumentException)
        {
            // No rotation info
            return 0;
        }
    }
}
