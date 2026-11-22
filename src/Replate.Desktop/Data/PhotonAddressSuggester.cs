using System.Net.Http.Json;
using System.Text.Json;
using Replate.Domain.ValueObjects;

namespace Replate.Desktop.Data;

internal sealed class PhotonAddressSuggester : IAddressSuggester
{
    public async Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, GeoPoint? near, CancellationToken cancellationToken = default)
    {
        query = query.Trim();
        if (query.Length < 3) return [];
        var url = "https://photon.komoot.io/api/?limit=5&bbox=95,-11,141,6&q=" + Uri.EscapeDataString(query);
        if (near is { } point)
            url += FormattableString.Invariant($"&lat={point.Latitude}&lon={point.Longitude}");
        try
        {
            var response = await GeoHttp.Client.GetFromJsonAsync<FeatureCollection>(url, cancellationToken).ConfigureAwait(false);
            if (response?.Features is not { } features) throw new JsonException();
            var results = new List<AddressSuggestion>();
            foreach (var feature in features)
            {
                if (feature?.Properties is not { Countrycode: "ID" } address ||
                    feature.Geometry?.Coordinates is not [>= -180 and <= 180, >= -90 and <= 90, ..]) continue;
                var coordinates = feature.Geometry.Coordinates;
                var street = $"{address.Street?.Trim()} {address.Housenumber?.Trim()}".Trim();
                var title = string.IsNullOrWhiteSpace(address.Name) ? street : address.Name.Trim();
                if (title.Length == 0) continue;
                var subtitle = string.Join(", ", new[] { address.Street, address.District, address.City, address.State }
                    .Select(part => part?.Trim()).Where(part => !string.IsNullOrEmpty(part) &&
                        !string.Equals(part, title, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase));
                if (results.Any(result => string.Equals(result.Title, title, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(result.Subtitle, subtitle, StringComparison.OrdinalIgnoreCase))) continue;
                results.Add(new AddressSuggestion(title, subtitle, new GeoPoint(coordinates[1], coordinates[0])));
                if (results.Count == 5) break;
            }
            return results;
        }
        catch (Exception error) when (error is HttpRequestException or IOException or JsonException ||
            (error is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new InvalidOperationException("Saran alamat tidak tersedia saat ini.", error);
        }
    }

    private sealed record FeatureCollection(Feature?[]? Features);
    private sealed record Feature(Geometry? Geometry, Address? Properties);
    private sealed record Geometry(double[]? Coordinates);
    private sealed record Address(string? Countrycode, string? Name, string? Street, string? Housenumber,
        string? District, string? City, string? State);
}
