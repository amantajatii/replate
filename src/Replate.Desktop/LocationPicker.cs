using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Replate.Desktop.Data;

namespace Replate.Desktop;

internal sealed class LocationPicker
{
    private const string IpFallback = "Lokasi browser tidak tersedia, memakai perkiraan dari IP.";
    private readonly DemoSession session;
    private readonly Button chip;
    private readonly Func<Task> locationChanged;
    private readonly Func<Task<bool>> confirmBrowser;
    private readonly Action<string>? notify;
    private readonly TextBlock error = Ui.FieldError();
    private readonly TextBlock searching = Ui.Text("Mencari...", 12, Ui.Muted);
    private readonly TextBlock waiting = Ui.Text("Menunggu izin lokasi di browser...", 13, Ui.Muted);
    private readonly Button cancelButton;
    private readonly StackPanel rows = Ui.Stack(4);
    private IReadOnlyList<AddressSuggestion> suggestions = [];
    private CancellationTokenSource? searchRequest;
    private CancellationTokenSource? deviceRequest;
    private int selected = -1;
    private int visit;
    private bool busy;

    internal Popup Menu { get; } = new() { IsLightDismissEnabled = true, ShouldUseOverlayLayer = true,
        Placement = PlacementMode.BottomEdgeAlignedRight, HorizontalOffset = 24, VerticalOffset = -8 };
    internal TextBox Input { get; } = Ui.Input("Contoh: Jl. Kaliurang, Yogyakarta");
    internal Button AddressButton { get; } = Ui.Button("Gunakan alamat", () => { }, true);
    internal Button DeviceButton { get; } = Ui.Button("Gunakan lokasi saya", () => { });

