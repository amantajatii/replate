using Replate.Domain.ValueObjects;

namespace Replate.Domain.Services;

public static class GeoDistance
{
    public const double EarthRadiusKm = 6371.0;

    public static double HaversineKm(GeoPoint a, GeoPoint b)
    {
        const double radiansPerDegree = Math.PI / 180;
        var latitudeDelta = (b.Latitude - a.Latitude) * radiansPerDegree;
        var longitudeDelta = (b.Longitude - a.Longitude) * radiansPerDegree;
        var latitudeTerm = Math.Sin(latitudeDelta / 2);
        var longitudeTerm = Math.Sin(longitudeDelta / 2);
        var haversine = latitudeTerm * latitudeTerm + longitudeTerm * longitudeTerm *
            Math.Cos(a.Latitude * radiansPerDegree) * Math.Cos(b.Latitude * radiansPerDegree);
        return 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(Math.Clamp(haversine, 0, 1)));
    }

    public static TravelEstimate StraightLine(GeoPoint a, GeoPoint b) => new(HaversineKm(a, b), null);
}
