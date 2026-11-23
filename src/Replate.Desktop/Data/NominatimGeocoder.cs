using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Replate.Domain.ValueObjects;

namespace Replate.Desktop.Data;

internal sealed class NominatimGeocoder : IGeocoder
{
    private static readonly SemaphoreSlim Requests = new(1, 1);
    private static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(1.1);
    private static readonly Dictionary<string, GeoPoint?> Cache = [];
    private static readonly Dictionary<GeoPoint, string?> ReverseCache = [];
    private static long? lastRequest;

    public async Task<GeoPoint?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        address = address.Trim();
        var key = address.ToLowerInvariant();
        await Requests.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var url = "https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&countrycodes=id&q=" +
                Uri.EscapeDataString(address);
            var results = await RequestAsync<SearchResult[]>(url, cancellationToken).ConfigureAwait(false)
                ?? throw new JsonException();
            GeoPoint? point = results.Length == 0 ? null : new GeoPoint(
                double.Parse(results[0]?.Lat ?? throw new JsonException(), NumberStyles.Float, CultureInfo.InvariantCulture),
                double.Parse(results[0]?.Lon ?? throw new JsonException(), NumberStyles.Float, CultureInfo.InvariantCulture));
            Cache[key] = point;
            return point;
        }
        catch (Exception e) when (e is HttpRequestException or IOException or JsonException or FormatException or ArgumentOutOfRangeException ||
            (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new InvalidOperationException("Lokasi alamat tidak dapat dicari. Periksa koneksi internet lalu coba lagi.", e);
        }
        finally { Requests.Release(); }
    }

    internal static async Task<string?> ReverseAsync(GeoPoint point, CancellationToken cancellationToken)
    {
        await Requests.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (ReverseCache.TryGetValue(point, out var cached)) return cached;
            var url = FormattableString.Invariant(
                $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={point.Latitude}&lon={point.Longitude}&zoom=16");
            var result = await RequestAsync<ReverseResult>(url, cancellationToken).ConfigureAwait(false);
            var address = result?.Address;
            var street = FirstAddressPart(address?.Road, address?.Suburb, address?.Village);
            var city = FirstAddressPart(address?.City, address?.Town, address?.County);
            var label = string.Join(", ", new[] { street, city }.Where(part => part is not null)
                .Distinct(StringComparer.OrdinalIgnoreCase));
            return ReverseCache[point] = label.Length == 0 ? null : label;
        }
        catch (Exception error) when (error is HttpRequestException or IOException or JsonException ||
            (error is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return null;
        }
        finally { Requests.Release(); }
    }

    private static string? FirstAddressPart(params string?[] parts) =>
        parts.FirstOrDefault(part => !string.IsNullOrWhiteSpace(part))?.Trim();

    private static async Task<T?> RequestAsync<T>(string url, CancellationToken cancellationToken)
    {
        if (lastRequest is { } timestamp)
        {
            var delay = RequestInterval - Stopwatch.GetElapsedTime(timestamp);
            if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        lastRequest = Stopwatch.GetTimestamp();
        return await GeoHttp.Client.GetFromJsonAsync<T>(url, cancellationToken).ConfigureAwait(false);
    }

    private sealed record SearchResult(string? Lat, string? Lon);
    private sealed record ReverseResult(ReverseAddress? Address);
    private sealed record ReverseAddress(string? Road, string? Suburb, string? Village, string? City, string? Town, string? County);
}
