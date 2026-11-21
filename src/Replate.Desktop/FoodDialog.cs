using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Replate.Domain.Entities;
using Path = Avalonia.Controls.Shapes.Path;

namespace Replate.Desktop;

internal sealed class FoodDialog : Grid
{
    internal const double CardWidth = 760;
    internal const double Inset = 32;
    internal const double PhotoSize = 300;
    internal const double StepperSize = QuantityStepper.ButtonSize;
    internal const string UnavailableText = "Penawaran tidak tersedia";
    internal const string FullText = "Sudah di keranjang dengan jumlah maksimal";
    internal const string AddLabel = "Tambah ke keranjang";
    internal static readonly IBrush Scrim = Brush.Parse("#6623382C");
    internal static readonly IBrush ErrorBrush = Brush.Parse("#9B3A25");
    internal static readonly IBrush DisabledFill = QuantityStepper.DisabledFill;
    private static readonly Geometry CloseIcon = Geometry.Parse("M6 6L18 18 M18 6L6 18");

    private readonly DemoSession session;
    private readonly Func<SurplusListing, int, string?> add;
    private readonly NavigationMethod focusMethod;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(15) };
    private bool closed;
    internal SurplusListing Listing { get; }
    internal int Quantity { get; private set; } = 1;
    internal Border ScrimLayer { get; }
    internal Border Card { get; }
    internal ScrollViewer Scroll { get; }
    internal MenuPhotoView Photo { get; }
    internal TextBlock NameText { get; }
    internal TextBlock MetaText { get; }
    internal TextBlock DescriptionText { get; }
    internal TextBlock PickupText { get; }
    internal TextBlock StockText { get; }
    internal TextBlock PriceText { get; }
    internal TextBlock NormalPriceText { get; }
    internal TextBlock SavingText { get; }
    internal QuantityStepper Stepper { get; }
    internal TextBlock QuantityText => Stepper.QuantityText;
    internal TextBlock LimitNote { get; }
    internal TextBlock ErrorText { get; }
    internal TextBlock TotalText { get; }
    internal Border Bar { get; }
    internal Button CloseButton { get; }
    internal Button MinusButton => Stepper.MinusButton;
    internal Button PlusButton => Stepper.PlusButton;
    internal Button AddButton { get; }
    internal event Action? Closed;

    internal int InCart => session.Cart.Items.Where(i => i.ListingId == Listing.Id).Sum(i => i.Quantity);
    internal int Limit => Math.Max(0, Listing.Quantity - InCart);
    internal bool Available => Listing.IsAvailable(DateTime.UtcNow) && Listing.Menu.IsActive;

    internal FoodDialog(DemoSession session, SurplusListing listing, Func<SurplusListing, int, string?> add, bool keyboard)
    {
        this.session = session; this.add = add; Listing = listing;
        focusMethod = keyboard ? NavigationMethod.Tab : NavigationMethod.Pointer;
        var menu = listing.Menu;

        // Scrim
        ScrimLayer = new Border { Background = Scrim };
        ScrimLayer.PointerPressed += (_, e) => { e.Handled = true; Close(); };
        Children.Add(ScrimLayer);

        // Details
        Photo = new MenuPhotoView(session.PhotoPath(menu.ImageUrl), PhotoSize, PhotoSize, placeholderSize: 14) { VerticalAlignment = VerticalAlignment.Top };
        NameText = Ui.Text(menu.Name, 28, Ui.Ink, true);
        NameText.FontWeight = FontWeight.Bold; NameText.VerticalAlignment = VerticalAlignment.Top;
        CloseButton = IconButton();
        var header = Ui.Columns("*,Auto", NameText, CloseButton);
        CloseButton.Margin = new Thickness(Ui.Spacing.Related, -6, -6, 0);
        MetaText = Ui.Text($"{menu.Restaurant?.Name} · {menu.Category}", 14, Ui.Muted);
        DescriptionText = Ui.Text(menu.Description, 14);
        PickupText = Ui.Text($"Pengambilan {listing.PickupStart.ToLocalTime():HH:mm}–{listing.PickupEnd.ToLocalTime():HH:mm}", 14);
        StockText = Ui.Text("", 13, Ui.Muted);
        PriceText = Ui.Text(DemoSession.Rupiah(listing.RescuePrice), 26, Ui.Green, true);
        var unit = Ui.Text("/porsi", 14, Ui.Muted);
        unit.VerticalAlignment = PriceText.VerticalAlignment = VerticalAlignment.Bottom;
        unit.Margin = new Thickness(0, 0, 0, 4);
        NormalPriceText = new TextBlock { Text = DemoSession.Rupiah(menu.NormalPrice), FontSize = 13, Foreground = Ui.Muted,
            TextDecorations = TextDecorations.Strikethrough, VerticalAlignment = VerticalAlignment.Center };
        SavingText = Ui.Text($"Hemat {listing.CalculateDiscount():0}%", 13, Ui.Green, true);

        // Quantity
        Stepper = new QuantityStepper("Kurangi jumlah porsi", "Tambah jumlah porsi", "Jumlah porsi", Change);
        Stepper.HorizontalAlignment = HorizontalAlignment.Left;
        LimitNote = Ui.Text("", 12, Ui.Muted);
        var stepper = Ui.Stack(8, Ui.Text("Jumlah porsi", 14, Ui.Ink, true), Stepper, LimitNote);

        var facts = Ui.Stack(12, header, MetaText, DescriptionText,
            Ui.Stack(4, PickupText, StockText),
            Ui.Stack(4, Ui.Row(6, PriceText, unit), Ui.Row(12, NormalPriceText, SavingText)),
            stepper);
        var content = new Grid { ColumnDefinitions = ColumnDefinitions.Parse($"{PhotoSize},*"), ColumnSpacing = Ui.Spacing.Section, Margin = new Thickness(Ui.Spacing.Section) };
        content.Children.Add(Photo);
        Grid.SetColumn(facts, 1); content.Children.Add(facts);
        Scroll = Ui.Scroll(content);

        // Bar
        ErrorText = Ui.Text("", 14, ErrorBrush);
        ErrorText.IsVisible = false; ErrorText.Margin = new Thickness(Ui.Spacing.Section, 0, Ui.Spacing.Section, Ui.Spacing.Related);
        TotalText = Ui.Text("", 20, Ui.Ink, true);
        AddButton = Ui.Button(AddLabel, Submit, true);
        AddButton.VerticalAlignment = VerticalAlignment.Center;
        AutomationProperties.SetName(AddButton, $"Tambah {menu.Name} ke keranjang");
        Bar = new Border
        {
            Background = Ui.Selection, CornerRadius = new CornerRadius(0, 0, 16, 16), Padding = new Thickness(Ui.Spacing.Section, Ui.Spacing.Related),
            Child = Ui.Columns("*,Auto", Ui.Stack(2, Ui.Text("Total", 12, Ui.Muted), TotalText), AddButton)
        };

        // Card
        var layout = new Grid { RowDefinitions = RowDefinitions.Parse("*,Auto,Auto") };
        layout.Children.Add(Scroll);
        Grid.SetRow(ErrorText, 1); layout.Children.Add(ErrorText);
        Grid.SetRow(Bar, 2); layout.Children.Add(Bar);
        Card = new Border
        {
            Width = CardWidth, Background = Brushes.White, CornerRadius = new CornerRadius(16), BoxShadow = MarketplaceView.CardShadow,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Child = layout
        };
        AutomationProperties.SetName(Card, menu.Name);
        KeyboardNavigation.SetTabNavigation(Card, KeyboardNavigationMode.Cycle);
        Children.Add(new Panel { Margin = new Thickness(Inset), Children = { Card } });

        Update();
        timer.Tick += (_, _) => Update();
        AttachedToVisualTree += (_, _) => { timer.Start(); Dispatcher.UIThread.Post(() => CloseButton.Focus(focusMethod)); };
        DetachedFromVisualTree += (_, _) => timer.Stop();
    }

    // State
    internal void Update()
    {
        var available = Available;
        var limit = Limit;
        if (Quantity > limit) Quantity = Math.Max(1, limit);
        Stepper.Show(Quantity, available && limit > 0 && Quantity > 1, available && Quantity < limit);
        StockText.Text = $"{Listing.Quantity} porsi tersisa";
        TotalText.Text = DemoSession.Rupiah(Listing.RescuePrice * Quantity);
        QuantityStepper.SetEnabled(AddButton, available && limit > 0);
        LimitNote.Text = !available ? UnavailableText : limit == 0 ? FullText : Quantity >= limit ? $"Maksimal {limit} porsi" : "";
        LimitNote.IsVisible = LimitNote.Text.Length > 0;
    }

    internal void Change(int delta)
    {
        Quantity = Math.Clamp(Quantity + delta, 1, Math.Max(1, Limit));
        Update();
    }

    internal void Submit()
    {
        Update();
        if (!AddButton.IsEnabled) return;
        var error = add(Listing, Quantity);
        if (error is null) { Close(); return; }
        ErrorText.Text = error; ErrorText.IsVisible = true;
        Update();
    }

    internal void Close()
    {
        if (closed) return;
        closed = true; timer.Stop();
        Closed?.Invoke();
    }

    // Controls
    private Button IconButton()
    {
        var glyph = new Path
        {
            Data = CloseIcon, Width = 24, Height = 24, Stretch = Stretch.None, StrokeThickness = 1.75,
            StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round
        };
        var button = Ui.Round(Ui.Button("", Close), 36, true);
        button.Content = glyph; button.VerticalAlignment = VerticalAlignment.Top;
        glyph[!Shape.StrokeProperty] = button[!TemplatedControl.ForegroundProperty];
        AutomationProperties.SetName(button, "Tutup"); ToolTip.SetTip(button, "Tutup");
        return button;
    }
}
