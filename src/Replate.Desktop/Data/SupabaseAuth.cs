using System.Net;
using System.Globalization;
using System.Net.Http.Headers;
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

    private static (Uri Project, string Key) ReadProjectConfiguration()
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
        return (project, key);
    }

    internal static async Task<Session> SignInAsync(string email, string password)
    {
        var (project, key) = ReadProjectConfiguration();
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
            if (result?.User is not { } user || user.Id == Guid.Empty || string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(result.AccessToken))
                throw new InvalidOperationException("Identitas akun tidak ditemukan di Supabase Auth.");

            return new Session(project, key, user, result.AccessToken);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new InvalidOperationException("Replate gagal menghubungi Supabase Auth.", e);
        }
    }

    internal static async Task<Session?> SignUpAsync(string name, string email, string password, RestaurantProfile? restaurant)
    {
        var (project, key) = ReadProjectConfiguration();
        var data = new Dictionary<string, object> { ["replate_name"] = name.Trim() };
        if (restaurant is not null)
        {
            data["replate_restaurant"] = new
            {
                name = restaurant.Name.Trim(), address = restaurant.Address.Trim(), phone = restaurant.Phone.Trim(),
                description = (restaurant.Description ?? "").Trim(),
                opening_time = restaurant.OpeningTime!.Value.ToString("HH:mm", CultureInfo.InvariantCulture),
                closing_time = restaurant.ClosingTime!.Value.ToString("HH:mm", CultureInfo.InvariantCulture)
            };
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(project, "/auth/v1/signup"));
        request.Headers.Add("apikey", key);
        request.Content = JsonContent.Create(new { email = AccountService.NormalizeEmail(email), password, data });
        try
        {
            using var response = await Http.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<SignupError>().ConfigureAwait(false);
                throw new InvalidOperationException(error?.Code switch
                {
                    "signup_disabled" => "Pendaftaran akun baru belum diaktifkan di Supabase.",
                    "weak_password" => "Kata sandi belum memenuhi aturan Supabase.",
                    "email_address_invalid" => "Gunakan alamat email yang valid.",
                    "email_exists" or "user_already_exists" => "Email sudah terdaftar. Silakan masuk.",
                    "unexpected_failure" => "Pendaftaran gagal. Periksa apakah migrasi pendaftaran sudah dipasang di Supabase.",
                    _ when response.StatusCode == HttpStatusCode.TooManyRequests => "Terlalu banyak percobaan. Coba lagi nanti.",
                    _ => "Pendaftaran tidak berhasil. Periksa data dan pengaturan Supabase."
                });
            }

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>().ConfigureAwait(false);
            if (result is null) throw new InvalidOperationException("Supabase tidak mengembalikan hasil pendaftaran.");
            if (string.IsNullOrWhiteSpace(result.AccessToken)) return null;
            if (result.User is not { } user || user.Id == Guid.Empty || string.IsNullOrWhiteSpace(user.Email))
                throw new InvalidOperationException("Identitas akun tidak ditemukan di Supabase Auth.");
            return new Session(project, key, user, result.AccessToken);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new InvalidOperationException("Replate gagal menghubungi Supabase Auth.", e);
        }
    }

    internal sealed record Identity(Guid Id, string Email, [property: JsonPropertyName("user_metadata")] UserMetadata? Metadata);
    internal sealed record UserMetadata([property: JsonPropertyName("replate_name")] string? DisplayName);
    private sealed record SignupError([property: JsonPropertyName("error_code")] string? Code);
    internal sealed class Session(Uri project, string publishableKey, Identity user, string accessToken)
    {
        internal Uri Project { get; } = project;
        internal string PublishableKey { get; } = publishableKey;
        internal Identity User { get; } = user;
        internal string AccessToken { get; } = accessToken;

        internal HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, new Uri(Project, path));
            request.Headers.Add("apikey", PublishableKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            return request;
        }
    }

    private sealed record TokenResponse(Identity? User, [property: JsonPropertyName("access_token")] string? AccessToken);
}
