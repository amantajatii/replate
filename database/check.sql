-- Jalankan: psql -d replate -v ON_ERROR_STOP=1 -f database/check.sql
-- Data pemeriksaan selalu dibatalkan.
BEGIN;
DO $$
DECLARE
    table_name text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY[
        'users', 'restaurants', 'menus', 'production_records',
        'production_recommendations', 'surplus_listings', 'carts',
        'cart_items', 'orders', 'order_items', 'pickup_verifications'
    ] LOOP
        IF to_regclass('public.' || table_name) IS NULL THEN
            RAISE EXCEPTION 'Tabel % belum dibuat', table_name;
        END IF;
    END LOOP;

    BEGIN
        INSERT INTO users (name, email, password_hash, role)
        VALUES ('Schema check', gen_random_uuid() || '@example.invalid', 'test', 'INVALID');
        RAISE EXCEPTION 'Role tidak valid seharusnya ditolak';
    EXCEPTION WHEN check_violation THEN
        NULL;
    END;

    BEGIN
        INSERT INTO carts (customer_id) VALUES (gen_random_uuid());
        RAISE EXCEPTION 'Customer yang tidak ada seharusnya ditolak';
    EXCEPTION WHEN foreign_key_violation THEN
        NULL;
    END;
END;
$$;
ROLLBACK;
