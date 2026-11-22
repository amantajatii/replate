using System.Security.Authentication;
using Npgsql;
using Replate.Domain.Entities;
using Replate.Domain.Enums;

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
            ALTER TABLE public.orders ADD COLUMN IF NOT EXISTS preparation_minutes integer;
            ALTER TABLE public.orders ADD COLUMN IF NOT EXISTS completed_at timestamptz;

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
            if (SupabaseAuth.IsConfigured)
            {
                await using var ownership = new NpgsqlCommand("""
                    ALTER TABLE public.restaurants
                        ADD COLUMN IF NOT EXISTS owner_id uuid REFERENCES auth.users(id);
                    CREATE INDEX IF NOT EXISTS restaurants_owner_id_idx ON public.restaurants (owner_id);
                    """, connection);
                await ownership.ExecuteNonQueryAsync();
            }
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

    internal List<Restaurant> LoadOwnedRestaurants(Guid ownerId)
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();
            using var command = new NpgsqlCommand("""
                SELECT id, name, address, phone, description, opening_time, closing_time
                FROM public.restaurants WHERE owner_id = @owner_id
                ORDER BY name, id
                """, connection);
            command.Parameters.AddWithValue("owner_id", ownerId);
            using var reader = command.ExecuteReader();
            var restaurants = new List<Restaurant>();
            while (reader.Read())
                restaurants.Add(new Restaurant
                {
                    Id = reader.GetGuid(0), Name = reader.GetString(1), Address = reader.GetString(2),
                    Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    OpeningTime = reader.IsDBNull(5) ? default : reader.GetFieldValue<TimeOnly>(5),
                    ClosingTime = reader.IsDBNull(6) ? default : reader.GetFieldValue<TimeOnly>(6)
                });
            return restaurants;
        }
        catch (NpgsqlException e)
        {
            throw new InvalidOperationException("Hak akses restoran gagal dibaca dari Supabase.", e);
        }
    }

    internal List<SurplusListing> LoadListings()
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();
            using var command = new NpgsqlCommand("""
                SELECT listing.id, listing.menu_id, COALESCE(menu.name, listing.menu_name),
                       COALESCE(menu.description, listing.description), COALESCE(menu.category, listing.category),
                       COALESCE(menu.normal_price, listing.normal_price),
                       COALESCE(menu.weight_per_portion, listing.weight_per_portion), listing.rescue_price, listing.quantity,
                       listing.pickup_start, listing.pickup_end, listing.closed,
                       restaurant.id, restaurant.name, restaurant.address, COALESCE(menu.is_active, true)
                FROM public.surplus_listings AS listing
                LEFT JOIN public.menus AS menu ON menu.id = listing.menu_id
                LEFT JOIN public.restaurants AS restaurant ON restaurant.id = menu.restaurant_id
                ORDER BY listing.pickup_start, listing.id
                """, connection);
            using var reader = command.ExecuteReader();
            var listings = new List<SurplusListing>();
            while (reader.Read())
            {
                var menu = new Menu { Id = reader.GetGuid(1) };
                menu.UpdateMenu(reader.GetString(2), reader.GetString(3), reader.GetString(4),
                    reader.GetDecimal(5), reader.GetDecimal(6));
                if (!reader.GetBoolean(15)) menu.Deactivate();
                if (!reader.IsDBNull(12))
                {
                    menu.RestaurantId = reader.GetGuid(12);
                    menu.Restaurant = new Restaurant
                    {
                        Id = menu.RestaurantId, Name = reader.GetString(13), Address = reader.GetString(14)
                    };
                }
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

    internal void SaveListing(Restaurant restaurant, SurplusListing listing, int? expectedQuantity, bool preserveCatalog)
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using var saveRestaurant = new NpgsqlCommand("""
                INSERT INTO public.restaurants (id, name, address)
                VALUES (@id, @name, @address)
                ON CONFLICT (id) DO UPDATE
                SET name = EXCLUDED.name, address = EXCLUDED.address
                WHERE NOT @preserve
                """, connection, transaction);
            saveRestaurant.Parameters.AddWithValue("id", restaurant.Id);
            saveRestaurant.Parameters.AddWithValue("name", restaurant.Name);
            saveRestaurant.Parameters.AddWithValue("address", restaurant.Address);
            saveRestaurant.Parameters.AddWithValue("preserve", preserveCatalog);
            saveRestaurant.ExecuteNonQuery();

            using var saveMenu = new NpgsqlCommand("""
                INSERT INTO public.menus AS menu (id, restaurant_id, name, description, category, normal_price, weight_per_portion)
                VALUES (@id, @restaurant_id, @name, @description, @category, @normal_price, @weight)
                ON CONFLICT (id) DO UPDATE SET name = CASE WHEN @preserve THEN menu.name ELSE EXCLUDED.name END
                WHERE menu.restaurant_id = EXCLUDED.restaurant_id AND menu.deleted_at IS NULL
                    AND (NOT @preserve OR (menu.is_active AND COALESCE(menu.normal_price, @normal_price) >= @rescue_price))
                """, connection, transaction);
            saveMenu.Parameters.AddWithValue("id", listing.MenuId);
            saveMenu.Parameters.AddWithValue("restaurant_id", restaurant.Id);
            saveMenu.Parameters.AddWithValue("name", listing.Menu.Name);
            saveMenu.Parameters.AddWithValue("description", listing.Menu.Description);
            saveMenu.Parameters.AddWithValue("category", listing.Menu.Category);
            saveMenu.Parameters.AddWithValue("normal_price", listing.Menu.NormalPrice);
            saveMenu.Parameters.AddWithValue("weight", listing.Menu.WeightPerPortion);
            saveMenu.Parameters.AddWithValue("rescue_price", listing.RescuePrice);
            saveMenu.Parameters.AddWithValue("preserve", preserveCatalog);
            if (saveMenu.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Menu sudah berubah atau tidak tersedia. Buka kembali daftar menu.");

            if (preserveCatalog)
            {
                using var checkProduction = new NpgsqlCommand("""
                    SELECT (p.produced_quantity::bigint - p.regular_sold_quantity - p.rescued_quantity
                        - public.replate_allocated_portions(p.menu_id, p.record_date, @listing_id))
                    FROM public.production_records AS p
                    WHERE p.menu_id = @menu_id AND p.record_date = (@pickup_start AT TIME ZONE 'Asia/Jakarta')::date
                    FOR UPDATE OF p
                    """, connection, transaction);
                checkProduction.Parameters.AddWithValue("listing_id", listing.Id);
                checkProduction.Parameters.AddWithValue("menu_id", listing.MenuId);
                checkProduction.Parameters.AddWithValue("pickup_start", listing.PickupStart);
                if (checkProduction.ExecuteScalar() is not long available)
                    throw new InvalidOperationException("Simpan catatan produksi di Supabase untuk tanggal pengambilan terlebih dahulu.");
                if (available < listing.Quantity)
                    throw new InvalidOperationException("Stok melebihi surplus yang belum dialokasikan.");
            }

            var sql = expectedQuantity.HasValue
                ? """
                    UPDATE public.surplus_listings
                    SET menu_name = @menu_name, description = @description, category = @category,
                        normal_price = @normal_price, weight_per_portion = @weight,
                        rescue_price = @price, quantity = @quantity,
                        pickup_start = @start, pickup_end = @end
                    WHERE id = @id AND menu_id = @menu_id AND NOT closed AND quantity = @expected
                        AND ((pickup_start = @start AND pickup_end = @end) OR NOT EXISTS (
                            SELECT 1 FROM public.order_items AS item
                            JOIN public.orders AS o ON o.id = item.order_id
                            WHERE item.listing_id = @id AND o.status IN ('Reserved', 'Confirmed', 'ReadyForPickup', 'Completed')
                        ))
                    """
                : """
                    INSERT INTO public.surplus_listings
                        (id, menu_id, menu_name, description, category, normal_price,
                         weight_per_portion, rescue_price, quantity, pickup_start, pickup_end)
                    VALUES (@id, @menu_id, @menu_name, @description, @category, @normal_price,
                            @weight, @price, @quantity, @start, @end)
                    """;
            using var command = new NpgsqlCommand(sql, connection, transaction);
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
                throw new InvalidOperationException("Stok atau jadwal tidak bisa diubah. Buka ulang daftar penawaran dan periksa pesanan terkait.");
            transaction.Commit();
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

    internal void ConfirmOrder(Guid orderId, Guid restaurantId, int preparationMinutes)
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();
            using var command = new NpgsqlCommand("""
                UPDATE public.orders
                SET status = 'Confirmed', preparation_minutes = @minutes
                WHERE id = @id AND restaurant_id = @restaurant_id
                    AND (status = 'Reserved' OR (status = 'Confirmed' AND preparation_minutes = @minutes))
                """, connection);
            command.Parameters.AddWithValue("id", orderId);
            command.Parameters.AddWithValue("restaurant_id", restaurantId);
            command.Parameters.AddWithValue("minutes", preparationMinutes);
            if (command.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Pesanan sudah berubah di Supabase. Buka ulang daftar pesanan.");
        }
        catch (NpgsqlException e)
        {
            throw new InvalidOperationException("Konfirmasi pesanan gagal disimpan ke Supabase.", e);
        }
    }

    internal void UpdateOrderStatus(Order order, OrderStatus next, DateTime? completedAt = null)
    {
        try
        {
            using var connection = CreateConnection();
            connection.Open();
            using var command = new NpgsqlCommand("""
                UPDATE public.orders
                SET status = @next, completed_at = COALESCE(completed_at, @completed_at)
                WHERE id = @id AND restaurant_id = @restaurant_id AND status IN (@previous, @next)
                """, connection);
            command.Parameters.AddWithValue("id", order.Id);
            command.Parameters.AddWithValue("restaurant_id", order.RestaurantId);
            command.Parameters.AddWithValue("previous", order.Status.ToString());
            command.Parameters.AddWithValue("next", next.ToString());
            command.Parameters.AddWithValue("completed_at", NpgsqlTypes.NpgsqlDbType.TimestampTz, (object?)completedAt ?? DBNull.Value);
            if (command.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Status pesanan sudah berubah di Supabase. Buka ulang daftar pesanan.");
        }
        catch (NpgsqlException e)
        {
            throw new InvalidOperationException("Status pesanan gagal disimpan ke Supabase.", e);
        }
    }

    internal List<Order> LoadOrders(Guid? restaurantId, IReadOnlyDictionary<Guid, SurplusListing> listings, Guid? customerId = null)
    {
        if (restaurantId is null && customerId is null)
            throw new ArgumentException("Restoran atau pelanggan harus ditentukan.");

        try
        {
            using var connection = CreateConnection();
            connection.Open();
            using var command = new NpgsqlCommand("""
                SELECT o.id, o.customer_id, o.pickup_code, o.total_price, o.status, o.ordered_at,
                       item.id, item.listing_id, item.menu_name, item.quantity, item.unit_price,
                       o.preparation_minutes, o.completed_at, o.restaurant_id
                FROM public.orders AS o
                JOIN public.order_items AS item ON item.order_id = o.id
                WHERE (@restaurant_id IS NULL OR o.restaurant_id = @restaurant_id)
                  AND (@customer_id IS NULL OR o.customer_id = @customer_id)
                ORDER BY o.ordered_at DESC, o.id, item.id
                """, connection);
            command.Parameters.AddWithValue("restaurant_id", NpgsqlTypes.NpgsqlDbType.Uuid, (object?)restaurantId ?? DBNull.Value);
            command.Parameters.AddWithValue("customer_id", NpgsqlTypes.NpgsqlDbType.Uuid, (object?)customerId ?? DBNull.Value);
            using var reader = command.ExecuteReader();
            var orders = new Dictionary<Guid, Order>();
            var incomplete = new HashSet<Guid>();
            while (reader.Read())
            {
                var id = reader.GetGuid(0);
                var listingId = reader.GetGuid(7);
                var owningRestaurantId = reader.GetGuid(13);
                if (!listings.TryGetValue(listingId, out var listing) || listing.Menu.RestaurantId != owningRestaurantId)
                {
                    incomplete.Add(id);
                    continue;
                }

                if (!orders.TryGetValue(id, out var order))
                {
                    if (!Enum.TryParse<OrderStatus>(reader.GetString(4), out var status) || !Enum.IsDefined(status))
                        throw new InvalidOperationException("Status pesanan dari PostgreSQL tidak dikenali.");

                    order = new Order(status, reader.IsDBNull(12) ? null : reader.GetDateTime(12))
                    {
                        Id = id, CustomerId = reader.GetGuid(1), RestaurantId = owningRestaurantId,
                        PickupCode = reader.GetString(2), TotalPrice = reader.GetDecimal(3), OrderedAt = reader.GetDateTime(5)
                    };
                    if (!reader.IsDBNull(11))
                        order.SetPreparationTime(TimeSpan.FromMinutes(reader.GetInt32(11)));
                    orders.Add(id, order);
                }

                order.Items.Add(new OrderItem
                {
                    Id = reader.GetGuid(6), OrderId = id, ListingId = listingId, Listing = listing,
                    MenuName = reader.GetString(8), Quantity = reader.GetInt32(9), UnitPrice = reader.GetDecimal(10)
                });
            }

            return orders.Values.Where(order => !incomplete.Contains(order.Id)).ToList();
        }
        catch (NpgsqlException e)
        {
            throw new InvalidOperationException("Pesanan restoran dari PostgreSQL gagal dimuat.", e);
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
