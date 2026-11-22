using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mapsui.UI.Avalonia;
using Replate.Domain.ValueObjects;

namespace Replate.Desktop;

internal static class LocationVerification
{
    internal static async Task Run(MainWindow window, Func<string, Task> shot)
    {
        Ensure(TravelText.Format(new TravelEstimate(.651, null)) == "650 m" &&
            TravelText.Format(new TravelEstimate(2.4, TimeSpan.FromMinutes(64.1))) == "2,4 km · 1 jam 5 menit",
            "Travel text uses Indonesian units and rounds distance and minutes");
        await CheckCustomer(window, shot);
        await CheckProfile(window, shot);
        window.SignOut();
        window.Session.AsCustomer();
        window.ShowMarketplace();
        await window.Session.RefreshTravelAsync();
        window.Marketplace.Refresh();
        await Settle();
    }

    private static async Task CheckCustomer(MainWindow window, Func<string, Task> shot)
    {
        var session = window.Session;
        var marketplace = window.Marketplace;
        var bar = window.TopBar;
        Ensure(marketplace.CollectionCards.Count == 6 && marketplace.CollectionCards.Keys.First() == MarketplaceView.Nearest,
            "Jelajah starts with Terdekat and has six collection cards");
        Ensure(bar.LocationLabel.Text == "Yogyakarta (perkiraan)" && session.CurrentCustomer?.Location == VerificationGeoFakes.DetectedPoint,
            "Location chip identifies the detected city as approximate");
        await CheckLocationPicker(window, shot);

        Click(bar.LocationChip);
        await Settle();
        Ensure(bar.LocationMenu.IsOpen && bar.LocationInput.IsFocused, "Location picker opens and focuses the address");
        bar.LocationInput.Text = "Jl. Malioboro, Yogyakarta";
        Click(bar.LocationAddressButton);
        await Settle();
        Ensure(!bar.LocationMenu.IsOpen && bar.LocationLabel.Text == "Jl. Malioboro, Yogyakarta" &&
            session.CurrentCustomer?.Location == VerificationGeoFakes.GeocodedPoint, "Location picker saves the geocoded address");
        Ensure(marketplace.ListingsPanel.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains(" km · 7 menit") == true),
            "A marketplace card shows distance and fake road time");
        await shot("marketplace-location");

