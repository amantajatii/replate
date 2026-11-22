using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Replate.Domain.Enums;
using Replate.Domain.Services;

namespace Replate.Domain.Entities;

public abstract class User
{
    private const int PasswordIterations = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public abstract UserRole Role { get; }
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    private string passwordHash = string.Empty;
    public string PasswordHash { get => passwordHash; init => passwordHash = value; }
    [JsonIgnore]
    public bool IsAuthenticated { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public void SetInitialPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (PasswordHash.Length != 0)
        {
            throw new InvalidOperationException("A password is already configured.");
        }

        passwordHash = PasswordHasher.Hash(password);
        UpdatedAt = DateTime.UtcNow;
    }

    public virtual void Login(string email, string password)
    {
        if (!TryLogin(email, password))
        {
            throw new UnauthorizedAccessException("Email atau kata sandi salah.");
        }

    }

    internal bool TryLogin(string? email, string? password)
    {
        IsAuthenticated = !string.IsNullOrWhiteSpace(Email) &&
            string.Equals(Email.Trim(), email?.Trim(), StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(password) && VerifyPassword(password);
        return IsAuthenticated;
    }

    public virtual void Logout()
    {
        IsAuthenticated = false;
    }

    protected void RequireAuthentication()
    {
        if (!IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Masuk terlebih dahulu.");
        }
    }

    private bool VerifyPassword(string password)
    {
        var parts = PasswordHash.Split(':');
        if (parts.Length != 3 || parts[0] != "pbkdf2-sha256-v1")
        {
            return PasswordHasher.Verify(password, PasswordHash);
        }

        Span<byte> salt = stackalloc byte[SaltSize];
        Span<byte> hash = stackalloc byte[HashSize];
        return Convert.TryFromBase64String(parts[1], salt, out var saltLength) && saltLength == SaltSize &&
            Convert.TryFromBase64String(parts[2], hash, out var hashLength) && hashLength == HashSize &&
            CryptographicOperations.FixedTimeEquals(hash, HashPassword(password, salt));
    }

    private static byte[] HashPassword(string password, ReadOnlySpan<byte> salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, HashSize);
}
