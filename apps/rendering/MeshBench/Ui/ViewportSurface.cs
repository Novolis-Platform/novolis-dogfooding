using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Novolis.Avalonia.Rendering;
using Novolis.Math.Geometry;
using Novolis.Rendering.Presentation.Abstractions;

namespace MeshBench.Ui;

/// <summary>
/// Panel-based CPU frame host (works before GPR picks up fixed <see cref="Rgba32FrameControl"/> layout).
/// </summary>
internal sealed class ViewportSurface : Panel, IFramePresenter
{
    private readonly Image _image;
    private WriteableBitmap? _bitmap;

    public ViewportSurface()
    {
        Background = new SolidColorBrush(Color.FromRgb(24, 24, 28));
        _image = new Image
        {
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        Children.Add(_image);
    }

    public void PresentCpuFrame(ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        var copy = pixels.ToArray();
        if (Dispatcher.UIThread.CheckAccess())
            ApplyFrame(copy, width, height);
        else
            Dispatcher.UIThread.Post(() => ApplyFrame(copy, width, height), DispatcherPriority.Render);
    }

    private void ApplyFrame(ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        if (_bitmap is null || _bitmap.PixelSize.Width != width || _bitmap.PixelSize.Height != height)
        {
            _bitmap = Rgba32Bitmap.CreateBitmap(width, height);
            _image.Source = _bitmap;
        }

        Rgba32Bitmap.CopyPixels(_bitmap, pixels, width, height);
        InvalidateVisual();
    }
}
