using System.Net.Http.Json;
using System.Text.Json;
using Replate.Domain.ValueObjects;

namespace Replate.Desktop.Data;

internal sealed class IpApiLocationProvider : ILocationProvider
{
    public async Task<DetectedLocation?> DetectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await GeoHttp.Client.GetAsync("https://ipapi.co/json/", cancellationToken).ConfigureAwait(false);
            var result = await response.Content.ReadFromJsonAsync<LocationResponse>(cancellationToken).ConfigureAwait(false)
                ?? throw new JsonException();
            if (result.Error) return null;
            response.EnsureSuccessStatusCode();
            if (result.Latitude is not { } latitude || result.Longitude is not { } longitude)
                return null;
            var label = string.IsNullOrWhiteSpace(result.City) ? "Lokasi saat ini" : result.City;
            return new DetectedLocation(new GeoPoint(latitude, longitude), label);
        }
        catch (Exception e) when (e is HttpRequestException or IOException or JsonException or ArgumentOutOfRangeException ||
            (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new InvalidOperationException("Lokasi otomatis tidak tersedia. Atur lokasi secara manual.", e);
        }
    }

    private sealed record LocationResponse(double? Latitude, double? Longitude, string? City, bool Error = false);
}
