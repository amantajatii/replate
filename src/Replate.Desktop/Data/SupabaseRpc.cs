using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Replate.Desktop.Data;

internal static class SupabaseRpc
{
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    internal static async Task<T?> PostAsync<T>(SupabaseAuth.Session session, string function, object payload, bool readResponse = true)
    {
        using var request = session.CreateRequest(HttpMethod.Post, "/rest/v1/rpc/" + function);
        request.Content = JsonContent.Create(payload);
        try
        {
            using var response = await Http.SendAsync(request).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new InvalidOperationException("Sesi Supabase sudah tidak berlaku. Keluar lalu masuk kembali.");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>(Json).ConfigureAwait(false);
                if (error?.Code is ("P0001" or "42501") && !string.IsNullOrWhiteSpace(error.Message))
                    throw new InvalidOperationException(error.Message);
                throw new InvalidOperationException("Perubahan gagal diproses. Periksa apakah migrasi terbaru sudah dipasang di Supabase.");
            }

            return readResponse ? await response.Content.ReadFromJsonAsync<T>(Json).ConfigureAwait(false) : default;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new InvalidOperationException("Replate gagal menghubungi Data API Supabase.", e);
        }
    }

    private sealed record ApiError(string Code, string Message);
}
