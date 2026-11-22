using System.Security.Authentication;
using Npgsql;
using Replate.Domain.Entities;

namespace Replate.Desktop.Data;

internal sealed class DatabaseService : IOrderStore
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
            CREATE TABLE IF NOT EXISTS public.orders (
                id uuid PRIMARY KEY,
                customer_id uuid NOT NULL,
                restaurant_id uuid NOT NULL,
                pickup_code varchar(20) NOT NULL,
                total_price numeric(18, 2) NOT NULL,
                status varchar(30) NOT NULL,
                ordered_at timestamp with time zone NOT NULL
            );

            CREATE TABLE IF NOT EXISTS public.order_items (
                id uuid PRIMARY KEY,
                order_id uuid NOT NULL REFERENCES public.orders(id) ON DELETE CASCADE,
                listing_id uuid NOT NULL,
                menu_name varchar(200) NOT NULL,
                quantity integer NOT NULL,
                unit_price numeric(18, 2) NOT NULL
            );
            CREATE TABLE IF NOT EXISTS public.surplus_listings (
                id uuid PRIMARY KEY,
                menu_id uuid NOT NULL,
                menu_name text NOT NULL,
                description text NOT NULL,
                category text NOT NULL,
                normal_price numeric(18, 2) NOT NULL CHECK (normal_price > 0),
                weight_per_portion numeric NOT NULL CHECK (weight_per_portion > 0),
                rescue_price numeric(18, 2) NOT NULL CHECK (rescue_price > 0),
                quantity integer NOT NULL CHECK (quantity >= 0),
                pickup_start timestamptz NOT NULL,
                pickup_end timestamptz NOT NULL CHECK (pickup_end > pickup_start),
                closed boolean NOT NULL DEFAULT false
            );
            ALTER TABLE IF EXISTS public.order_items
                ADD COLUMN IF NOT EXISTS stock_reserved boolean NOT NULL DEFAULT false;
            """;

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (NpgsqlException e)
        {
            var target = new NpgsqlConnectionStringBuilder(_connectionString);
            var stage = connected ? "gagal menyiapkan tabel pesanan dan penawaran" : "gagal membuka koneksi";
            var detail = e switch
            {
                PostgresException { SqlState: "28P01" or "28000" } =>
                    "Autentikasi ditolak. Periksa username Session pooler dan password database.",
                PostgresException { SqlState: "42501" } =>
                    "Izin database kurang. Periksa izin pengguna untuk membuat tabel pesanan dan penawaran.",
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

    internal List<SurplusListing> LoadListings()
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();
            using var command = new NpgsqlCommand("""
                SELECT id, menu_id, menu_name, description, category, normal_price,
                       weight_per_portion, rescue_price, quantity, pickup_start, pickup_end, closed
                FROM public.surplus_listings
                ORDER BY pickup_start, id
                """, connection);
            using var reader = command.ExecuteReader();
            var listings = new List<SurplusListing>();
            while (reader.Read())
            {
                var menu = new Menu { Id = reader.GetGuid(1) };
                menu.UpdateMenu(reader.GetString(2), reader.GetString(3), reader.GetString(4),
                    reader.GetDecimal(5), reader.GetDecimal(6));
                var listing = new SurplusListing
                {
                    Id = reader.GetGuid(0),
                    MenuId = menu.Id,
                    Menu = menu,
                    RescuePrice = reader.GetDecimal(7),
                    PickupStart = reader.GetDateTime(9),
                    PickupEnd = reader.GetDateTime(10)
                };
                listing.UpdateStock(reader.GetInt32(8));
                if (reader.GetBoolean(11)) listing.Cancel();
                listings.Add(listing);
            }

            return listings;
        }
        catch (NpgsqlException e)
        {
            throw new InvalidOperationException("Penawaran dari PostgreSQL gagal dimuat.", e);
        }
    }

    internal void SaveListing(SurplusListing listing, int? expectedQuantity)
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();
            var sql = expectedQuantity.HasValue
                ? """
                    UPDATE public.surplus_listings
                    SET menu_name = @menu_name, description = @description, category = @category,
                        normal_price = @normal_price, weight_per_portion = @weight,
                        rescue_price = @price, quantity = @quantity,
                        pickup_start = @start, pickup_end = @end
                    WHERE id = @id AND NOT closed AND quantity = @expected
                    """
                : """
                    INSERT INTO public.surplus_listings
                        (id, menu_id, menu_name, description, category, normal_price,
                         weight_per_portion, rescue_price, quantity, pickup_start, pickup_end)
                    VALUES (@id, @menu_id, @menu_name, @description, @category, @normal_price,
                            @weight, @price, @quantity, @start, @end)
                    """;
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", listing.Id);
            command.Parameters.AddWithValue("menu_id", listing.MenuId);
            command.Parameters.AddWithValue("menu_name", listing.Menu.Name);
            command.Parameters.AddWithValue("description", listing.Menu.Description);
            command.Parameters.AddWithValue("category", listing.Menu.Category);
            command.Parameters.AddWithValue("normal_price", listing.Menu.NormalPrice);
            command.Parameters.AddWithValue("weight", listing.Menu.WeightPerPortion);
            command.Parameters.AddWithValue("price", listing.RescuePrice);
            command.Parameters.AddWithValue("quantity", listing.Quantity);
            command.Parameters.AddWithValue("start", listing.PickupStart);
            command.Parameters.AddWithValue("end", listing.PickupEnd);
            if (expectedQuantity is int previous) command.Parameters.AddWithValue("expected", previous);
            if (command.ExecuteNonQuery() == 0)
                throw new InvalidOperationException("Stok penawaran sudah berubah. Buka ulang daftar penawaran sebelum mengedit.");
        }
        catch (NpgsqlException e)
        {
            throw new InvalidOperationException("Penawaran gagal disimpan ke PostgreSQL.", e);
        }
    }

    internal void CloseListing(Guid listingId)
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();
            using var command = new NpgsqlCommand(
                "UPDATE public.surplus_listings SET closed = true WHERE id = @id", connection);
            command.Parameters.AddWithValue("id", listingId);
            command.ExecuteNonQuery();
        }
        catch (NpgsqlException e)
        {
            throw new InvalidOperationException("Penawaran gagal ditutup di PostgreSQL.", e);
        }
    }

    internal bool CancelOrder(Guid orderId, bool allowAlreadyCancelled)
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            using var cancel = new NpgsqlCommand("""
                UPDATE public.orders SET status = 'Cancelled'
                WHERE id = @id AND status IN ('Reserved', 'Confirmed', 'ReadyForPickup')
                """, connection, transaction);
            cancel.Parameters.AddWithValue("id", orderId);
            if (cancel.ExecuteNonQuery() != 1)
            {
                if (allowAlreadyCancelled)
                {
                    using var status = new NpgsqlCommand(
                        "SELECT status FROM public.orders WHERE id = @id", connection, transaction);
                    status.Parameters.AddWithValue("id", orderId);
                    if (status.ExecuteScalar() is "Cancelled") return false;
                }

                throw new InvalidOperationException("Pesanan sudah dibatalkan atau tidak dapat dibatalkan.");
            }

            using var restore = new NpgsqlCommand("""
                UPDATE public.surplus_listings AS listing
                SET quantity = listing.quantity + items.quantity
                FROM (
                    SELECT listing_id, SUM(quantity)::integer AS quantity
                    FROM public.order_items WHERE order_id = @id AND stock_reserved GROUP BY listing_id
                ) AS items
                WHERE listing.id = items.listing_id
                """, connection, transaction);
            restore.Parameters.AddWithValue("id", orderId);
            restore.ExecuteNonQuery();
            transaction.Commit();
            return true;
        }
        catch (NpgsqlException e)
        {
            throw new InvalidOperationException("Pembatalan gagal disimpan ke PostgreSQL.", e);
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
                (id, order_id, listing_id, menu_name, quantity, unit_price, stock_reserved)
            VALUES
                (@id, @order_id, @listing_id, @menu_name, @quantity, @unit_price, true);
            """;

            const string reserveSql = """
                UPDATE public.surplus_listings
                SET quantity = quantity - @quantity
                WHERE id = @id AND NOT closed AND quantity >= @quantity
                    AND rescue_price = @price
                    AND CURRENT_TIMESTAMP BETWEEN pickup_start AND pickup_end;
                """;

            foreach (var item in order.Items.OrderBy(item => item.ListingId))
            {
                await using var reserve = new NpgsqlCommand(reserveSql, connection, transaction);
                reserve.Parameters.AddWithValue("id", item.ListingId);
                reserve.Parameters.AddWithValue("quantity", item.Quantity);
                reserve.Parameters.AddWithValue("price", item.UnitPrice);
                if (await reserve.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException("Stok, harga, atau jadwal penawaran sudah berubah. Buka kembali Jelajah dan periksa keranjang.");

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
