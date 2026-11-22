using Replate.Domain.Entities;
using Replate.Domain.Services;

namespace Replate.Desktop.Data;

internal static class SupabaseCatalog
{
    internal static async Task<List<MenuRow>> LoadMenusAsync(SupabaseAuth.Session session, Guid restaurantId) =>
        await SupabaseRpc.PostAsync<List<MenuRow>>(session, "replate_owner_menus", new { p_restaurant_id = restaurantId })
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Supabase tidak mengembalikan daftar menu.");

    internal static Task SaveMenuAsync(SupabaseAuth.Session session, Menu menu, bool isActive) =>
        SupabaseRpc.PostAsync<object>(session, "replate_save_menu", new
        {
            p_restaurant_id = menu.RestaurantId, p_menu_id = menu.Id, p_name = menu.Name,
            p_description = menu.Description, p_category = menu.Category, p_price = menu.NormalPrice,
            p_weight = menu.WeightPerPortion, p_is_active = isActive
        }, readResponse: false);

    internal static Task DeleteMenuAsync(SupabaseAuth.Session session, Menu menu) =>
        SupabaseRpc.PostAsync<object>(session, "replate_delete_menu",
            new { p_restaurant_id = menu.RestaurantId, p_menu_id = menu.Id }, readResponse: false);

    internal static Task SetMenuActiveAsync(SupabaseAuth.Session session, Menu menu, bool isActive) =>
        SupabaseRpc.PostAsync<object>(session, "replate_set_menu_active",
            new { p_restaurant_id = menu.RestaurantId, p_menu_id = menu.Id, p_is_active = isActive }, readResponse: false);

    internal static Task SaveRestaurantAsync(SupabaseAuth.Session session, Guid restaurantId, RestaurantProfile profile) =>
        SupabaseRpc.PostAsync<object>(session, "replate_save_restaurant_profile", new
        {
            p_restaurant_id = restaurantId, p_name = profile.Name.Trim(), p_address = profile.Address.Trim(),
            p_phone = profile.Phone.Trim(), p_description = profile.Description.Trim(),
            p_opening_time = profile.OpeningTime, p_closing_time = profile.ClosingTime
        }, readResponse: false);

    internal sealed record MenuRow(Guid Id, string Name, string? Description, string? Category,
        decimal? NormalPrice, decimal? WeightPerPortion, bool IsActive, DateTimeOffset? DeletedAt);
}
