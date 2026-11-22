using System.Text.RegularExpressions;
using Replate.Domain.Entities;

namespace Replate.Domain.Services;

public sealed record RestaurantProfile(
    string Name,
    string Address,
    string Phone,
    TimeOnly? OpeningTime,
    TimeOnly? ClosingTime,
    string Description = "");

public static partial class AccountService
{
    public const string SignInFailedMessage = "Email atau kata sandi salah.";
    public const int MinimumPasswordLength = 8;
    private static readonly Lazy<string> DummyHash = new(() => PasswordHasher.Hash(Guid.NewGuid().ToString("N")));

    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    public static User? FindByEmail(IEnumerable<User> users, string? email)
    {
        var normalized = NormalizeEmail(email);
        return normalized.Length == 0 ? null : users.FirstOrDefault(user => NormalizeEmail(user.Email) == normalized);
    }

    public static Dictionary<string, string> ValidateAccount(IEnumerable<User> users, string? name, string? email, string? password)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(name)) errors["name"] = "Nama wajib diisi.";
        else if (name.Trim().Length > 80) errors["name"] = "Nama maksimal 80 karakter.";
        var normalized = NormalizeEmail(email);
        if (normalized.Length == 0) errors["email"] = "Email wajib diisi.";
        else if (normalized.Length > 254 || !EmailPattern().IsMatch(normalized)) errors["email"] = "Format email tidak valid.";
        else if (FindByEmail(users, normalized) is not null) errors["email"] = "Email sudah terdaftar.";
        if (string.IsNullOrEmpty(password)) errors["password"] = "Kata sandi wajib diisi.";
        else if (password.Length < MinimumPasswordLength) errors["password"] = $"Kata sandi minimal {MinimumPasswordLength} karakter.";
        return errors;
    }

    public static Dictionary<string, string> ValidateRestaurantProfile(RestaurantProfile? profile)
    {
        var errors = new Dictionary<string, string>();
        if (profile is null)
        {
            errors["restaurantName"] = "Nama restoran wajib diisi.";
            return errors;
        }

        if (string.IsNullOrWhiteSpace(profile.Name)) errors["restaurantName"] = "Nama restoran wajib diisi.";
        else if (profile.Name.Trim().Length > 80) errors["restaurantName"] = "Nama restoran maksimal 80 karakter.";
        if (string.IsNullOrWhiteSpace(profile.Address)) errors["address"] = "Alamat restoran wajib diisi.";
        var phone = (profile.Phone ?? string.Empty).Trim();
        var digits = phone.Count(char.IsAsciiDigit);
        if (phone.Length == 0) errors["phone"] = "Nomor telepon wajib diisi.";
        else if (!PhonePattern().IsMatch(phone)) errors["phone"] = "Nomor telepon hanya boleh berisi angka, spasi, +, dan -.";
        else if (digits is < 8 or > 15) errors["phone"] = "Nomor telepon harus terdiri dari 8–15 digit.";
        if (profile.OpeningTime is null) errors["openingTime"] = "Jam buka wajib diisi.";
        if (profile.ClosingTime is null) errors["closingTime"] = "Jam tutup wajib diisi.";
        else if (profile.OpeningTime == profile.ClosingTime) errors["closingTime"] = "Jam tutup harus berbeda dari jam buka.";
        if ((profile.Description ?? string.Empty).Trim().Length > 500) errors["description"] = "Deskripsi maksimal 500 karakter.";
        return errors;
    }

    public static Dictionary<string, string> ValidateOwnerRegistration(IEnumerable<User> users, string? name, string? email,
        string? password, RestaurantProfile? profile)
    {
        var errors = ValidateAccount(users, name, email, password);
        foreach (var (key, message) in ValidateRestaurantProfile(profile)) errors[key] = message;
        return errors;
    }

    public static Customer CreateCustomer(IEnumerable<User> users, string name, string email, string password)
    {
        var errors = ValidateAccount(users, name, email, password);
        if (errors.Count > 0) throw new InvalidOperationException(errors.First().Value);
        return new Customer { Name = name.Trim(), Email = NormalizeEmail(email), PasswordHash = PasswordHasher.Hash(password) };
    }

    public static RestaurantOwner CreateOwner(IEnumerable<User> users, string name, string email, string password, RestaurantProfile profile)
    {
        var errors = ValidateOwnerRegistration(users, name, email, password, profile);
        if (errors.Count > 0) throw new InvalidOperationException(errors.First().Value);
        var owner = new RestaurantOwner { Name = name.Trim(), Email = NormalizeEmail(email), PasswordHash = PasswordHasher.Hash(password) };
        var restaurant = new Restaurant();
        restaurant.UpdateProfile(profile.Name.Trim(), (profile.Description ?? string.Empty).Trim(), profile.Address.Trim(),
            profile.Phone.Trim(), profile.OpeningTime!.Value, profile.ClosingTime!.Value);
        restaurant.Owner = owner;
        restaurant.OwnerId = owner.Id;
        owner.Restaurants.Add(restaurant);
        return owner;
    }

    public static User? Authenticate(IEnumerable<User> users, string? email, string? password)
    {
        var user = FindByEmail(users, email);
        if (user is null)
        {
            PasswordHasher.Verify(password ?? string.Empty, DummyHash.Value);
            return null;
        }

        return user.TryLogin(email, password) ? user : null;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s.]{2,}$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[0-9 +\-]+$")]
    private static partial Regex PhonePattern();
}
