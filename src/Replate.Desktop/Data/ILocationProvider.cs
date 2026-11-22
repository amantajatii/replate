using Replate.Domain.ValueObjects;

namespace Replate.Desktop.Data;

internal interface ILocationProvider
{
    Task<DetectedLocation?> DetectAsync(CancellationToken cancellationToken = default);
}

internal sealed record DetectedLocation(GeoPoint Point, string Label);
