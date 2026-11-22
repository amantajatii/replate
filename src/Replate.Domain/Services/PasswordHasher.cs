using System.Security.Cryptography;

namespace Replate.Domain.Services;

public static class PasswordHasher
{
    public const string Algorithm = "pbkdf2-sha256";
    public const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MaxIterations = 10_000_000;

    public static string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string? password, string? stored)
    {
        if (password is null || string.IsNullOrEmpty(stored))
        {
            return false;
        }

        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != Algorithm ||
            !int.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var iterations) ||
            iterations < 1 || iterations > MaxIterations)
        {
            return false;
        }

        var salt = TryDecode(parts[2]);
        var expected = TryDecode(parts[3]);
        if (salt is null || expected is null || salt.Length == 0 || expected.Length == 0)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[]? TryDecode(string value)
    {
        var buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out var written) ? buffer[..written] : null;
    }
}
