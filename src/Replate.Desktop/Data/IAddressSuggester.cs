using Replate.Domain.ValueObjects;

namespace Replate.Desktop.Data;

internal interface IAddressSuggester
{
    Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, GeoPoint? near, CancellationToken cancellationToken = default);
}

internal sealed record AddressSuggestion(string Title, string Subtitle, GeoPoint Point);
