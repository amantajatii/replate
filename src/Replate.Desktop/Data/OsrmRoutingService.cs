using System.Net.Http.Json;
using System.Text.Json;
using Replate.Domain.ValueObjects;

namespace Replate.Desktop.Data;

internal sealed class OsrmRoutingService : IRoutingService
{
    public async Task<IReadOnlyDictionary<Guid, TravelEstimate>> EstimateAsync(GeoPoint origin,
        IReadOnlyList<RoutingDestination> destinations, CancellationToken cancellationToken = default)
    {
        var estimates = new Dictionary<Guid, TravelEstimate>();
        if (destinations.Count == 0) return estimates;

        var coordinates = destinations.Select(destination => Format(destination.Point)).Prepend(Format(origin));
        var url = $"https://router.project-osrm.org/table/v1/driving/{string.Join(';', coordinates)}?sources=0&annotations=duration,distance";
        try
        {
            var result = await GeoHttp.Client.GetFromJsonAsync<TableResponse>(url, cancellationToken).ConfigureAwait(false);
            if (result is not { Code: "Ok", Distances: [ { } distances ], Durations: [ { } durations ] } ||
                distances.Length != destinations.Count + 1 || durations.Length != destinations.Count + 1)
                throw new JsonException();

            for (var i = 0; i < destinations.Count; i++)
            {
                if (distances[i + 1] is not { } meters || durations[i + 1] is not { } seconds) continue;
                if (!double.IsFinite(meters) || !double.IsFinite(seconds) || meters < 0 || seconds < 0)
                    throw new JsonException();
                estimates[destinations[i].Id] = new TravelEstimate(meters / 1000, TimeSpan.FromSeconds(seconds));
            }
            return estimates;
        }
        catch (Exception e) when (e is HttpRequestException or IOException or JsonException or OverflowException ||
            (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new InvalidOperationException("Waktu tempuh tidak dapat dihitung saat ini.", e);
        }
    }

    private static string Format(GeoPoint point) => FormattableString.Invariant($"{point.Longitude},{point.Latitude}");

    private sealed record TableResponse(string? Code, double?[][]? Durations, double?[][]? Distances);
}
