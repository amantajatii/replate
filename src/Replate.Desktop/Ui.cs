using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Automation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CultureInfo = System.Globalization.CultureInfo;

namespace Replate.Desktop;

internal static class Ui
{
    // Palette
    private const string Primary700Hex = "1F6B4F";
    private const string TextPrimaryHex = "202522";
    public static readonly IBrush Green = Brush.Parse("#" + Primary700Hex);
    public static readonly IBrush PrimaryHover = Brush.Parse("#2F7D5F");
    public static readonly IBrush Primary500 = Brush.Parse("#59AC87");
    public static readonly IBrush Selection = Brush.Parse("#DDEDE5");
    public static readonly IBrush Accent = Brush.Parse("#F4A261");
    public static readonly IBrush Canvas = Brush.Parse("#FFFFFF");
    public static readonly IBrush Surface = Brushes.White;
    public static readonly IBrush Ink = Brush.Parse("#" + TextPrimaryHex);
    public static readonly IBrush Muted = Brush.Parse("#69736D");
    public static readonly IBrush MutedFaint = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.Parse("#69736D"), .6);
    public static readonly IBrush OnPrimary = Brushes.White;
    public static readonly IBrush DividerFill = Brush.Parse("#D7E4DF");
    public static readonly IBrush Sidebar = Brush.Parse("#EEF4F1");
    public static readonly IBrush Scrim = Brush.Parse("#66" + TextPrimaryHex);
    public static readonly IBrush WorkspaceFill = Brush.Parse("#FFFFFF");
    public static readonly IBrush CardBorder = DividerFill;
    public static readonly Thickness CardBorderThickness = new(1);
    public static readonly IBrush Danger = Brush.Parse("#B3261E");
    public static readonly IBrush DangerTint = Brush.Parse("#FBE4E1");
    public static readonly IBrush DangerPressed = Brush.Parse("#F6D3CE");
    public static readonly BoxShadows CardShadow = BoxShadows.Parse("0 8 24 0 #1F" + TextPrimaryHex);
    public static readonly BoxShadows PrimaryCardShadow = BoxShadows.Parse("0 6 18 0 #4D" + Primary700Hex);
    public static readonly BoxShadows PrimaryCardHoverShadow = BoxShadows.Parse("0 10 24 0 #59" + Primary700Hex);
    public static readonly FontFamily CodeFont = new("Cascadia Mono, Consolas, Menlo, monospace");

    // Logo
    public const string LogoName = "Replate";
    public static readonly Uri LogoGreenUri = new("avares://Replate.Desktop/Assets/logo-green.webp");
    public static readonly Uri LogoWhiteUri = new("avares://Replate.Desktop/Assets/logo-white.webp");
    private static readonly Lazy<Bitmap> GreenLogo = new(() => Load(LogoGreenUri));
    private static readonly Lazy<Bitmap> WhiteLogo = new(() => Load(LogoWhiteUri));
    public static Bitmap LogoGreen => GreenLogo.Value;
    public static Bitmap LogoWhite => WhiteLogo.Value;
    private static Bitmap Load(Uri uri) { using var stream = AssetLoader.Open(uri); return new Bitmap(stream); }
    public static Image Logo(Bitmap source, double height)
    {
        var logo = new Image { Source = source, Height = height, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
        AutomationProperties.SetName(logo, LogoName);
        return logo;
    }

    // Outline icons
    public const double OutlineGrid = 24;
    public const double EmptyIconSize = 80;
    public const double EmptyIconStroke = .08;
    public static readonly IBrush EmptyIconBrush = Green;
    public const string CartOutline =
        "M2 3.5H3.4C4.2 3.5 4.9 4.1 5.1 4.9L5.4 6.5 " +
        "M5.4 6.5H20.6C21.2 6.5 21.6 7 21.5 7.6L20.2 13.2C20.1 13.7 19.6 14 19.1 14H8.2C7.7 14 7.3 13.7 7.2 13.2Z " +
        "M6.4 10.25H20.9 M9.4 6.5L10.4 14 M13.4 6.5V14 M17.4 6.5L16.6 14 " +
        "M7.2 13.2L7.7 16.2C7.8 16.7 8.2 17 8.7 17H19.5 " +
        "M9.6 19.1A1.3 1.3 0 1 1 9.6 21.7A1.3 1.3 0 1 1 9.6 19.1Z M17.6 19.1A1.3 1.3 0 1 1 17.6 21.7A1.3 1.3 0 1 1 17.6 19.1Z";
    public const string ReceiptOutline =
        "M4 21.5V5.5C4 4.1 5.1 3 6.5 3H17.5C18.9 3 20 4.1 20 5.5V8H16 " +
        "M17.5 3C16.6 3 16 4 16 5.5V21.5L14 19L12 21.5L10 19L8 21.5L6 19L4 21.5 " +
        "M7.5 9H12.5 M7.5 12.5H12.5 M7.5 16H11";
    public static Avalonia.Controls.Shapes.Path OutlineIcon(string data, double size, double strokeRatio, IBrush? stroke)
    {
        var geometry = Geometry.Parse(data);
        geometry.Transform = new ScaleTransform(size / OutlineGrid, size / OutlineGrid);
        return new Avalonia.Controls.Shapes.Path
        {
            Data = geometry, Width = size, Height = size, Stretch = Stretch.None, Stroke = stroke, StrokeThickness = size * strokeRatio,
            StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round, Fill = null,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false
        };
    }
    public static Avalonia.Controls.Shapes.Path EmptyIcon(string data) => OutlineIcon(data, EmptyIconSize, EmptyIconStroke, EmptyIconBrush);

    // Spacing
    internal static class Spacing
    {
        public const double Related = 16;
        public const double Section = 24;
        public const double Page = 32;
    }

    // Focus
    public const double FocusRingWidth = 2;
    public const double FocusRingOffset = 2;
    public const string FocusRingPart = "PART_FocusRing";
    public const string OnPrimaryClass = "on-primary";
    public const string FocusFillClass = "focus-fill";
    public static readonly ITemplate<Control> NoFocusAdorner = new FuncTemplate<Control>(() => new Panel { IsHitTestVisible = false });
    public static Border FocusRing(CornerRadius radius)
    {
        var grow = FocusRingWidth + FocusRingOffset;
        return new Border
        {
            Name = FocusRingPart, BorderThickness = new Thickness(FocusRingWidth),
            Margin = new Thickness(-grow), IsHitTestVisible = false,
            CornerRadius = new CornerRadius(radius.TopLeft + grow, radius.TopRight + grow, radius.BottomRight + grow, radius.BottomLeft + grow)
        };
    }
    public static Panel FocusFrame(Control face, CornerRadius radius) => new() { Children = { face, FocusRing(radius) } };
    private static ContentPresenter PrimaryFill(Button button, ContentPresenter presenter)
    {
        if (button.Classes.Contains(PrimaryClass) || button.Classes.Contains(DangerClass) || button.Classes.Contains(CardActionClass))
            presenter.Bind(ContentPresenter.BackgroundProperty, button.GetObservable(TemplatedControl.BackgroundProperty), Avalonia.Data.BindingPriority.Template);
        else presenter[!ContentPresenter.BackgroundProperty] = button[!TemplatedControl.BackgroundProperty];
        return presenter;
    }
    private static readonly FuncControlTemplate<Button> RingedTemplate = new((button, _) => FocusFrame(PrimaryFill(button, new ContentPresenter
    {
        Name = "PART_ContentPresenter", RecognizesAccessKey = true,
        [!ContentPresenter.BorderBrushProperty] = button[!TemplatedControl.BorderBrushProperty],
        [!ContentPresenter.BorderThicknessProperty] = button[!TemplatedControl.BorderThicknessProperty],
        [!ContentPresenter.CornerRadiusProperty] = button[!TemplatedControl.CornerRadiusProperty],
        [!ContentPresenter.PaddingProperty] = button[!TemplatedControl.PaddingProperty],
        [!ContentPresenter.ContentProperty] = button[!ContentControl.ContentProperty],
        [!ContentPresenter.ContentTemplateProperty] = button[!ContentControl.ContentTemplateProperty],
        [!ContentPresenter.HorizontalContentAlignmentProperty] = button[!ContentControl.HorizontalContentAlignmentProperty],
        [!ContentPresenter.VerticalContentAlignmentProperty] = button[!ContentControl.VerticalContentAlignmentProperty]
    }), button.CornerRadius));
    public static Styles FocusStyles()
    {
        var fluent = Application.Current?.FindResource(typeof(Button)) as ControlTheme;
        var ringed = new ControlTheme(typeof(Button))
        {
            BasedOn = fluent,
            Setters =
            {
                new Setter(Visual.ClipToBoundsProperty, false),
                new Setter(TemplatedControl.TemplateProperty, RingedTemplate)
            }
        };
        Style Ring(Func<Selector?, Selector> selector, IBrush brush) =>
            new(x => selector(x).Template().OfType<Border>().Name(FocusRingPart)) { Setters = { new Setter(Border.BorderBrushProperty, brush) } };
        return new Styles
        {
            new Style(x => x.Is<Control>()) { Setters = { new Setter(Control.FocusAdornerProperty, NoFocusAdorner) } },
            new Style(x => x.OfType<Button>()) { Setters = { new Setter(StyledElement.ThemeProperty, ringed) } },
            Ring(x => x!.OfType<Button>().Class(":focus-visible"), Green),
            Ring(x => x!.OfType<Button>().Class(OnPrimaryClass).Class(":focus-visible"), OnPrimary),
            Ring(x => x!.OfType<Button>().Class(FocusFillClass).Class(":focus-visible"), Brushes.Transparent),
            new Style(x => x.OfType<Button>().Class(FocusFillClass).Class(":focus-visible"))
            {
                Setters =
                {
                    new Setter(TemplatedControl.BackgroundProperty, Selection),
                    new Setter(TemplatedControl.ForegroundProperty, Green)
                }
            },
            new Style(x => x.OfType<ComboBox>().Class(":focus-visible"))
            {
                Setters = { new Setter(TemplatedControl.BorderBrushProperty, Green) }
            }
        };
    }

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
        if (primary) button.Classes.AddRange([PrimaryClass, CompactClass]);
        button.Click += (_, _) => action(); return button;
    }
    public static Button DangerButton(string text, Action action)
    {
        var button = Button(text, action);
        button.Background = Danger; button.Foreground = OnPrimary;
        button.Classes.AddRange([DangerClass, CompactClass]);
        return button;
    }
    public static Button Round(Button button, double size, bool icon = false)
    {
        button.Classes.Remove(CompactClass);
        button.Classes.Add(icon ? IconClass : RoundClass);
        button.Width = button.Height = size; button.Padding = default; button.CornerRadius = new CornerRadius(size / 2);
        button.HorizontalContentAlignment = HorizontalAlignment.Center; button.VerticalContentAlignment = VerticalAlignment.Center;
        if (icon) { button.ClearValue(TemplatedControl.BackgroundProperty); button.ClearValue(TemplatedControl.ForegroundProperty); }
        return button;
    }
    public static Button FullWidth(Button button)
    {
        button.Classes.Remove(CompactClass);
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        return button;
    }

    // Pill
    public const double PillHeight = 36;
    public const double PillGap = 8;
    public const string PillSelectedClass = "selected";
    public readonly record struct PillFace(IBrush Background, IBrush Border, IBrush Foreground, FontWeight Weight);
    public sealed record PillPalette(PillFace Rest, PillFace Hover, PillFace Pressed, PillFace Selected, PillFace SelectedHover, PillFace SelectedPressed);
    public static readonly PillPalette QuietPills = new(
        new(Brushes.Transparent, Brushes.Transparent, Muted, FontWeight.Normal),
        new(Sidebar, Brushes.Transparent, Ink, FontWeight.Normal),
        new(DividerFill, Brushes.Transparent, Ink, FontWeight.Normal),
        new(Selection, Brushes.Transparent, Green, FontWeight.SemiBold),
        new(Selection, Brushes.Transparent, Green, FontWeight.SemiBold),
        new(DividerFill, Brushes.Transparent, Green, FontWeight.SemiBold));
    public static readonly PillPalette SolidPills = new(
        new(Surface, DividerFill, Ink, FontWeight.Normal),
        new(Selection, Selection, Green, FontWeight.Normal),
        new(DividerFill, DividerFill, Green, FontWeight.Normal),
        new(Green, Green, OnPrimary, FontWeight.SemiBold),
        new(Green, Green, OnPrimary, FontWeight.SemiBold),
        new(Green, Green, OnPrimary, FontWeight.SemiBold));
    public static Button Pill(string label, string kind, double border, Action select)
    {
        var button = new Button
        {
            Content = label, Height = PillHeight, Padding = new Thickness(Spacing.Related, 0), FontSize = 14,
            CornerRadius = new CornerRadius(PillHeight / 2), BorderThickness = new Thickness(border), VerticalContentAlignment = VerticalAlignment.Center,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        button.Classes.Add(kind);
        AutomationProperties.SetName(button, label);
        button.Click += (_, _) => select();
        return button;
    }
    public static Styles PillStyles(string kind, PillPalette palette)
    {
        Style State(Func<Selector, Selector> state, PillFace face) =>
            new(x => state(x.OfType<Button>().Class(kind)))
            {
                Setters =
                {
                    new Setter(TemplatedControl.BackgroundProperty, face.Background), new Setter(TemplatedControl.BorderBrushProperty, face.Border),
                    new Setter(TemplatedControl.ForegroundProperty, face.Foreground), new Setter(TemplatedControl.FontWeightProperty, face.Weight),
                    new Setter(Visual.RenderTransformProperty, TransformOperations.Identity)
                }
            };
        Style Face(Func<Selector, Selector> state, PillFace face) =>
            new(x => state(x.OfType<Button>().Class(kind)).Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
            {
                Setters =
                {
                    new Setter(ContentPresenter.BackgroundProperty, face.Background), new Setter(ContentPresenter.BorderBrushProperty, face.Border),
                    new Setter(ContentPresenter.ForegroundProperty, face.Foreground)
                }
            };
        var styles = new Styles();
        foreach (var (state, face) in new (Func<Selector, Selector>, PillFace)[]
        {
            (x => x, palette.Rest),
            (x => x.Class(":pointerover"), palette.Hover),
            (x => x.Class(":pressed"), palette.Pressed),
            (x => x.Class(PillSelectedClass), palette.Selected),
            (x => x.Class(PillSelectedClass).Class(":pointerover"), palette.SelectedHover),
            (x => x.Class(PillSelectedClass).Class(":pressed"), palette.SelectedPressed)
        })
        {
            styles.Add(State(state, face));
            styles.Add(Face(state, face));
        }
        return styles;
    }

    // Primary button
    public const string PrimaryClass = "primary";
    public const string CompactClass = "compact";
    public const string RoundClass = "round";
    public const string IconClass = "icon";
    public const string DangerClass = "danger";
    public const double PrimaryHoverScale = 1.03;
    public static readonly TimeSpan PrimaryHoverDuration = TimeSpan.FromMilliseconds(120);
    public static readonly BoxShadows PrimaryButtonHoverShadow = BoxShadows.Parse("0 4 12 0 #4D" + Primary700Hex);
    public static readonly BoxShadows DangerButtonHoverShadow = BoxShadows.Parse("0 4 12 0 #4DB3261E");
    public static void UsePrimaryButtons(Application app) { app.Styles.Add(PrimaryStyles()); app.Styles.Add(IconStyles()); }
    private static Styles PrimaryStyles()
    {
        var ease = new CubicEaseOut();
        Style Face(string kind, Func<Selector, Selector> state, object background, object foreground, BoxShadows shadow) =>
            new(x => state(x.OfType<Button>().Class(kind).Not(y => y.Class(":disabled"))).Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
            {
                Setters =
                {
                    new Setter(ContentPresenter.BackgroundProperty, background),
                    new Setter(ContentPresenter.ForegroundProperty, foreground),
                    new Setter(ContentPresenter.BoxShadowProperty, shadow)
                }
            };
        Style Scale(string kind, Func<Selector, Selector> state, double scale, int z) =>
            new(x => state(x.OfType<Button>().Class(kind)))
            {
                Setters =
                {
                    new Setter(Visual.RenderTransformProperty, TransformOperations.Parse($"scale({scale.ToString(System.Globalization.CultureInfo.InvariantCulture)})")),
                    new Setter(Visual.ZIndexProperty, z)
                }
            };
        Style[] Motion(string kind) =>
        [
            new Style(x => x.OfType<Button>().Class(kind).Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
            {
                Setters =
                {
                    new Setter(Animatable.TransitionsProperty, new Transitions
                    {
                        new BrushTransition { Property = ContentPresenter.BackgroundProperty, Duration = PrimaryHoverDuration, Easing = ease },
                        new BrushTransition { Property = ContentPresenter.ForegroundProperty, Duration = PrimaryHoverDuration, Easing = ease },
                        new BoxShadowsTransition { Property = ContentPresenter.BoxShadowProperty, Duration = PrimaryHoverDuration, Easing = ease }
                    })
                }
            },
            new Style(x => x.OfType<Button>().Class(kind).Class(CompactClass))
            {
                Setters =
                {
                    new Setter(Visual.RenderTransformOriginProperty, RelativePoint.Center),
                    new Setter(Animatable.TransitionsProperty, new Transitions
                    {
                        new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = PrimaryHoverDuration, Easing = ease }
                    })
                }
            }
        ];
        Style[] States(string kind, object rest, object restText, IBrush hover, IBrush pressed, IBrush text, BoxShadows shadow) =>
        [
            Face(kind, x => x, rest, restText, default),
            Face(kind, x => x.Class(":pointerover"), hover, text, shadow),
            Face(kind, x => x.Class(":pressed"), pressed, text, shadow),
            Face(kind, x => x.Class(":pressed").Not(y => y.Class(":pointerover")), pressed, text, default)
        ];
        Style[] Rest(string kind) =>
        [
            new Style(x => x.OfType<Button>().Class(kind).Class(":disabled").Template().OfType<Panel>().Child().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
            {
                Setters = { new Setter(ContentPresenter.BackgroundProperty, new Avalonia.Data.TemplateBinding(TemplatedControl.BackgroundProperty)) }
            },
            Scale(kind, x => x, 1, 0),
            Scale(kind, x => x.Class(CompactClass).Class(":pointerover").Not(y => y.Class(":disabled")), PrimaryHoverScale, 1)
        ];
        var styles = new Styles();
        // Primary
        styles.AddRange(Motion(PrimaryClass));
        styles.AddRange(States(PrimaryClass, Green, OnPrimary, Selection, DividerFill, Green, PrimaryButtonHoverShadow));
        styles.Add(Face(PrimaryClass, x => x.Class(RoundClass).Class(":pointerover"), Selection, Green, default));
        styles.Add(Face(PrimaryClass, x => x.Class(RoundClass).Class(":pressed"), DividerFill, Green, default));
        styles.AddRange(Rest(PrimaryClass));
        // Danger
        styles.AddRange(Motion(DangerClass));
        styles.AddRange(States(DangerClass, Danger, OnPrimary, DangerTint, DangerPressed, Danger, DangerButtonHoverShadow));
        styles.AddRange(Rest(DangerClass));
        // Card action
        foreach (var (kind, hover, pressed, text, shadow) in new[]
        {
            (CardActionTonalClass, Green, Green, OnPrimary, PrimaryButtonHoverShadow),
            (CardActionNeutralClass, Selection, DividerFill, Green, PrimaryButtonHoverShadow),
            (CardActionDangerClass, DangerTint, DangerPressed, Danger, DangerButtonHoverShadow)
        })
        {
            styles.AddRange(Motion(kind));
            styles.AddRange(States(kind, new Avalonia.Data.TemplateBinding(TemplatedControl.BackgroundProperty),
                new Avalonia.Data.TemplateBinding(TemplatedControl.ForegroundProperty), hover, pressed, text, shadow));
            styles.AddRange(Rest(kind));
        }
        return styles;
    }

    // Card action
    public const string CardActionClass = "card-action";
    public const string CardActionTonalClass = "card-action-tonal";
    public const string CardActionNeutralClass = "card-action-neutral";
    public const string CardActionDangerClass = "card-action-danger";

    // Icon button
    private static Styles IconStyles()
    {
        Style State(Func<Selector, Selector> state, IBrush background, IBrush foreground) =>
            new(x => state(x.OfType<Button>().Class(IconClass)))
            {
                Setters = { new Setter(TemplatedControl.BackgroundProperty, background), new Setter(TemplatedControl.ForegroundProperty, foreground) }
            };
        return new Styles
        {
            State(x => x, Brushes.Transparent, Ink),
            State(x => x.Class(":pointerover"), Selection, Green),
            State(x => x.Class(":pressed"), DividerFill, Green)
        };
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
    public static TextBlock FieldError() => Text("", 14, Danger);
    public static Border Divider() => new() { Height = 1, Background = DividerFill };

    public static ScrollViewer Scroll(Control body) => new() { Content = body,
        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    public static Control Page(params Control[] children)
    {
        var body = Stack(24, children); body.Margin = new Thickness(32);
        return Scroll(body);
    }
    public static Control Heading(string title, string subtitle, Control? action = null)
    {
        var heading = Text(title, MarketplaceView.TitleSize);
        heading.FontWeight = FontWeight.Bold;
        return Columns("*,Auto", Stack(6, heading, Text(subtitle, 14, Muted)), action ?? new Border());
    }
    public static StackPanel Field(string label, Control input)
    {
        Avalonia.Automation.AutomationProperties.SetName(input, label);
        input.HorizontalAlignment = input is DatePicker or TimePicker ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        return Stack(8, Text(label, 14, bold: true), input);
    }
    public static readonly Thickness InputPadding = new(10, 5.5, 6, 5.5);
    public static readonly Thickness MultilinePadding = new(10, 10, 6, 5.5);
    public static void UseInputs(Application app)
    {
        app.Styles.Add(InputStyles());
        app.Styles.Add(FieldStyles());
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<TextBox>((_, e) =>
        {
            if (e.NameScope.Find<TextBlock>("PART_Placeholder") is { } placeholder) placeholder.Opacity = 1;
        });
    }
    private static Styles InputStyles() => new()
    {
        new Style(x => x.OfType<TextBox>())
        {
            Setters =
            {
                new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                new Setter(TextBox.PaddingProperty, InputPadding),
                new Setter(TextBox.PlaceholderForegroundProperty, Muted)
            }
        },
        new Style(x => x.OfType<TextBox>().PropertyEquals(TextBox.AcceptsReturnProperty, true))
        {
            Setters =
            {
                new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Top),
                new Setter(TextBox.PaddingProperty, MultilinePadding)
            }
        },
        new Style(x => x.OfType<NumericUpDown>())
        {
            Setters =
            {
                new Setter(NumericUpDown.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                new Setter(NumericUpDown.PaddingProperty, InputPadding),
                new Setter(NumericUpDown.PlaceholderForegroundProperty, Muted)
            }
        }
    };
    public static TextBox Input(string placeholder, string value = "", bool multiline = false) => new()
    {
        PlaceholderText = placeholder, Text = value, AcceptsReturn = multiline, MinHeight = multiline ? 88 : 42,
        TextWrapping = TextWrapping.Wrap, CornerRadius = new CornerRadius(6), MaxLength = multiline ? 500 : 80
    };
    public static NumericUpDown Number(string placeholder, decimal value = 0, decimal max = 1000000, decimal increment = 1) => new()
    { PlaceholderText = placeholder, Value = value, Minimum = 0, Maximum = max, Increment = increment, FormatString = increment < 1 ? "0.000" : "0", MinHeight = 42,
        ParsingNumberStyle = increment < 1 ? System.Globalization.NumberStyles.Number : System.Globalization.NumberStyles.Integer };
    public static Border TableRow(string columns, bool header, params Control[] cells)
    {
        var grid = Columns(columns, cells); grid.MinHeight = header ? 40 : 64;
        for (var i = 0; i < cells.Length; i++)
        {
            cells[i].Margin = new Thickness(i == 0 ? 0 : 16, 10, 0, 10);
            cells[i].VerticalAlignment = VerticalAlignment.Center;
        }
        return new Border { Child = grid, BorderBrush = DividerFill, BorderThickness = new Thickness(0, 0, 0, 1) };
    }

    // Field
    public const string FieldClass = "field";
    public const string BareClass = "bare";
    public const string OpenClass = "open";
    public const string InvalidClass = "invalid";
    public const double FieldHeight = 48;
    public const double FieldRadius = 12;
    public const double FieldInset = 16;
    public const double FieldGap = 8;
    public const double FieldIconSize = 20;
    public const double FieldIconStroke = 1.75 / OutlineGrid;
    public const double FieldDropOffset = 4;
    public const double FieldDropHeight = 280;
    public const double FormCardRadius = 16;
    public static readonly Thickness FieldPadding = new(FieldInset, 0);
    public static readonly IBrush FieldBorder = DividerFill;
    public static readonly IBrush FieldHoverBorder = Primary500;
    public static readonly IBrush FieldFocusBorder = Green;
    public static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");
    public const string DateFormat = "d MMMM yyyy";
    public const string ChevronDown = "M6 9L12 15L18 9";
    public const string CalendarOutline =
        "M6 5H18C19.4 5 20.5 6.1 20.5 7.5V18C20.5 19.4 19.4 20.5 18 20.5H6C4.6 20.5 3.5 19.4 3.5 18V7.5C3.5 6.1 4.6 5 6 5Z " +
        "M3.5 10H20.5 M8 3V7 M16 3V7";
    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);
    public static Border FormCard(Control content) => new()
    {
        Child = content, Background = Surface, BorderBrush = CardBorder, BorderThickness = CardBorderThickness,
        CornerRadius = new CornerRadius(FormCardRadius), Padding = new Thickness(Spacing.Section)
    };
    public static FieldFrame TextField(TextBox input)
    {
        input.Classes.Add(BareClass); input.FocusAdorner = null;
        return new FieldFrame(input);
    }
    public static FieldFrame NumberField(NumericUpDown input)
    {
        input.Classes.Add(BareClass); input.FocusAdorner = null;
        return new FieldFrame(input);
    }
    public static FieldFrame SelectField(ComboBox input)
    {
        input.Theme = SelectTheme; input.ItemContainerTheme = OptionTheme; input.FocusAdorner = null;
        var frame = new FieldFrame(input);
        input.DropDownOpened += (_, _) => frame.Face.Classes.Set(OpenClass, true);
        input.DropDownClosed += (_, _) => frame.Face.Classes.Set(OpenClass, false);
        return frame;
    }
    public const double CompactFieldHeight = 44;
    public const double CompactSelectMinWidth = 180;
    public static FieldFrame CompactSelectField(ComboBox input)
    {
        var frame = SelectField(input);
        frame.Face.Height = CompactFieldHeight;
        frame.HorizontalAlignment = HorizontalAlignment.Left;
        void Fit()
        {
            var longest = input.Items.Select(item =>
            {
                var text = new TextBlock { Text = item?.ToString(), FontSize = input.FontSize, FontFamily = input.FontFamily, FontWeight = FontWeight.SemiBold };
                text.Measure(Size.Infinity);
                return text.DesiredSize.Width;
            }).DefaultIfEmpty(0).Max();
            frame.MinWidth = Math.Max(CompactSelectMinWidth, Math.Ceiling(longest + 2 * FieldInset + FieldGap + FieldIconSize + 2));
        }
        frame.AttachedToVisualTree += (_, _) => Fit();
        Fit();
        return frame;
    }
    private static Styles FieldStyles()
    {
        Style Set(Func<Selector?, Selector> selector, params Setter[] setters)
        {
            var style = new Style(selector);
            foreach (var setter in setters) style.Setters.Add(setter);
            return style;
        }
        Setter[] Plain() =>
        [
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent), new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent),
            new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)), new Setter(Layoutable.MinHeightProperty, 0d),
            new Setter(Control.FocusAdornerProperty, null)
        ];
        Setter[] PlainBorder() =>
        [
            new Setter(Border.BackgroundProperty, Brushes.Transparent), new Setter(Border.BorderBrushProperty, Brushes.Transparent),
            new Setter(Border.BorderThicknessProperty, new Thickness(0))
        ];
        Setter[] Fill() =>
        [
            new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Stretch),
            new Setter(TemplatedControl.PaddingProperty, FieldPadding)
        ];
        Selector Face(Selector? x) => x.OfType<Border>().Class(FieldClass);
        Selector Number(Selector? x) => x.OfType<NumericUpDown>().Class(BareClass);
        return new Styles
        {
            Set(x => Face(x),
                new Setter(Border.BackgroundProperty, Surface), new Setter(Border.BorderBrushProperty, FieldBorder),
                new Setter(Border.BorderThicknessProperty, new Thickness(1)), new Setter(Border.CornerRadiusProperty, new CornerRadius(FieldRadius)),
                new Setter(Layoutable.HeightProperty, FieldHeight)),
            Set(x => Face(x).Class(":pointerover"), new Setter(Border.BorderBrushProperty, FieldHoverBorder)),
            Set(x => Face(x).Class(":focus-within"), new Setter(Border.BorderBrushProperty, FieldFocusBorder)),
            Set(x => Face(x).Class(OpenClass), new Setter(Border.BorderBrushProperty, FieldFocusBorder)),
            Set(x => Face(x).Class(InvalidClass), new Setter(Border.BorderBrushProperty, Danger)),
            Set(x => x.OfType<TextBox>().Class(BareClass), [.. Plain(), .. Fill(), new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center)]),
            Set(x => x.OfType<TextBox>().Class(BareClass).Template().OfType<Border>().Name("PART_BorderElement"), PlainBorder()),
            Set(x => Number(x), [.. Plain(), .. Fill(), new Setter(NumericUpDown.ShowButtonSpinnerProperty, false),
                new Setter(NumericUpDown.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                new Setter(NumericUpDown.VerticalContentAlignmentProperty, VerticalAlignment.Center)]),
            Set(x => Number(x).Template().OfType<ButtonSpinner>(), [.. Plain(), new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Stretch)]),
            Set(x => Number(x).Template().OfType<ButtonSpinner>().Template().OfType<Border>(), PlainBorder()),
            Set(x => Number(x).Template().OfType<TextBox>(), [.. Plain(), .. Fill(),
                new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                new Setter(TextBox.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                new Setter(TextBox.TextAlignmentProperty, TextAlignment.Center)]),
            Set(x => Number(x).Template().OfType<TextBox>().Template().OfType<Border>().Name("PART_BorderElement"), PlainBorder())
        };
    }

    // Select field
    private static ControlTheme? selectTheme;
    private static ControlTheme? optionTheme;
    public static ControlTheme SelectTheme => selectTheme ??= new ControlTheme(typeof(ComboBox))
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(TemplatedControl.ForegroundProperty, Ink),
            new Setter(TemplatedControl.PaddingProperty, FieldPadding),
            new Setter(Layoutable.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
            new Setter(Layoutable.VerticalAlignmentProperty, VerticalAlignment.Stretch),
            new Setter(ComboBox.PlaceholderForegroundProperty, Muted),
            new Setter(ComboBox.MaxDropDownHeightProperty, FieldDropHeight),
            new Setter(InputElement.CursorProperty, HandCursor),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ComboBox>(SelectTemplate))
        }
    };
    private static Control SelectTemplate(ComboBox combo, INameScope scope)
    {
        var shown = new ContentPresenter
        {
            [!ContentPresenter.ContentProperty] = combo[!ComboBox.SelectionBoxItemProperty],
            [!ContentPresenter.ContentTemplateProperty] = combo[!ItemsControl.ItemTemplateProperty],
            VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false
        };
        var placeholder = new TextBlock
        {
            [!TextBlock.TextProperty] = combo[!ComboBox.PlaceholderTextProperty],
            [!TextBlock.ForegroundProperty] = combo[!ComboBox.PlaceholderForegroundProperty],
            VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false
        };
        placeholder.Bind(Visual.IsVisibleProperty, combo.GetObservable(ComboBox.SelectionBoxItemProperty, item => item is null));
        var chevron = OutlineIcon(ChevronDown, FieldIconSize, FieldIconStroke, Muted);
        chevron.Margin = new Thickness(FieldGap, 0, 0, 0);
        var face = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("*,Auto"), Background = Brushes.Transparent,
            [!Layoutable.MarginProperty] = combo[!TemplatedControl.PaddingProperty],
            Children = { new Panel { Children = { placeholder, shown } }, chevron }
        };
        Grid.SetColumn(chevron, 1);
        var items = new ItemsPresenter { Name = "PART_ItemsPresenter", [!ItemsPresenter.ItemsPanelProperty] = combo[!ItemsControl.ItemsPanelProperty] };
        var sheet = new Border
        {
            Background = Surface, BorderBrush = FieldBorder, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(FieldRadius), Padding = new Thickness(4),
            Child = new ScrollViewer
            {
                Content = items, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                [!Layoutable.MaxHeightProperty] = combo[!ComboBox.MaxDropDownHeightProperty]
            }
        };
        combo.SizeChanged += (_, e) => sheet.MinWidth = e.NewSize.Width + 2;
        var popup = new Popup
        {
            Name = "PART_Popup", Child = sheet, Placement = PlacementMode.BottomEdgeAlignedLeft,
            HorizontalOffset = -1, VerticalOffset = FieldDropOffset + 1, IsLightDismissEnabled = true, WindowManagerAddShadowHint = false,
            [!!Popup.IsOpenProperty] = combo[!!ComboBox.IsDropDownOpenProperty]
        };
        var root = new Grid { Background = Brushes.Transparent, Children = { face, popup } };
        popup.PlacementTarget = root;
        popup.RegisterInNameScope(scope);
        items.RegisterInNameScope(scope);
        return root;
    }
    public static ControlTheme OptionTheme => optionTheme ??= new ControlTheme(typeof(ComboBoxItem))
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(TemplatedControl.ForegroundProperty, Ink),
            new Setter(TemplatedControl.PaddingProperty, new Thickness(12, 10)),
            new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(FieldGap)),
            new Setter(TemplatedControl.FontWeightProperty, FontWeight.Normal),
            new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch),
            new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center),
            new Setter(InputElement.CursorProperty, HandCursor),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ComboBoxItem>((item, _) => new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [!ContentPresenter.BackgroundProperty] = item[!TemplatedControl.BackgroundProperty],
                [!ContentPresenter.CornerRadiusProperty] = item[!TemplatedControl.CornerRadiusProperty],
                [!ContentPresenter.PaddingProperty] = item[!TemplatedControl.PaddingProperty],
                [!ContentPresenter.ContentProperty] = item[!ContentControl.ContentProperty],
                [!ContentPresenter.ContentTemplateProperty] = item[!ContentControl.ContentTemplateProperty],
                [!ContentPresenter.HorizontalContentAlignmentProperty] = item[!ContentControl.HorizontalContentAlignmentProperty],
                [!ContentPresenter.VerticalContentAlignmentProperty] = item[!ContentControl.VerticalContentAlignmentProperty]
            }))
        },
        Children =
        {
            new Style(x => x.Nesting().Class(":pointerover")) { Setters = { new Setter(TemplatedControl.BackgroundProperty, Selection), new Setter(TemplatedControl.ForegroundProperty, Green) } },
            new Style(x => x.Nesting().Class(":focus-visible")) { Setters = { new Setter(TemplatedControl.BackgroundProperty, Selection), new Setter(TemplatedControl.ForegroundProperty, Green) } },
            new Style(x => x.Nesting().Class(":pressed")) { Setters = { new Setter(TemplatedControl.BackgroundProperty, DividerFill), new Setter(TemplatedControl.ForegroundProperty, Green) } },
            new Style(x => x.Nesting().Class(":selected")) { Setters = { new Setter(TemplatedControl.FontWeightProperty, FontWeight.SemiBold) } }
        }
    };

    // Calendar
    public const double CalendarDaySize = 36;
    public const double CalendarNavSize = 32;
    public const string CalendarHeaderPart = "PART_HeaderButton";
    public static readonly string[] CalendarNavParts = ["PART_PreviousButton", "PART_NextButton"];
    public static readonly string[] DayNames = ["Min", "Sen", "Sel", "Rab", "Kam", "Jum", "Sab"];
    public static readonly string[] MonthNames = ["Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agu", "Sep", "Okt", "Nov", "Des"];
    public const string NowClass = "now";
    private static ControlTheme? dayTheme;
    private static ControlTheme? monthTheme;
    private static ControlTheme? headerTheme;
    public static ControlTheme HeaderTheme => headerTheme ??= new ControlTheme(typeof(Button))
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(TemplatedControl.ForegroundProperty, Ink),
            new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent),
            new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
            new Setter(TemplatedControl.PaddingProperty, new Thickness(FieldGap, 0)),
            new Setter(TemplatedControl.FontSizeProperty, 14d),
            new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
            new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center),
            new Setter(InputElement.CursorProperty, HandCursor),
            new Setter(Visual.ClipToBoundsProperty, false),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, RingedTemplate)
        }
    };
    private static readonly FuncDataTemplate<object?> MonthHeader = new((value, _) => new TextBlock { Text = HeaderText(value), VerticalAlignment = VerticalAlignment.Center });
    public static string HeaderText(object? content)
    {
        var shown = content?.ToString() ?? "";
        var culture = CultureInfo.CurrentCulture;
        return DateTime.TryParseExact(shown, culture.DateTimeFormat.YearMonthPattern, culture, System.Globalization.DateTimeStyles.None, out var month)
            ? month.ToString("MMMM yyyy", Indonesian) : shown;
    }
    private static readonly FuncTemplate<Control> DayTitle = new(() =>
    {
        var title = new TextBlock { FontSize = 12, Foreground = Muted, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        void Name() => title.Text = DayNames[(Grid.GetColumn(title) + (int)(title.FindAncestorOfType<Calendar>()?.FirstDayOfWeek ?? DayOfWeek.Monday)) % DayNames.Length];
        title.PropertyChanged += (_, e) => { if (e.Property == Grid.ColumnProperty) Name(); };
        title.AttachedToVisualTree += (_, _) => Name();
        Name();
        return title;
    });
    public static void UseCalendar(Calendar calendar)
    {
        calendar.Background = Surface; calendar.HeaderBackground = Surface;
        calendar.BorderBrush = Brushes.Transparent; calendar.BorderThickness = new Thickness(0);
        calendar.TemplateApplied += (_, _) =>
        {
            foreach (var item in calendar.GetVisualDescendants().OfType<CalendarItem>())
            {
                item.DayTitleTemplate = DayTitle;
                item.TemplateApplied -= CalendarItemApplied;
                item.TemplateApplied += CalendarItemApplied;
            }
        };
    }
    private static void CalendarItemApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (e.NameScope.Find<Border>("BackgroundLayer") is { } layer) layer.Background = Surface;
        foreach (var name in CalendarNavParts.Append(CalendarHeaderPart))
        {
            if (e.NameScope.Find<Button>(name) is not { } button) continue;
            button.Classes.Clear();
            if (name == CalendarHeaderPart) button.ContentTemplate = MonthHeader;
            else Round(button, CalendarNavSize, icon: true);
            button.Theme = HeaderTheme;
        }
    }
    public static object? MonthText(object? content)
    {
        var index = content is string text ? Array.IndexOf(CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames, text) : -1;
        if (index < 0 && content is string name) index = Array.IndexOf(Indonesian.DateTimeFormat.AbbreviatedMonthNames, name);
        return index is >= 0 and < 12 ? MonthNames[index] : content;
    }
    private static void MonthLabel(Button cell, ContentPresenter presenter)
    {
        void Sync()
        {
            var shown = MonthText(cell.Content);
            presenter.Content = shown;
            var month = !Equals(shown, cell.Content) || MonthNames.Contains(shown);
            var today = DateTime.Today;
            cell.Classes.Set(NowClass, cell.DataContext is DateTime date && date.Year == today.Year && (!month || date.Month == today.Month));
        }
        cell.PropertyChanged += (_, e) => { if (e.Property == ContentControl.ContentProperty || e.Property == StyledElement.DataContextProperty) Sync(); };
        Sync();
    }
    private static ControlTheme CellTheme(Type type, double width, double height, double radius, string focused, bool month = false) => new(type)
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(TemplatedControl.ForegroundProperty, Ink),
            new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent),
            new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
            new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(radius)),
            new Setter(TemplatedControl.FontSizeProperty, 14d),
            new Setter(TemplatedControl.FontWeightProperty, FontWeight.Normal),
            new Setter(Layoutable.WidthProperty, width),
            new Setter(Layoutable.HeightProperty, height),
            new Setter(Layoutable.MarginProperty, new Thickness(1)),
            new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
            new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center),
            new Setter(InputElement.CursorProperty, HandCursor),
            new Setter(Visual.ClipToBoundsProperty, false),
            new Setter(Control.FocusAdornerProperty, null),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Button>((cell, _) =>
            {
                var presenter = new ContentPresenter
                {
                Name = "PART_ContentPresenter",
                [!ContentPresenter.BackgroundProperty] = cell[!TemplatedControl.BackgroundProperty],
                [!ContentPresenter.BorderBrushProperty] = cell[!TemplatedControl.BorderBrushProperty],
                [!ContentPresenter.BorderThicknessProperty] = cell[!TemplatedControl.BorderThicknessProperty],
                [!ContentPresenter.CornerRadiusProperty] = cell[!TemplatedControl.CornerRadiusProperty],
                [!ContentPresenter.ContentTemplateProperty] = cell[!ContentControl.ContentTemplateProperty],
                [!ContentPresenter.HorizontalContentAlignmentProperty] = cell[!ContentControl.HorizontalContentAlignmentProperty],
                [!ContentPresenter.VerticalContentAlignmentProperty] = cell[!ContentControl.VerticalContentAlignmentProperty]
                };
                if (month) MonthLabel(cell, presenter);
                else presenter[!ContentPresenter.ContentProperty] = cell[!ContentControl.ContentProperty];
                return presenter;
            }))
        },
        Children =
        {
            new Style(x => x.Nesting().Class(":inactive")) { Setters = { new Setter(TemplatedControl.ForegroundProperty, MutedFaint) } },
            new Style(x => x.Nesting().Class(":pointerover"))
            {
                Setters =
                {
                    new Setter(TemplatedControl.BackgroundProperty, Selection),
                    new Setter(TemplatedControl.ForegroundProperty, Green), new Setter(TemplatedControl.FontWeightProperty, FontWeight.Medium)
                }
            },
            new Style(x => x.Nesting().Class(":pressed"))
            {
                Setters =
                {
                    new Setter(TemplatedControl.BackgroundProperty, DividerFill),
                    new Setter(TemplatedControl.ForegroundProperty, Green), new Setter(TemplatedControl.FontWeightProperty, FontWeight.Medium)
                }
            },
            new Style(x => Selectors.Or(x.Nesting().Class(":today"), x.Nesting().Class(NowClass)))
            {
                Setters =
                {
                    new Setter(TemplatedControl.BorderBrushProperty, Green), new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
                    new Setter(TemplatedControl.ForegroundProperty, Green), new Setter(TemplatedControl.FontWeightProperty, FontWeight.SemiBold)
                }
            },
            new Style(x => x.Nesting().Class(":selected"))
            {
                Setters =
                {
                    new Setter(TemplatedControl.BackgroundProperty, Green), new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent),
                    new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
                    new Setter(TemplatedControl.ForegroundProperty, OnPrimary), new Setter(TemplatedControl.FontWeightProperty, FontWeight.SemiBold)
                }
            },
            new Style(x => x.Nesting().Class(":disabled"))
            {
                Setters =
                {
                    new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent), new Setter(TemplatedControl.BorderBrushProperty, Brushes.Transparent),
                    new Setter(TemplatedControl.ForegroundProperty, Muted), new Setter(TemplatedControl.FontWeightProperty, FontWeight.Normal)
                }
            },
            new Style(x => x.Nesting().Class(focused).Class(":focus-visible"))
            {
                Setters = { new Setter(TemplatedControl.BorderBrushProperty, Green), new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)) }
            }
        }
    };
    public static ControlTheme DayTheme => dayTheme ??= CellTheme(typeof(CalendarDayButton), CalendarDaySize, CalendarDaySize, CalendarDaySize / 2, ":dayfocused");
    public static ControlTheme MonthTheme => monthTheme ??= CellTheme(typeof(CalendarButton), 60, FieldHeight, FieldGap, ":btnfocused", month: true);
    public static Styles CalendarStyles()
    {
        Style Header(Func<Selector, Selector> state, IBrush background, IBrush foreground) =>
            new(x => state(x.OfType<CalendarItem>().Template().OfType<Button>()))
            {
                Setters = { new Setter(TemplatedControl.BackgroundProperty, background), new Setter(TemplatedControl.ForegroundProperty, foreground) }
            };
        return new Styles
        {
            new Style(x => x.OfType<CalendarDayButton>()) { Setters = { new Setter(StyledElement.ThemeProperty, DayTheme) } },
            new Style(x => x.OfType<CalendarButton>()) { Setters = { new Setter(StyledElement.ThemeProperty, MonthTheme) } },
            new Style(x => x.OfType<CalendarItem>().Template().OfType<Button>())
            {
                Setters =
                {
                    new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
                    new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(FieldGap)),
                    new Setter(InputElement.CursorProperty, HandCursor)
                }
            },
            Header(x => x, Brushes.Transparent, Ink),
            Header(x => x.Class(":pointerover"), Selection, Green),
            Header(x => x.Class(":pressed"), DividerFill, Green),
            new Style(x => x.OfType<Calendar>().Class(":focus-visible").Descendant().OfType<CalendarDayButton>().Class(":dayfocused"))
            {
                Setters = { new Setter(TemplatedControl.BorderBrushProperty, Green), new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)) }
            }
        };
    }
}

