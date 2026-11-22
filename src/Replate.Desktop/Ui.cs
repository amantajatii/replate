using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Automation;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using Avalonia.Platform;
using Avalonia.Styling;

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
        if (button.Classes.Contains(PrimaryClass) || button.Classes.Contains(DangerClass))
            presenter.Bind(ContentPresenter.BackgroundProperty, button.GetObservable(TemplatedControl.BackgroundProperty), Avalonia.Data.BindingPriority.Template);
        else presenter[!ContentPresenter.BackgroundProperty] = button[!TemplatedControl.BackgroundProperty];
        return presenter;
    }
    public static Styles FocusStyles()
    {
        var fluent = Application.Current?.FindResource(typeof(Button)) as ControlTheme;
        var ringed = new ControlTheme(typeof(Button))
        {
            BasedOn = fluent,
            Setters =
            {
                new Setter(Visual.ClipToBoundsProperty, false),
                new Setter(TemplatedControl.TemplateProperty,
                    new FuncControlTemplate<Button>((button, _) => FocusFrame(PrimaryFill(button, new ContentPresenter
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
                    }), button.CornerRadius)))
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
        Style Face(string kind, Func<Selector, Selector> state, IBrush background, IBrush foreground, BoxShadows shadow) =>
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
        Style[] States(string kind, IBrush rest, IBrush hover, IBrush pressed, IBrush text, BoxShadows shadow) =>
        [
            Face(kind, x => x, rest, OnPrimary, default),
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
        styles.AddRange(States(PrimaryClass, Green, Selection, DividerFill, Green, PrimaryButtonHoverShadow));
        styles.Add(Face(PrimaryClass, x => x.Class(RoundClass).Class(":pointerover"), Selection, Green, default));
        styles.Add(Face(PrimaryClass, x => x.Class(RoundClass).Class(":pressed"), DividerFill, Green, default));
        styles.AddRange(Rest(PrimaryClass));
        // Danger
        styles.AddRange(Motion(DangerClass));
        styles.AddRange(States(DangerClass, Danger, DangerTint, DangerPressed, Danger, DangerButtonHoverShadow));
        styles.AddRange(Rest(DangerClass));
        return styles;
    }

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

}
