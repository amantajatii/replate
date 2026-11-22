using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Replate.Domain.Entities;
using Replate.Domain.Enums;

namespace Replate.Desktop.Data;

internal static class SupabaseOrderReader
{
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private const string Columns = "id,customer_id,restaurant_id,total_price,status,ordered_at," +
        "preparation_minutes,completed_at,order_items(id,listing_id,menu_name,quantity,unit_price)";

    internal static Task<List<Order>> LoadCustomerOrdersAsync(
        SupabaseAuth.Session session, IReadOnlyDictionary<Guid, SurplusListing> listings) =>
        LoadOrdersAsync(session, listings, restaurantId: null);

    internal static Task<List<Order>> LoadRestaurantOrdersAsync(
        SupabaseAuth.Session session, Guid restaurantId, IReadOnlyDictionary<Guid, SurplusListing> listings) =>
        LoadOrdersAsync(session, listings, restaurantId);

    private static async Task<List<Order>> LoadOrdersAsync(
        SupabaseAuth.Session session, IReadOnlyDictionary<Guid, SurplusListing> listings, Guid? restaurantId)
    {
        var filter = restaurantId is Guid id ? $"restaurant_id=eq.{id:D}" : $"customer_id=eq.{session.User.Id:D}";
        var orders = new List<Order>();
        try
        {
            for (var offset = 0; ; )
            {
                var path = FormattableString.Invariant(
                    $"/rest/v1/orders?select={Columns}&{filter}&order=ordered_at.desc,id.desc&limit=100&offset={offset}");
                using var request = session.CreateRequest(HttpMethod.Get, path);
                using var response = await Http.SendAsync(request).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new InvalidOperationException("Sesi Supabase sudah tidak berlaku. Keluar lalu masuk kembali.");
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("Pesanan gagal dibaca. Periksa apakah Data API Supabase dan aturan akses pesanan sudah aktif.");

                var rows = await response.Content.ReadFromJsonAsync<List<OrderRow>>(Json).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Supabase tidak mengembalikan daftar pesanan.");
                if (rows.Count == 0) return orders;
                var pickupCodes = restaurantId is null
                    ? await LoadPickupCodesAsync(session, rows.Select(row => row.Id)).ConfigureAwait(false)
                    : null;

                foreach (var row in rows)
                {
                    if (restaurantId is Guid restaurant ? row.RestaurantId != restaurant : row.CustomerId != session.User.Id)
                        throw new InvalidOperationException("Supabase mengembalikan pesanan di luar daftar yang diminta.");
                    if (row.OrderItems.Count == 0 || row.OrderItems.Any(item =>
                        !listings.TryGetValue(item.ListingId, out var listing) || listing.Menu.RestaurantId != row.RestaurantId))
                        continue;
                    if (!Enum.TryParse<OrderStatus>(row.Status, out var status) || !Enum.IsDefined(status))
                        throw new InvalidOperationException("Status pesanan dari Supabase tidak dikenali.");

                    var order = new Order(status, row.CompletedAt?.UtcDateTime)
                    {
                        Id = row.Id, CustomerId = row.CustomerId, RestaurantId = row.RestaurantId,
                        PickupCode = pickupCodes?.GetValueOrDefault(row.Id) ?? "",
                        TotalPrice = row.TotalPrice, OrderedAt = row.OrderedAt.UtcDateTime
                    };
                    if (row.PreparationMinutes is int minutes)
                        order.SetPreparationTime(TimeSpan.FromMinutes(minutes));
                    order.Items.AddRange(row.OrderItems.Select(item => new OrderItem
                    {
                        Id = item.Id, OrderId = row.Id, ListingId = item.ListingId, Listing = listings[item.ListingId],
                        MenuName = item.MenuName, Quantity = item.Quantity, UnitPrice = item.UnitPrice
                    }));
                    orders.Add(order);
                }

                offset += rows.Count;
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new InvalidOperationException("Replate gagal membaca pesanan dari Data API Supabase.", e);
        }
    }

    private static async Task<Dictionary<Guid, string>> LoadPickupCodesAsync(SupabaseAuth.Session session, IEnumerable<Guid> orderIds)
    {
        using var request = session.CreateRequest(HttpMethod.Post, "/rest/v1/rpc/replate_customer_pickup_codes");
        request.Content = JsonContent.Create(new { p_order_ids = orderIds });
        using var response = await Http.SendAsync(request).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Sesi Supabase sudah tidak berlaku. Keluar lalu masuk kembali.");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Kode pengambilan gagal dibaca dari Supabase.");
        var rows = await response.Content.ReadFromJsonAsync<List<PickupCodeRow>>(Json).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Supabase tidak mengembalikan kode pengambilan.");
        return rows.ToDictionary(row => row.OrderId, row => row.PickupCode);
    }

    private sealed record PickupCodeRow(Guid OrderId, string PickupCode);
    private sealed record OrderRow(Guid Id, Guid CustomerId, Guid RestaurantId,
        decimal TotalPrice, string Status, DateTimeOffset OrderedAt, int? PreparationMinutes, DateTimeOffset? CompletedAt,
        List<ItemRow> OrderItems);
    private sealed record ItemRow(Guid Id, Guid ListingId, string MenuName, int Quantity, decimal UnitPrice);
}
