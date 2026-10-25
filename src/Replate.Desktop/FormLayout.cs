using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;

namespace Replate.Desktop;

// Constrain only the editor/filter, while allowing its page and tables to fill the window.
internal sealed class FormArea : Border
{
    internal FormArea(Control content, double maximumWidth = 860)
    {
        Child = content;
        MaxWidth = maximumWidth;
        HorizontalAlignment = HorizontalAlignment.Left;
        Styles.Add(new Style(x => x.OfType<TextBox>()) { Setters = { new Setter(TextBox.FontSizeProperty, 15d) } });
        Styles.Add(new Style(x => x.OfType<NumericUpDown>()) { Setters = { new Setter(NumericUpDown.FontSizeProperty, 15d) } });
        Styles.Add(new Style(x => x.OfType<ComboBox>()) { Setters = { new Setter(ComboBox.FontSizeProperty, 15d) } });
        Styles.Add(new Style(x => x.OfType<DatePicker>()) { Setters = { new Setter(DatePicker.FontSizeProperty, 15d) } });
        Styles.Add(new Style(x => x.OfType<TimePicker>()) { Setters = { new Setter(TimePicker.FontSizeProperty, 15d) } });
        foreach (var button in content.GetLogicalDescendants().OfType<Button>()) button.FontSize = 14;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? MaxWidth : Math.Min(MaxWidth, availableSize.Width);
        var measured = base.MeasureOverride(new Size(width, availableSize.Height));
        return new Size(width, measured.Height);
    }
}

internal sealed class FormColumns : Grid
{
    internal FormColumns(Control first, Control second)
    {
        ColumnSpacing = 24; RowSpacing = 20;
        Children.Add(first); Children.Add(second);
        var columns = 0;
        void ArrangeFields(double width)
        {
            var next = width >= 800 ? 2 : 1;
            if (next == columns) return;
            columns = next;
            ColumnDefinitions = ColumnDefinitions.Parse(next == 2 ? "*,*" : "*");
            RowDefinitions = RowDefinitions.Parse(next == 2 ? "Auto" : "Auto,Auto");
            SetColumn(second, next == 2 ? 1 : 0);
            SetRow(second, next == 2 ? 0 : 1);
        }
        ArrangeFields(0);
        SizeChanged += (_, e) => ArrangeFields(e.NewSize.Width);
    }
}