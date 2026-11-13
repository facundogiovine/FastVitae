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
    public const double WidthOverHeight = 0.8;

    // Longest side kept in memory
    private const int LoadSide = 2000;

    // Upright and shrunk, for cropping
    public static BitmapSource LoadForCrop(string path)
    {
        int width, height;
        using (var stream = File.OpenRead(path))
        {
            var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            width = frame.PixelWidth;
            height = frame.PixelHeight;
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(path);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        if (Math.Max(width, height) > LoadSide)
        {
            if (width >= height)
                image.DecodePixelWidth = LoadSide;
            else
                image.DecodePixelHeight = LoadSide;
        }
        image.EndInit();
        image.Freeze();

        var upright = new TransformedBitmap(image, new RotateTransform(RotationOf(path)));
        upright.Freeze();
        return upright;
    }

    // Biggest centered 4:5 area
    public static System.Windows.Int32Rect CenterArea(BitmapSource picture)
    {
        int width = picture.PixelWidth;
        int height = picture.PixelHeight;
        if ((double)width / height > WidthOverHeight)
            width = (int)Math.Round(height * WidthOverHeight);
        else
            height = (int)Math.Round(width / WidthOverHeight);
        return new System.Windows.Int32Rect((picture.PixelWidth - width) / 2, (picture.PixelHeight - height) / 2, width, height);
    }

    // Cut the area, shrink it
    public static BitmapSource Crop(BitmapSource picture, System.Windows.Int32Rect area)
    {
        double scale = Math.Min(1.0, (double)MaxSide / area.Height);
        var result = new TransformedBitmap(new CroppedBitmap(picture, area), new ScaleTransform(scale, scale));
        result.Freeze();
        return result;
    }

    public static void SaveJpeg(BitmapSource picture, string targetPath)
    {
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
