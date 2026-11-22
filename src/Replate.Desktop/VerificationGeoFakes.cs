using Replate.Desktop.Data;
using Replate.Domain.Services;
using Replate.Domain.ValueObjects;

namespace Replate.Desktop;

internal static class VerificationGeoFakes
{
    internal static readonly GeoPoint GeocodedPoint = new(-7.7927, 110.3658);
    internal static readonly GeoPoint DetectedPoint = new(-7.805, 110.377);
    internal static readonly GeoPoint DevicePoint = new(-7.7908, 110.3667);
    internal static readonly AddressSuggestion Malioboro = new("Jalan Malioboro", "Gedongtengen, Yogyakarta",
        new GeoPoint(-7.7931, 110.3659));

    internal static DemoSession CreateSession(string path)
    {
        var session = new DemoSession(path);
        Configure(session);
        return session;
    }

    internal static void Configure(DemoSession session)
    {
        session.Geocoder = new FakeGeocoder(GeocodedPoint);
        session.LocationProvider = new FakeLocationProvider();
        session.DeviceLocationProvider = new FakeDeviceLocationProvider();
        session.AddressSuggester = new FakeAddressSuggester();
        session.Routing = new FakeRouting();
    }

    internal sealed class FakeGeocoder(GeoPoint? point) : IGeocoder
    {
        public Task<GeoPoint?> GeocodeAsync(string address, CancellationToken cancellationToken = default) =>
            Task.FromResult(point);
    }

    internal sealed class FakeLocationProvider(bool available = true) : ILocationProvider
    {
        public Task<DetectedLocation?> DetectAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<DetectedLocation?>(available ? new(DetectedPoint, "Yogyakarta") : null);
    }

    internal sealed class FakeDeviceLocationProvider(bool available = true) : ILocationProvider
    {
        public Task<DetectedLocation?> DetectAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<DetectedLocation?>(available ? new(DevicePoint, "Jalan Sosrowijayan, Yogyakarta") : null);
    }

    internal sealed class FakeAddressSuggester : IAddressSuggester
    {
        public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, GeoPoint? near, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AddressSuggestion>>(query.StartsWith("mal", StringComparison.OrdinalIgnoreCase)
                ? [Malioboro, new("Malioboro Mall", "Jalan Malioboro, Yogyakarta", GeocodedPoint)] : []);
    }

    internal sealed class FakeRouting : IRoutingService
    {
        public Task<IReadOnlyDictionary<Guid, TravelEstimate>> EstimateAsync(GeoPoint origin,
            IReadOnlyList<RoutingDestination> destinations, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, TravelEstimate>>(destinations.ToDictionary(destination => destination.Id,
                destination => new TravelEstimate(1 + GeoDistance.HaversineKm(origin, destination.Point), TimeSpan.FromMinutes(7))));
    }

    internal sealed class FailingRouting : IRoutingService
    {
        internal const string Message = "Waktu tempuh tidak dapat dihitung saat ini.";

        public Task<IReadOnlyDictionary<Guid, TravelEstimate>> EstimateAsync(GeoPoint origin,
            IReadOnlyList<RoutingDestination> destinations, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyDictionary<Guid, TravelEstimate>>(new InvalidOperationException(Message));
    }
}
