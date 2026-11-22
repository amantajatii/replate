using Replate.Domain.ValueObjects;

namespace Replate.Desktop.Data;

internal interface IRoutingService
{
    Task<IReadOnlyDictionary<Guid, TravelEstimate>> EstimateAsync(GeoPoint origin,
        IReadOnlyList<RoutingDestination> destinations, CancellationToken cancellationToken = default);
}

internal sealed record RoutingDestination(Guid Id, GeoPoint Point);
