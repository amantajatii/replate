using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Replate.Domain.Entities;
using Path = Avalonia.Controls.Shapes.Path;

namespace Replate.Desktop;

public sealed class CollectionView : UserControl
{
    internal const string BackLabel = "Kembali ke Jelajah";
    internal const string SearchPrefix = "Hasil untuk ";
    internal const string CollectionEmpty = "Belum ada menu di sini";
    internal const string SearchEmpty = "Tidak ada menu yang cocok";
    internal const string TextLinkClass = "text-link";
    internal const double BackIconSize = 16;
    internal const double TextLinkMinHeight = 32;
    internal const double IntroSize = 16;
    internal const double IntroGap = 8;
    private static readonly Geometry BackIcon = Geometry.Parse("M13 8H3 M7.5 3.5L3 8L7.5 12.5");
    internal static readonly IReadOnlyDictionary<string, string> Intros = new Dictionary<string, string>
    {
        [MarketplaceView.Cheapest] = "Temukan berbagai makanan lezat, pas, dan ramah di kantong!",
        [MarketplaceView.EndingSoon] = "Nikmati hidangan favorit sebelum terlambat!",
        [MenuCategories.MainCourse] = "Temukan hidangan mengenyangkan untuk menemani harimu!",
        [MenuCategories.Bakery] = "Maniskan harimu dengan roti dan kue lezat yang menggugah selera!",
        [MenuCategories.Produce] = "Nikmati pilihan hidangan sehat yang lezat, segar, dan penuh kebaikan!"
    };

    internal string? Collection { get; }
    internal string? Query { get; }
    internal Button BackLink { get; }
    internal Path BackGlyph { get; }
    internal TextBlock Title { get; }
    internal Grid TitleRow { get; }
    internal TextBlock? Intro { get; }
    internal StackPanel Header { get; }
    internal StackPanel Body { get; }
    internal ScrollViewer Scroll { get; }
    internal ListingFeed Feed { get; }
    internal IReadOnlyList<SurplusListing> VisibleListings => Feed.Listings;

    private CollectionView(MainWindow owner, DemoSession session, string? collection, string? query, string title,
        Func<IEnumerable<SurplusListing>> source, string emptyTitle, string emptyMessage, string? intro = null)
    {
        Collection = collection; Query = query;
        Feed = new ListingFeed(owner, session, source, () => ListingFeed.EmptyState(emptyTitle, emptyMessage, owner.ShowMarketplace));
        AddTextLinkStyles(Styles);

        // Back link
        BackLink = CreateBackLink(owner.ShowMarketplace, out var glyph);
        BackGlyph = glyph;

        // Title
        Title = Ui.Text(title, MarketplaceView.TitleSize, Ui.Ink, true);
        Title.FontWeight = FontWeight.Bold; Title.TextWrapping = TextWrapping.NoWrap; Title.TextTrimming = TextTrimming.CharacterEllipsis;
        Title.HorizontalAlignment = HorizontalAlignment.Left;
        ToolTip.SetTip(Title, title);
        TitleRow = Ui.Columns("*,Auto", Title, Feed.Notice);
        Header = Ui.Stack(IntroGap, TitleRow);

        // Intro
        if (intro is not null)
        {
            Intro = Ui.Text(intro, IntroSize, Ui.Muted);
            Header.Children.Add(Intro);
            Feed.Refreshed += () => Intro.IsVisible = Feed.Listings.Count > 0;
        }

        // Layout
        Body = Ui.Stack(Ui.Spacing.Related, BackLink, Header, Feed.Panel);
        Body.Margin = new Thickness(Ui.Spacing.Page, Ui.Spacing.Section, Ui.Spacing.Page, Ui.Spacing.Page);
        Body.MaxWidth = MarketplaceView.ContentMaxWidth; Body.HorizontalAlignment = HorizontalAlignment.Stretch;
        Scroll = Ui.Scroll(Body);
        Content = Scroll;
    }

    internal static CollectionView ForCollection(MainWindow owner, DemoSession session, string name) =>
        new(owner, session, name, null, name, () => ListingQuery.Collection(session, name), CollectionEmpty, "Menu di koleksi ini sedang habis. Coba lagi nanti.",
            Intros[name]);

