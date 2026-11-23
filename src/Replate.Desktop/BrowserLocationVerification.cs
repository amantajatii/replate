using System.Net;
using System.Text;
using Replate.Desktop.Data;
using Replate.Domain.ValueObjects;

namespace Replate.Desktop;

internal static class BrowserLocationVerification
{
    internal static async Task RunAsync()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var client = new HttpClient(new HttpClientHandler { UseProxy = false });
        var opened = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new BrowserLocationProvider(url => opened.SetResult(url),
            (point, _) => Task.FromResult<string?>(point == new GeoPoint(-7.79, 110.36) ? "Jalan Malioboro, Yogyakarta" : null));
        var detecting = provider.DetectAsync(deadline.Token);
        var pageUrl = await opened.Task.WaitAsync(deadline.Token);
        var html = await client.GetStringAsync(pageUrl, deadline.Token);
        Ensure(html.Contains("navigator.geolocation.getCurrentPosition") && html.Contains("enableHighAccuracy: true"),
            "The loopback page requests browser geolocation");
        const string valid = """{"latitude":-7.79,"longitude":110.36,"accuracy":25}""";
        var wrong = new Uri(new Uri(pageUrl), "/locate/wrong-token/result").AbsoluteUri;
        Ensure(await PostAsync(client, wrong, valid, deadline.Token) == HttpStatusCode.NotFound, "Wrong token is rejected");
        var resultUrl = pageUrl + "/result";
        Ensure(await PostAsync(client, resultUrl, """{"latitude":100,"longitude":110}""", deadline.Token) == HttpStatusCode.BadRequest &&
            await PostAsync(client, resultUrl, """{"latitude":"-7.79","longitude":110}""", deadline.Token) == HttpStatusCode.BadRequest &&
            !detecting.IsCompleted, "Out-of-range and nonnumeric coordinates cannot finish the handshake");
        Ensure(await PostAsync(client, resultUrl, valid, deadline.Token) == HttpStatusCode.NoContent, "Valid local coordinates are acknowledged");
        Ensure(await detecting == new DetectedLocation(new GeoPoint(-7.79, 110.36), "Jalan Malioboro, Yogyakarta"),
            "The listener returns the first validated location and resolves a label without opening a browser");
        await VerifyCancellationAsync(deadline.Token);
        Console.WriteLine("All Replate browser location loopback checks passed.");
    }

    private static async Task VerifyCancellationAsync(CancellationToken deadline)
    {
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(deadline);
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new BrowserLocationProvider(_ => opened.SetResult());
        var detecting = provider.DetectAsync(cancel.Token);
        await opened.Task.WaitAsync(deadline);
        cancel.Cancel();
        try { await detecting.WaitAsync(deadline); }
        catch (OperationCanceledException) when (!deadline.IsCancellationRequested) { return; }
        throw new InvalidOperationException("Cancelling permission wait must stop browser location");
    }

    private static async Task<HttpStatusCode> PostAsync(HttpClient client, string url, string json, CancellationToken token)
    {
        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, body, token);
        return response.StatusCode;
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
