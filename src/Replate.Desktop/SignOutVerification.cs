using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Replate.Domain.Entities;

namespace Replate.Desktop;

internal static class SignOutVerification
{
    // Checks
    internal static async Task Check(MainWindow window, string label)
    {
        var owner = window.Session.CurrentUser is RestaurantOwner;
        var user = window.Session.CurrentUser ?? throw new InvalidOperationException($"{label}: signed in before Keluar");
        var page = window.CurrentPage;
        var cart = window.Session.Cart.Items.Sum(i => i.Quantity);
        bool Unchanged() => window.Session.IsSignedIn && window.Session.CurrentUser == user && window.CurrentPage == page &&
            window.Session.Cart.Items.Sum(i => i.Quantity) == cart;

        // Open
        var dialog = await Open(window);
        Ensure(Unchanged() && !window.TopBar.ProfileMenu.IsOpen, $"{label}: Keluar opens the confirmation and the user stays signed in");
        Ensure(window.GetVisualDescendants().Contains(dialog.Card) && dialog.ScrimLayer.Background == ModalDialog.Scrim &&
            Math.Abs(dialog.Card.Width - SignOutDialog.CardWidth) < .5 && dialog.Card.CornerRadius == new CornerRadius(16) &&
            dialog.Card.Background == Brushes.White, $"{label}: the confirmation reuses the shared scrim and white card");
        Ensure(dialog.Title.Text == SignOutDialog.TitleText && dialog.Title.FontSize == 20 && dialog.Title.FontWeight == FontWeight.Bold &&
            dialog.MessageTextBlock.Text == SignOutDialog.MessageText && dialog.MessageTextBlock.Foreground == Ui.Muted &&
            !$"{dialog.Title.Text}{dialog.MessageTextBlock.Text}".Contains('—'), $"{label}: title and message");
        Ensure(dialog.CancelButton.Classes.Contains(CollectionView.TextLinkClass) && dialog.CancelButton.Content is TextBlock { Text: SignOutDialog.CancelLabel } &&
            dialog.ConfirmButton.Content as string == SignOutDialog.ConfirmLabel && dialog.ConfirmButton.Classes.Contains(Ui.PrimaryClass) &&
            dialog.CancelButton.TranslatePoint(default, dialog.Card)!.Value.X < dialog.ConfirmButton.TranslatePoint(default, dialog.Card)!.Value.X,
            $"{label}: Batal is the shared text link left of the primary Keluar");
        Ensure(AutomationProperties.GetName(dialog.Card) == SignOutDialog.TitleText && AutomationProperties.GetName(dialog) == SignOutDialog.TitleText &&
            !string.IsNullOrEmpty(AutomationProperties.GetName(dialog.CancelButton)) && !string.IsNullOrEmpty(AutomationProperties.GetName(dialog.ConfirmButton)),
            $"{label}: the dialog and its buttons have automation names");
        Ensure(window.FocusManager?.GetFocusedElement() == dialog.ConfirmButton, $"{label}: focus starts on Keluar");

        // Focus
        Ensure(KeyboardNavigation.GetTabNavigation(dialog.Card) == KeyboardNavigationMode.Cycle, $"{label}: the card cycles Tab");
        dialog.ConfirmButton.Focus(NavigationMethod.Tab);
        foreach (var modifiers in new[] { KeyModifiers.None, KeyModifiers.Shift })
            for (var i = 0; i < 4; i++)
            {
                Key((Control)window.FocusManager!.GetFocusedElement()!, Avalonia.Input.Key.Tab, modifiers); await Settle();
                Ensure(window.FocusManager.GetFocusedElement() is Visual focused && dialog.Card.IsVisualAncestorOf(focused),
                    $"{label}: Tab stays inside the dialog ({window.FocusManager.GetFocusedElement()})");
            }

        // Dismissal
        Click(dialog.CancelButton); await Settle();
        Ensure(window.SignOutConfirmation is null && Unchanged(), $"{label}: Batal closes without signing out");
        Ensure(window.FocusManager?.GetFocusedElement() == Trigger(window), $"{label}: Batal returns focus to the trigger");
        dialog = await Open(window);
        Key(dialog.ConfirmButton, Avalonia.Input.Key.Escape); await Settle();
        Ensure(window.SignOutConfirmation is null && Unchanged(), $"{label}: Esc closes without signing out");
        dialog = await Open(window);
        var corner = new Point(8, window.Bounds.Height - 8);
        Ensure(window.InputHitTest(corner) is Visual hit && (hit == dialog.ScrimLayer || dialog.ScrimLayer.IsVisualAncestorOf(hit)),
            $"{label}: the scrim covers the window");
        dialog.ScrimLayer.RaiseEvent(new PointerPressedEventArgs(dialog.ScrimLayer, new Pointer(125, PointerType.Mouse, true), window, corner, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        await Settle();
        Ensure(window.SignOutConfirmation is null && Unchanged(), $"{label}: a click on the scrim closes without signing out");

        // Layout
        var (width, height) = (window.Width, window.Height);
        foreach (var (w, h) in new[] { (1040d, 720d), (1920d, 1080d) })
        {
            window.Width = w; window.Height = h; await Settle();
            dialog = await Open(window);
            var card = dialog.Card.TranslatePoint(default, window)!.Value;
            var root = window.GetVisualDescendants().OfType<ScrollViewer>().Where(s => s.IsEffectivelyVisible && !dialog.IsVisualAncestorOf(s));
            Ensure(card.X >= ModalDialog.Inset - .5 && card.X + dialog.Card.Bounds.Width <= window.ClientSize.Width - ModalDialog.Inset + .5 &&
                card.Y >= 0 && card.Y + dialog.Card.Bounds.Height <= window.ClientSize.Height + .5 &&
                dialog.Bounds.Width <= window.ClientSize.Width + .5 && root.All(s => s.Extent.Width <= s.Viewport.Width + .5) &&
                dialog.Title.DesiredSize.Width <= dialog.Title.Bounds.Width + .5 && dialog.MessageTextBlock.DesiredSize.Width <= dialog.MessageTextBlock.Bounds.Width + .5,
                $"{label} {w:0}x{h:0}: the confirmation fits with no horizontal scroll");
            Key(dialog.ConfirmButton, Avalonia.Input.Key.Escape); await Settle();
            Ensure(window.SignOutConfirmation is null && Unchanged(), $"{label} {w:0}x{h:0}: Esc closes");
        }
        window.Width = width; window.Height = height; await Settle();
        Ensure(owner == window.Session.CurrentUser is RestaurantOwner, $"{label}: the role is unchanged");
    }

    internal static async Task Confirm(MainWindow window, bool enter = false)
    {
        var dialog = await Open(window);
        Ensure(window.Session.IsSignedIn, "The user stays signed in while the confirmation is open");
        if (enter) Key(dialog.ConfirmButton, Avalonia.Input.Key.Enter);
        else Click(dialog.ConfirmButton);
        await Settle();
        Ensure(window.SignOutConfirmation is null && window.CurrentPage is SignInView && !window.Session.IsSignedIn &&
            !window.TopBar.ProfileMenu.IsOpen && window.TopBar.SearchBox.Text == "" && !window.GetVisualDescendants().OfType<CustomerTopBar>().Any() &&
            (window.SidebarSignOut is null || !window.GetVisualDescendants().Contains(window.SidebarSignOut)),
            $"Keluar{(enter ? " with Enter" : "")} signs out and returns to sign-in");
    }

    // Helpers
    private static async Task<SignOutDialog> Open(MainWindow window)
    {
        if (window.Session.CurrentUser is Customer)
        {
            Click(window.TopBar.ProfileButton); await Settle();
            Ensure(window.TopBar.ProfileMenu.IsOpen, "The profile menu opens before Keluar");
            Click(window.TopBar.SignOutItem);
        }
        else Click(window.SidebarSignOut ?? throw new InvalidOperationException("The sidebar shows Keluar"));
        await Settle();
        Ensure(!window.TopBar.ProfileMenu.IsOpen, "The profile menu closes when the confirmation opens");
        return window.SignOutConfirmation ?? throw new InvalidOperationException("Keluar opens the confirmation");
    }

    private static Control? Trigger(MainWindow window) =>
        window.Session.CurrentUser is Customer ? window.TopBar.ProfileButton : window.SidebarSignOut;

    private static void Key(Control target, Key key, KeyModifiers modifiers = KeyModifiers.None) =>
        target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers, Source = target });

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task Settle()
    { await Task.Delay(250); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render); }

    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