    internal LocationPicker(DemoSession session, Button chip, Func<Task> locationChanged,
        Func<Task<bool>> confirmBrowser, Action<string>? notify)
    {
        this.session = session;
        this.chip = chip;
        this.locationChanged = locationChanged;
        this.confirmBrowser = confirmBrowser;
        this.notify = notify;
        cancelButton = Ui.Button("Batalkan", () => deviceRequest?.Cancel());
        cancelButton.Classes.Add(CollectionView.TextLinkClass);
        cancelButton.ClearValue(Button.BackgroundProperty);
        cancelButton.ClearValue(Button.ForegroundProperty);
        AutomationProperties.SetName(Input, "Alamat lokasi");
        searching.IsVisible = waiting.IsVisible = cancelButton.IsVisible = error.IsVisible = rows.IsVisible = false;
        Menu.PlacementTarget = chip;
        Menu.Child = new Border
        {
            Width = 340, Padding = new Thickness(16), Background = Brushes.White, CornerRadius = new CornerRadius(16),
            BoxShadow = Ui.CardShadow, Margin = new Thickness(24, 16, 24, 32),
            Child = Ui.Stack(12, Ui.Text("Lokasi kamu", 18, bold: true), Input, searching, rows,
                error, Ui.Row(8, AddressButton, DeviceButton), waiting, cancelButton)
        };
        Menu.Opened += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            if (Menu.IsOpen && !busy) Input.Focus();
        });
        Menu.Closed += (_, _) => ClearSuggestions();
        CollectionView.AddTextLinkStyles(Menu.Child.Styles);
        Menu.Child.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Input.TextChanged += async (_, _) => await QueueSuggestionsAsync();
        AddressButton.Click += async (_, _) => await SubmitAsync(null);
        DeviceButton.Click += async (_, _) => await UseDeviceAsync();
    }

    internal void Close()
    {
        visit++;
        deviceRequest?.Cancel();
        Menu.IsOpen = false;
        ClearSuggestions();
    }

    private async Task QueueSuggestionsAsync()
    {
        ClearSuggestions();
        var query = Input.Text?.Trim() ?? "";
        if (!Menu.IsOpen || busy || query.Length < 3 || session.CurrentCustomer is not { } customer) return;
        using var request = new CancellationTokenSource();
        searchRequest = request;
        searching.IsVisible = true;
        try
        {
            await Task.Delay(350, request.Token);
            var results = await session.AddressSuggester.SuggestAsync(query, customer.Location, request.Token);
            if (searchRequest == request && session.CurrentCustomer == customer && Menu.IsOpen)
                ShowSuggestions(results.Take(5).ToList());
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (InvalidOperationException) { }
        finally
        {
            if (searchRequest == request)
            {
                searchRequest = null;
                searching.IsVisible = false;
            }
        }
    }

    private void ShowSuggestions(IReadOnlyList<AddressSuggestion> results)
    {
        suggestions = results;
        foreach (var suggestion in results)
        {
            var button = Ui.Button(suggestion.Title, () => { });
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            button.Padding = new Thickness(8, 6);
            var pin = Ui.OutlineIcon(MarketplaceView.PinOutline, 16, .08, Ui.Green);
            AutomationProperties.SetAccessibilityView(pin, AccessibilityView.Raw);
            var subtitle = Ui.Text(suggestion.Subtitle, 12, Ui.Muted);
            subtitle.IsVisible = suggestion.Subtitle.Length > 0;
            var title = Ui.Text(suggestion.Title, 14, bold: true);
            title.MaxLines = subtitle.MaxLines = 1;
            title.TextTrimming = subtitle.TextTrimming = TextTrimming.CharacterEllipsis;
            var content = Ui.Columns("Auto,*", pin, Ui.Stack(2, title, subtitle));
            content.ColumnSpacing = 8;
            button.Content = content;
            AutomationProperties.SetName(button, suggestion.Title);
            AutomationProperties.SetHelpText(button, suggestion.Subtitle);
            button.Click += async (_, _) => await SubmitAsync(suggestion);
            rows.Children.Add(button);
        }
        rows.IsVisible = results.Count > 0;
    }

    private void ClearSuggestions()
    {
        searchRequest?.Cancel();
        searchRequest = null;
        searching.IsVisible = false;
        rows.IsVisible = false;
        rows.Children.Clear();
        suggestions = [];
        selected = -1;
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            if (deviceRequest is not null) deviceRequest.Cancel();
            else if (suggestions.Count != 0 || searchRequest is not null) { ClearSuggestions(); Input.Focus(); }
            else { Close(); chip.Focus(NavigationMethod.Tab); }
        }
        else if (!busy && suggestions.Count > 0 && e.Key is Key.Up or Key.Down)
        {
            e.Handled = true;
            selected = e.Key == Key.Down
                ? (selected + 1) % suggestions.Count
                : selected <= 0 ? suggestions.Count - 1 : selected - 1;
            for (var index = 0; index < rows.Children.Count; index++)
                ((Button)rows.Children[index]).Background = index == selected ? Ui.Selection : Brushes.White;
            rows.Children[selected].Focus(NavigationMethod.Directional);
        }
        else if (!busy && e.Key == Key.Enter && (Input.IsFocused || rows.IsKeyboardFocusWithin))
        {
            e.Handled = true;
            var index = rows.Children.ToList().FindIndex(row => row.IsFocused);
            await SubmitAsync(index < 0 ? null : suggestions[index]);
        }
    }

    private Task SubmitAsync(AddressSuggestion? suggestion) => RunAsync(async _ =>
    {
        if (suggestion is null) await session.SetCustomerLocationAsync(Input.Text ?? "");
        else await session.SetCustomerLocationAsync(suggestion);
        return true;
    });

    private Task UseDeviceAsync() => RunAsync(async stillCurrent =>
    {
        Menu.IsOpen = false;
        var approved = await confirmBrowser();
        if (!stillCurrent()) return false;
        Menu.IsOpen = true;
        if (!approved) return false;

        using var request = new CancellationTokenSource();
        deviceRequest = request;
        waiting.IsVisible = cancelButton.IsVisible = true;
        Menu.IsLightDismissEnabled = false;
        try
        {
            var result = await session.UseDeviceLocationAsync(request.Token);
            if (!stillCurrent()) return false;
            if (result == LocationDetectResult.Failed)
                throw new InvalidOperationException("Lokasi tidak dapat ditentukan. Ketik alamatmu di atas.");
            if (result == LocationDetectResult.ApproximateIp) notify?.Invoke(IpFallback);
            return true;
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { return false; }
        finally
        {
            deviceRequest = null;
            waiting.IsVisible = cancelButton.IsVisible = false;
            Menu.IsLightDismissEnabled = true;
        }
    });

    private async Task RunAsync(Func<Func<bool>, Task<bool>> submit)
    {
        if (busy || session.CurrentCustomer is not { } customer) return;
        var originalVisit = visit;
        bool StillCurrent() => originalVisit == visit && customer == session.CurrentCustomer;
        busy = true;
        Input.IsEnabled = AddressButton.IsEnabled = DeviceButton.IsEnabled = false;
        error.IsVisible = false;
        ClearSuggestions();
        try
        {
            if (!await submit(StillCurrent) || !StillCurrent()) return;
            Menu.IsOpen = false;
            chip.Focus();
            await locationChanged();
        }
        catch (Exception failure) when (failure is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            if (!StillCurrent()) return;
            error.Text = failure is InvalidOperationException ? failure.Message : "Lokasi tidak dapat disimpan.";
            error.IsVisible = true;
        }
        finally
        {
            busy = false;
            Input.IsEnabled = AddressButton.IsEnabled = DeviceButton.IsEnabled = true;
            if (StillCurrent() && Menu.IsOpen) Input.Focus();
        }
    }
}
