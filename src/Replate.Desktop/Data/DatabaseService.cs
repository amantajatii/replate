using System.Security.Authentication;
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
        var connected = false;
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            connected = true;

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
            var target = new NpgsqlConnectionStringBuilder(_connectionString);
            var stage = connected ? "gagal menyiapkan tabel orders" : "gagal membuka koneksi";
            var detail = e switch
            {
                PostgresException { SqlState: "28P01" or "28000" } =>
                    "Autentikasi ditolak. Periksa username Session pooler dan password database.",
                PostgresException { SqlState: "42501" } =>
                    "Izin database kurang. Periksa izin pengguna untuk membuat tabel orders.",
                PostgresException postgres => $"PostgreSQL mengembalikan SQLSTATE {postgres.SqlState}.",
                _ when e.GetBaseException() is AuthenticationException =>
                    "Validasi TLS gagal. Periksa sertifikat dan kecocokan host Supabase.",
                _ when e.GetBaseException() is TimeoutException =>
                    "Koneksi atau perintah melewati batas waktu. Periksa jaringan dan status proyek Supabase.",
                _ => "Periksa host, port, dan apakah server database dapat dijangkau."
            };
            throw new InvalidOperationException(
                $"PostgreSQL {stage} di {target.Host}:{target.Port}/{target.Database}. {detail}", e);
        }
    }

    public async Task SaveOrderAsync(Order order)
    {
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            const string sql = """
            INSERT INTO public.orders
                (id, customer_id, restaurant_id, pickup_code, total_price, status, ordered_at)
            VALUES
                (@id, @customer_id, @restaurant_id, @pickup_code, @total_price, @status, @ordered_at)
            ON CONFLICT (id) DO NOTHING;
            """;

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("id", order.Id);
            command.Parameters.AddWithValue("customer_id", order.CustomerId);
            command.Parameters.AddWithValue("restaurant_id", order.RestaurantId);
            command.Parameters.AddWithValue("pickup_code", order.PickupCode);
            command.Parameters.AddWithValue("total_price", order.TotalPrice);
            command.Parameters.AddWithValue("status", order.Status.ToString());
            command.Parameters.AddWithValue("ordered_at", order.OrderedAt);

            if (await command.ExecuteNonQueryAsync() == 0)
            {
                await transaction.CommitAsync();
                return;
            }

            const string itemSql = """
            INSERT INTO public.order_items
                (id, order_id, listing_id, menu_name, quantity, unit_price)
            VALUES
                (@id, @order_id, @listing_id, @menu_name, @quantity, @unit_price);
            """;

            foreach (var item in order.Items)
            {
                await using var itemCommand = new NpgsqlCommand(itemSql, connection, transaction);
                itemCommand.Parameters.AddWithValue("id", item.Id);
                itemCommand.Parameters.AddWithValue("order_id", order.Id);
                itemCommand.Parameters.AddWithValue("listing_id", item.ListingId);
                itemCommand.Parameters.AddWithValue("menu_name", item.MenuName);
                itemCommand.Parameters.AddWithValue("quantity", item.Quantity);
                itemCommand.Parameters.AddWithValue("unit_price", item.UnitPrice);
                await itemCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (NpgsqlException e)
        {
            throw new InvalidOperationException("Pesanan dan detailnya gagal disimpan ke PostgreSQL. " +
                "Periksa koneksi serta tabel orders dan order_items.", e);
        }
    }
}