    internal static CollectionView ForSearch(MainWindow owner, DemoSession session, string query) =>
        new(owner, session, null, query, SearchPrefix + query, () => ListingQuery.Find(session, query), SearchEmpty, "Coba kata kunci lain.");

    internal void Refresh() => Feed.Refresh();

    // Text link
    internal static Button CreateBackLink(Action back, out Path glyph, string text = BackLabel)
    {
        glyph = new Path
        {
            Data = BackIcon, Width = BackIconSize, Height = BackIconSize, Stretch = Stretch.None, StrokeThickness = 1.75,
            StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round, VerticalAlignment = VerticalAlignment.Center
        };
        var link = CreateTextLink(text, back, glyph);
        link.Margin = new Thickness(-8, 0, 0, 0); link.HorizontalAlignment = HorizontalAlignment.Left;
        return link;
    }

    internal static Button CreateTextLink(string text, Action action, Path? glyph = null)
    {
        var label = new TextBlock { Text = text, FontSize = 14, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        var link = new Button
        {
            Content = glyph is null ? label : Ui.Row(8, glyph, label), Padding = new Thickness(8, 6),
            MinHeight = TextLinkMinHeight, VerticalContentAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(6), BorderThickness = default, Cursor = new Cursor(StandardCursorType.Hand)
        };
        link.Classes.Add(TextLinkClass);
        if (glyph is not null) glyph[!Shape.StrokeProperty] = link[!TemplatedControl.ForegroundProperty];
        AutomationProperties.SetName(link, text);
        link.Click += (_, _) => action();
        return link;
    }

    internal static void AddTextLinkStyles(Styles styles)
    {
        Style State(Func<Selector, Selector> state, IBrush foreground) =>
            new(x => state(x.OfType<Button>().Class(TextLinkClass)))
            {
                Setters =
                {
                    new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent), new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent),
                    new Setter(TemplatedControl.ForegroundProperty, foreground), new Setter(Visual.RenderTransformProperty, TransformOperations.Identity)
                }
            };
        Style Face(Func<Selector, Selector> state, IBrush foreground) =>
            new(x => state(x.OfType<Button>().Class(TextLinkClass)).Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
            {
                Setters =
                {
                    new Setter(ContentPresenter.BackgroundProperty, Brushes.Transparent), new Setter(ContentPresenter.BorderBrushProperty, Brushes.Transparent),
                    new Setter(ContentPresenter.ForegroundProperty, foreground)
                }
            };
        foreach (var (state, foreground) in new (Func<Selector, Selector>, IBrush)[]
        {
            (x => x, Ui.Green), (x => x.Class(":pointerover"), Ui.PrimaryHover), (x => x.Class(":pressed"), Ui.Green), (x => x.Class(":disabled"), Ui.Green)
        })
        {
            styles.Add(State(state, foreground));
            styles.Add(Face(state, foreground));
        }
    }
}

internal static class ListingQuery
{
    internal static IEnumerable<SurplusListing> Available(DemoSession session) => session.Search("", "Semua");

    internal static IEnumerable<SurplusListing> Collection(DemoSession session, string name) => name switch
    {
        MarketplaceView.Cheapest => Available(session).OrderBy(l => l.RescuePrice),
        MarketplaceView.EndingSoon => Available(session).OrderBy(l => l.PickupEnd),
        _ => Available(session).Where(l => l.Menu.Category.Equals(name, StringComparison.OrdinalIgnoreCase))
    };

    internal static string[] Terms(string query) => query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    internal static IEnumerable<SurplusListing> Find(DemoSession session, string query)
    {
        var terms = Terms(query);
        return Available(session).Where(l => terms.Length > 0 && terms.All(term =>
            new[] { l.Menu.Name, l.Menu.Description, l.Menu.Restaurant?.Name ?? "" }.Concat(MenuCategories.SearchTerms(l.Menu.Category))
                .Any(field => field.Contains(term, StringComparison.OrdinalIgnoreCase))));
    }
}
