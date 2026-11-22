using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using Replate.Domain.Services;

namespace Replate.Desktop.Data;

internal static class SupabaseAuth
{
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static string? ProjectUrl => Environment.GetEnvironmentVariable("REPLATE_SUPABASE_URL");
    private static string? PublishableKey => Environment.GetEnvironmentVariable("REPLATE_SUPABASE_PUBLISHABLE_KEY");
    internal static bool IsConfigured => !string.IsNullOrWhiteSpace(ProjectUrl) || !string.IsNullOrWhiteSpace(PublishableKey);

    internal static async Task<Identity> SignInAsync(string email, string password)
    {
        var key = PublishableKey?.Trim();
        if (!Uri.TryCreate(ProjectUrl?.Trim(), UriKind.Absolute, out var project) ||
            project.Scheme != Uri.UriSchemeHttps || !project.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase) ||
            project.AbsolutePath != "/" || project.UserInfo.Length != 0 || project.Query.Length != 0 || project.Fragment.Length != 0 ||
            key is null || !key.StartsWith("sb_publishable_", StringComparison.Ordinal))
            throw new InvalidOperationException("Isi URL proyek Supabase dan publishable key di konfigurasi Replate.");

        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("REPLATE_DB_CONNECTION_STRING") ?? "");
        var projectRef = project.Host[..^".supabase.co".Length];
        var matchingDatabase = string.Equals(connection.Host, $"db.{projectRef}.supabase.co", StringComparison.OrdinalIgnoreCase) ||
            (connection.Host?.EndsWith(".pooler.supabase.com", StringComparison.OrdinalIgnoreCase) == true &&
             connection.Username?.EndsWith($".{projectRef}", StringComparison.OrdinalIgnoreCase) == true);
        if (!matchingDatabase)
            throw new InvalidOperationException("URL Supabase Auth harus memakai proyek yang sama dengan koneksi database.");

        email = AccountService.NormalizeEmail(email);
        if (email.Length == 0 || string.IsNullOrEmpty(password))
            throw new InvalidOperationException(AccountService.SignInFailedMessage);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(project, "/auth/v1/token?grant_type=password"));
        request.Headers.Add("apikey", key);
        request.Content = JsonContent.Create(new { email, password });
        try
        {
            using var response = await Http.SendAsync(request).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
                throw new InvalidOperationException("Email atau kata sandi salah, atau email belum dikonfirmasi.");
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("Supabase Auth belum bisa memproses login. Coba lagi sebentar.");

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>().ConfigureAwait(false);
            if (result?.User is not { } user || user.Id == Guid.Empty || string.IsNullOrWhiteSpace(user.Email))
                throw new InvalidOperationException("Identitas akun tidak ditemukan di Supabase Auth.");

            return user;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new InvalidOperationException("Replate gagal menghubungi Supabase Auth.", e);
        }
    }

    internal sealed record Identity(Guid Id, string Email);
    private sealed record TokenResponse([property: JsonPropertyName("user")] Identity? User);
}
