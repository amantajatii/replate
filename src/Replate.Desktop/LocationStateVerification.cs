using System.Text.Json.Nodes;
using Replate.Desktop.Data;
using Replate.Domain.Services;

namespace Replate.Desktop;

internal static class LocationStateVerification
{
    internal static void Run() => RunAsync().GetAwaiter().GetResult();

    private static async Task RunAsync()
    {
        var directory = Directory.CreateTempSubdirectory("replate-location-");
        try
        {
            var path = Path.Combine(directory.FullName, "state.json");
            var session = VerificationGeoFakes.CreateSession(path);
            var externalCustomerId = Guid.NewGuid();
            session.ExternalCustomerLocations.Add(externalCustomerId, new DetectedLocation(VerificationGeoFakes.DetectedPoint, "Yogyakarta"));
            await VerifyCustomerAsync(session);
            await VerifyRestaurantAsync(session);
            VerifyReload(session, path, externalCustomerId);
            await VerifyProfileRollbackAsync(directory.FullName);
            await VerifyLocationProvidersAsync(Path.Combine(directory.FullName, "providers.json"));
            await BrowserLocationVerification.RunAsync();
        }
        finally { directory.Delete(true); }
        Console.WriteLine("All Replate location state checks passed.");
    }

    private static async Task VerifyCustomerAsync(DemoSession session)
    {
        session.AsCustomer();
        var restaurant = session.Restaurants[0];
        Ensure(await session.EnsureCustomerLocationAsync() &&
            session.CurrentCustomer?.Location == VerificationGeoFakes.DetectedPoint &&
            session.CurrentCustomer.LocationLabel == "Yogyakarta (perkiraan)", "Automatic location identifies the IP estimate");
        await session.RefreshTravelAsync();
        Ensure(session.TravelError is null && session.TravelTo(restaurant)?.Duration == TimeSpan.FromMinutes(7),
            "Routing stores the road estimate");

        session.Routing = new VerificationGeoFakes.FailingRouting();
        await session.RefreshTravelAsync();
        Ensure(session.TravelTo(restaurant)?.Duration == TimeSpan.FromMinutes(7) && session.TravelError is not null,
            "A failed refresh preserves an existing estimate");
        await session.SetCustomerLocationAsync("  Jl. Malioboro, Kecamatan Gedongtengen, Kota Yogyakarta  ");
        Ensure(session.CurrentCustomer?.Location == VerificationGeoFakes.GeocodedPoint &&
            session.CurrentCustomer.LocationLabel == "Jl. Malioboro" && session.TravelTo(restaurant) is { Duration: null },
            "Manual geocoding updates the label and invalidates the road estimate");
        Ensure(await session.EnsureCustomerLocationAsync() && session.CurrentCustomer?.Location == VerificationGeoFakes.GeocodedPoint,
            "Automatic detection preserves an existing manual location");
        await session.RefreshTravelAsync();
        Ensure(session.TravelError == VerificationGeoFakes.FailingRouting.Message &&
            session.TravelTo(restaurant) is { Duration: null, DistanceKm: > 0 }, "Routing failure exposes error and straight-line estimate");
    }

    private static async Task VerifyLocationProvidersAsync(string path)
    {
        var session = VerificationGeoFakes.CreateSession(path).AsCustomer();
        session.Geocoder = new VerificationGeoFakes.FakeGeocoder(null);
        var suggestion = VerificationGeoFakes.Malioboro;
        await session.SetCustomerLocationAsync(suggestion);
        Ensure(session.CurrentCustomer?.Location == suggestion.Point && session.CurrentCustomer.LocationLabel == suggestion.Title,
            "Selecting an address saves the suggestion directly without forward geocoding");
        Ensure(await session.UseDeviceLocationAsync() == LocationDetectResult.Device &&
            session.CurrentCustomer?.Location == VerificationGeoFakes.DevicePoint &&
            session.CurrentCustomer.LocationLabel == "Jalan Sosrowijayan, Yogyakarta", "Device location is preferred to IP");
        session.DeviceLocationProvider = new VerificationGeoFakes.FakeDeviceLocationProvider(false);
        Ensure(await session.UseDeviceLocationAsync() == LocationDetectResult.ApproximateIp &&
            session.CurrentCustomer?.Location == VerificationGeoFakes.DetectedPoint &&
            session.CurrentCustomer.LocationLabel == "Yogyakarta (perkiraan dari IP)", "Device denial falls back to a labelled IP estimate");
        session.LocationProvider = new VerificationGeoFakes.FakeLocationProvider(false);
        Ensure(await session.UseDeviceLocationAsync() == LocationDetectResult.Failed &&
            session.CurrentCustomer?.Location == VerificationGeoFakes.DetectedPoint, "Both failures preserve the last saved location");
        session = VerificationGeoFakes.CreateSession(path).AsCustomer();
        Ensure(session.CurrentCustomer?.LocationLabel == "Yogyakarta (perkiraan dari IP)", "The IP fallback label survives reload");
    }

