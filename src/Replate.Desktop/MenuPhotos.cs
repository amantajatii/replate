using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Replate.Desktop;

public sealed class MenuPhotoChange
{
    public const int MaximumBytes = 5 * 1024 * 1024;
    internal byte[]? Data { get; }
    internal string Extension { get; }
    private MenuPhotoChange(byte[]? data, string extension) { Data = data; Extension = extension; }
    public static MenuPhotoChange Remove { get; } = new(null, "");

    internal static async Task<MenuPhotoChange> Read(Stream source, string name)
    {
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png"))
            throw new InvalidOperationException("Pilih foto JPG, JPEG, atau PNG.");
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        while (true)
        {
            var count = await source.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, MaximumBytes + 1 - (int)buffer.Length)));
            if (count == 0) break;
            buffer.Write(chunk, 0, count);
            if (buffer.Length > MaximumBytes) throw new InvalidOperationException("Ukuran foto maksimal 5 MB.");
        }
        var data = buffer.ToArray();
        var png = data.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = data.Length >= 3 && data[0] == 255 && data[1] == 216 && data[2] == 255;
        if ((!png && !jpeg) || (extension == ".png" ? !png : !jpeg))
            throw new InvalidOperationException("File tidak dapat dibaca sebagai gambar JPG atau PNG.");
        try
        {
            using var stream = new MemoryStream(data, false);
            using var bitmap = Bitmap.DecodeToWidth(stream, 680);
            if (bitmap.PixelSize.Width <= 0 || bitmap.PixelSize.Height <= 0) throw new InvalidDataException();
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or NotSupportedException or NullReferenceException)
        { throw new InvalidOperationException("File tidak dapat dibaca sebagai gambar JPG atau PNG.", e); }
        return new MenuPhotoChange(data, png ? ".png" : ".jpg");
    }
}

internal sealed class MenuPhotoView : Border
{
    private readonly string? path;
    private readonly byte[]? data;
    private readonly int decodeWidth;
    private Bitmap? bitmap;
    internal bool HasPhoto => bitmap is not null;

    internal MenuPhotoView(string? path, double width, double height, byte[]? data = null)
    {
        this.path = path; this.data = data; decodeWidth = (int)width;
        Width = width; Height = height; CornerRadius = new CornerRadius(8); ClipToBounds = true;
        Background = Brush.Parse("#E0E9DF");
        Avalonia.Automation.AutomationProperties.SetName(this, "Foto menu");
        Placeholder();
        AttachedToVisualTree += (_, _) => Load();
        DetachedFromVisualTree += (_, _) => { Child = null; bitmap?.Dispose(); bitmap = null; };
    }
    private void Placeholder()
    {
        var text = Ui.Text("Foto belum tersedia", 12, Ui.Muted);
        text.TextAlignment = TextAlignment.Center;
        text.HorizontalAlignment = HorizontalAlignment.Center;
        text.Margin = new Thickness(8);
        Child = text;
    }
    private void Load()
    {
        bitmap?.Dispose(); bitmap = null; Placeholder();
        if (data is null && string.IsNullOrWhiteSpace(path)) return;
        try
        {
            using Stream stream = data is not null ? new MemoryStream(data, false) : File.OpenRead(path!);
            bitmap = Bitmap.DecodeToWidth(stream, decodeWidth);
            Child = new Image { Source = bitmap, Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException or NullReferenceException)
        { bitmap?.Dispose(); bitmap = null; Placeholder(); }
    }
}