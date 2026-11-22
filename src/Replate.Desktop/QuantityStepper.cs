using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Replate.Desktop;

internal sealed class QuantityStepper : StackPanel
{
    internal const double ButtonSize = 36;
    internal const double Gap = 12;
    internal static readonly IBrush DisabledFill = Brush.Parse("#DDE3DA");
    private static readonly Geometry MinusIcon = Geometry.Parse("M5 11H19V13H5Z");
    private static readonly Geometry PlusIcon = Geometry.Parse("M11 5H13V11H19V13H13V19H11V13H5V11H11Z");

    internal Button MinusButton { get; }
    internal Button PlusButton { get; }
    internal TextBlock QuantityText { get; }

    internal QuantityStepper(string minusName, string plusName, string quantityName, Action<int> change)
    {
        Orientation = Orientation.Horizontal; Spacing = Gap; VerticalAlignment = VerticalAlignment.Center;
        MinusButton = Step(MinusIcon, minusName, () => change(-1));
        PlusButton = Step(PlusIcon, plusName, () => change(1));
        QuantityText = Ui.Text("1", 18, Ui.Ink, true);
        QuantityText.MinWidth = 32; QuantityText.TextAlignment = TextAlignment.Center;
        AutomationProperties.SetName(QuantityText, quantityName);
        Children.Add(MinusButton); Children.Add(QuantityText); Children.Add(PlusButton);
    }

    internal void Show(int quantity, bool canDecrease, bool canIncrease)
    {
        QuantityText.Text = quantity.ToString();
        SetEnabled(MinusButton, canDecrease);
        SetEnabled(PlusButton, canIncrease);
    }

    internal static void SetEnabled(Button button, bool enabled)
    {
        button.IsEnabled = enabled;
        button.Background = enabled ? Ui.Green : DisabledFill;
        button.Foreground = enabled ? Ui.OnPrimary : Ui.Muted;
    }

    private static Button Step(Geometry icon, string name, Action action)
    {
        var button = Ui.Round(Ui.Button("", action, true), ButtonSize);
        button.Content = new PathIcon { Data = icon, Width = 14, Height = 14 };
        AutomationProperties.SetName(button, name); ToolTip.SetTip(button, name);
        return button;
    }
}
