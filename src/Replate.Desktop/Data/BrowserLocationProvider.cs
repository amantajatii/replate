using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Replate.Domain.ValueObjects;

namespace Replate.Desktop.Data;

internal sealed class BrowserLocationProvider(
    Action<string>? openBrowser = null,
    Func<GeoPoint, CancellationToken, Task<string?>>? reverse = null) : ILocationProvider
{
    public async Task<DetectedLocation?> DetectAsync(CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(90));
        var point = await ReceiveAsync(deadline.Token).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (point is null) return null;

        string? label = null;
        try { label = await (reverse ?? NominatimGeocoder.ReverseAsync)(point.Value, deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
        cancellationToken.ThrowIfCancellationRequested();
        return new DetectedLocation(point.Value, string.IsNullOrWhiteSpace(label) ? "Lokasi saya" : label);
    }

    private async Task<GeoPoint?> ReceiveAsync(CancellationToken cancellationToken)
    {
        using var listener = new HttpListener();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Windows http.sys lets non-admin users register "localhost" but not "127.0.0.1".
            var host = OperatingSystem.IsWindows() ? "localhost" : "127.0.0.1";
            var prefix = $"http://{host}:{FreePort()}/";
            var path = "/locate/" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            listener.Prefixes.Add(prefix);
            listener.Start();
            (openBrowser ?? OpenBrowser)(prefix.TrimEnd('/') + path);
            while (true)
            {
                var context = await listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var result = await HandleAsync(context, path, cancellationToken).ConfigureAwait(false);
                    if (result.Done) return result.Point;
                }
                catch (Exception error) when (error is HttpListenerException or IOException) { }
                finally { context.Response.Close(); }
            }
        }
        catch (Exception error) when (error is HttpListenerException or SocketException or IOException or UnauthorizedAccessException or Win32Exception or
            InvalidOperationException or NotSupportedException or OperationCanceledException)
        {
            return null;
        }
        finally { if (listener.IsListening) listener.Stop(); }
    }

    private static async Task<(bool Done, GeoPoint? Point)> HandleAsync(HttpListenerContext context, string path, CancellationToken token)
    {
        var request = context.Request;
        var response = context.Response;
        response.Headers["Cache-Control"] = "no-store";
        response.Headers["Referrer-Policy"] = "no-referrer";
        if (request.RemoteEndPoint is not { Address: var remote } || !IPAddress.IsLoopback(remote))
            response.StatusCode = 403;
        else if (request.HttpMethod == "GET" && request.RawUrl == path)
        {
            response.ContentType = "text/html; charset=utf-8";
            response.Headers["Content-Security-Policy"] =
                "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'";
            var page = System.Text.Encoding.UTF8.GetBytes(Page);
            response.ContentLength64 = page.Length;
            await response.OutputStream.WriteAsync(page, token).ConfigureAwait(false);
        }
        else if (request.HttpMethod == "POST" && request.RawUrl == path + "/result")
        {
            var result = await ReadResultAsync(request, token).ConfigureAwait(false);
            response.StatusCode = result.Done ? 204 : 400;
            return result;
        }
        else response.StatusCode = 404;
        return (false, null);
    }

    private static async Task<(bool Done, GeoPoint? Point)> ReadResultAsync(HttpListenerRequest request, CancellationToken token)
    {
        const int maxBytes = 1024;
        if (!string.Equals(request.ContentType?.Split(';')[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase) ||
            request.ContentLength64 > maxBytes)
            return (false, null);
        var buffer = new byte[maxBytes + 1];
        var count = await request.InputStream.ReadAtLeastAsync(buffer, buffer.Length, false, token)
            .AsTask().WaitAsync(token).ConfigureAwait(false);
        if (count > maxBytes) return (false, null);
        try
        {
            using var json = JsonDocument.Parse(buffer.AsMemory(0, count));
            var result = json.RootElement;
            if (result.ValueKind != JsonValueKind.Object) return (false, null);
            if (result.TryGetProperty("error", out var error))
                return (error.ValueKind == JsonValueKind.Number && error.TryGetInt32(out var code) && code is >= 1 and <= 3, null);
            if (!TryNumber(result, "latitude", out var latitude) || latitude is < -90 or > 90 ||
                !TryNumber(result, "longitude", out var longitude) || longitude is < -180 or > 180 ||
                (result.TryGetProperty("accuracy", out _) && (!TryNumber(result, "accuracy", out var accuracy) || accuracy < 0)))
                return (false, null);
            return (true, new GeoPoint(latitude, longitude));
        }
        catch (JsonException) { return (false, null); }
    }

    private static bool TryNumber(JsonElement result, string name, out double value)
    {
        value = 0;
        return result.TryGetProperty(name, out var number) && number.ValueKind == JsonValueKind.Number &&
            number.TryGetDouble(out value) && double.IsFinite(value);
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static void OpenBrowser(string url)
    {
        using var process = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private const string Page = """
        <!doctype html>
        <html lang="id">
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>Lokasi perangkat | Replate</title>
        <style>
          body { margin: 0; padding: 32px; background: #FAF8F3; color: #202522; font: 1rem/1.55 system-ui, sans-serif; }
          main { max-width: 35rem; margin: 10vh auto; border-top: 4px solid #1F6B4F; padding: 24px; background: white; border-radius: 12px; }
          h1 { color: #1F6B4F; margin-top: 0; font-size: 1.5rem; }
        </style>
        <main>
          <h1>Lokasi perangkat</h1>
          <p>Replate meminta lokasimu untuk menampilkan restoran terdekat.</p>
          <p id="status" role="status">Izinkan akses lokasi saat browser memintanya.</p>
        </main>
        <script>
          const statusText = document.getElementById('status');
          async function send(data, message) {
            try {
              const response = await fetch(location.pathname + '/result', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
              });
              if (!response.ok) throw new Error();
              statusText.textContent = message;
            } catch {
              statusText.textContent = 'Sesi lokasi sudah berakhir. Kembali ke Replate untuk mencoba lagi.';
            }
          }
          function unavailable(error) {
            const messages = {
              1: 'Izin lokasi ditolak. Kembali ke Replate untuk memilih lokasi.',
              2: 'Lokasi perangkat tidak tersedia. Kembali ke Replate untuk memilih lokasi.',
              3: 'Waktu pencarian lokasi habis. Kembali ke Replate untuk mencoba lagi.'
            };
            send({ error: error.code }, messages[error.code]);
          }
          if (!navigator.geolocation) unavailable({ code: 2 });
          else navigator.geolocation.getCurrentPosition(({ coords }) => {
            const { latitude, longitude, accuracy } = coords;
            send({ latitude, longitude, accuracy }, 'Lokasi terkirim. Kamu boleh menutup tab ini dan kembali ke Replate.');
          }, unavailable, { enableHighAccuracy: true, timeout: 20000, maximumAge: 60000 });
        </script>
        </html>
        """;
}
