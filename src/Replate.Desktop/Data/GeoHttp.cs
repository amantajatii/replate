namespace Replate.Desktop.Data;

internal static class GeoHttp
{
    internal const string UserAgent = "Replate/1.0 (student project)";
    internal static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

    static GeoHttp() => Client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
}
