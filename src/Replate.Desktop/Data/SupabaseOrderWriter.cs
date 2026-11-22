using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Replate.Domain.Enums;

namespace Replate.Desktop.Data;

internal static class SupabaseOrderWriter
{
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    internal static async Task<DateTime?> UpdateStatusAsync(SupabaseAuth.Session session, Guid orderId,
        OrderStatus status, int? preparationMinutes = null, string? pickupCode = null)
    {
        using var request = session.CreateRequest(HttpMethod.Post, "/rest/v1/rpc/replate_update_order_status");
        request.Content = JsonContent.Create(new
        {
            p_order_id = orderId, p_next_status = status.ToString(),
            p_preparation_minutes = preparationMinutes, p_pickup_code = pickupCode
        });
        try
        {
            using var response = await Http.SendAsync(request).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new InvalidOperationException("Sesi Supabase sudah tidak berlaku. Keluar lalu masuk kembali.");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>().ConfigureAwait(false);
                if (error?.Code is ("P0001" or "42501") && !string.IsNullOrWhiteSpace(error.Message))
                    throw new InvalidOperationException(error.Message);
                throw new InvalidOperationException("Status gagal disimpan. Periksa apakah fungsi pesanan sudah dipasang di Supabase.");
            }

            if (status != OrderStatus.Completed) return null;
            return (await response.Content.ReadFromJsonAsync<DateTimeOffset?>().ConfigureAwait(false))?.UtcDateTime;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new InvalidOperationException("Replate gagal menyimpan status pesanan lewat Data API Supabase.", e);
        }
    }

    private sealed record ApiError(string Code, string Message);
}
