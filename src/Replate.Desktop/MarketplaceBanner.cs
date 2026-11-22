using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Replate.Desktop;

internal sealed class MarketplaceBanner : Panel
{
    internal const double TextPadding = 40;
    internal const double TextShare = .45;
    internal const double IllustrationStart = .462;
    internal const double CabinetStart = .878;
    internal const double GreetingShare = .0287;
    internal const double MessageShare = .0184;
    internal const double MinGreetingSize = 22;
    internal const double MaxGreetingSize = 32;
    internal const double MinMessageSize = 15;
    internal const double MaxMessageSize = 20;
    internal const string Tagline = "Yuk, Selamatkan Makanan Lewat Setiap Pesanan!";
    internal static readonly Uri HeroUri = new("avares://Replate.Desktop/Assets/marketplace-hero.webp");
    private static readonly Lazy<Bitmap> Hero = new(() => { using var stream = AssetLoader.Open(HeroUri); return new Bitmap(stream); });

    internal Control Art { get; } = new BannerArt();
    internal StackPanel TextArea { get; }
    internal TextBlock Greeting { get; }
    internal TextBlock Message { get; }

    internal MarketplaceBanner(string name) : this($"Halo, {name}.", Tagline) { }

    internal MarketplaceBanner(string title, string description, Control? action = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Greeting = new TextBlock
        {
            Text = title, FontWeight = FontWeight.Bold, Foreground = Ui.Green,
            TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis
        };
        Message = new TextBlock
        {
            Text = description, Foreground = Ui.Muted,
            TextWrapping = TextWrapping.Wrap, MaxLines = 2, TextTrimming = TextTrimming.WordEllipsis
        };
        TextArea = Ui.Stack(8, Greeting, Message);
        if (action is not null) TextArea.Children.Add(action);
        AutomationProperties.SetAccessibilityView(Art, AccessibilityView.Raw);
        Children.Add(Art); Children.Add(TextArea);
    }

    internal static Size ImageSize => Hero.Value.Size;

    internal static double HeightFor(double width) => width * ImageSize.Height / ImageSize.Width;

    internal static double IllustrationLeft(Size size) => IllustrationStart * size.Width;

    internal static double CabinetTop(Size size) => CabinetStart * size.Height;

    internal static double TextLimit(Size size) => Math.Max(0, TextShare * size.Width - TextPadding);

    internal static double GreetingSize(double width) => Math.Clamp(Math.Round(width * GreetingShare), MinGreetingSize, MaxGreetingSize);

    internal static double MessageSize(double width) => Math.Clamp(Math.Round(width * MessageShare), MinMessageSize, MaxMessageSize);

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? MarketplaceView.ContentMaxWidth : availableSize.Width;
        var size = new Size(width, HeightFor(width));
        Greeting.FontSize = GreetingSize(width);
        Message.FontSize = MessageSize(width);
        Art.Measure(size);
        TextArea.Measure(new Size(TextLimit(size), size.Height));
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Art.Arrange(new Rect(finalSize));
        var height = TextArea.DesiredSize.Height;
        var top = Math.Max(0, (CabinetTop(finalSize) - height) / 2);
        TextArea.Arrange(new Rect(TextPadding, top, TextLimit(finalSize), height));
        return finalSize;
    }

    // Art
    private sealed class BannerArt : Control
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);

        public override void Render(DrawingContext context)
        {
            var size = Bounds.Size;
            if (size.Width <= 0 || size.Height <= 0) return;
            var bitmap = Hero.Value;
            var scale = Math.Min(size.Width / bitmap.Size.Width, size.Height / bitmap.Size.Height);
            var drawn = new Size(bitmap.Size.Width * scale, bitmap.Size.Height * scale);
            var target = new Rect(new Point((size.Width - drawn.Width) / 2, (size.Height - drawn.Height) / 2), drawn);
            using (context.PushClip(new RoundedRect(target, 16)))
                context.DrawImage(bitmap, new Rect(bitmap.Size), target);
        }
    }
}
