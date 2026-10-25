using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Replate.Desktop;

internal static class Ui
{
    public static readonly IBrush Green = Brush.Parse("#245B43");
    public static readonly IBrush Ink = Brush.Parse("#23382C");
    public static readonly IBrush Muted = Brush.Parse("#59675D");
    public static TextBlock Text(string text, double size = 14, IBrush? color = null, bool bold = false) => new()
    {
        Text = text, FontSize = size, Foreground = color ?? Ink,
        FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal,
        TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center
    };
    public static StackPanel Stack(double spacing = 12, params Control[] children)
    {
        var panel = new StackPanel { Spacing = spacing };
        foreach (var child in children) panel.Children.Add(child);
        return panel;
    }
    public static StackPanel Row(double spacing = 12, params Control[] children)
    {
        var panel = Stack(spacing, children); panel.Orientation = Orientation.Horizontal; return panel;
    }
    public static Button Button(string text, Action action, bool primary = false)
    {
        var button = new Button { Content = text, Padding = new Thickness(16, 11), CornerRadius = new CornerRadius(6),
            Background = primary ? Green : Brushes.White, Foreground = primary ? Brushes.White : Ink,
            FontWeight = FontWeight.SemiBold, FontSize = 13, Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
        button.Click += (_, _) => action(); return button;
    }
    public static Grid Columns(string definitions, params Control[] children)
    {
        var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse(definitions) };
        for (var i = 0; i < children.Length; i++) { Grid.SetColumn(children[i], i); grid.Children.Add(children[i]); }
        return grid;
    }
    public static Control Form(Control content, double maximumWidth = 860) => new FormArea(content, maximumWidth);
    public static Control FormPair(Control first, Control second) => new FormColumns(first, second);
    public static TextBlock SupportingText(string text, IBrush? color = null) => Text(text, 14, color ?? Muted);
    public static TextBlock FieldError() => Text("", 14, Brush.Parse("#B3261E"));
    public static Border Divider() => new() { Height = 1, Background = Brush.Parse("#DDE3DA") };

    public static ScrollViewer Scroll(Control body) => new() { Content = body,
        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    public static Control Page(params Control[] children)
    {
        var body = Stack(24, children); body.Margin = new Thickness(32);
        return Scroll(body);
    }
    public static Control Heading(string title, string subtitle, Control? action = null) =>
        Columns("*,Auto", Stack(6, Text(title, 28, bold: true), Text(subtitle, 14, Muted)), action ?? new Border());
    public static StackPanel Field(string label, Control input)
    {
        Avalonia.Automation.AutomationProperties.SetName(input, label);
        input.HorizontalAlignment = input is DatePicker or TimePicker ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        return Stack(8, Text(label, 14, bold: true), input);
    }
    public static TextBox Input(string value = "", bool multiline = false) => new()
    {
        Text = value, AcceptsReturn = multiline, MinHeight = multiline ? 88 : 42,
        TextWrapping = TextWrapping.Wrap, CornerRadius = new CornerRadius(6), MaxLength = multiline ? 500 : 80
    };
    public static NumericUpDown Number(decimal value = 0, decimal max = 1000000, decimal increment = 1) => new()
    { Value = value, Minimum = 0, Maximum = max, Increment = increment, FormatString = increment < 1 ? "0.000" : "0", MinHeight = 42,
        ParsingNumberStyle = increment < 1 ? System.Globalization.NumberStyles.Number : System.Globalization.NumberStyles.Integer };
    public static Border TableRow(string columns, bool header, params Control[] cells)
    {
        var grid = Columns(columns, cells); grid.MinHeight = header ? 40 : 64;
        for (var i = 0; i < cells.Length; i++)
        {
            cells[i].Margin = new Thickness(i == 0 ? 0 : 16, 10, 0, 10);
            cells[i].VerticalAlignment = VerticalAlignment.Center;
        }
        return new Border { Child = grid, BorderBrush = Brush.Parse("#DDE3DA"), BorderThickness = new Thickness(0, 0, 0, 1) };
    }

}
