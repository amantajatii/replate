using System.Text;
using Replate.Domain.Entities;
using Replate.Domain.Services;

namespace Replate.Desktop;

internal static class DemoAccounts
{
    internal const string Password = "replate123";
    internal const string Domain = "replate.demo";
    internal const string CustomerEmail = "pelanggan@" + Domain;
    internal const string CustomerName = "Pelanggan Demo";

    internal static string OwnerEmail(string restaurantName) => Slug(restaurantName) + "@" + Domain;

    internal static string Slug(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD).ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character)) builder.Append(character);
            else if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            else if (builder.Length > 0 && builder[^1] != '-') builder.Append('-');
        }
        var slug = builder.ToString().Trim('-');
        return slug.Length == 0 ? "restoran" : slug;
    }

    internal static void Ensure(List<User> users, IEnumerable<Restaurant> restaurants, IEnumerable<Cart> carts)
    {
        foreach (var restaurant in restaurants.Where(r => r.Owner is null))
        {
            var email = OwnerEmail(restaurant.Name);
            for (var suffix = 2; AccountService.FindByEmail(users, email) is not null; suffix++)
                email = $"{Slug(restaurant.Name)}-{suffix}@{Domain}";
            var owner = new RestaurantOwner { Name = "Pemilik " + restaurant.Name, Email = email, PasswordHash = PasswordHasher.Hash(Password) };
            restaurant.Owner = owner;
            restaurant.OwnerId = owner.Id;
            owner.Restaurants.Add(restaurant);
            users.Add(owner);
        }

        if (AccountService.FindByEmail(users, CustomerEmail) is not null) return;
        var known = users.Select(u => u.Id).ToHashSet();
        var orphan = carts.Select(c => c.CustomerId).FirstOrDefault(id => id != Guid.Empty && !known.Contains(id));
        users.Add(new Customer { Id = orphan == Guid.Empty ? Guid.NewGuid() : orphan, Name = CustomerName,
            Email = CustomerEmail, PasswordHash = PasswordHasher.Hash(Password) });
    }
}