    private static async Task VerifyRestaurantAsync(DemoSession session)
    {
        session.AsOwner();
        var restaurant = session.Restaurant;
        var profile = new RestaurantProfile("Dapur Bumi", "Jl. Prawirotaman, Yogyakarta",
            restaurant.Phone, restaurant.OpeningTime, restaurant.ClosingTime, restaurant.Description);
        await session.SaveRestaurantProfileAsync(profile);
        Ensure(restaurant.Address == profile.Address && restaurant.Location == VerificationGeoFakes.GeocodedPoint &&
            session.LastGeocodeWarning is null, "Saving a new address updates the restaurant coordinates");
        try
        {
            await session.SaveRestaurantProfileAsync(profile with { Address = " " });
            throw new InvalidOperationException("Expected restaurant profile validation failure");
        }
        catch (ProfileValidationException e)
        {
            Ensure(e.Errors.ContainsKey("address") && restaurant.Address == profile.Address, "Profile validation carries field errors");
        }

        var missing = session.Restaurants[1];
        missing.SetLocation(null);
        Ensure(await session.EnsureRestaurantLocationsAsync() == 1 && missing.Location == VerificationGeoFakes.GeocodedPoint,
            "Restaurant backfill locates only missing coordinates");

        session.AsOwner("Roti Pagi Kotabaru");
        var anotherRestaurant = session.Restaurant;
        session.Geocoder = new VerificationGeoFakes.FakeGeocoder(null);
        await session.SaveRestaurantProfileAsync(profile with { Name = anotherRestaurant.Name, Address = "Alamat belum dipetakan" });
        Ensure(anotherRestaurant.Address == "Alamat belum dipetakan" && anotherRestaurant.Location is null &&
            session.LastGeocodeWarning == "Alamat tersimpan, tetapi lokasinya belum ditemukan di peta.",
            "An unmapped address saves with a warning and clears stale coordinates");
        session.AsOwner();
    }

    private static void VerifyReload(DemoSession session, string path, Guid externalCustomerId)
    {
        var reloaded = VerificationGeoFakes.CreateSession(path);
        reloaded.AsCustomer();
        Ensure(reloaded.CurrentCustomer?.Location == VerificationGeoFakes.GeocodedPoint &&
            reloaded.CurrentCustomer.LocationLabel == "Jl. Malioboro", "Customer coordinates and label survive reload");
        Ensure(reloaded.FindRestaurant(session.Restaurant.Id)?.Location == VerificationGeoFakes.GeocodedPoint &&
            reloaded.FindRestaurant(session.Restaurants[1].Id)?.Location == VerificationGeoFakes.GeocodedPoint,
            "Restaurant coordinates survive reload");
        Ensure(reloaded.ExternalCustomerLocations.GetValueOrDefault(externalCustomerId) ==
            new DetectedLocation(VerificationGeoFakes.DetectedPoint, "Yogyakarta"), "External account location survives reload");

        var legacy = JsonNode.Parse(File.ReadAllText(path))!;
        legacy.AsObject().Remove("ExternalCustomerLocations");
        foreach (var item in legacy["Restaurants"]!.AsArray().Concat(legacy["Users"]!.AsArray()))
        {
            item!.AsObject().Remove("Latitude");
            item.AsObject().Remove("Longitude");
            item.AsObject().Remove("LocationLabel");
        }
        var legacyPath = Path.Combine(Path.GetDirectoryName(path)!, "legacy-v2.json");
        File.WriteAllText(legacyPath, legacy.ToJsonString());
        var legacySession = VerificationGeoFakes.CreateSession(legacyPath);
        legacySession.AsCustomer();
        Ensure(legacy["Version"]!.GetValue<int>() == 2 && legacySession.StorageError is null &&
            legacySession.CurrentCustomer is { Location: null, LocationLabel: null } &&
            legacySession.Restaurants.All(restaurant => restaurant.Location is null), "Version 2 without location fields still loads");
    }

    private static async Task VerifyProfileRollbackAsync(string directory)
    {
        var path = Path.Combine(directory, "blocked-state.json");
        var session = VerificationGeoFakes.CreateSession(path);
        session.AsOwner();
        var restaurant = session.Restaurant;
        var original = (restaurant.Name, restaurant.Address, restaurant.Description, restaurant.Phone,
            restaurant.OpeningTime, restaurant.ClosingTime, restaurant.Location, restaurant.UpdatedAt);
        Directory.CreateDirectory(path);
        try
        {
            await session.SaveRestaurantProfileAsync(new RestaurantProfile("Nama baru", "Alamat baru",
                "0274 555 4321", new TimeOnly(8, 0), new TimeOnly(20, 0), "Deskripsi baru"));
            throw new InvalidOperationException("Expected failed state write");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Ensure(original == (restaurant.Name, restaurant.Address, restaurant.Description, restaurant.Phone,
                restaurant.OpeningTime, restaurant.ClosingTime, restaurant.Location, restaurant.UpdatedAt),
                "A failed state write restores the previous profile and coordinates");
        }
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
