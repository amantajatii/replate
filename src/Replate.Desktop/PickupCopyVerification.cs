using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace Replate.Desktop;

internal static class PickupCopyVerification
{
    internal static async Task Run(string output)
    {
        void Ensure(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        async Task Settle() => await Task.Delay(350);
        var session = new DemoSession().AsCustomer();
        session.AddToCart(session.Listings[0], 1);
        var order = session.Checkout(session.Listings[0].Menu.RestaurantId);
        var window = new MainWindow(session) { Width = 1040, Height = 720 };
        try
        {
            window.Show(); window.ShowOrder(order, false); await Settle();
            var clipboard = window.Clipboard;
            var view = (OrderDetailView)window.CurrentPage!;
            var button = view.CopyCodeButton ?? throw new InvalidOperationException("Copy button missing on an active order");
            Ensure(button.Focusable && button.Content is PathIcon &&
                ToolTip.GetTip(button) as string == "Salin kode pengambilan" && AutomationProperties.GetName(button) == "Salin kode pengambilan",
                "Copy icon has tooltip, accessible name, and keyboard focus");
            // Hit test
            Ensure(!OperatingSystem.IsWindows() || Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "UI thread is STA so the Windows clipboard can open");
            Ensure(OrdersView.IsActive(order) && view.CodeCard!.GetVisualDescendants().Contains(button) && button.IsEffectivelyVisible && button.IsEnabled &&
                button.IsEffectivelyEnabled && button.IsHitTestVisible && button.Bounds.Width >= 44 && button.Bounds.Height >= 44,
                "Copy button sits in the active order's code card, enabled, hit-testable and at least 44 px");
            var at = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            var hit = window.InputHitTest(at) as Control;
            Ensure(hit is not null && hit.FindAncestorOfType<Button>(true) == button, $"Hit test at the copy button center reaches the button ({hit?.GetType().Name ?? "nothing"})");
            var pointer = new Pointer(127, PointerType.Mouse, true);
            hit!.RaiseEvent(new PointerPressedEventArgs(hit, pointer, window, at, 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
            hit.RaiseEvent(new PointerReleasedEventArgs(hit, pointer, window, at, 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
            await Settle();
            Ensure(view.CopyRequest == view.PickupCodeText!.Text && view.PickupCodeText.Text == order.PickupCode && button.IsEnabled,
                "Pointer click at the center copies the displayed code and leaves the button enabled");
            if (clipboard is null) { Console.WriteLine("Pickup copy clipboard checks skipped: no system clipboard in this environment."); return; }
            Ensure(await clipboard.TryGetTextAsync() == order.PickupCode && window.SuccessMessage == "Kode berhasil disalin.",
                "Pointer click puts the displayed code on the clipboard with the success notification");
            Click(button); await Settle();
            Ensure(await clipboard.TryGetTextAsync() == view.PickupCodeText!.Text && view.PickupCodeText.Text == order.PickupCode,
                "Clipboard text matches displayed pickup code exactly");
            Ensure(window.SuccessMessage == "Kode berhasil disalin.", "Successful clipboard write uses shared success notification");
            var body = (StackPanel)((ScrollViewer)view.Content!).Content!;
            var pasted = Ui.Input("Tempel kode pengambilan"); body.Children.Add(pasted); await Settle();
            pasted.Focus(); pasted.Paste(); await Settle();
            Ensure(pasted.Text == view.PickupCodeText.Text && pasted.Text!.Length == 6, "Actual TextBox paste contains only the displayed six-character code");
            body.Children.Remove(pasted);
            async Task CheckVisible(string size)
            {
                var copyButton = button ?? throw new InvalidOperationException("Copy button unavailable");
                copyButton.BringIntoView(); await Settle();
                var code = view.PickupCodeText!;
                var codePoint = code.TranslatePoint(default, window)!.Value;
                var buttonPoint = copyButton.TranslatePoint(default, window)!.Value;
                Ensure(codePoint.X >= 0 && buttonPoint.X > codePoint.X + code.Bounds.Width &&
                    buttonPoint.X + copyButton.Bounds.Width <= window.Bounds.Width &&
                    buttonPoint.Y >= 0 && buttonPoint.Y + copyButton.Bounds.Height <= window.Bounds.Height,
                    "Pickup code and copy button fit side by side within supported window");
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
                bitmap.Render(window); bitmap.Save(Path.Combine(output, $"pickup-copy-{size}.png"), PngBitmapEncoderOptions.Default);
            }
            await CheckVisible("minimum");
            window.ShowCustomerOrders();
            Ensure(window.SuccessMessage == "Kode berhasil disalin.", "Copy success notification survives navigation");
            await Task.Delay(4200);
            Ensure(window.SuccessMessage is null, "Copy notification expires after four seconds");
            window.ShowOrder(order, false); await Settle();
            view = (OrderDetailView)window.CurrentPage!; button = view.CopyCodeButton!;
            button.Focus();
            button.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Space });
            button.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Space });
            await Settle();
            Ensure(window.SuccessMessage == "Kode berhasil disalin." && await clipboard.TryGetTextAsync() == order.PickupCode,
                "Space key activates focused copy button");
            // A detached view has no TopLevel clipboard: exercise the real unavailable-clipboard branch.
            var detached = new OrderDetailView(window, session, order, false);
            Click(detached.CopyCodeButton!); await Settle();
            Ensure(window.SuccessMessage is null && detached.CopyCodeButton!.IsEnabled && window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.IsVisible &&
                t.Text == "Kode belum berhasil disalin. Silakan coba lagi."), "Unavailable clipboard shows only failure feedback");
            Click(button); await Settle();
            Ensure(window.SuccessMessage == "Kode berhasil disalin." && !window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.IsVisible &&
                t.Text == "Kode belum berhasil disalin. Silakan coba lagi."), "Retry succeeds and clears failure feedback");
            window.WindowState = WindowState.Maximized; await Task.Delay(650);
            await CheckVisible("maximized");
            session.AsOwner();
            window.ShowOrder(order, true);
            Ensure(((OrderDetailView)window.CurrentPage!).CopyCodeButton is null, "Copy button never appears in restaurant area");
            foreach (var transition in new Action[] { () => session.ConfirmOrder(order, 15), () => session.ReadyOrder(order), () => session.CompletePickup(order, order.PickupCode) })
            {
                session.AsOwner(); transition(); session.AsCustomer(); window.ShowOrder(order, false);
                Ensure(((OrderDetailView)window.CurrentPage!).CopyCodeButton is not null, "Existing code visibility preserved through order transitions");
            }
            var savedCode = order.PickupCode;
            order.PickupCode = ""; window.ShowOrder(order, false);
            Ensure(((OrderDetailView)window.CurrentPage!).CopyCodeButton is null, "No copy button for empty code");
            order.PickupCode = savedCode;
            session.AsCustomer();
            session.AddToCart(session.Listings[0], 1);
            var cancelled = session.Checkout(session.Listings[0].Menu.RestaurantId); session.CancelOrder(cancelled);
            window.ShowOrder(cancelled, false);
            Ensure(((OrderDetailView)window.CurrentPage!).CopyCodeButton is null && ((OrderDetailView)window.CurrentPage!).PickupCodeText?.Text == cancelled.PickupCode,
                "Cancelled order shows its pickup code but keeps the copy button hidden");
            Console.WriteLine("All Replate pickup copy checks passed (native clipboard and exact paste, keyboard, success/failure/retry, visibility, minimum/maximized).");
        }
        finally { window.Close(); }
    }
}