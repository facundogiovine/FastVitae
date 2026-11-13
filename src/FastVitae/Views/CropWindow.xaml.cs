using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FastVitae.Storage;

namespace FastVitae.Views;

// Pick the 4:5 part to use
public partial class CropWindow : Window
{
    private const double StageSide = 440;
    private const double Ratio = PictureFile.WidthOverHeight;

    private readonly BitmapSource picture;
    private readonly double scale;  // screen pixels per picture pixel

    // Frame in picture pixels
    private double frameX, frameY, frameHeight;
    private double FrameWidth => frameHeight * Ratio;

    // Drag state
    private string grip = "";
    private Point dragStart;
    private double startX, startY;

    // Cropped picture, after OK
    public BitmapSource? Cropped { get; private set; }

    public CropWindow(BitmapSource picture, string name, string title)
    {
        InitializeComponent();
        this.picture = picture;
        NameText.Text = name;
        TitleText.Text = title;

        scale = Math.Min(StageSide / picture.PixelWidth, StageSide / picture.PixelHeight);
        Photo.Source = picture;
        Stage.Width = picture.PixelWidth * scale;
        Stage.Height = picture.PixelHeight * scale;

        var area = PictureFile.CenterArea(picture);
        frameX = area.X;
        frameY = area.Y;
        frameHeight = area.Height;
        Redraw();
    }

    private double LargestFrame => Math.Min(picture.PixelHeight, picture.PixelWidth / Ratio);
    private double SmallestFrame => Math.Min(LargestFrame, Math.Max(40, LargestFrame / 10));

    private Int32Rect Area()
    {
        int height = Math.Max(1, (int)Math.Round(frameHeight));
        int width = Math.Max(1, (int)Math.Round(height * Ratio));
        int x = Math.Clamp((int)Math.Round(frameX), 0, picture.PixelWidth - width);
        int y = Math.Clamp((int)Math.Round(frameY), 0, picture.PixelHeight - height);
        return new Int32Rect(x, y, width, height);
    }

    // Draw frame, shade, preview
    private void Redraw()
    {
        double left = frameX * scale, top = frameY * scale;
        double width = FrameWidth * scale, height = frameHeight * scale;

        Canvas.SetLeft(Frame, left);
        Canvas.SetTop(Frame, top);
        Frame.Width = width;
        Frame.Height = height;
        Place(TopLeft, left, top);
        Place(TopRight, left + width, top);
        Place(BottomLeft, left, top + height);
        Place(BottomRight, left + width, top + height);

        Shade.Data = new CombinedGeometry(GeometryCombineMode.Exclude,
            new RectangleGeometry(new Rect(0, 0, Stage.Width, Stage.Height)),
            new RectangleGeometry(new Rect(left, top, width, height)));

        Result.Source = new CroppedBitmap(picture, Area());
    }

    private static void Place(Rectangle handle, double x, double y)
    {
        Canvas.SetLeft(handle, x - handle.Width / 2);
        Canvas.SetTop(handle, y - handle.Height / 2);
    }

    private void Grip_MouseDown(object sender, MouseButtonEventArgs e)
    {
        grip = (string)((FrameworkElement)sender).Tag;
        dragStart = e.GetPosition(Stage);
        startX = frameX;
        startY = frameY;
        Stage.CaptureMouse();
        e.Handled = true;
    }

    private void Stage_MouseUp(object sender, MouseButtonEventArgs e)
    {
        grip = "";
        Stage.ReleaseMouseCapture();
    }

    private void Stage_MouseMove(object sender, MouseEventArgs e)
    {
        if (grip == "")
            return;
        var mouse = e.GetPosition(Stage);

        if (grip == "move")
        {
            frameX = Math.Clamp(startX + (mouse.X - dragStart.X) / scale, 0, picture.PixelWidth - FrameWidth);
            frameY = Math.Clamp(startY + (mouse.Y - dragStart.Y) / scale, 0, picture.PixelHeight - frameHeight);
        }
        else
        {
            Resize(mouse.X / scale, mouse.Y / scale);
        }
        Redraw();
    }

    // Opposite corner stays put
    private void Resize(double mouseX, double mouseY)
    {
        bool left = grip[1] == 'l', top = grip[0] == 't';
        double anchorX = left ? frameX + FrameWidth : frameX;
        double anchorY = top ? frameY + frameHeight : frameY;

        double roomX = left ? anchorX : picture.PixelWidth - anchorX;
        double roomY = top ? anchorY : picture.PixelHeight - anchorY;
        double wanted = Math.Max(Math.Abs(mouseY - anchorY), Math.Abs(mouseX - anchorX) / Ratio);
        frameHeight = Math.Clamp(wanted, SmallestFrame, Math.Max(SmallestFrame, Math.Min(roomY, roomX / Ratio)));

        frameX = left ? anchorX - FrameWidth : anchorX;
        frameY = top ? anchorY - frameHeight : anchorY;
    }

    // Wheel zooms around the center
    private void Stage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        double centerX = frameX + FrameWidth / 2, centerY = frameY + frameHeight / 2;
        frameHeight = Math.Clamp(frameHeight * (e.Delta > 0 ? 0.9 : 1.1), SmallestFrame, LargestFrame);
        frameX = Math.Clamp(centerX - FrameWidth / 2, 0, picture.PixelWidth - FrameWidth);
        frameY = Math.Clamp(centerY - frameHeight / 2, 0, picture.PixelHeight - frameHeight);
        Redraw();
    }

    private void Use_Click(object sender, RoutedEventArgs e)
    {
        Cropped = PictureFile.Crop(picture, Area());
        DialogResult = true;
    }
}