// Field frame
internal sealed class FieldFrame : Panel
{
    internal Border Face { get; }
    internal Border Ring { get; }
    internal Control Input { get; }
    internal bool KeyboardFocus => Ring.IsVisible;
    internal bool Invalid
    {
        get => Face.Classes.Contains(Ui.InvalidClass);
        set => Face.Classes.Set(Ui.InvalidClass, value);
    }
    internal FieldFrame(Control input)
    {
        Input = input;
        Face = new Border { Child = input };
        Face.Classes.Add(Ui.FieldClass);
        Ring = Ui.FocusRing(new CornerRadius(Ui.FieldRadius));
        Ring.BorderBrush = Ui.Green;
        Ring.IsVisible = false;
        Children.Add(Face); Children.Add(Ring);
        AddHandler(GotFocusEvent, (_, e) =>
        {
            if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional) Show(true);
            else if (e.NavigationMethod == NavigationMethod.Pointer) Show(false);
        }, RoutingStrategies.Bubble, true);
        AddHandler(LostFocusEvent, (_, _) => Dispatcher.UIThread.Post(() =>
        {
            if (!IsKeyboardFocusWithin) Show(false);
        }), RoutingStrategies.Bubble, true);
        AddHandler(PointerPressedEvent, (_, _) => Show(false), RoutingStrategies.Tunnel, true);
    }
    private void Show(bool visible)
    {
        Ring.IsVisible = visible;
        PseudoClasses.Set(":focus-visible", visible);
    }
}

