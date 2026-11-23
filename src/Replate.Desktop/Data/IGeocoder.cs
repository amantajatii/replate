using Replate.Domain.ValueObjects;

namespace Replate.Desktop.Data;

internal interface IGeocoder
{
    Task<GeoPoint?> GeocodeAsync(string address, CancellationToken cancellationToken = default);
}