        Click(marketplace.CollectionCards[MarketplaceView.Nearest]);
        await Settle();
        Ensure(window.CurrentPage is CollectionView { Collection: MarketplaceView.Nearest }, "Terdekat opens its collection page");
        var nearest = (CollectionView)window.CurrentPage!;
        var sorted = nearest.VisibleListings.Select(listing => session.TravelTo(listing.Menu.Restaurant)!.DistanceKm).ToList();
        Ensure(sorted.Distinct().Count() > 1 && sorted.SequenceEqual(sorted.Order()), "The fake route distances sort the Terdekat cards");
        await shot("collection-nearest");
        await CheckFoodMap(window, nearest, shot);
        await CheckMissingLocation(window, nearest);
    }

    private static async Task CheckLocationPicker(MainWindow window, Func<string, Task> shot)
    {
        var bar = window.TopBar;
        var session = window.Session;
        Click(bar.LocationChip);
        await Settle();
        bar.LocationInput.Text = "mal";
        await Task.Delay(400);
        await Settle();
        var suggestion = VerificationGeoFakes.Malioboro;
        Ensure(bar.LocationMenu.Child!.GetVisualDescendants().OfType<Button>()
            .Any(button => AutomationProperties.GetName(button) == suggestion.Title && button.IsEffectivelyVisible),
            "Typing mal shows address suggestions after debounce");
        await shot("location-suggestions");
        Key(bar.LocationInput, Avalonia.Input.Key.Down);
        Key((Control)window.FocusManager!.GetFocusedElement()!, Avalonia.Input.Key.Enter);
        await Settle();
        Ensure(bar.LocationLabel.Text == suggestion.Title && session.CurrentCustomer?.Location == suggestion.Point &&
            !bar.LocationMenu.IsOpen, "Down and Enter select the first suggestion and update the location chip");

        Click(bar.LocationChip);
        await Settle();
        Click(bar.LocationDeviceButton);
        await Settle();
        var confirmation = window.LocationConfirmation ?? throw new InvalidOperationException("Device location requires app confirmation");
        Ensure(session.CurrentCustomer?.Location == suggestion.Point, "Opening confirmation leaves the location unchanged");
        await shot("location-browser-confirm");
        Click(confirmation.CancelButton);
        await Settle();
        Ensure(window.LocationConfirmation is null && bar.LocationMenu.IsOpen &&
            session.CurrentCustomer?.Location == suggestion.Point, "Batal returns to the picker without changing location");
        Click(bar.LocationDeviceButton);
        await Settle();
        Click(window.LocationConfirmation!.ConfirmButton);
        await Settle();
        Ensure(window.LocationConfirmation is null && !bar.LocationMenu.IsOpen &&
            session.CurrentCustomer?.Location == VerificationGeoFakes.DevicePoint &&
            bar.LocationLabel.Text == "Jalan Sosrowijayan, Yogyakarta", "Lanjutkan saves the fake device location and refreshes the chip");
    }

    private static void Key(Control control, Key key) => control.RaiseEvent(new KeyEventArgs
    {
        RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = control
    });

    private static async Task CheckMissingLocation(MainWindow window, CollectionView nearest)
    {
        var customer = window.Session.CurrentCustomer!;
        var point = customer.Location!.Value;
        var label = customer.LocationLabel!;
        customer.ClearLocation();
        nearest.Refresh();
        await Settle();
        Ensure(nearest.GetVisualDescendants().OfType<TextBlock>().Any(t => t.IsVisible && t.Text == CollectionView.LocationHint) &&
            nearest.VisibleListings.Select(listing => listing.PickupEnd).SequenceEqual(nearest.VisibleListings.Select(listing => listing.PickupEnd).Order()),
            "Terdekat shows the location hint and falls back to pickup order");
        customer.SetLocation(point, label);

        var listing = nearest.VisibleListings[0];
        var restaurant = listing.Menu.Restaurant!;
        var restaurantPoint = restaurant.Location;
        restaurant.SetLocation(null);
        Click(nearest.Feed.DetailsButton(listing));
        await Settle();
        var dialog = window.Dialog ?? throw new InvalidOperationException("Food details open without coordinates");
        Ensure(dialog.LocationMap is null && dialog.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == MapView.Unavailable),
            "Food details explain when the restaurant map is unavailable");
        dialog.Close();
        restaurant.SetLocation(restaurantPoint);
        await Settle();
    }

    private static async Task CheckFoodMap(MainWindow window, CollectionView nearest, Func<string, Task> shot)
    {
        var listing = nearest.VisibleListings[0];
        Click(nearest.Feed.DetailsButton(listing));
        await Settle();
        var dialog = window.Dialog ?? throw new InvalidOperationException("Food details open from Terdekat");
        var map = dialog.LocationMap?.GetVisualDescendants().OfType<MapControl>().SingleOrDefault();
        Ensure(map is not null && MapView.OfflineTiles && map.Map.Layers.Count == 1 &&
            dialog.TravelLine.Text?.Contains(" km") == true, "Food details show travel and an offline Mapsui control");
        dialog.Update();
        Ensure(ReferenceEquals(map, dialog.LocationMap?.GetVisualDescendants().OfType<MapControl>().Single()),
            "Updating food availability keeps the map control");
        dialog.Scroll.Offset = new Vector(0, dialog.Scroll.Extent.Height);
        await shot("food-dialog-map");
        dialog.Close();
        await Settle();
    }

    private static async Task CheckProfile(MainWindow window, Func<string, Task> shot)
    {
        var session = window.Session;
        session.AsOwner();
        window.ShowDashboard();
        await Settle();
        Click(window.GetVisualDescendants().OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Profil"));
        await Settle();
        var profile = window.CurrentPage as RestaurantProfileView ?? throw new InvalidOperationException("Owner sidebar opens Profil");
        var restaurant = session.Restaurant;
        var initialLocation = restaurant.Location;
        var initialAddress = restaurant.Address;
        profile.AddressInput.Text = "Jl. Prawirotaman, Yogyakarta";
        Click(profile.SaveButton);
        await Settle();
        var saved = window.CurrentPage as RestaurantProfileView;
        Ensure(saved is { LocationMap: not null } && saved != profile && restaurant.Address == "Jl. Prawirotaman, Yogyakarta" &&
            restaurant.Location == VerificationGeoFakes.GeocodedPoint && restaurant.Location != initialLocation &&
            window.SuccessMessage == "Profil restoran tersimpan.", "Saving the profile re-geocodes and rebuilds the map");
        await shot("restaurant-profile-map");
        saved!.PhoneInput.Text = "123-abc";
        Click(saved.SaveButton);
        await Settle();
        Ensure(saved.FieldErrors["phone"].IsVisible && saved.PhoneInput.IsFocused, "Invalid phone shows a field error and gets focus");
        restaurant.Address = initialAddress;
        restaurant.SetLocation(initialLocation);
        session.Persist();
    }

    private static async Task Settle()
    {
        await Task.Delay(250);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
    }

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Ensure(bool result, string message)
    {
        if (!result) throw new InvalidOperationException(message);
    }
}