// Number stepper
internal sealed class NumberStepper : Grid
{
    internal NumericUpDown Input { get; }
    internal FieldFrame Frame { get; }
    internal Button MinusButton { get; }
    internal Button PlusButton { get; }
    internal NumberStepper(NumericUpDown input, string label)
    {
        Input = input;
        ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto");
        ColumnSpacing = QuantityStepper.Gap;
        Frame = Ui.NumberField(input);
        var name = label.Length > 0 ? char.ToLowerInvariant(label[0]) + label[1..] : label;
        MinusButton = QuantityStepper.StepButton(QuantityStepper.MinusIcon, $"Kurangi {name}", () => Step(-1));
        PlusButton = QuantityStepper.StepButton(QuantityStepper.PlusIcon, $"Tambah {name}", () => Step(1));
        MinusButton.VerticalAlignment = PlusButton.VerticalAlignment = VerticalAlignment.Center;
        SetColumn(Frame, 1); SetColumn(PlusButton, 2);
        Children.Add(MinusButton); Children.Add(Frame); Children.Add(PlusButton);
        input.PropertyChanged += (_, e) =>
        {
            if (e.Property == NumericUpDown.ValueProperty || e.Property == NumericUpDown.MinimumProperty || e.Property == NumericUpDown.MaximumProperty) Sync();
        };
        Sync();
    }
    internal void Step(int direction)
    {
        var current = Input.Value ?? Input.Minimum;
        Input.Value = Math.Clamp(current + direction * Input.Increment, Input.Minimum, Input.Maximum);
    }
    private void Sync()
    {
        var current = Input.Value ?? Input.Minimum;
        QuantityStepper.SetEnabled(MinusButton, current > Input.Minimum);
        QuantityStepper.SetEnabled(PlusButton, current < Input.Maximum);
    }
}

