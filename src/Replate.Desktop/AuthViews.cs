using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Replate.Domain.Enums;
using Replate.Domain.Services;

namespace Replate.Desktop;

internal sealed class AuthSurface : Grid
{
    internal const double CardWidth = 440;
    internal const double MinCardWidth = 360;
    internal const double MinPlate = 360;
    internal const double SmallestPlate = 320;
    internal const double PlateGap = 24;
    internal const double BoundaryShare = 0.72;
    internal const double CardShare = 0.12;
    internal const double PlateShare = 0.56;
    internal const double GapShare = 0.08;
    internal const double MinMargin = 48;
    internal const double Inset = 32;
    internal const double CardMargin = 32;
    internal const double LogoGap = 24;
    internal const double LogoHeight = 44;
    internal const double ScrollBarWidth = 6;
    internal const double ScrollGap = 16;
    internal const double ShadowPad = 24;
    internal const double ClearGap = PlateGap + ScrollBarWidth + ScrollGap;
    internal const string ScrollingClass = "auth-scrolling";
    internal const string RevealName = "Tampilkan kata sandi";
    internal const string ConcealName = "Sembunyikan kata sandi";
    internal static readonly Uri HeroUri = new("avares://Replate.Desktop/Assets/auth-hero.png");
    internal static readonly Geometry EyeIcon = Geometry.Parse("M1,8 C3.5,3 12.5,3 15,8 C12.5,13 3.5,13 1,8 Z M8,5.5 A2.5,2.5 0 1 1 8,10.5 A2.5,2.5 0 1 1 8,5.5 Z");
    internal static readonly Geometry EyeOffIcon = Geometry.Parse("M1,8 C3.5,3 12.5,3 15,8 C12.5,13 3.5,13 1,8 Z M8,5.5 A2.5,2.5 0 1 1 8,10.5 A2.5,2.5 0 1 1 8,5.5 Z M2,2 L14,14");
    internal static readonly IBrush Selection = Ui.Selection;
    internal static readonly IBrush Sidebar = Ui.Sidebar;
    internal static readonly IBrush Leaf = Ui.Primary500;
    internal static readonly IBrush CanvasFill = Ui.Canvas;
    private static readonly Lazy<Bitmap> HeroBitmap = new(() => { using var stream = AssetLoader.Open(HeroUri); return new Bitmap(stream); });
    private static readonly IBrush WarningText = Brush.Parse("#7A4E00");
    private static readonly ControlTheme PlainButton = Plain(typeof(Button));
    private static readonly ControlTheme PlainRadio = Plain(typeof(RadioButton));
    private static readonly ControlTheme ThinThumb = new(typeof(Thumb))
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Ui.Muted),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Thumb>((thumb, _) => new Border
            {
                [!Border.BackgroundProperty] = thumb[!TemplatedControl.BackgroundProperty], CornerRadius = new CornerRadius(ScrollBarWidth / 2)
            }))
        }
    };
    internal static readonly ControlTheme ThinScrollBar = new(typeof(ScrollBar))
    {
        Setters =
        {
            new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
            new Setter(Layoutable.WidthProperty, ScrollBarWidth),
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ScrollBar>((bar, _) => new Track
            {
                [!Track.MinimumProperty] = bar[!RangeBase.MinimumProperty],
                [!Track.MaximumProperty] = bar[!RangeBase.MaximumProperty],
                [!!Track.ValueProperty] = bar[!!RangeBase.ValueProperty],
                [!Track.ViewportSizeProperty] = bar[!ScrollBar.ViewportSizeProperty],
                [!Track.OrientationProperty] = bar[!ScrollBar.OrientationProperty],
                IsDirectionReversed = true, Thumb = new Thumb { Theme = ThinThumb }
            }))
        }
    };

    internal Border Card { get; }
    internal Border HeroPanel { get; }
    internal Canvas HeroLayer { get; }
    internal Image Hero { get; }
    internal Image Logo { get; }
    internal ScrollViewer FormScroll { get; }
    internal Border FormClip { get; }
    internal Border Header { get; }
    internal Border? Warning { get; }
    private readonly StackPanel column;
    private readonly CenterBox centered;

    internal AuthSurface(Control cardContent, string? databaseError = null)
    {
        ColumnDefinitions = ColumnDefinitions.Parse("3*,2*");
        Card = new Border
        {
            Child = cardContent, Background = Ui.Surface, CornerRadius = new CornerRadius(16),
            Padding = new Thickness(32), BoxShadow = Ui.CardShadow
        };
        column = Ui.Stack(16);
        column.HorizontalAlignment = HorizontalAlignment.Left; column.Margin = new Thickness(ShadowPad, 0, 0, 0);
        if (databaseError is not null)
        {
            var detail = Ui.Text(databaseError, 12, WarningText);
            detail.MaxLines = 2; detail.TextTrimming = TextTrimming.CharacterEllipsis;
            Warning = new Border
            {
                Child = Ui.Stack(6, Ui.Text("Database belum tersambung. Pesanan belum dapat disimpan.", 13, WarningText, true), detail),
                Background = Brush.Parse("#FFF4D6"), BorderBrush = Ui.Accent, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(12)
            };
            ToolTip.SetTip(Warning, databaseError);
            column.Children.Add(Warning);
        }
        column.Children.Add(Card);
        Logo = Ui.Logo(Ui.LogoGreen, LogoHeight);
        Logo.Margin = new Thickness(Inset, Inset, Inset, 0);
        centered = new CenterBox(column, Card) { Above = Inset + LogoHeight };
        FormScroll = Ui.Scroll(centered);
        FormScroll.HorizontalAlignment = HorizontalAlignment.Left;
        ThinScroll(FormScroll);
        FormScroll.SizeChanged += (_, e) => { centered.Viewport = e.NewSize.Height; centered.InvalidateMeasure(); };
        FormClip = new Border { Child = FormScroll, ClipToBounds = true, Background = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Left };
        FormClip.PointerWheelChanged += (_, e) =>
        {
            if (e.Handled || FormScroll.Extent.Height <= FormScroll.Viewport.Height) return;
            var y = Math.Clamp(FormScroll.Offset.Y - e.Delta.Y * 50, 0, FormScroll.Extent.Height - FormScroll.Viewport.Height);
            FormScroll.Offset = new Vector(0, y); e.Handled = true;
        };
        Header = new Border { Child = Logo, Background = CanvasFill };
        var left = new Grid { RowDefinitions = RowDefinitions.Parse("Auto,*") };
        SetRow(FormClip, 1);
        left.Children.Add(FormClip); left.Children.Add(Header);
        HeroPanel = new Border { Background = Ui.Green };
        SetColumn(HeroPanel, 1);
        Hero = new Image { Source = HeroBitmap.Value, Stretch = Stretch.Uniform };
        HeroLayer = new Canvas { IsHitTestVisible = false, ClipToBounds = true, Children = { Hero } };
        SetColumnSpan(HeroLayer, 2);
        foreach (var decorative in new Control[] { Hero, HeroLayer, HeroPanel })
            AutomationProperties.SetAccessibilityView(decorative, AccessibilityView.Raw);
        Children.Add(left); Children.Add(HeroPanel); Children.Add(HeroLayer);
        Styles.Add(Hover("auth-secondary", Selection, Ui.Green));
        Styles.Add(ScrollBarOpacity(x => x, 0));
        Styles.Add(ScrollBarOpacity(x => x.Class(":pointerover"), 1));
        Styles.Add(ScrollBarOpacity(x => x.Class(ScrollingClass), 1));
        AddStateStyles();
    }

    // Scrolling
    private static void ThinScroll(ScrollViewer scroll)
    {
        scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        scroll.AllowAutoHide = true;
        ScrollViewer.SetBringIntoViewOnFocusChange(scroll, true);
        scroll.Classes.Add("auth-scroll");
        scroll.TemplateApplied += (_, e) =>
        {
            foreach (var name in new[] { "PART_VerticalScrollBar", "PART_HorizontalScrollBar" })
                if (e.NameScope.Find<ScrollBar>(name) is { } bar) bar.Theme = ThinScrollBar;
            if (e.NameScope.Find<ScrollContentPresenter>("PART_ContentPresenter") is { } presenter) presenter.ClipToBounds = false;
        };
        var hide = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        hide.Tick += (_, _) => { hide.Stop(); scroll.Classes.Remove(ScrollingClass); };
        scroll.ScrollChanged += (_, e) =>
        {
            if (e.OffsetDelta == default) return;
            scroll.Classes.Add(ScrollingClass); hide.Stop(); hide.Start();
        };
    }

    private static Style ScrollBarOpacity(Func<Selector, Selector> state, double opacity) =>
        new(x => state(x.OfType<ScrollViewer>().Class("auth-scroll")).Template().OfType<ScrollBar>())
        {
            Setters = { new Setter(OpacityProperty, opacity) }
        };

    // Layout
    internal static (double Boundary, double Plate, double Card, double Left) Layout(Size size)
    {
        var plate = Math.Min(Math.Max(MinPlate, size.Height * PlateShare), Math.Min(size.Height, size.Width * 2 * (1 - BoundaryShare)));
        var boundary = size.Width * BoundaryShare;
        var left = Math.Max(MinMargin, size.Width * CardShare);
        var gap = Math.Max(ClearGap, Math.Min(size.Width * GapShare, boundary - plate / 2 - left - MinCardWidth));
        if (boundary - plate / 2 - left - gap < MinCardWidth)
        {
            left = Math.Max(MinMargin, boundary - plate / 2 - MinCardWidth - ClearGap);
            var need = left + MinCardWidth + ClearGap;
            if (need > boundary - plate / 2)
            {
                if (need + plate > size.Width) plate = Math.Max(SmallestPlate, size.Width - need);
                boundary = need + plate / 2;
            }
        }
        var card = Math.Max(0, Math.Min(CardWidth, boundary - plate / 2 - gap - left));
        return (boundary, plate, card, left);
    }

    private Size measuredFor;
    protected override Size MeasureOverride(Size availableSize)
    {
        measuredFor = availableSize;
        Place(availableSize);
        return base.MeasureOverride(availableSize);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (finalSize != measuredFor) MeasureOverride(finalSize);
        return base.ArrangeOverride(finalSize);
    }

    private void Place(Size size)
    {
        if (!double.IsFinite(size.Width) || !double.IsFinite(size.Height)) return;
        var (boundary, plate, cardWidth, left) = Layout(size);
        var plateX = boundary - plate / 2;
        if (ColumnDefinitions[0].Width != new GridLength(boundary)) ColumnDefinitions[0].Width = new GridLength(boundary);
        FormClip.Width = Math.Max(0, plateX);
        FormScroll.Width = ShadowPad + cardWidth + ScrollGap + ScrollBarWidth;
        var cardLeft = CardLeft(plateX, cardWidth);
        FormScroll.Margin = new Thickness(cardLeft - ShadowPad, 0, 0, 0);
        column.Width = cardWidth;
        Logo.Margin = new Thickness(left, Inset, 0, 0);
        Hero.Width = plate; Hero.Height = plate;
        Canvas.SetLeft(Hero, plateX); Canvas.SetTop(Hero, (size.Height - plate) / 2);
    }

    internal static double CardLeft(double plateX, double cardWidth) => Math.Max(CardMargin, (plateX - cardWidth) / 2);

    private sealed class CenterBox : Decorator
    {
        private readonly Control anchor;
        internal double Above;
        internal double Viewport;

        internal CenterBox(Control child, Control anchor) { Child = child; this.anchor = anchor; }

        private double Top(Size child)
        {
            var anchorTop = child.Height - anchor.DesiredSize.Height;
            var window = Above + Viewport;
            return Math.Max(LogoGap, (window - anchor.DesiredSize.Height) / 2 - Above - anchorTop);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Child is null) return default;
            Child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            var size = Child.DesiredSize;
            return new Size(size.Width, Math.Max(Viewport, Top(size) + size.Height + Inset));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child is null) return finalSize;
            var size = Child.DesiredSize;
            Child.Arrange(new Rect(0, Top(size), finalSize.Width, size.Height));
            return finalSize;
        }
    }

    // States
    private static ControlTheme Plain(Type type) => new(type)
    {
        Setters =
        {
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ContentControl>((control, _) => new Border
            {
                [!Border.BackgroundProperty] = control[!TemplatedControl.BackgroundProperty],
                [!Border.BorderBrushProperty] = control[!TemplatedControl.BorderBrushProperty],
                [!Border.BorderThicknessProperty] = control[!TemplatedControl.BorderThicknessProperty],
                [!Border.CornerRadiusProperty] = control[!TemplatedControl.CornerRadiusProperty],
                [!Border.PaddingProperty] = control[!TemplatedControl.PaddingProperty],
                Child = new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = control[!ContentControl.ContentProperty],
                    [!ContentPresenter.ForegroundProperty] = control[!TemplatedControl.ForegroundProperty],
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                }
            }))
        }
    };

    private static Style State<T>(string kind, Func<Selector, Selector> state, IBrush? background, IBrush? foreground, IBrush? border = null) where T : TemplatedControl
    {
        var style = new Style(x => state(x.OfType<T>().Class(kind)));
        if (background is not null) style.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, background));
        if (foreground is not null) style.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, foreground));
        if (border is not null) style.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, border));
        return style;
    }

    private void AddStateStyles()
    {
        Styles.Add(State<Button>("password-toggle", x => x, Brushes.Transparent, Ui.Ink, Brushes.Transparent));
        Styles.Add(State<Button>("password-toggle", x => x.Class(":pointerover"), Brushes.Transparent, Leaf));
        Styles.Add(State<Button>("password-toggle", x => x.Class(":focus-visible"), null, null, Ui.Green));
        Styles.Add(State<RadioButton>("role-button", x => x, Sidebar, Ui.Ink, Brushes.Transparent));
        Styles.Add(State<RadioButton>("role-button", x => x.Class(":pointerover").Not(y => y.Class(":checked")), Selection, Ui.Green));
        Styles.Add(State<RadioButton>("role-button", x => x.Class(":checked"), Ui.Green, Brushes.White));
        Styles.Add(State<RadioButton>("role-button", x => x.Class(":checked").Class(":pointerover"), Ui.Ink, Brushes.White));
        Styles.Add(State<RadioButton>("role-button", x => x.Class(":focus-visible"), null, null, Ui.Ink));
    }

    private static Style Hover(string kind, IBrush background, IBrush foreground) =>
        new(x => x.OfType<Button>().Class(kind).Class(":pointerover").Not(y => y.Class(":pressed"))
            .Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
        {
            Setters = { new Setter(ContentPresenter.BackgroundProperty, background), new Setter(ContentPresenter.ForegroundProperty, foreground) }
        };

    // Controls
    internal static TextBlock Title(string text)
    {
        var title = Ui.Text(text, 28);
        title.FontWeight = FontWeight.Bold;
        return title;
    }

    internal static Button Action(string text, Action action, bool primary)
    {
        var button = Ui.FullWidth(Ui.Button(text, action, primary));
        button.Classes.Add(primary ? "auth-primary" : "auth-secondary");
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        return button;
    }

    internal static TextBox Password(string placeholder)
    {
        var input = Ui.Input(placeholder);
        input.TextWrapping = TextWrapping.NoWrap;
        var icon = new Path
        {
            Data = EyeIcon, Width = 16, Height = 16, StrokeThickness = 1.5,
            StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round
        };
        var toggle = new Button
        {
            Theme = PlainButton, Content = icon, Width = 20, Height = 20, Padding = default, Margin = new Thickness(6, 6, 10, 6),
            CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(2), VerticalAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        toggle.Classes.Add("password-toggle");
        icon[!Shape.StrokeProperty] = toggle[!TemplatedControl.ForegroundProperty];
        toggle.Click += (_, _) => SetRevealed(input, input.PasswordChar != default);
        input.InnerRightContent = toggle;
        SetRevealed(input, false);
        return input;
    }

    internal static Button RevealToggle(TextBox input) => (Button)input.InnerRightContent!;

    internal static void SetRevealed(TextBox input, bool revealed)
    {
        input.PasswordChar = revealed ? default : '•';
        var toggle = RevealToggle(input);
        ((Path)toggle.Content!).Data = revealed ? EyeOffIcon : EyeIcon;
        AutomationProperties.SetName(toggle, revealed ? ConcealName : RevealName);
    }

    internal static void ClearPassword(TextBox input)
    {
        input.Text = "";
        SetRevealed(input, false);
    }

    internal static RadioButton RoleOption(string text, string group, bool isChecked = false)
    {
        var option = new RadioButton
        {
            Theme = PlainRadio, Content = text, GroupName = group, IsChecked = isChecked,
            HorizontalAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(16, 11), BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(6), FontSize = 13, FontWeight = FontWeight.SemiBold,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        option.Classes.Add("role-button");
        AutomationProperties.SetName(option, text);
        return option;
    }

    internal static Grid RoleGroup(params RadioButton[] options)
    {
        var group = Ui.Columns(string.Join(',', options.Select(_ => "*")), options);
        group.ColumnSpacing = 12;
        group.AddHandler(KeyDownEvent, (_, e) =>
        {
            var step = e.Key switch { Key.Left or Key.Up => -1, Key.Right or Key.Down => 1, _ => 0 };
            var current = Array.IndexOf(options, e.Source as RadioButton);
            if (step == 0 || current < 0) return;
            var next = options[(current + step + options.Length) % options.Length];
            next.IsChecked = true; next.Focus(NavigationMethod.Directional);
            e.Handled = true;
        }, RoutingStrategies.Tunnel);
        return group;
    }

    internal static void SubmitOnEnter(Action submit, params TextBox[] inputs)
    {
        foreach (var input in inputs)
            input.AddHandler(KeyDownEvent, (_, e) =>
            {
                if (e.Key != Key.Enter || e.Source is Button) return;
                e.Handled = true;
                submit();
            }, RoutingStrategies.Tunnel);
    }
}

internal sealed class TimeField : Border
{
    internal ComboBox Hour { get; }
    internal ComboBox Minute { get; }
    internal event Action? ValueChanged;

    internal TimeField(string label)
    {
        Hour = Part(24, $"{label}, jam");
        Minute = Part(60, $"{label}, menit");
        Background = Brushes.White; BorderThickness = new Thickness(1); CornerRadius = new CornerRadius(21);
        MinHeight = 42; Padding = new Thickness(6, 0);
        this.Bind(BorderBrushProperty, this.GetResourceObservable("TextControlBorderBrush"));
        var colon = Ui.Text(":", 14, bold: true);
        colon.VerticalAlignment = VerticalAlignment.Center;
        var parts = Ui.Columns("*,Auto,*", Hour, colon, Minute);
        parts.ColumnSpacing = 6;
        Child = parts;
        Hour.SelectionChanged += (_, _) =>
        {
            if (Hour.SelectedIndex >= 0 && Minute.SelectedIndex < 0) Minute.SelectedIndex = 0;
            ValueChanged?.Invoke();
        };
        Minute.SelectionChanged += (_, _) => ValueChanged?.Invoke();
    }

    internal TimeOnly? Value
    {
        get => Hour.SelectedIndex >= 0 && Minute.SelectedIndex >= 0 ? new TimeOnly(Hour.SelectedIndex, Minute.SelectedIndex) : null;
        set { Hour.SelectedIndex = value?.Hour ?? -1; Minute.SelectedIndex = value?.Minute ?? -1; }
    }

    private static ComboBox Part(int count, string name)
    {
        var box = new ComboBox
        {
            ItemsSource = Enumerable.Range(0, count).Select(i => i.ToString("00")).ToList(), PlaceholderText = "--",
            Background = Brushes.Transparent, BorderThickness = default, CornerRadius = new CornerRadius(6),
            MinWidth = 0, Padding = new Thickness(8, 5, 0, 7), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center
        };
        AutomationProperties.SetName(box, name);
        return box;
    }
}

public sealed class SignInView : UserControl
{
    internal TextBox EmailInput { get; }
    internal TextBox PasswordInput { get; }
    internal Button SignInButton { get; }
    internal Button RegisterButton { get; }
    internal TextBlock TitleText { get; } = AuthSurface.Title("Masuk");
    internal TextBlock ErrorText { get; } = Ui.Text("", 14, Brush.Parse("#963E28"));
    internal AuthSurface Surface { get; }

    internal SignInView(MainWindow host, DemoSession session, string? startupError)
    {
        ErrorText.IsVisible = false;
        EmailInput = Ui.Input("Masukkan email Anda");
        PasswordInput = AuthSurface.Password("Masukkan kata sandi Anda");
        void Submit()
        {
            var email = EmailInput.Text ?? "";
            var password = PasswordInput.Text ?? "";
            AuthSurface.ClearPassword(PasswordInput);
            host.Run(() => session.SignIn(email, password), host.ShowHome, onError: message =>
            {
                ErrorText.Text = message; ErrorText.IsVisible = true;
                PasswordInput.Focus();
            });
        }
        AuthSurface.SubmitOnEnter(Submit, PasswordInput);
        SignInButton = AuthSurface.Action("Masuk", Submit, true);
        RegisterButton = AuthSurface.Action("Daftar akun baru", host.ShowRegister, false);
        var card = Ui.Stack(20, TitleText, ErrorText,
            Ui.Field("Email", EmailInput), Ui.Field("Kata sandi", PasswordInput),
            Ui.Stack(12, SignInButton, RegisterButton));
        Surface = new AuthSurface(card, startupError);
        Content = Surface;
        Loaded += (_, _) => EmailInput.Focus();
    }
}

public sealed class RegisterView : UserControl
{
    internal RadioButton CustomerOption { get; }
    internal RadioButton OwnerOption { get; }
    internal Grid RoleGroup { get; }
    internal TextBox NameInput { get; }
    internal TextBox EmailInput { get; }
    internal TextBox PasswordInput { get; }
    internal TextBox ConfirmPasswordInput { get; }
    internal TextBox RestaurantNameInput { get; }
    internal TextBox AddressInput { get; }
    internal TextBox PhoneInput { get; }
    internal TextBox DescriptionInput { get; }
    internal TimeField OpeningInput { get; }
    internal TimeField ClosingInput { get; }
    internal StackPanel ProfileSection { get; }
    internal Button RegisterButton { get; }
    internal Button BackButton { get; }
    internal TextBlock TitleText { get; } = AuthSurface.Title("Daftar akun");
    internal TextBlock ErrorText { get; } = Ui.Text("", 14, Brush.Parse("#963E28"));
    internal Dictionary<string, TextBlock> FieldErrors { get; } = new();
    internal AuthSurface Surface { get; }

    internal RegisterView(MainWindow host, DemoSession session)
    {
        ErrorText.IsVisible = false;
        CustomerOption = AuthSurface.RoleOption("Pelanggan", "register-role", true);
        OwnerOption = AuthSurface.RoleOption("Pemilik restoran", "register-role");
        RoleGroup = AuthSurface.RoleGroup(CustomerOption, OwnerOption);
        AutomationProperties.SetName(RoleGroup, "Daftar sebagai");
        NameInput = Ui.Input("Masukkan nama Anda"); EmailInput = Ui.Input("Masukkan email Anda");
        PasswordInput = AuthSurface.Password("Masukkan kata sandi Anda"); ConfirmPasswordInput = AuthSurface.Password("Masukkan ulang kata sandi Anda");
        RestaurantNameInput = Ui.Input("Masukkan nama restoran"); AddressInput = Ui.Input("Masukkan alamat restoran");
        PhoneInput = Ui.Input("Masukkan nomor telepon");
        DescriptionInput = Ui.Input("Masukkan deskripsi restoran", "", true);
        OpeningInput = new TimeField("Jam buka");
        ClosingInput = new TimeField("Jam tutup");
        var inputs = new Dictionary<string, Control>
        {
            ["name"] = NameInput, ["email"] = EmailInput, ["password"] = PasswordInput, ["confirmPassword"] = ConfirmPasswordInput,
            ["restaurantName"] = RestaurantNameInput, ["address"] = AddressInput, ["phone"] = PhoneInput,
            ["description"] = DescriptionInput, ["openingTime"] = OpeningInput, ["closingTime"] = ClosingInput
        };
        var outlines = new Dictionary<string, Border>();
        var red = Ui.Danger;
        var attempted = false;
        Control Field(string key, string label)
        {
            inputs[key].HorizontalAlignment = HorizontalAlignment.Stretch;
            var error = Ui.FieldError(); error.IsVisible = false;
            FieldErrors[key] = error;
            var outline = new Border { Child = inputs[key], CornerRadius = new CornerRadius(inputs[key] is TimeField ? 22 : 6),
                BorderThickness = new Thickness(1), BorderBrush = Brushes.Transparent };
            outlines[key] = outline;
            AutomationProperties.SetName(inputs[key], label);
            return Ui.Stack(8, Ui.Text(label, 14, bold: true), outline, error);
        }
        bool IsOwner() => OwnerOption.IsChecked == true;
        RestaurantProfile Profile() => new(RestaurantNameInput.Text ?? "", AddressInput.Text ?? "", PhoneInput.Text ?? "",
            OpeningInput.Value, ClosingInput.Value, DescriptionInput.Text ?? "");
        Dictionary<string, string> Errors()
        {
            var password = PasswordInput.Text ?? "";
            var errors = session.ValidateRegistration(IsOwner() ? UserRole.RestaurantOwner : UserRole.Customer,
                NameInput.Text ?? "", EmailInput.Text ?? "", password, IsOwner() ? Profile() : null);
            var confirm = ConfirmPasswordInput.Text ?? "";
            if (confirm.Length == 0) { if (password.Length > 0) errors["confirmPassword"] = "Ulangi kata sandi wajib diisi."; }
            else if (confirm != password) errors["confirmPassword"] = "Kata sandi tidak sama.";
            return errors;
        }
        void Display(Dictionary<string, string> errors)
        {
            foreach (var pair in inputs)
            {
                var invalid = errors.TryGetValue(pair.Key, out var message);
                FieldErrors[pair.Key].Text = message ?? "";
                FieldErrors[pair.Key].IsVisible = invalid;
                outlines[pair.Key].BorderBrush = invalid ? red : Brushes.Transparent;
                AutomationProperties.SetHelpText(pair.Value, message ?? "");
            }
        }
        void Revalidate() { if (attempted) Display(Errors()); }
        void Submit()
        {
            attempted = true;
            ErrorText.IsVisible = false;
            AuthSurface.SetRevealed(PasswordInput, false); AuthSurface.SetRevealed(ConfirmPasswordInput, false);
            var errors = Errors(); Display(errors);
            if (errors.Count > 0)
            {
                var first = inputs.First(p => errors.ContainsKey(p.Key)).Value;
                first.BringIntoView(); (first is TimeField time ? time.Hour : first).Focus();
                return;
            }
            var owner = IsOwner();
            var email = EmailInput.Text ?? "";
            var password = PasswordInput.Text ?? "";
            var profile = owner ? Profile() : null;
            AuthSurface.ClearPassword(PasswordInput); AuthSurface.ClearPassword(ConfirmPasswordInput);
            var signedIn = false;
            host.Run(() => signedIn = session.RegisterAndSignIn(owner ? UserRole.RestaurantOwner : UserRole.Customer,
                NameInput.Text ?? "", email, password, profile), () =>
            {
                if (signedIn) host.ShowHome();
                else host.ShowSignIn();
                host.ShowSuccess(signedIn ? "Akun berhasil dibuat." : "Cek email untuk konfirmasi, lalu masuk ke Replate.");
            }, onError: message => { ErrorText.Text = message; ErrorText.IsVisible = true; ErrorText.BringIntoView(); });
        }
        AuthSurface.SubmitOnEnter(Submit, NameInput, EmailInput, PasswordInput, ConfirmPasswordInput, RestaurantNameInput, AddressInput, PhoneInput);
        RegisterButton = AuthSurface.Action("Daftar", Submit, true);
        BackButton = AuthSurface.Action("Kembali ke masuk", host.ShowSignIn, false);
        var hours = Ui.Columns("*,*", Field("openingTime", "Jam buka"), Field("closingTime", "Jam tutup"));
        hours.ColumnSpacing = 16;
        ProfileSection = Ui.Stack(20, Ui.Text("Profil restoran", 20, bold: true),
            Field("restaurantName", "Nama restoran"), Field("address", "Alamat"), Field("phone", "Telepon"),
            Field("description", "Deskripsi"), hours,
            Ui.SupportingText("Untuk buka 24 jam, isi 00:00 sampai 23:59."));
        ProfileSection.IsVisible = false;
        var card = Ui.Stack(20, TitleText,
            ErrorText, Ui.Stack(8, Ui.Text("Daftar sebagai", 14, bold: true), RoleGroup),
            Field("name", "Nama"), Field("email", "Email"), Field("password", "Kata sandi"), Field("confirmPassword", "Ulangi kata sandi"),
            ProfileSection, Ui.Stack(12, RegisterButton, BackButton));
        OwnerOption.IsCheckedChanged += (_, _) => { ProfileSection.IsVisible = IsOwner(); Revalidate(); };
        foreach (var pair in inputs)
            pair.Value.PropertyChanged += (_, e) => { if (e.Property == TextBox.TextProperty) Revalidate(); };
        OpeningInput.ValueChanged += Revalidate;
        ClosingInput.ValueChanged += Revalidate;
        Surface = new AuthSurface(card);
        Content = Surface;
        Loaded += (_, _) => NameInput.Focus();
    }
}
