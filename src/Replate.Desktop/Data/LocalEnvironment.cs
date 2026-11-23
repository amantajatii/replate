using Npgsql;

namespace Replate.Desktop.Data;

internal static class LocalEnvironment
{
    private const string PoolerHost = "aws-1-ap-northeast-1.pooler.supabase.com";
    private const int PoolerPort = 5432;
    private const string CertificateFile = "prod-ca-2021.crt";

    public static void Load() => Load(AppContext.BaseDirectory);

    internal static void Load(string directory)
    {
        try
        {
            var envPath = Path.Combine(directory, ".env");
            if (File.Exists(envPath))
                Apply(Parse(File.ReadAllLines(envPath)));

            var certificate = Path.Combine(directory, CertificateFile);
            if (!File.Exists(certificate))
                return;

            if (IsMissing("REPLATE_DB_CONNECTION_STRING") &&
                SupabaseConnectionString(Environment.GetEnvironmentVariable("REPLATE_SUPABASE_URL"), certificate) is { } connection)
                Environment.SetEnvironmentVariable("REPLATE_DB_CONNECTION_STRING", connection, EnvironmentVariableTarget.Process);

            if (IsMissing("PGSSLROOTCERT"))
                Environment.SetEnvironmentVariable("PGSSLROOTCERT", certificate, EnvironmentVariableTarget.Process);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
        {
            System.Diagnostics.Trace.TraceWarning("File .env tidak dapat dibaca: {0}", e.Message);
        }
    }

    // Parser
    internal static List<KeyValuePair<string, string>> Parse(IEnumerable<string> lines)
    {
        var result = new List<KeyValuePair<string, string>>();
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
                value = value[1..^1].Trim();
            if (key.Length > 0)
                result.Add(new(key, value));
        }
        return result;
    }

    internal static void Apply(IEnumerable<KeyValuePair<string, string>> pairs)
    {
        foreach (var (key, value) in pairs)
            if (IsMissing(key))
                Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
    }

    // Supabase
    internal static string? SupabaseConnectionString(string? supabaseUrl, string certificate)
    {
        if (!Uri.TryCreate(supabaseUrl?.Trim(), UriKind.Absolute, out var uri))
            return null;
        const string suffix = ".supabase.co";
        var host = uri.Host;
        if (!host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return null;
        var projectRef = host[..^suffix.Length];
        if (projectRef.Length == 0 || projectRef.Contains('.'))
            return null;

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = PoolerHost,
            Port = PoolerPort,
            Database = "postgres",
            Username = "postgres." + projectRef,
            SslMode = SslMode.VerifyFull,
            Timeout = 10,
            RootCertificate = certificate
        };
        return builder.ConnectionString;
    }

    private static bool IsMissing(string key) => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key));
}
