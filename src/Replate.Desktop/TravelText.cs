using System.Globalization;
using Replate.Domain.ValueObjects;

namespace Replate.Desktop;

internal static class TravelText
{
    private static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");

    internal static string Format(TravelEstimate? estimate)
    {
        if (estimate is null) return "";
        var distance = estimate.DistanceKm < 1
            ? $"{Math.Round(estimate.DistanceKm * 20, MidpointRounding.AwayFromZero) * 50:0} m"
            : $"{estimate.DistanceKm.ToString("0.0", Indonesian)} km";
        if (estimate.Duration is not { } duration) return distance;
        var minutes = Math.Max(1, (long)Math.Ceiling(duration.TotalMinutes));
        return minutes < 60 ? $"{distance} · {minutes} menit" : minutes % 60 == 0
            ? $"{distance} · {minutes / 60} jam"
            : $"{distance} · {minutes / 60} jam {minutes % 60} menit";
    }
}
