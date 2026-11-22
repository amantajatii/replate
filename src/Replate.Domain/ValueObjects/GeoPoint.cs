namespace Replate.Domain.ValueObjects;

public readonly record struct GeoPoint
{
    public double Latitude { get; }
    public double Longitude { get; }

    public GeoPoint(double latitude, double longitude)
    {
        if (!(latitude >= -90 && latitude <= 90))
            throw new ArgumentOutOfRangeException(nameof(latitude), "Lintang harus berada di antara -90 dan 90 derajat.");
        if (!(longitude >= -180 && longitude <= 180))
            throw new ArgumentOutOfRangeException(nameof(longitude), "Bujur harus berada di antara -180 dan 180 derajat.");
        Latitude = latitude;
        Longitude = longitude;
    }
}
