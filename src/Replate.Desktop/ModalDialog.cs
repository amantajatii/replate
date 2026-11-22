using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Path = Avalonia.Controls.Shapes.Path;

namespace Replate.Desktop;

internal abstract class ModalDialog : Grid
{
    internal const double Inset = 32;
    internal static readonly IBrush Scrim = Ui.Scrim;
    internal static readonly IBrush ErrorBrush = Brush.Parse("#9B3A25");
    private static readonly Geometry CloseIcon = Geometry.Parse("M6 6L18 18 M18 6L6 18");

    private readonly NavigationMethod focusMethod;
    private bool closed;
    internal Border ScrimLayer { get; }
    internal Border Card { get; private set; } = null!;
    internal Button CloseButton { get; }
    internal event Action? Closed;

    protected ModalDialog(bool keyboard)
    {
        focusMethod = keyboard ? NavigationMethod.Tab : NavigationMethod.Pointer;

        // Scrim
        ScrimLayer = new Border { Background = Scrim };
        ScrimLayer.PointerPressed += (_, e) => { e.Handled = true; Close(); };
        Children.Add(ScrimLayer);

        // Close
        var glyph = new Path
        {
            Data = CloseIcon, Width = 24, Height = 24, Stretch = Stretch.None, StrokeThickness = 1.75,
            StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round
        };
        CloseButton = Ui.Round(Ui.Button("", Close), 36, true);
        CloseButton.Content = glyph; CloseButton.VerticalAlignment = VerticalAlignment.Top;
        glyph[!Shape.StrokeProperty] = CloseButton[!TemplatedControl.ForegroundProperty];
        AutomationProperties.SetName(CloseButton, "Tutup"); ToolTip.SetTip(CloseButton, "Tutup");
        AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(() => CloseButton.Focus(focusMethod));
    }

    // Card
    protected void SetCard(double width, Control content, string name)
    {
        Card = new Border
        {
            Width = width, Background = Brushes.White, CornerRadius = new CornerRadius(16), BoxShadow = MarketplaceView.CardShadow,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Child = content
        };
        AutomationProperties.SetName(Card, name);
        KeyboardNavigation.SetTabNavigation(Card, KeyboardNavigationMode.Cycle);
        Children.Add(new Panel { Margin = new Thickness(Inset), Children = { Card } });
    }

    // State
    internal void Close()
    {
        if (closed) return;
        closed = true;
        OnClosing();
        Closed?.Invoke();
    }

    protected virtual void OnClosing() { }
}
