using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Replate.Domain.Entities;

namespace Replate.Desktop;

internal sealed class CheckoutDialog : ModalDialog
{
    internal const double CardWidth = 520;
    internal const string TitleText = "Konfirmasi pesanan";
    internal const string CancelLabel = "Batal";
    internal const string ConfirmLabel = "Pesan sekarang";
    internal static string QuestionText(string restaurant) => $"Pesan dari {restaurant}?";
    internal static string ItemLine(CartItem item) => $"{item.Quantity} x {DemoSession.Rupiah(item.UnitPrice)}";
    internal static string PickupLine(IEnumerable<CartItem> items) => "Pengambilan " + string.Join(", ", items
        .Select(i => $"{i.Listing.PickupStart.ToLocalTime():HH:mm}–{i.Listing.PickupEnd.ToLocalTime():HH:mm}").Distinct());

    private readonly Func<Task<string?>> submit;
    internal Guid RestaurantId { get; }
    internal ScrollViewer Scroll { get; }
    internal TextBlock Title { get; }
    internal TextBlock QuestionTextBlock { get; }
    internal StackPanel ItemsPanel { get; }
    internal IReadOnlyList<Row> Rows { get; }
    internal TextBlock PickupText { get; }
    internal TextBlock TotalText { get; }
    internal TextBlock ErrorText { get; }
    internal Button CancelButton { get; }
    internal Button ConfirmButton { get; }
    internal bool Submitting { get; private set; }

    internal CheckoutDialog(string restaurant, CartRestaurantGroup group, Func<Task<string?>> submit, bool keyboard) : base(keyboard)
    {
        this.submit = submit; RestaurantId = group.RestaurantId;

        // Header
        Title = Ui.Text(TitleText, 22, Ui.Ink, true);
        Title.FontWeight = FontWeight.Bold; Title.VerticalAlignment = VerticalAlignment.Top;
        CloseButton.Margin = new Thickness(Ui.Spacing.Related, -6, -6, 0);
        QuestionTextBlock = Ui.Text(QuestionText(restaurant), 15);

        // Items
        Rows = group.Items.Select(item => new Row(item)).ToList();
        ItemsPanel = Ui.Stack(12, [.. Rows.Select(row => (Control)row.Panel)]);
        PickupText = Ui.Text(PickupLine(group.Items), 13, Ui.Muted);

        // Total
        TotalText = Ui.Text(DemoSession.Rupiah(group.CalculateSubtotal()), 24, Ui.Green, true);
        TotalText.FontWeight = FontWeight.Bold; TotalText.HorizontalAlignment = HorizontalAlignment.Right;
        TotalText.TextWrapping = TextWrapping.NoWrap;
        var total = Ui.Columns("*,Auto", Ui.Text("Total", 16, Ui.Ink, true), TotalText);

        var content = Ui.Stack(Ui.Spacing.Related,
            Ui.Columns("*,Auto", Title, CloseButton), QuestionTextBlock, ItemsPanel, PickupText, Ui.Divider(), total);
        content.Margin = new Thickness(Ui.Spacing.Section, Ui.Spacing.Section, Ui.Spacing.Section, 0);
        Scroll = Ui.Scroll(content);

        // Actions
        ErrorText = Ui.Text("", 14, ErrorBrush);
        ErrorText.IsVisible = false;
        CancelButton = Ui.Button(CancelLabel, Close);
        ConfirmButton = Ui.Button(ConfirmLabel, async () => await Submit(), true);
        AutomationProperties.SetName(ConfirmButton, $"Pesan sekarang dari {restaurant}");
        var buttons = Ui.Row(12, CancelButton, ConfirmButton);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        var actions = Ui.Stack(12, ErrorText, buttons);
        actions.Margin = new Thickness(Ui.Spacing.Section, Ui.Spacing.Related, Ui.Spacing.Section, Ui.Spacing.Section);

        // Card
        var layout = new Grid { RowDefinitions = RowDefinitions.Parse("*,Auto") };
        layout.Children.Add(Scroll);
        Grid.SetRow(actions, 1); layout.Children.Add(actions);
        SetCard(CardWidth, layout, TitleText);
    }

    // State
    internal async Task Submit()
    {
        if (Submitting) return;
        Submitting = true;
        QuantityStepper.SetEnabled(ConfirmButton, false);
        ErrorText.IsVisible = false;
        var error = await submit();
        Submitting = false;
        if (error is null) { Close(); return; }
        QuantityStepper.SetEnabled(ConfirmButton, true);
        ErrorText.Text = error; ErrorText.IsVisible = true;
    }

    // Item row
    internal sealed class Row
    {
        internal CartItem Item { get; }
        internal Grid Panel { get; }
        internal TextBlock NameText { get; }
        internal TextBlock QuantityText { get; }
        internal TextBlock SubtotalText { get; }

        internal Row(CartItem item)
        {
            Item = item;
            NameText = Ui.Text(item.Listing.Menu.Name, 14, Ui.Ink, true);
            QuantityText = Ui.Text(ItemLine(item), 13, Ui.Muted);
            SubtotalText = Ui.Text(DemoSession.Rupiah(item.CalculateSubtotal()), 14, Ui.Ink, true);
            SubtotalText.TextWrapping = TextWrapping.NoWrap; SubtotalText.Margin = new Thickness(Ui.Spacing.Related, 0, 0, 0);
            Panel = Ui.Columns("*,Auto", Ui.Stack(2, NameText, QuantityText), SubtotalText);
            AutomationProperties.SetName(Panel, item.Listing.Menu.Name);
        }
    }
}