// Date field
internal sealed class DateField : Panel
{
    internal const string Placeholder = "Pilih tanggal";
    internal FieldFrame Frame { get; }
    internal Border Face => Frame.Face;
    internal TextBlock ValueText { get; }
    internal Popup Popup { get; }
    internal Calendar Calendar { get; }
    internal event EventHandler? SelectedDateChanged;
    private DateTimeOffset? selected;
    private bool syncing;
    private NavigationMethod opening;
    internal DateField(DateTime last)
    {
        ValueText = new TextBlock { FontSize = 15, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        var icon = Ui.OutlineIcon(Ui.CalendarOutline, Ui.FieldIconSize, Ui.FieldIconStroke, Ui.Muted);
        icon.Margin = new Thickness(Ui.FieldGap, 0, 0, 0);
        var content = Ui.Columns("*,Auto", ValueText, icon);
        content.Margin = Ui.FieldPadding;
        content.Background = Brushes.Transparent;
        Frame = new FieldFrame(content);
        Face.Focusable = true;
        Face.Cursor = new Cursor(StandardCursorType.Hand);
        Calendar = new Calendar
        {
            SelectionMode = CalendarSelectionMode.SingleDate, DisplayDateEnd = last, FirstDayOfWeek = DayOfWeek.Monday, Foreground = Ui.Ink
        };
        Ui.UseCalendar(Calendar);
        var sheet = new Border
        {
            Background = Ui.Surface, BorderBrush = Ui.FieldBorder, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(Ui.FieldRadius), Padding = new Thickness(Ui.FieldGap), Child = Calendar
        };
        sheet.Styles.Add(Ui.CalendarStyles());
        Popup = new Popup
        {
            Child = sheet, PlacementTarget = Face, Placement = PlacementMode.BottomEdgeAlignedLeft, VerticalOffset = Ui.FieldDropOffset,
            IsLightDismissEnabled = true, WindowManagerAddShadowHint = false
        };
        Children.Add(Frame); Children.Add(Popup);
        Popup.Opened += (_, _) =>
        {
            Face.Classes.Set(Ui.OpenClass, true);
            Calendar.Focus(opening);
        };
        Popup.Closed += (_, _) => Face.Classes.Set(Ui.OpenClass, false);
        Face.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(Face).Properties.IsLeftButtonPressed) return;
            e.Handled = true;
            Face.Focus(NavigationMethod.Pointer);
            if (Popup.IsOpen) Close(false); else Open(false);
        };
        Face.KeyDown += (_, e) =>
        {
            if (e.Key is not (Key.Enter or Key.Space or Key.F4) && !(e.Key == Key.Down && e.KeyModifiers.HasFlag(KeyModifiers.Alt))) return;
            e.Handled = true;
            Open(true);
        };
        Calendar.SelectedDatesChanged += (_, _) =>
        {
            if (!syncing && Calendar.SelectedDate is { } date) SelectedDate = new DateTimeOffset(date.Date);
        };
        Calendar.AddHandler(PointerReleasedEvent, (_, e) =>
        {
            if (e.Source is Visual source && (source as CalendarDayButton ?? source.FindAncestorOfType<CalendarDayButton>()) is { IsEnabled: true })
                Dispatcher.UIThread.Post(() => Close(false));
        }, RoutingStrategies.Bubble, true);
        sheet.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key != Key.Escape && !(e.Key == Key.Enter && Calendar.DisplayMode == CalendarMode.Month)) return;
            e.Handled = true;
            Close(true);
        }, RoutingStrategies.Tunnel);
        Show();
    }
    internal DateTimeOffset? SelectedDate
    {
        get => selected;
        set
        {
            if (selected == value) return;
            selected = value;
            Show();
            SelectedDateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    internal void Open(bool keyboard)
    {
        syncing = true;
        var end = Calendar.DisplayDateEnd ?? DateTime.MaxValue;
        var date = selected?.Date;
        Calendar.DisplayMode = CalendarMode.Month;
        Calendar.SelectedDate = date <= end ? date : null;
        Calendar.DisplayDate = date is { } shown && shown <= end ? shown : DateTime.Today <= end ? DateTime.Today : end;
        syncing = false;
        opening = keyboard ? NavigationMethod.Tab : NavigationMethod.Pointer;
        Popup.IsOpen = true;
    }
    internal void Close(bool keyboard)
    {
        if (!Popup.IsOpen) return;
        Popup.IsOpen = false;
        Face.Focus(keyboard ? NavigationMethod.Tab : NavigationMethod.Pointer);
    }
    private void Show()
    {
        ValueText.Text = selected?.ToString(Ui.DateFormat, Ui.Indonesian) ?? Placeholder;
        ValueText.Foreground = selected is null ? Ui.Muted : Ui.Ink;
    }
}

// Field grid
internal sealed class FieldGrid : Grid
{
    internal const double NarrowWidth = 900;
    internal int Columns { get; private set; }
    private readonly Control[] fields;
    private TopLevel? top;
    internal FieldGrid(params Control[] fields)
    {
        this.fields = fields;
        ColumnSpacing = Ui.Spacing.Related; RowSpacing = Ui.Spacing.Related;
        foreach (var field in fields) Children.Add(field);
        Fit(double.PositiveInfinity);
    }
    internal void Fit(double windowWidth)
    {
        var count = windowWidth <= NarrowWidth ? 1 : 2;
        if (count == Columns) return;
        Columns = count;
        ColumnDefinitions = ColumnDefinitions.Parse(count == 2 ? "*,*" : "*");
        RowDefinitions = RowDefinitions.Parse(string.Join(",", Enumerable.Repeat("Auto", (fields.Length + count - 1) / count)));
        for (var i = 0; i < fields.Length; i++) { SetColumn(fields[i], i % count); SetRow(fields[i], i / count); }
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        top = TopLevel.GetTopLevel(this);
        if (top is null) return;
        top.SizeChanged += Resized;
        if (top.ClientSize.Width > 0) Fit(top.ClientSize.Width);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (top is not null) top.SizeChanged -= Resized;
        top = null;
    }
    private void Resized(object? sender, SizeChangedEventArgs e) => Fit(e.NewSize.Width);
}
