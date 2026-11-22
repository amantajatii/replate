using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Replate.Desktop;

internal static class InputVerification
{
    private static double CenterOffset(Control part, Control box) =>
        Top(part, box) + part.Bounds.Height / 2 - box.Bounds.Height / 2;

    private static double Top(Control part, Control box) =>
        (part.TranslatePoint(default, box) ?? throw new InvalidOperationException($"{part.GetType().Name} is not inside the field")).Y;

    private static double TopOffset(Control part, TextBox box) => Top(part, box) - box.BorderThickness.Top - box.Padding.Top;

    internal static int Check(Visual root, string screen)
    {
        var failures = new List<string>();
        var boxes = root.GetVisualDescendants().OfType<TextBox>().Where(b => b.IsEffectivelyVisible && b.Bounds.Height > 0).ToList();
        foreach (var box in boxes)
        {
            var host = box.FindAncestorOfType<NumericUpDown>() as Control ?? box;
            var name = $"{screen}: {Avalonia.Automation.AutomationProperties.GetName(host)}";
            void Fail(bool ok, string message) { if (!ok) failures.Add($"{name}: {message}"); }
            var placeholder = box.PlaceholderText ?? "";
            Fail(placeholder.Trim().Length > 0, "placeholder is empty");
            Fail(!placeholder.Contains('—') && !placeholder.Contains('!'), $"placeholder \"{placeholder}\" uses an em dash or exclamation mark");
            Fail(box.PlaceholderForeground is ISolidColorBrush { Color: var ink } && ink == Color.Parse("#69736D"), "placeholder does not use the muted token");
            var multiline = box.AcceptsReturn;
            Fail(box.VerticalContentAlignment == (multiline ? VerticalAlignment.Top : VerticalAlignment.Center), $"vertical content alignment is {box.VerticalContentAlignment}");
            Fail(multiline ? Math.Abs(box.Padding.Top - box.Padding.Left) < .01 : Math.Abs(box.Padding.Top - box.Padding.Bottom) < .01,
                $"padding {box.Padding} does not match the {(multiline ? "multiline" : "single-line")} rule");
            double Offset(Control part) => multiline ? TopOffset(part, box) : CenterOffset(part, box);
            var where = multiline ? "top" : "center";
            var presenter = box.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
            Fail(presenter is not null && Math.Abs(Offset(presenter)) <= 1,
                $"text {where} is off by {(presenter is null ? double.NaN : Offset(presenter)):0.##}");
            if (string.IsNullOrEmpty(box.Text))
            {
                var hint = box.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.IsEffectivelyVisible && t.Text == placeholder);
                Fail(hint is not null && Math.Abs(Offset(hint)) <= 1,
                    $"placeholder {where} is off by {(hint is null ? double.NaN : Offset(hint)):0.##}");
                Fail(hint is not null && hint.Opacity >= 1 && hint.Foreground is ISolidColorBrush { Color: var shown } && shown == Color.Parse("#69736D"),
                    $"placeholder {hint?.Name} is drawn at opacity {hint?.Opacity} in {(hint?.Foreground as ISolidColorBrush)?.Color}");
            }
            if (box.InnerRightContent is Control { IsEffectivelyVisible: true } inner)
                Fail(Math.Abs(CenterOffset(inner, box)) <= 1, $"inner right content center is off by {CenterOffset(inner, box):0.##}");
        }
        if (failures.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        return boxes.Count;
    }
}
