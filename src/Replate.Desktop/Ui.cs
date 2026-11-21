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
    public static readonly IBrush Green = Brush.Parse("#245B43");
    public static readonly IBrush Ink = Brush.Parse("#23382C");
    public static readonly IBrush Muted = Brush.Parse("#59675D");
    public static readonly IBrush Selection = Brush.Parse("#E0E9DF");
    public static readonly IBrush OnPrimary = Brushes.White;
    public static readonly IBrush DividerFill = Brush.Parse("#DDE3DA");

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
        if (button.Classes.Contains(PrimaryClass))
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
    public static Button FullWidth(Button button)
    {
        button.Classes.Remove(CompactClass);
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        return button;
    }

    // Primary button
    public const string PrimaryClass = "primary";
    public const string CompactClass = "compact";
    public const double PrimaryHoverScale = 1.03;
    public static readonly TimeSpan PrimaryHoverDuration = TimeSpan.FromMilliseconds(120);
    public static readonly BoxShadows PrimaryButtonHoverShadow = BoxShadows.Parse("0 4 12 0 #4D245B43");
    public static void UsePrimaryButtons(Application app) => app.Styles.Add(PrimaryStyles());
    private static Styles PrimaryStyles()
    {
        var ease = new CubicEaseOut();
        Style Face(Func<Selector, Selector> state, IBrush background, IBrush foreground, BoxShadows shadow) =>
            new(x => state(x.OfType<Button>().Class(PrimaryClass).Not(y => y.Class(":disabled"))).Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
            {
                Setters =
                {
                    new Setter(ContentPresenter.BackgroundProperty, background),
                    new Setter(ContentPresenter.ForegroundProperty, foreground),
                    new Setter(ContentPresenter.BoxShadowProperty, shadow)
                }
            };
        Style Scale(Func<Selector, Selector> state, double scale, int z) =>
            new(x => state(x.OfType<Button>().Class(PrimaryClass)))
            {
                Setters =
                {
                    new Setter(Visual.RenderTransformProperty, TransformOperations.Parse($"scale({scale.ToString(System.Globalization.CultureInfo.InvariantCulture)})")),
                    new Setter(Visual.ZIndexProperty, z)
                }
            };
        return new Styles
        {
            new Style(x => x.OfType<Button>().Class(PrimaryClass).Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
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
            new Style(x => x.OfType<Button>().Class(PrimaryClass).Class(CompactClass))
            {
                Setters =
                {
                    new Setter(Visual.RenderTransformOriginProperty, RelativePoint.Center),
                    new Setter(Animatable.TransitionsProperty, new Transitions
                    {
                        new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = PrimaryHoverDuration, Easing = ease }
                    })
                }
            },
            Face(x => x, Green, OnPrimary, default),
            Face(x => x.Class(":pointerover"), Selection, Green, PrimaryButtonHoverShadow),
            Face(x => x.Class(":pressed"), DividerFill, Green, PrimaryButtonHoverShadow),
            Face(x => x.Class(":pressed").Not(y => y.Class(":pointerover")), DividerFill, Green, default),
            new Style(x => x.OfType<Button>().Class(PrimaryClass).Class(":disabled").Template().OfType<Panel>().Child().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
            {
                Setters = { new Setter(ContentPresenter.BackgroundProperty, new Avalonia.Data.TemplateBinding(TemplatedControl.BackgroundProperty)) }
            },
            Scale(x => x, 1, 0),
            Scale(x => x.Class(CompactClass).Class(":pointerover").Not(y => y.Class(":disabled")), PrimaryHoverScale, 1)
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
    public static TextBlock FieldError() => Text("", 14, Brush.Parse("#B3261E"));
    public static Border Divider() => new() { Height = 1, Background = DividerFill };

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
        return new Border { Child = grid, BorderBrush = Brush.Parse("#DDE3DA"), BorderThickness = new Thickness(0, 0, 0, 1) };
    }

}
