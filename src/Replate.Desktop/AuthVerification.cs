using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Automation;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Replate.Domain.Entities;
using Replate.Domain.Services;

namespace Replate.Desktop;

internal static class AuthVerification
{
    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }

    private static Rect RectIn(Control control, Visual root) =>
        new(control.TranslatePoint(default, root) ?? throw new InvalidOperationException($"{control.GetType().Name} is not in the surface"), control.Bounds.Size);

    private static double Gap(Rect a, Rect b) =>
        Math.Max(Math.Max(b.X - a.Right, a.X - b.Right), Math.Max(b.Y - a.Bottom, a.Y - b.Bottom));

    private static Color ColorOf(IBrush? brush) => brush is ISolidColorBrush solid ? solid.Color : default;

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    internal static async Task CheckSurface(Window window, AuthSurface surface, string screen, Size? frame = null)
    {
        await Task.Delay(300);
        var size = frame ?? window.ClientSize;
        var where = $"{screen} at {size.Width:0}x{size.Height:0}";
        var bounds = surface.Bounds; var panel = surface.HeroPanel.Bounds;
        Check(surface.ColumnDefinitions.Count == 2 && Math.Abs(bounds.Width - size.Width) < 1 && Math.Abs(bounds.Height - size.Height) < 1 &&
            panel.X >= bounds.Width * AuthSurface.BoundaryShare - .5 && Math.Abs(panel.Right - bounds.Width) < 1 && Math.Abs(panel.Height - bounds.Height) < 1,
            $"{where}: primary panel is full height on the right and starts at or after 72% ({panel.X:0.#} of {bounds.Width:0.#})");
        Check(ColorOf(surface.HeroPanel.Background) == Color.Parse("#245B43"), $"{where}: panel uses the primary color");
        Check(surface.Hero.Source is Bitmap { PixelSize: { Width: 1080, Height: 1080 } } && surface.Hero.Stretch == Stretch.Uniform &&
            surface.Hero.Bounds.Width > 0, $"{where}: hero image loaded from {AuthSurface.HeroUri}");
        Check(surface.Hero.GetVisualParent() == surface.HeroLayer && !surface.HeroLayer.IsHitTestVisible &&
            !surface.FormScroll.GetVisualDescendants().Contains(surface.Hero), $"{where}: plate sits in a fixed overlay outside the scroll area");
        Check(new Control[] { surface.Hero, surface.HeroLayer, surface.HeroPanel }.All(c => AutomationProperties.GetAccessibilityView(c) == AccessibilityView.Raw),
            $"{where}: hero is decorative for screen readers");
        Check(!surface.HeroPanel.GetVisualDescendants().Concat(surface.HeroLayer.GetVisualDescendants()).OfType<TextBlock>().Any(), $"{where}: no text over the photo");
        var plate = RectIn(surface.Hero, surface);
        var diameter = Math.Max(AuthSurface.MinPlate, size.Height * AuthSurface.PlateShare);
        Check(Math.Abs(plate.Width - plate.Height) < 1 && Math.Abs(plate.Width - diameter) < 1 && Math.Abs(plate.Center.Y - bounds.Height / 2) < 1 &&
            plate.Right <= bounds.Width + .5, $"{where}: plate diameter follows window height and is centered vertically ({plate.Width:0.#}, expected {diameter:0.#})");
        var onCanvas = (panel.X - plate.X) / plate.Width;
        Check(Math.Abs(onCanvas - .5) <= .02, $"{where}: panel edge sits at the plate center, half on canvas and half on the panel ({onCanvas:0.###})");
        var card = surface.Card;
        Check(ColorOf(card.Background) == Colors.White && card.CornerRadius == new CornerRadius(16) &&
            card.Padding == new Thickness(32) && card.BorderThickness == default && card.BoxShadow == BoxShadows.Parse("0 8 24 0 #1F23382C"),
            $"{where}: card is white, rounded, shadowed, padded and borderless");
        var cardBox = RectIn(card, surface);
        Check(card.Bounds.Width >= AuthSurface.MinCardWidth - .5 && card.Bounds.Width <= AuthSurface.CardWidth + .5,
            $"{where}: card width stays between {AuthSurface.MinCardWidth} and {AuthSurface.CardWidth} ({card.Bounds.Width:0.#})");
        var atMinCard = card.Bounds.Width <= AuthSurface.MinCardWidth + .5;
        var gap = Gap(cardBox, plate);
        var targetGap = bounds.Width * AuthSurface.GapShare;
        var atMinGap = gap <= AuthSurface.ClearGap + .5;
        var roomy = bounds.Width * AuthSurface.BoundaryShare - diameter / 2 - Math.Max(AuthSurface.MinMargin, bounds.Width * AuthSurface.CardShare) - AuthSurface.MinCardWidth >= targetGap;
        Check(cardBox.X >= AuthSurface.MinMargin - .5 && cardBox.X <= bounds.Width * AuthSurface.CardShare + .5 &&
            (Math.Abs(cardBox.X - bounds.Width * AuthSurface.CardShare) < 1 || atMinCard && atMinGap),
            $"{where}: card starts at about 12% and at least {AuthSurface.MinMargin} ({cardBox.X:0.#}, target {bounds.Width * AuthSurface.CardShare:0.#})");
        Check(panel.X <= bounds.Width * AuthSurface.BoundaryShare + .5 || atMinCard && atMinGap && cardBox.X <= AuthSurface.MinMargin + .5,
            $"{where}: panel edge moves right of 72% only when the gap, card and margin are at their minimum ({panel.X:0.#})");
        Check(gap >= AuthSurface.PlateGap - .5, $"{where}: card and plate do not intersect and stay at least 24 apart (gap {gap:0.#})");
        Check(roomy ? gap >= targetGap - .5 : atMinCard && gap >= AuthSurface.PlateGap - .5,
            $"{where}: card and plate keep a gap of 8% of the width when the window allows, otherwise the card is at its minimum first (gap {gap:0.#}, target {targetGap:0.#})");
        if (surface.Below is { } below)
        {
            var line = RectIn(below, surface);
            Check(Math.Abs(line.X - cardBox.X) < 1 && Math.Abs(line.Width - cardBox.Width) < 1 && line.Y >= cardBox.Bottom,
                $"{where}: demo account line follows the card width and left edge ({line.X:0.#}+{line.Width:0.#}, card {cardBox.X:0.#}+{cardBox.Width:0.#})");
        }
        var buttons = card.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("auth-primary")).ToList();
        Check(buttons.Count == 1 && Math.Abs(buttons[0].Bounds.Width - (card.Bounds.Width - 64)) < 1, $"{where}: primary button spans the card");
        var mark = RectIn(surface.Wordmark, surface); var scrollBox = RectIn(surface.FormScroll, surface);
        Check(surface.Wordmark.Text == "replate." && mark.X >= 0 && mark.Y >= 0 && mark.Bottom <= scrollBox.Y + .5 && Gap(mark, plate) > 0 &&
            !surface.FormScroll.GetVisualDescendants().Contains(surface.Wordmark), $"{where}: wordmark stays fully visible above the scroll area");
        Check(Math.Abs(mark.X - cardBox.X) < 1, $"{where}: wordmark aligns with the card left edge ({mark.X:0.#}, card {cardBox.X:0.#})");
        var header = RectIn(surface.Header, surface); var clip = RectIn(surface.FormClip, surface);
        Check(ColorOf(surface.Header.Background) == Color.Parse("#FAFAF8") && header.Contains(mark) && header.Bottom <= clip.Y + .5 &&
            surface.FormClip.ClipToBounds && surface.Header.GetVisualParent() is Panel host && host.Children.IndexOf(surface.Header) > host.Children.IndexOf(surface.FormClip),
            $"{where}: wordmark sits on a clean canvas band above a clipped scroll area (band bottom {header.Bottom:0.#}, scroll top {clip.Y:0.#})");
        Check(cardBox.X >= scrollBox.X - .5 && cardBox.Right <= scrollBox.Right + .5 && scrollBox.Right <= plate.X + 1,
            $"{where}: card is not clipped and the scroll area stays clear of the plate (card {cardBox.X:0.#}-{cardBox.Right:0.#}, scroll {scrollBox.X:0.#}-{scrollBox.Right:0.#}, plate {plate.X:0.#})");
        Check(scrollBox.X <= cardBox.X - AuthSurface.ShadowPad + .5 && clip.X <= cardBox.X - AuthSurface.ShadowPad + .5 && clip.Right >= cardBox.Right + AuthSurface.ShadowPad - .5 &&
            surface.FormScroll.GetVisualDescendants().OfType<ScrollContentPresenter>().Single(p => p.TemplatedParent == surface.FormScroll).ClipToBounds == false,
            $"{where}: card shadow has {AuthSurface.ShadowPad} of room on both sides (scroll {scrollBox.X:0.#}, clip {clip.X:0.#}-{clip.Right:0.#}, card {cardBox.X:0.#}-{cardBox.Right:0.#})");
        var barBox = VerticalBar(surface) is { IsVisible: true } shown ? RectIn(shown, surface)
            : new Rect(scrollBox.Right - AuthSurface.ScrollBarWidth, scrollBox.Y, AuthSurface.ScrollBarWidth, scrollBox.Height);
        Check(barBox.X >= cardBox.Right - .5 && barBox.X - cardBox.Right <= 24 + .5 && Math.Abs(barBox.Right - scrollBox.Right) < 1,
            $"{where}: scrollbar sits beside the card without covering it ({barBox.X - cardBox.Right:0.#} from the card edge)");
        Check(Gap(barBox, plate) >= AuthSurface.PlateGap - .5, $"{where}: scrollbar stays at least {AuthSurface.PlateGap} from the plate ({Gap(barBox, plate):0.#})");
        var scroll = surface.FormScroll;
        Check(scroll.HorizontalScrollBarVisibility == Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled &&
            scroll.Extent.Width <= scroll.Viewport.Width + .5 &&
            surface.GetVisualDescendants().OfType<ScrollViewer>().Where(s => s.TemplatedParent is null)
                .All(s => s.HorizontalScrollBarVisibility == Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled),
            $"{where}: no horizontal scrolling (extent {scroll.Extent.Width:0.#}, viewport {scroll.Viewport.Width:0.#})");
        Check(!window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.IsVisible && t.Text?.Contains((char)0x2014) == true), $"{where}: no em dash in UI text");
        await CheckScrollBars(surface, where);
    }

    private static List<ScrollBar> FormBars(AuthSurface surface) =>
        surface.FormScroll.GetVisualDescendants().OfType<ScrollBar>().Where(b => b.TemplatedParent == surface.FormScroll).ToList();

    private static ScrollBar VerticalBar(AuthSurface surface) =>
        FormBars(surface).Single(b => b.Orientation == Avalonia.Layout.Orientation.Vertical);

    private static async Task CheckScrollBars(AuthSurface surface, string where)
    {
        var scroll = surface.FormScroll;
        var bars = FormBars(surface);
        Check(bars.Count > 0 && bars.All(b => b.Theme == AuthSurface.ThinScrollBar), $"{where}: form scroll uses the thin scrollbar");
        foreach (var bar in bars.Where(b => b.GetVisualChildren().Any()))
        {
            var parts = bar.GetVisualDescendants().ToList();
            Check(!parts.OfType<RepeatButton>().Any() && !parts.OfType<Button>().Any(), $"{where}: scrollbar has no arrow buttons");
            Check(ColorOf(bar.Background) == Colors.Transparent && parts.OfType<Thumb>().Count() == 1 &&
                parts.OfType<Border>().All(b => b.TemplatedParent is Thumb), $"{where}: {bar.Orientation} scrollbar has no track, only a thumb");
        }
        var vertical = VerticalBar(surface);
        if (vertical.IsVisible)
        {
            var thumb = vertical.GetVisualDescendants().OfType<Thumb>().Single();
            var face = thumb.GetVisualDescendants().OfType<Border>().Single();
            Check(vertical.Bounds.Width >= 4 && vertical.Bounds.Width <= 6 && face.CornerRadius.TopLeft >= vertical.Bounds.Width / 2 - .01 &&
                ColorOf(thumb.Background) == Color.Parse("#59675D"), $"{where}: scrollbar is {vertical.Bounds.Width:0.#} wide with a rounded muted thumb");
        }
        Check(surface.GetVisualDescendants().OfType<ScrollBar>().Where(b => b.IsEffectivelyVisible && b.Opacity > 0)
            .All(b => !b.GetVisualDescendants().OfType<RepeatButton>().Any()), $"{where}: no scrollbar with a track or arrows is shown");
        Check(vertical.IsVisible == scroll.Extent.Height > scroll.Viewport.Height + .5, $"{where}: scrollbar exists only when the form overflows");
        await Pseudo(scroll, ":pointerover", false);
        Check(vertical.Opacity == 0 || scroll.Classes.Contains(AuthSurface.ScrollingClass), $"{where}: scrollbar is hidden at rest");
        await Pseudo(scroll, ":pointerover", true);
        var hovered = vertical.Opacity;
        await Pseudo(scroll, ":pointerover", false);
        Check(hovered == 1 && vertical.Opacity == 0, $"{where}: scrollbar shows on hover");
    }

    internal static async Task CheckTabIntoView(AuthSurface surface, Control first, Control last, string where)
    {
        var scroll = surface.FormScroll;
        var root = TopLevel.GetTopLevel(surface) ?? throw new InvalidOperationException($"{where}: surface has no window");
        scroll.Offset = default; await Task.Delay(200);
        first.Focus(NavigationMethod.Tab); await Task.Delay(100);
        var hidden = last.TranslatePoint(new Point(0, last.Bounds.Height), scroll)!.Value.Y > scroll.Viewport.Height + .5;
        var visited = 0;
        for (var i = 0; i < 60; i++)
        {
            var focused = root.FocusManager?.GetFocusedElement() as Control ?? throw new InvalidOperationException($"{where}: focus lost while tabbing");
            if (scroll.GetVisualDescendants().Contains(focused))
            {
                var box = new Rect(focused.TranslatePoint(default, scroll)!.Value, focused.Bounds.Size);
                Check(box.Y >= -.5 && box.Bottom <= scroll.Viewport.Height + .5,
                    $"{where}: Tab scrolls {AutomationProperties.GetName(focused)} ({focused.GetType().Name}) into view (top {box.Y:0.#}, bottom {box.Bottom:0.#}, viewport {scroll.Viewport.Height:0.#})");
                visited++;
            }
            if (focused == last) break;
            focused.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Tab, Source = focused });
            await Task.Delay(120);
        }
        Check(root.FocusManager?.GetFocusedElement() == last && visited > 2 && (!hidden || scroll.Offset.Y > 0),
            $"{where}: Tab reaches {last.GetType().Name} at the bottom of the form and scrolls the form ({visited} stops)");
        scroll.Offset = default; await Task.Delay(200);
    }

    internal static async Task CheckScrolled(AuthSurface surface, string where, Action? capture = null)
    {
        var scroll = surface.FormScroll;
        var mark = RectIn(surface.Wordmark, surface);
        scroll.Offset = new Vector(0, scroll.Extent.Height); await Task.Delay(300);
        var plate = RectIn(surface.Hero, surface); var card = RectIn(surface.Card, surface);
        Check(scroll.Offset.Y > 0 && scroll.Offset.X == 0, $"{where}: left column scrolls vertically only");
        Check(RectIn(surface.Wordmark, surface) == mark, $"{where}: wordmark does not move or clip while scrolling");
        Check(Math.Abs(plate.Center.Y - surface.Bounds.Height / 2) < 1 && Gap(card, plate) >= AuthSurface.PlateGap - .5, $"{where}: plate stays fixed and clear of the scrolled card");
        var bar = VerticalBar(surface);
        var thumb = RectIn(bar.GetVisualDescendants().OfType<Thumb>().Single(), bar);
        Check(scroll.Classes.Contains(AuthSurface.ScrollingClass) && bar.Opacity == 1 && bar.IsVisible && thumb.Bottom >= bar.Bounds.Height - 1 && thumb.Height > 8,
            $"{where}: scrollbar shows while scrolling with the thumb at the bottom (thumb {thumb.Y:0.#}-{thumb.Bottom:0.#} of {bar.Bounds.Height:0.#})");
        capture?.Invoke();
        scroll.Offset = default; await Task.Delay(1200);
        Check(!scroll.Classes.Contains(AuthSurface.ScrollingClass) && bar.Opacity == 0, $"{where}: scrollbar hides again after scrolling stops");
    }

    internal static async Task CheckSizes(MainWindow host, string output)
    {
        var screens = new (string Name, Func<Control> Make, Func<Control, AuthSurface> Surface)[]
        {
            ("sign-in", () => new SignInView(host, host.Session, "Simulated connection failure"), v => ((SignInView)v).Surface),
            ("register", () => new RegisterView(host, host.Session), v => ((RegisterView)v).Surface),
            ("register-owner", () => { var v = new RegisterView(host, host.Session); v.OwnerOption.IsChecked = true; return v; }, v => ((RegisterView)v).Surface)
        };
        foreach (var size in new[] { new Size(1360, 940), new Size(1040, 720), new Size(1283, 780), new Size(1920, 1080) })
            foreach (var (name, make, surfaceOf) in screens)
            {
                var view = make();
                var canvas = new Border { Child = view, Width = size.Width, Height = size.Height, Background = Brush.Parse("#FAFAF8") };
                var frame = new Window
                {
                    Width = 1040, Height = 720, FontFamily = new FontFamily("Helvetica Neue, Segoe UI, sans-serif"),
                    Content = new Canvas { Children = { canvas } }
                };
                try
                {
                    frame.Show();
                    var surface = surfaceOf(view);
                    await CheckSurface(frame, surface, name, size);
                    Check(InputVerification.Check(canvas, $"{name} at {size.Width:0}x{size.Height:0}") >= (name == "sign-in" ? 2 : name == "register" ? 4 : 8),
                        $"{name} at {size.Width:0}x{size.Height:0}: every text field is checked");
                    var share = surface.HeroPanel.Bounds.Width / size.Width;
                    Check(Math.Abs(share - .28) <= .01, $"{name} at {size.Width:0}x{size.Height:0}: panel is about 28% of the width ({share:0.###})");
                    var gap = RectIn(surface.Hero, surface).X - RectIn(surface.Card, surface).Right;
                    Check(gap >= AuthSurface.PlateGap - .5 && (size.Width == 1040 || gap >= size.Width * AuthSurface.GapShare - .5),
                        $"{name} at {size.Width:0}x{size.Height:0}: card and plate gap is at least {(size.Width == 1040 ? AuthSurface.PlateGap : size.Width * AuthSurface.GapShare):0.#} ({gap:0.#})");
                    var expectedCard = size.Width == 1040 ? AuthSurface.MinCardWidth : AuthSurface.CardWidth;
                    Check(Math.Abs(surface.Card.Bounds.Width - expectedCard) < 1, $"{name} at {size.Width:0}x{size.Height:0}: card is {expectedCard} wide ({surface.Card.Bounds.Width:0.#})");
                    var scroll = surface.FormScroll;
                    Check(name != "register-owner" || scroll.Extent.Height > scroll.Viewport.Height || size.Height > 720,
                        $"{name} at {size.Width:0}x{size.Height:0}: long owner form scrolls vertically");
                    void Capture(string suffix)
                    {
                        using var bitmap = new RenderTargetBitmap(new PixelSize((int)size.Width, (int)size.Height), new Vector(96, 96));
                        bitmap.Render(canvas); bitmap.Save(Path.Combine(output, $"auth-{name}-{size.Width:0}x{size.Height:0}{suffix}.png"), PngBitmapEncoderOptions.Default);
                    }
                    if (scroll.Extent.Height > scroll.Viewport.Height) await CheckScrolled(surface, $"{name} at {size.Width:0}x{size.Height:0}", () => Capture("-scrolled"));
                    if (view is RegisterView tabbed && scroll.Extent.Height > scroll.Viewport.Height)
                        await CheckTabIntoView(surface, tabbed.NameInput, tabbed.BackButton, $"{name} at {size.Width:0}x{size.Height:0}");
                    Capture("");
                }
                finally { frame.Close(); }
            }
        host.Activate();
        Console.WriteLine("All Replate auth surface checks passed (sign-in, registration, owner registration at 1360x940, 1040x720, 1283x780 and 1920x1080).");
    }

    private static async Task Pseudo(StyledElement element, string pseudo, bool on)
    {
        ((IPseudoClasses)element.Classes).Set(pseudo, on); await Task.Delay(100);
    }

    internal static async Task CheckState(TemplatedControl control, string pseudo, Color background, Color foreground, string what)
    {
        await Pseudo(control, pseudo, true);
        var (fill, ink) = (ColorOf(control.Background), ColorOf(control.Foreground));
        await Pseudo(control, pseudo, false);
        Check(fill == background && ink == foreground, $"{what} {pseudo} uses {background}/{foreground} (got {fill}/{ink})");
    }

    internal static async Task CheckReveal(TextBox input, string screen)
    {
        var previous = input.Text;
        input.Text = ""; await Task.Delay(300);
        var toggle = AuthSurface.RevealToggle(input);
        var icon = (Avalonia.Controls.Shapes.Path)toggle.Content!;
        Check(!input.Classes.Contains("revealPasswordButton") && !input.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>().Any(),
            $"{screen}: native Fluent reveal button is not used");
        Check(toggle.IsEffectivelyVisible && toggle.Bounds.Width > 0 && toggle.GetVisualAncestors().Contains(input) && input.PasswordChar != default &&
            AutomationProperties.GetName(toggle) == AuthSurface.RevealName && icon.Data == AuthSurface.EyeIcon,
            $"{screen}: password toggle is visible inside the empty field with an eye icon");
        Click(toggle); await Task.Delay(100);
        Check(input.PasswordChar == default && AutomationProperties.GetName(toggle) == AuthSurface.ConcealName && icon.Data == AuthSurface.EyeOffIcon,
            $"{screen}: toggle shows the password and switches to the crossed eye");
        Click(toggle); await Task.Delay(100);
        Check(input.PasswordChar != default && AutomationProperties.GetName(toggle) == AuthSurface.RevealName && icon.Data == AuthSurface.EyeIcon,
            $"{screen}: toggle hides the password again");
        var ink = Color.Parse("#23382C"); var green = Color.Parse("#245B43"); var leaf = Color.Parse("#4FA37E");
        Check(ColorOf(toggle.Background) == Colors.Transparent && ColorOf(toggle.Foreground) == ink && ColorOf(icon.Stroke) == ink && toggle.Focusable && toggle.IsTabStop,
            $"{screen}: toggle defaults to an ink icon and is keyboard focusable");
        Check(toggle.Bounds.Width <= icon.Bounds.Width + 4 + .5 && toggle.Bounds.Height <= icon.Bounds.Height + 4 + .5 && toggle.Transitions is null or { Count: 0 },
            $"{screen}: toggle hover area hugs the icon ({toggle.Bounds.Width:0.#}x{toggle.Bounds.Height:0.#}) with no transition");
        await Pseudo(toggle, ":pointerover", true);
        var hover = (ColorOf(toggle.Background), ColorOf(toggle.Foreground), ColorOf(icon.Stroke), ColorOf(toggle.BorderBrush));
        await Pseudo(toggle, ":pointerover", false);
        Check(hover == (Colors.Transparent, leaf, leaf, Colors.Transparent) && ColorOf(icon.Stroke) == ink,
            $"{screen}: password toggle hover turns only the icon light green, without fill or border (got {hover}), and returns to ink");
        await CheckState(toggle, ":pressed", Colors.Transparent, ink, $"{screen}: password toggle");
        var masked = input.PasswordChar != default;
        input.Focus(NavigationMethod.Tab); await Task.Delay(100);
        input.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Tab, Source = input }); await Task.Delay(100);
        Check(TopLevel.GetTopLevel(input)?.FocusManager?.GetFocusedElement() == toggle, $"{screen}: Tab from the field moves focus to the toggle");
        toggle.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, Source = toggle }); await Task.Delay(100);
        var afterEnter = input.PasswordChar != default;
        toggle.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Space, Source = toggle });
        toggle.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Space, Source = toggle }); await Task.Delay(100);
        Check(masked && !afterEnter && input.PasswordChar != default, $"{screen}: Enter and Space on the toggle show and hide the password");
        await Pseudo(toggle, ":focus-visible", true);
        var ring = ColorOf(toggle.BorderBrush);
        await Pseudo(toggle, ":focus-visible", false);
        Check(ring == green && ColorOf(toggle.BorderBrush) == Colors.Transparent, $"{screen}: password toggle shows a keyboard focus ring");
        input.Text = previous;
    }

    internal static async Task CheckHover(Button button, Color background, string screen)
    {
        var presenter = button.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_ContentPresenter");
        Color Fill() => ColorOf(presenter.Background);
        await Pseudo(button, ":pointerover", false);
        var normal = Fill();
        await Pseudo(button, ":pointerover", true);
        var hovered = Fill();
        await Pseudo(button, ":pointerover", false);
        Check(hovered != normal && hovered == background && Fill() == normal, $"{screen}: {button.Content} hover changes its background ({normal} to {hovered}, expected {background})");
    }

    internal static void CheckTitle(TextBlock title, string text, string screen) =>
        Check(title.Text == text && title.FontSize == 28 && title.FontWeight == FontWeight.Bold && title.IsEffectivelyVisible, $"{screen}: title {text} is 28 bold");

    internal static async Task CheckRoles(RegisterView register)
    {
        var (customer, owner) = (register.CustomerOption, register.OwnerOption);
        var content = register.Surface.Card.Bounds.Width - 64;
        Check(customer.GroupName == owner.GroupName && AutomationProperties.GetName(customer) == "Pelanggan" && AutomationProperties.GetName(owner) == "Pemilik restoran",
            "Daftar: role options are one radio group with automation names");
        Check(Math.Abs(customer.Bounds.Width - owner.Bounds.Width) < 1 && Math.Abs(owner.Bounds.Right - customer.Bounds.X - content) < 1 &&
            customer.CornerRadius == new CornerRadius(6) && Math.Abs(customer.Bounds.Y - owner.Bounds.Y) < 1,
            "Daftar: role buttons are equal width, rounded, and span the card together");
        var (green, ink, white) = (Color.Parse("#245B43"), Color.Parse("#23382C"), Colors.White);
        customer.IsChecked = true; await Task.Delay(100);
        Check(ColorOf(customer.Background) == green && ColorOf(customer.Foreground) == white &&
            ColorOf(owner.Background) == Color.Parse("#F1F3EE") && ColorOf(owner.Foreground) == ink &&
            ColorOf(customer.BorderBrush) == Colors.Transparent && ColorOf(owner.BorderBrush) == Colors.Transparent,
            "Daftar: selected role is primary with white text, unselected role is sidebar with ink text and no border");
        Check(!customer.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Ellipse>().Any(), "Daftar: role buttons show no radio circle");
        await CheckState(customer, ":pointerover", ink, white, "Daftar: selected role");
        await CheckState(owner, ":pointerover", Color.Parse("#E0E9DF"), green, "Daftar: unselected role");
        owner.IsChecked = true; await Task.Delay(100);
        Check(customer.IsChecked == false, "Daftar: role buttons are exclusive");
        customer.IsChecked = true; customer.Focus(NavigationMethod.Directional); await Task.Delay(100);
        customer.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Right }); await Task.Delay(100);
        Check(owner.IsChecked == true && customer.IsChecked == false && owner.IsFocused, "Daftar: right arrow selects and focuses Pemilik restoran");
        owner.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Left }); await Task.Delay(100);
        Check(customer.IsChecked == true && owner.IsChecked == false && customer.IsFocused, "Daftar: left arrow returns to Pelanggan");
        await Pseudo(customer, ":focus-visible", true);
        var ring = ColorOf(customer.BorderBrush);
        await Pseudo(customer, ":focus-visible", false);
        Check(ring == ink, "Daftar: role buttons show a keyboard focus ring");
    }

    internal static async Task CheckTimeFields(RegisterView register)
    {
        await Task.Delay(300);
        Check(!register.GetVisualDescendants().OfType<TimePicker>().Any() && !register.GetLogicalDescendants().OfType<TimePicker>().Any(), "Daftar pemilik: no TimePicker remains");
        foreach (var (field, label) in new[] { (register.OpeningInput, "Jam buka"), (register.ClosingInput, "Jam tutup") })
        {
            Check(field.Hour.ItemsSource is IEnumerable<string> hours && hours.SequenceEqual(Enumerable.Range(0, 24).Select(i => i.ToString("00"))) &&
                field.Minute.ItemsSource is IEnumerable<string> minutes && minutes.SequenceEqual(Enumerable.Range(0, 60).Select(i => i.ToString("00"))),
                $"{label}: hour 00-23 and minute 00-59 dropdowns");
            Check(AutomationProperties.GetName(field) == label && AutomationProperties.GetName(field.Hour) == $"{label}, jam" &&
                AutomationProperties.GetName(field.Minute) == $"{label}, menit", $"{label}: hour and minute automation names");
            Check(field.CornerRadius.TopLeft >= field.Bounds.Height / 2 - .5 && ColorOf(field.Background) == Colors.White &&
                field.BorderThickness == new Thickness(1) && field.BorderBrush is not null && field.BorderBrush.Equals(register.NameInput.BorderBrush) &&
                field.BoxShadow == default && field.Effect is null && field.GetVisualDescendants().All(v => v.Effect is null),
                $"{label}: white pill with the text field border and no shadow or effect");
            Check(!field.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text is { } s && (s.Contains("AM") || s.Contains("PM"))), $"{label}: no AM/PM text");
            var pill = RectIn(field, register);
            foreach (var part in new[] { field.Hour, field.Minute }.SelectMany(c => c.GetVisualDescendants().OfType<Control>().Prepend(c)).Where(c => c is ComboBox or Viewbox or TextBlock or ContentPresenter && c.IsVisible && c.Bounds.Width > 0 && !c.GetVisualAncestors().OfType<Viewbox>().Any()))
            {
                var box = RectIn(part, register);
                Check(box.X >= pill.X - .5 && box.Right <= pill.Right + .5, $"{label}: hour and minute stay inside the pill ({part.GetType().Name} {box.X:0.#}-{box.Right:0.#}, pill {pill.X:0.#}-{pill.Right:0.#})");
            }
            field.Value = null;
            field.Hour.SelectedIndex = 7; await Task.Delay(50);
            Check(field.Value == new TimeOnly(7, 0), $"{label}: picking an hour fills minute 00");
            field.Minute.SelectedIndex = 30;
            Check(field.Value == new TimeOnly(7, 30), $"{label}: hour and minute produce a TimeOnly");
            field.Hour.SelectedIndex = 23; field.Minute.SelectedIndex = 59;
            Check(field.Value == new TimeOnly(23, 59), $"{label}: 24-hour values without AM/PM");
            field.Value = null;
            Check(field.Value is null && field.Hour.SelectedIndex == -1 && field.Minute.SelectedIndex == -1, $"{label}: value can be cleared");
        }
        var open = RectIn(register.OpeningInput, register); var close = RectIn(register.ClosingInput, register);
        Check(Math.Abs(open.Y - close.Y) < 1 && open.Right < close.X, "Jam buka and Jam tutup sit side by side");
        foreach (var (field, label) in new[] { (open, "Jam buka"), (close, "Jam tutup") })
        {
            var text = register.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == label && t.IsEffectivelyVisible);
            var box = RectIn(text, register);
            Check(box.Bottom <= field.Y && Math.Abs(box.X - field.X) < 2, $"{label}: visible label sits above its time field");
        }
    }

    internal static async Task Run(string output)
    {
        void Ensure(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        async Task Settle() => await Task.Delay(300);
        var path = Path.Combine(Path.GetTempPath(), "replate-auth-" + Guid.NewGuid().ToString("N"), "state.json");
        var session = new DemoSession(path);
        var window = new MainWindow(session) { Width = 1040, Height = 720 };
        try
        {
            window.Show(); await Settle();
            void Shot(string name)
            {
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
                bitmap.Render(window); bitmap.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
            }
            IEnumerable<TextBlock> Texts() => window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsVisible);
            Button Find(string text) => window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == text);
            SignInView SignInPage() => window.CurrentPage as SignInView ?? throw new InvalidOperationException("Sign-in page expected");
            async Task SignIn(string email, string password, bool enter = false)
            {
                var page = SignInPage();
                page.EmailInput.Text = email; page.PasswordInput.Text = password;
                if (enter)
                {
                    page.PasswordInput.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
                }
                else Click(page.SignInButton);
                await Settle();
            }
            async Task SignOut()
            {
                if (session.CurrentUser is Customer) await TopBarVerification.SignOut(window);
                else { Click(Find("Keluar")); await Settle(); }
                Ensure(window.CurrentPage is SignInView && !session.IsSignedIn, "Keluar signs out and returns to sign-in");
                Ensure(!window.GetVisualDescendants().OfType<Button>().Any(b => b.Content as string is "Dashboard") &&
                    !window.GetVisualDescendants().OfType<CustomerTopBar>().Any(),
                    "Signed-out window shows no area navigation");
            }

            // Sign-in screen
            var signIn = SignInPage();
            Ensure(window.FocusManager?.GetFocusedElement() == signIn.EmailInput, "Initial focus is Email");
            Ensure(signIn.PasswordInput.PasswordChar != default, "Password is masked");
            Ensure(Avalonia.Automation.AutomationProperties.GetName(signIn.EmailInput) == "Email" &&
                Avalonia.Automation.AutomationProperties.GetName(signIn.PasswordInput) == "Kata sandi", "Sign-in automation names match labels");
            Ensure(Texts().Any(t => t.Text == "Masuk") && Texts().Any(t => t.Text == "replate.") &&
                Texts().Any(t => t.Text == "Akun demo: pelanggan@replate.demo atau dapur-bumi@replate.demo, kata sandi replate123."),
                "Sign-in shows wordmark, heading and demo account hint");
            Ensure(!window.GetVisualDescendants().OfType<Button>().Any(b => b.Content as string is "Jelajahi makanan" or "Buka area restoran" or "Ganti area"),
                "Old automatic demo entry buttons are removed");
            Ensure(((Avalonia.Media.ISolidColorBrush)signIn.SignInButton.Background!).Color == Avalonia.Media.Color.Parse("#245B43"), "Masuk is the green primary action");
            await CheckSurface(window, signIn.Surface, "Masuk");
            Ensure(signIn.Surface.Warning is null, "No database warning without a startup error");
            Ensure(Texts().Single(t => t.Text?.StartsWith("Akun demo:") == true).TranslatePoint(default, signIn.Surface)!.Value.Y >
                signIn.Surface.Card.TranslatePoint(default, signIn.Surface)!.Value.Y + signIn.Surface.Card.Bounds.Height, "Demo account line sits below the card");
            CheckTitle(signIn.TitleText, "Masuk", "Masuk");
            await CheckReveal(signIn.PasswordInput, "Masuk");
            await CheckHover(signIn.SignInButton, Color.Parse("#23382C"), "Masuk");
            await CheckHover(signIn.RegisterButton, Color.Parse("#E0E9DF"), "Masuk");
            Shot("auth-sign-in");
            Click(AuthSurface.RevealToggle(signIn.PasswordInput));
            Ensure(signIn.PasswordInput.PasswordChar == default, "Password revealed before submission");
            await SignIn(DemoAccounts.CustomerEmail, "salah-sekali", true);
            Ensure(window.CurrentPage == signIn && !session.IsSignedIn && signIn.ErrorText.IsVisible &&
                signIn.ErrorText.Text == AccountService.SignInFailedMessage, "Wrong password shows generic message and stays on sign-in");
            Ensure(signIn.PasswordInput.Text == "" && signIn.EmailInput.Text == DemoAccounts.CustomerEmail, "Password cleared after submission");
            Ensure(signIn.PasswordInput.PasswordChar != default && AutomationProperties.GetName(AuthSurface.RevealToggle(signIn.PasswordInput)) == AuthSurface.RevealName,
                "Password returns to hidden mode after submission");
            Shot("auth-sign-in-error");
            await SignIn("tidak-ada@replate.demo", "replate123");
            Ensure(window.CurrentPage == signIn && signIn.ErrorText.Text == AccountService.SignInFailedMessage, "Unknown email uses the same generic message");

            // Demo accounts
            await SignIn(DemoAccounts.OwnerEmail("Dapur Bumi"), DemoAccounts.Password, true);
            Ensure(window.CurrentPage is DashboardView && session.CurrentUser is RestaurantOwner && session.Restaurant.Name == "Dapur Bumi",
                "Demo owner sign-in opens dashboard");
            Ensure(Texts().Any(t => t.Text == "Dapur Bumi"), "Restaurant rail shows owner's restaurant");
            await SignOut();
            await SignIn(DemoAccounts.CustomerEmail, DemoAccounts.Password);
            Ensure(window.CurrentPage is MarketplaceView && session.CurrentUser is Customer, "Demo customer sign-in opens marketplace");
            await SignOut();

            // Role guards
            var listing = session.Listings[0];
            var bumiOrder = session.FindRestaurant(listing.Menu.RestaurantId)!.Orders[0];
            var customerRoutes = new (string Name, Action Show)[]
            {
                ("ShowMarketplace", window.ShowMarketplace), ("ShowFood", () => window.ShowFood(listing)), ("ShowCart", window.ShowCart),
                ("ShowCustomerOrders", window.ShowCustomerOrders), ("ShowOrder(customer)", () => window.ShowOrder(bumiOrder, false))
            };
            var ownerRoutes = new (string Name, Action Show)[]
            {
                ("ShowDashboard", window.ShowDashboard), ("ShowMenus", window.ShowMenus), ("ShowMenuEditor", () => window.ShowMenuEditor()),
                ("ShowListings", window.ShowListings), ("ShowListingEditor", () => window.ShowListingEditor()), ("ShowProduction", window.ShowProduction),
                ("ShowRestaurantOrders", window.ShowRestaurantOrders), ("ShowOrder(restaurant)", () => window.ShowOrder(bumiOrder, true))
            };
            void Guarded(Action signIn, (string Name, Action Show)[] routes, string role)
            {
                foreach (var route in routes)
                {
                    signIn();
                    route.Show();
                    Ensure(window.CurrentPage is SignInView && !session.IsSignedIn, $"{route.Name} redirects {role} to sign-in");
                }
            }
            Guarded(() => { }, customerRoutes.Concat(ownerRoutes).ToArray(), "signed-out user");
            Guarded(() => session.AsOwner(), customerRoutes, "owner");
            Guarded(() => session.AsCustomer(), ownerRoutes, "customer");
            Guarded(() => session.AsOwner("Warung Sambal Ijo"), new (string, Action)[]
            {
                ("ShowOrder(other restaurant)", () => window.ShowOrder(bumiOrder, true)),
                ("ShowMenuEditor(other restaurant)", () => window.ShowMenuEditor(listing.Menu)),
                ("ShowListingEditor(other restaurant)", () => window.ShowListingEditor(listing))
            }, "other owner");

            // Registration validation
            Click(SignInPage().RegisterButton); await Settle();
            var register = window.CurrentPage as RegisterView ?? throw new InvalidOperationException("Daftar akun baru opens registration");
            Ensure(window.FocusManager?.GetFocusedElement() == register.NameInput, "Registration focuses Nama");
            Ensure(register.CustomerOption.IsChecked == true && !register.ProfileSection.IsVisible, "Customer is default and hides restaurant profile");
            Ensure(Texts().Any(t => t.Text == "Daftar akun"), "Registration card shows its title");
            CheckTitle(register.TitleText, "Daftar akun", "Daftar");
            Ensure(!window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text is { } text &&
                (text.Contains("Pemilik restoran mengisi profil restoran") || text.StartsWith("Kata sandi minimal"))), "Registration subtitle sentence is removed");
            await CheckSurface(window, register.Surface, "Daftar");
            await CheckReveal(register.PasswordInput, "Daftar");
            await CheckReveal(register.ConfirmPasswordInput, "Daftar");
            await CheckHover(register.RegisterButton, Color.Parse("#23382C"), "Daftar");
            await CheckHover(register.BackButton, Color.Parse("#E0E9DF"), "Daftar");
            await CheckRoles(register);
            var users = session.Users.Count;
            register.NameInput.Text = "Pelanggan Uji";
            register.EmailInput.Text = DemoAccounts.CustomerEmail.ToUpperInvariant();
            register.PasswordInput.Text = "pendek"; register.ConfirmPasswordInput.Text = "berbeda";
            Click(AuthSurface.RevealToggle(register.PasswordInput));
            register.ConfirmPasswordInput.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            await Settle();
            Ensure(register.FieldErrors["email"].IsVisible && register.FieldErrors["email"].Text == "Email sudah terdaftar.", "Duplicate email shown inline");
            Ensure(register.FieldErrors["password"].Text == $"Kata sandi minimal {AccountService.MinimumPasswordLength} karakter.", "Short password shown inline");
            Ensure(register.FieldErrors["confirmPassword"].Text == "Kata sandi tidak sama.", "Mismatched confirmation shown inline");
            Ensure(!register.FieldErrors["restaurantName"].IsVisible, "Customer registration does not validate restaurant profile");
            Ensure(register.PasswordInput.PasswordChar != default && register.PasswordInput.Text == "pendek", "Submitting hides the password and keeps it for correction");
            Ensure(window.CurrentPage == register && session.Users.Count == users && !session.IsSignedIn, "Invalid registration creates nothing");
            Ensure(window.FocusManager?.GetFocusedElement() == register.EmailInput, "Focus first invalid registration field");
            Shot("auth-register-errors");
            register.OwnerOption.IsChecked = true; await Settle();
            Ensure(register.ProfileSection.IsVisible, "Owner role shows restaurant profile");
            foreach (var (key, message) in new[] { ("restaurantName", "Nama restoran wajib diisi."), ("address", "Alamat restoran wajib diisi."),
                ("phone", "Nomor telepon wajib diisi."), ("openingTime", "Jam buka wajib diisi."), ("closingTime", "Jam tutup wajib diisi.") })
                Ensure(register.FieldErrors[key].IsVisible && register.FieldErrors[key].Text == message, $"Empty profile error for {key} shown inline");
            foreach (var (input, label) in new (Control, string)[] { (register.NameInput, "Nama"), (register.EmailInput, "Email"),
                (register.PasswordInput, "Kata sandi"), (register.ConfirmPasswordInput, "Ulangi kata sandi"), (register.RestaurantNameInput, "Nama restoran"),
                (register.AddressInput, "Alamat"), (register.PhoneInput, "Telepon"), (register.DescriptionInput, "Deskripsi"),
                (register.OpeningInput, "Jam buka"), (register.ClosingInput, "Jam tutup"), (register.CustomerOption, "Pelanggan"), (register.OwnerOption, "Pemilik restoran") })
                Ensure(Avalonia.Automation.AutomationProperties.GetName(input) == label && (input is RadioButton || Texts().Any(t => t.Text == label)),
                    $"{label} has visible label and matching automation name");
            Ensure(Texts().Any(t => t.Text == "Untuk buka 24 jam, isi 00:00 sampai 23:59."), "24-hour helper text shown");
            await CheckSurface(window, register.Surface, "Daftar pemilik");
            await CheckTimeFields(register);
            var formScroll = register.Surface.FormScroll;
            Ensure(formScroll.Extent.Height > formScroll.Viewport.Height, "Owner registration form scrolls vertically at 1040x720");
            await CheckScrolled(register.Surface, "Daftar pemilik at 1040x720");
            await CheckTabIntoView(register.Surface, register.NameInput, register.BackButton, "Daftar pemilik at 1040x720");
            Shot("auth-register-owner-errors");
            register.CustomerOption.IsChecked = true; await Settle();
            Ensure(!register.ProfileSection.IsVisible && !register.FieldErrors["restaurantName"].IsVisible, "Switching back hides profile errors");

            // Customer registration
            register.EmailInput.Text = "pelanggan-baru@replate.demo";
            register.PasswordInput.Text = "rahasia123"; register.ConfirmPasswordInput.Text = "rahasia123";
            Ensure(register.FieldErrors.Values.All(e => !e.IsVisible), "Corrected registration fields clear errors");
            Click(register.RegisterButton); await Settle();
            Ensure(window.CurrentPage is MarketplaceView && session.CurrentUser is Customer { Email: "pelanggan-baru@replate.demo" } &&
                window.SuccessMessage == "Akun berhasil dibuat.", "New customer registration signs in to marketplace with notification");
            Ensure(!session.CustomerOrders.Any() && session.Cart.Items.Count == 0, "New customer starts with empty cart and orders");
            await SignOut();

            // Owner registration
            Click(SignInPage().RegisterButton); await Settle();
            register = (RegisterView)window.CurrentPage!;
            register.OwnerOption.IsChecked = true;
            register.NameInput.Text = "Pemilik Uji"; register.EmailInput.Text = "kedai-uji@replate.demo";
            register.PasswordInput.Text = "rahasia123"; register.ConfirmPasswordInput.Text = "rahasia123";
            register.RestaurantNameInput.Text = "Kedai Uji Baru"; register.AddressInput.Text = "Jl. Uji No. 1, Yogyakarta";
            register.PhoneInput.Text = "0274 555 0909"; register.DescriptionInput.Text = "Restoran untuk pemeriksaan.";
            register.OpeningInput.Hour.SelectedIndex = 0; register.OpeningInput.Minute.SelectedIndex = 0;
            register.ClosingInput.Hour.SelectedIndex = 23; register.ClosingInput.Minute.SelectedIndex = 59;
            Ensure(register.OpeningInput.Value == new TimeOnly(0, 0) && register.ClosingInput.Value == new TimeOnly(23, 59), "Time fields hold 24-hour TimeOnly values");
            Shot("auth-register-owner");
            Click(register.RegisterButton); await Settle();
            Ensure(window.CurrentPage is DashboardView && session.CurrentUser is RestaurantOwner && session.Restaurant.Name == "Kedai Uji Baru" &&
                window.SuccessMessage == "Akun berhasil dibuat.", "New owner registration opens own dashboard");
            Ensure(register.PasswordInput.Text == "" && register.ConfirmPasswordInput.Text == "" && register.PasswordInput.PasswordChar != default,
                "Submitted registration clears both password fields and hides them");
            var created = session.Restaurant;
            Ensure(created.Menus.Count == 0 && created.Orders.Count == 0 && !session.RestaurantListings.Any(), "New restaurant starts empty");
            Ensure(created.OpeningTime == new TimeOnly(0, 0) && created.ClosingTime == new TimeOnly(23, 59), "Opening hours saved");
            Ensure(Texts().Any(t => t.Text == "Kedai Uji Baru") && !Texts().Any(t => t.Text == "Dapur Bumi"), "Rail shows the new restaurant name");
            async Task CheckEmptyPage<T>(Action show, string title, string action) where T : Control
            {
                show(); await Settle();
                Ensure(window.CurrentPage is T && session.CurrentUser is RestaurantOwner, $"{typeof(T).Name} opens for new restaurant");
                Ensure(Texts().Any(t => t.Text == title) && window.GetVisualDescendants().OfType<Button>().Any(b => b.IsVisible && b.Content as string == action),
                    $"{typeof(T).Name} has empty state with {action}");
                Ensure(!Texts().Any(t => t.Text is { } text && (text.Contains("NaN") || text.Contains('∞') || text.Contains("Dapur Bumi") ||
                    text.Contains("Chicken rice bowl") || text.Contains("Restoran ini bukan milik"))), $"{typeof(T).Name} shows no invalid numbers or other restaurant data");
            }
            await CheckEmptyPage<DashboardView>(window.ShowDashboard, "Belum ada menu", "Tambah menu");
            Ensure(Texts().Any(t => t.Text == "Belum ada produksi pada periode ini") && Texts().Any(t => t.Text == "Belum ada surplus pada periode ini") &&
                !Texts().Any(t => t.Text?.Contains('%') == true), "Empty dashboard avoids meaningless percentages");
            Shot("auth-new-owner-dashboard");
            await CheckEmptyPage<MenusView>(window.ShowMenus, "Belum ada menu. Tambahkan menu untuk mulai mencatat produksi.", "Tambah menu");
            await CheckEmptyPage<ListingsView>(window.ShowListings, "Belum ada penawaran. Catat produksi, lalu tawarkan surplus yang tersedia.", "Buat penawaran");
            await CheckEmptyPage<ListingEditorView>(() => window.ShowListingEditor(), "Tambahkan menu aktif terlebih dahulu.", "Tambah menu");
            await CheckEmptyPage<ProductionView>(window.ShowProduction, "Tambahkan menu sebelum mencatat produksi.", "Tambah menu");
            await CheckEmptyPage<OrdersView>(window.ShowRestaurantOrders, "Belum ada pesanan", "Buat penawaran");
            Shot("auth-new-owner-orders");
            Click(Find("Buat penawaran")); await Settle();
            Ensure(window.CurrentPage is ListingEditorView, "Orders empty state leads to listing creation");
            window.ShowDashboard(); ((DashboardView)window.CurrentPage!).PeriodPicker.SelectedIndex = 0; await Settle();
            Ensure(window.CurrentPage is DashboardView && Texts().Any(t => t.Text == "Belum ada menu"), "Empty dashboard survives period change");
            Click(Find("Tambah menu")); await Settle();
            Ensure(window.CurrentPage is MenuEditorView, "Dashboard empty state leads to menu creation");
            var newMenu = session.SaveMenu(null, "Nasi uji", "", "Makanan utama", 15000, .3m);
            window.ShowDashboard(); await Settle();
            Ensure(Texts().Any(t => t.Text == "Surplus per menu") && Texts().Any(t => t.Text == "Belum ada data") &&
                !Texts().Any(t => t.Text?.Contains("NaN") == true), "Menu without production shows rows without invalid numbers");
            window.ShowProduction(); await Settle();
            Ensure(window.CurrentPage is ProductionView production && production.MenuInput.SelectedItem == newMenu, "Production opens with new menu");
            await SignOut();

            // Isolation between owners
            await SignIn(DemoAccounts.OwnerEmail("Dapur Bumi"), DemoAccounts.Password);
            Ensure(session.Restaurant.Name == "Dapur Bumi" && !session.Restaurant.Menus.Contains(newMenu), "Other owner keeps own restaurant");
            window.ShowMenus(); await Settle();
            Ensure(!Texts().Any(t => t.Text is "Nasi uji" or "Kedai Uji Baru"), "New owner's data is hidden from other owner");
            window.ShowMenuEditor(newMenu);
            Ensure(window.CurrentPage is SignInView && !session.IsSignedIn, "Other owner cannot open the new owner's menu");
            await SignIn(DemoAccounts.CustomerEmail, DemoAccounts.Password);
            Ensure(window.Marketplace.VisibleCount > 0 && !Texts().Any(t => t.Text == "Nasi uji"), "Menu without listing is not in marketplace");
            await SignOut();

            // Database warning
            var warned = new MainWindow(new DemoSession(Path.Combine(Path.GetDirectoryName(path)!, "warning", "state.json")), "Simulated connection failure") { Width = 1040, Height = 720 };
            try
            {
                warned.Show(); await Settle();
                var warnedPage = warned.CurrentPage as SignInView ?? throw new InvalidOperationException("Warning window opens on sign-in");
                var warning = warnedPage.Surface.Warning ?? throw new InvalidOperationException("Database warning shown on startup error");
                var warningTop = warning.TranslatePoint(default, warnedPage.Surface)!.Value;
                var cardTop = warnedPage.Surface.Card.TranslatePoint(default, warnedPage.Surface)!.Value;
                Ensure(warning.IsVisible && Math.Abs(warning.Bounds.Width - warnedPage.Surface.Card.Bounds.Width) < 1 &&
                    Math.Abs(warningTop.X - cardTop.X) < 1 && warningTop.Y + warning.Bounds.Height < cardTop.Y, "Database warning sits above the card at card width");
                Ensure(warning.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Database belum tersambung. Pesanan belum dapat disimpan."),
                    "Database warning uses the short message");
                await CheckSurface(warned, warnedPage.Surface, "Masuk with database warning");
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)warned.Bounds.Width, (int)warned.Bounds.Height), new Vector(96, 96));
                bitmap.Render(warned); bitmap.Save(Path.Combine(output, "auth-sign-in-database-warning.png"), PngBitmapEncoderOptions.Default);
            }
            finally { warned.Close(); }

            // Persistence
            var reloaded = new DemoSession(path);
            Ensure(reloaded.StorageError is null && reloaded.SignIn("kedai-uji@replate.demo", "rahasia123") is RestaurantOwner &&
                reloaded.Restaurant.Name == "Kedai Uji Baru" && reloaded.Restaurant.Menus.Single().Name == "Nasi uji", "Registered owner survives reload");
            Ensure(reloaded.SignIn("pelanggan-baru@replate.demo", "rahasia123") is Customer, "Registered customer survives reload");
            Console.WriteLine("All Replate sign-in checks passed (sign-in, demo accounts, wrong password, registration validation, new customer/owner, empty restaurant pages, Keluar, role guards, owner isolation, reload).");
        }
        finally { window.Close(); }
    }
}
