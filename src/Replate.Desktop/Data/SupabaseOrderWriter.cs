using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Replate.Domain.Entities;
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
        var completedAt = await PostAsync<DateTimeOffset?>(session, "replate_update_order_status", new
        {
            p_order_id = orderId, p_next_status = status.ToString(),
            p_preparation_minutes = preparationMinutes, p_pickup_code = pickupCode
        }, readResponse: status == OrderStatus.Completed).ConfigureAwait(false);
        return completedAt?.UtcDateTime;
    }

    internal static async Task<Dictionary<Guid, int>> CancelOrderAsync(SupabaseAuth.Session session, Guid orderId)
    {
        var rows = await PostAsync<List<ListingStock>>(session, "replate_cancel_order", new { p_order_id = orderId })
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Supabase tidak mengembalikan stok setelah pembatalan.");
        return rows.ToDictionary(row => row.ListingId, row => row.Quantity);
    }

    internal static async Task<(Order Order, Dictionary<Guid, int> Stock)> CheckoutAsync(SupabaseAuth.Session session, Order request)
    {
        var result = await PostAsync<CheckoutResult>(session, "replate_checkout", new
        {
            p_order_id = request.Id,
            p_restaurant_id = request.RestaurantId,
            p_items = request.Items.Select(item => new
            {
                listing_id = item.ListingId, quantity = item.Quantity, unit_price = item.UnitPrice
            }).ToArray()
        }).ConfigureAwait(false);
        if (result is null || result.Id != request.Id || result.Items.Count != request.Items.Count ||
            !Enum.TryParse<OrderStatus>(result.Status, out var status) || !Enum.IsDefined(status))
            throw new InvalidOperationException("Supabase mengembalikan hasil checkout yang tidak lengkap.");

        var order = new Order(status, result.CompletedAt?.UtcDateTime)
        {
            Id = result.Id, CustomerId = session.User.Id, RestaurantId = request.RestaurantId,
            PickupCode = result.PickupCode, TotalPrice = result.TotalPrice, OrderedAt = result.OrderedAt.UtcDateTime
        };
        if (result.PreparationMinutes is int minutes)
            order.SetPreparationTime(TimeSpan.FromMinutes(minutes));

        var requestedItems = request.Items.ToDictionary(item => item.ListingId);
        var remainingStock = new Dictionary<Guid, int>();
        foreach (var saved in result.Items)
        {
            if (!requestedItems.Remove(saved.ListingId, out var requested) ||
                saved.Quantity != requested.Quantity || saved.UnitPrice != requested.UnitPrice)
                throw new InvalidOperationException("Rincian pesanan dari Supabase tidak cocok dengan keranjang.");
            order.Items.Add(new OrderItem
            {
                Id = saved.Id, OrderId = order.Id, ListingId = saved.ListingId, Listing = requested.Listing,
                MenuName = saved.MenuName, Quantity = saved.Quantity, UnitPrice = saved.UnitPrice
            });
            if (saved.Stock is int quantity) remainingStock.Add(saved.ListingId, quantity);
        }

        return (order, remainingStock);
    }

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static async Task<T?> PostAsync<T>(SupabaseAuth.Session session, string function, object payload, bool readResponse = true)
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
                throw new InvalidOperationException("Pesanan gagal disimpan. Periksa apakah fungsi pesanan sudah dipasang di Supabase.");
            }

            return readResponse ? await response.Content.ReadFromJsonAsync<T>(Json).ConfigureAwait(false) : default;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new InvalidOperationException("Replate gagal menyimpan status pesanan lewat Data API Supabase.", e);
        }
    }

    private sealed record ApiError(string Code, string Message);
    private sealed record ListingStock([property: JsonPropertyName("listing_id")] Guid ListingId, int Quantity);
    private sealed record CheckoutResult(Guid Id, string PickupCode, decimal TotalPrice, string Status,
        DateTimeOffset OrderedAt, DateTimeOffset? CompletedAt, int? PreparationMinutes, List<CheckoutItem> Items);
    private sealed record CheckoutItem(Guid Id, Guid ListingId, string MenuName, int Quantity, decimal UnitPrice, int? Stock);
}
