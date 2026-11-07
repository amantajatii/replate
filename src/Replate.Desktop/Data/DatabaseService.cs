using Npgsql;
using Replate.Domain.Entities;

namespace Replate.Desktop.Data;

internal sealed class DatabaseService
{
    private readonly string _connectionString = BuildConnectionString();

    private static string BuildConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("REPLATE_DB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=replate_db;Username=postgres;Timeout=5";
        var password = Environment.GetEnvironmentVariable("REPLATE_DB_PASSWORD");

        if (!string.IsNullOrWhiteSpace(password))
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString) { Password = password };
            return builder.ConnectionString;
        }

        return connectionString;
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task InitializeAsync()
    {
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            const string sql = """
            CREATE TABLE IF NOT EXISTS orders (
                id uuid PRIMARY KEY,
                customer_id uuid NOT NULL,
                restaurant_id uuid NOT NULL,
                pickup_code varchar(20) NOT NULL,
                total_price numeric(18, 2) NOT NULL,
                status varchar(30) NOT NULL,
                ordered_at timestamp with time zone NOT NULL
            );
            """;

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (NpgsqlException e)
        {
            throw new InvalidOperationException(
                "PostgreSQL belum dapat dihubungkan. Pastikan PostgreSQL berjalan, database replate_db tersedia, " +
                "dan REPLATE_DB_CONNECTION_STRING berisi username/password yang benar.", e);
        }
    }

    public async Task SaveOrderAsync(Order order)
    {
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();

            const string sql = """
            INSERT INTO orders
                (id, customer_id, restaurant_id, pickup_code, total_price, status, ordered_at)
            VALUES
                (@id, @customer_id, @restaurant_id, @pickup_code, @total_price, @status, @ordered_at)
            ON CONFLICT (id) DO NOTHING;
            """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", order.Id);
            command.Parameters.AddWithValue("customer_id", order.CustomerId);
            command.Parameters.AddWithValue("restaurant_id", order.RestaurantId);
            command.Parameters.AddWithValue("pickup_code", order.PickupCode);
            command.Parameters.AddWithValue("total_price", order.TotalPrice);
            command.Parameters.AddWithValue("status", order.Status.ToString());
            command.Parameters.AddWithValue("ordered_at", order.OrderedAt);

            await command.ExecuteNonQueryAsync();
        }
        catch (NpgsqlException e)
        {
            throw new InvalidOperationException("Pesanan dibuat, tetapi belum dapat disimpan ke PostgreSQL. " +
                "Periksa koneksi database lalu coba lagi.", e);
        }
    }
}