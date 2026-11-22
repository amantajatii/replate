using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Replate.Domain.Entities;
using Path = Avalonia.Controls.Shapes.Path;

namespace Replate.Desktop;

public sealed class MarketplaceView : UserControl
{
    internal const double ContentMaxWidth = 1200;
    internal const string Cheapest = "Termurah";
    internal const string EndingSoon = "Segera berakhir";
    internal const string SectionTitle = "Jelajahi Menu";
    internal const double TitleSize = 32;
    internal static readonly string[] CollectionNames = [Cheapest, EndingSoon, MenuCategories.MainCourse, MenuCategories.Bakery, MenuCategories.Produce];
    internal static readonly BoxShadows CardShadow = Ui.CardShadow;
    internal static readonly BoxShadows PrimaryCardShadow = Ui.PrimaryCardShadow;
    internal static readonly BoxShadows PrimaryCardHoverShadow = Ui.PrimaryCardHoverShadow;
    internal const double HoverScale = 1.03;
    internal static readonly TimeSpan HoverDuration = TimeSpan.FromMilliseconds(120);
    internal const string CardFacePart = "PART_CardFace";
    internal static readonly IBrush PressedFill = Ui.DividerFill;
    private static readonly Dictionary<string, Geometry> CollectionIcons = new()
    {
        [Cheapest] = Geometry.Parse("M3.5 12.2V4.5A1 1 0 0 1 4.5 3.5H12.2L20.5 11.8A1.5 1.5 0 0 1 20.5 13.9L13.9 20.5A1.5 1.5 0 0 1 11.8 20.5Z M8 6.5A1.5 1.5 0 1 1 8 9.5A1.5 1.5 0 1 1 8 6.5Z"),
        [EndingSoon] = Geometry.Parse("M12 3A9 9 0 1 1 12 21A9 9 0 1 1 12 3Z M12 7V12L15.5 14"),
        [MenuCategories.MainCourse] = Geometry.Parse("M6 3V8A3 3 0 0 0 12 8V3 M9 3V21 M17 3C15.3 3 14.5 5.2 14.5 7.5C14.5 9.6 15.6 11 17 11C18.4 11 19.5 9.6 19.5 7.5C19.5 5.2 18.7 3 17 3Z M17 11V21"),
        [MenuCategories.Bakery] = Geometry.Parse("M5 11.5C3.6 11 3 9.9 3 8.8C3 6.2 6.9 4.5 12 4.5C17.1 4.5 21 6.2 21 8.8C21 9.9 20.4 11 19 11.5V18.5A1.5 1.5 0 0 1 17.5 20H6.5A1.5 1.5 0 0 1 5 18.5Z M9 9L10.5 11 M13.5 9L15 11"),
        [MenuCategories.Produce] = Geometry.Parse("M5 19C5 10 10.5 4.5 20 4C19.5 13.5 14 19 5 19Z M5 19L13 11")
    };
    private static readonly ControlTheme CardButton = new(typeof(Button))
    {
        Setters =
        {
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Button>((button, _) => Ui.FocusFrame(new Border
            {
                Name = CardFacePart,
                Transitions = [new BoxShadowsTransition { Property = Border.BoxShadowProperty, Duration = HoverDuration, Easing = new CubicEaseOut() }],
                [!Border.BackgroundProperty] = button[!TemplatedControl.BackgroundProperty],
                [!Border.BorderBrushProperty] = button[!TemplatedControl.BorderBrushProperty],
                [!Border.BorderThicknessProperty] = button[!TemplatedControl.BorderThicknessProperty],
                [!Border.PaddingProperty] = button[!TemplatedControl.PaddingProperty],
                CornerRadius = new CornerRadius(16),
                Child = new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = button[!ContentControl.ContentProperty],
                    [!ContentPresenter.ForegroundProperty] = button[!TemplatedControl.ForegroundProperty],
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                }
            }, new CornerRadius(16))))
        }
    };

    private readonly Dictionary<string, Button> collectionCards = [];
    internal MarketplaceBanner Banner { get; }
    internal Grid CollectionRow { get; }
    internal IReadOnlyDictionary<string, Button> CollectionCards => collectionCards;
    internal ListingFeed Feed { get; }
    internal StackPanel ListingsPanel => Feed.Panel;
    internal TextBlock Title { get; } = Ui.Text(SectionTitle, TitleSize, Ui.Ink, true);
    internal Grid TitleRow { get; }
    internal StackPanel Section { get; }
    internal StackPanel Body { get; }
    internal ScrollViewer Scroll { get; }
    internal IReadOnlyList<SurplusListing> VisibleListings => Feed.Listings;
    internal int VisibleCount => VisibleListings.Count;

    internal MarketplaceView(MainWindow owner, DemoSession session)
    {
        Feed = new ListingFeed(owner, session, () => ListingQuery.Available(session),
            () => ListingFeed.EmptyState("Belum ada menu tersedia", "Penawaran surplus baru akan tampil di sini.", null));
        Banner = new MarketplaceBanner(session.CurrentUser?.Name ?? "");
        CollectionRow = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,*,*,*,*"), ColumnSpacing = Ui.Spacing.Related };
        for (var i = 0; i < CollectionNames.Length; i++)
        {
            var name = CollectionNames[i];
            var card = CollectionCard(name, () => owner.ShowCollection(name));
            Grid.SetColumn(card, i); CollectionRow.Children.Add(card);
        }
        AddStyles();
        Title.HorizontalAlignment = HorizontalAlignment.Left; Title.TextWrapping = TextWrapping.NoWrap; Title.FontWeight = FontWeight.Bold;
        TitleRow = Ui.Columns("Auto,*", Title, Feed.Notice);
        Section = Ui.Stack(Ui.Spacing.Related, TitleRow, ListingsPanel);
        Body = Ui.Stack(Ui.Spacing.Section, Banner, CollectionRow, Section,
            Ui.Text("Harga dan stok contoh. Reservasi tidak melibatkan pembayaran.", 12, Ui.Muted));
        Body.Margin = new Thickness(Ui.Spacing.Page, Ui.Spacing.Section, Ui.Spacing.Page, Ui.Spacing.Page); Body.MaxWidth = ContentMaxWidth;
        Body.HorizontalAlignment = HorizontalAlignment.Stretch;
        Scroll = Ui.Scroll(Body);
        Content = Scroll;
    }

    // Collection cards
    private Button CollectionCard(string name, Action open)
    {
        var icon = new Path
        {
            Data = CollectionIcons[name], Width = 24, Height = 24, Stretch = Stretch.None, StrokeThickness = 1.75,
            StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round, HorizontalAlignment = HorizontalAlignment.Center
        };
        var label = new TextBlock
        {
            Text = name, FontSize = 14, TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center
        };
        var card = new Button
        {
            Theme = CardButton, Content = Ui.Stack(8, icon, label), Height = 96, Padding = new Thickness(12, 16),
            BorderThickness = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Stretch, ClipToBounds = false,
            HorizontalContentAlignment = HorizontalAlignment.Center, Cursor = new Cursor(StandardCursorType.Hand),
            RenderTransformOrigin = RelativePoint.Center,
            Transitions =
            [
                new BrushTransition { Property = TemplatedControl.BackgroundProperty, Duration = HoverDuration, Easing = new CubicEaseOut() },
                new BrushTransition { Property = TemplatedControl.ForegroundProperty, Duration = HoverDuration, Easing = new CubicEaseOut() },
                new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = HoverDuration, Easing = new CubicEaseOut() }
            ]
        };
        icon.Bind(Shape.StrokeProperty, card.GetObservable(TemplatedControl.ForegroundProperty));
        card.Classes.Add("collection-card");
        AutomationProperties.SetName(card, name);
        card.Click += (_, _) => open();
        collectionCards.Add(name, card);
        return card;
    }

    private void AddStyles()
    {
        Style State(Func<Selector, Selector> state, IBrush? background, IBrush? foreground, IBrush? border = null, FontWeight? weight = null)
        {
            var style = new Style(x => state(x.OfType<Button>().Class("collection-card")));
            if (background is not null) style.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, background));
            if (foreground is not null) style.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, foreground));
            if (border is not null) style.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, border));
            if (weight is not null) style.Setters.Add(new Setter(TemplatedControl.FontWeightProperty, weight.Value));
            return style;
        }
        var rest = State(x => x, Ui.Green, Ui.OnPrimary, Brushes.Transparent, FontWeight.Normal);
        rest.Setters.Add(new Setter(Visual.RenderTransformProperty, TransformOperations.Parse("scale(1)")));
        Styles.Add(rest);
        var hover = State(x => x.Class(":pointerover"), Ui.Selection, Ui.Green);
        hover.Setters.Add(new Setter(Visual.RenderTransformProperty, TransformOperations.Parse($"scale({HoverScale.ToString(System.Globalization.CultureInfo.InvariantCulture)})")));
        hover.Setters.Add(new Setter(Visual.ZIndexProperty, 1));
        Styles.Add(hover);
        Styles.Add(State(x => x.Class(":pressed"), PressedFill, Ui.Green));
        Style Face(Func<Selector, Selector> state, BoxShadows shadow) => new(x => state(x.OfType<Button>().Class("collection-card")).Template().OfType<Border>().Name(CardFacePart))
        { Setters = { new Setter(Border.BoxShadowProperty, shadow) } };
        Styles.Add(Face(x => x, PrimaryCardShadow));
        Styles.Add(Face(x => x.Class(":pointerover"), PrimaryCardHoverShadow));
    }

    // Listings
    internal void Refresh() => Feed.Refresh();

    internal string? Add(SurplusListing listing, int quantity = 1) => Feed.Add(listing, quantity);
}
