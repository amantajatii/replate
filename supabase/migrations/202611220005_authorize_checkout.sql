begin;

create or replace function public.replate_checkout(p_order_id uuid, p_restaurant_id uuid, p_items jsonb)
returns jsonb
language plpgsql security definer set search_path = ''
as $$
declare
    buyer uuid := (select auth.uid());
    saved public.orders%rowtype;
    requested record;
    listing public.surplus_listings%rowtype;
    total numeric := 0;
    pickup_bytes bytea := uuid_send(gen_random_uuid());
begin
    if buyer is null or p_order_id is null or p_restaurant_id is null
        or exists (select 1 from public.restaurants as r where r.owner_id = buyer) then
        raise exception using errcode = '42501', message = 'Masuk sebagai pelanggan untuk membuat pesanan.';
    end if;
    if p_items is null or jsonb_typeof(p_items) <> 'array' then
        raise exception 'Keranjang tidak valid.';
    end if;
    if jsonb_array_length(p_items) = 0 or (
        select count(distinct i.listing_id)
        from jsonb_to_recordset(p_items) as i(listing_id uuid)
    ) <> jsonb_array_length(p_items) then
        raise exception 'Keranjang kosong atau berisi penawaran ganda.';
    end if;

    perform pg_advisory_xact_lock(hashtextextended(p_order_id::text, 0));
    select o.* into saved from public.orders as o where o.id = p_order_id;
    if found then
        if saved.customer_id <> buyer or saved.restaurant_id <> p_restaurant_id then
            raise exception using errcode = '42501', message = 'Pesanan tidak tersedia untuk akun ini.';
        end if;
        if jsonb_array_length(p_items) <> (select count(*) from public.order_items where order_id = saved.id)
            or exists (
                select 1 from jsonb_to_recordset(p_items) as i(listing_id uuid, quantity integer, unit_price numeric)
                where not exists (
                    select 1 from public.order_items as s where s.order_id = saved.id and s.listing_id = i.listing_id
                        and s.quantity = i.quantity and s.unit_price = i.unit_price
                )
            ) then
            raise exception 'Isi keranjang berbeda dari pesanan yang sudah tersimpan.';
        end if;
    else
        insert into public.orders (id, customer_id, restaurant_id, pickup_code, total_price, status, ordered_at)
        values (p_order_id, buyer, p_restaurant_id,
            (select string_agg(substr('ABCDEFGHJKLMNPQRSTUVWXYZ23456789',
                get_byte(pickup_bytes, n) % 32 + 1, 1), '' order by n) from generate_series(0, 5) as n),
            0, 'Reserved', clock_timestamp());

        for requested in
            select * from jsonb_to_recordset(p_items) as i(listing_id uuid, quantity integer, unit_price numeric)
            order by i.listing_id
        loop
            if requested.quantity is null or requested.quantity <= 0 or requested.unit_price is null then
                raise exception 'Jumlah atau harga di keranjang tidak valid.';
            end if;

            update public.surplus_listings as s
            set quantity = s.quantity - requested.quantity
            where s.id = requested.listing_id and not s.closed and s.quantity >= requested.quantity
                and s.rescue_price = requested.unit_price
                and clock_timestamp() between s.pickup_start and s.pickup_end
                and exists (
                    select 1 from public.menus as m
                    where m.id = s.menu_id and m.restaurant_id = p_restaurant_id
                )
            returning s.* into listing;

            if not found then
                raise exception 'Stok, harga, atau jadwal penawaran sudah berubah. Buka kembali Jelajah dan periksa keranjang.';
            end if;

            insert into public.order_items (id, order_id, listing_id, menu_name, quantity, unit_price, stock_reserved)
            values (gen_random_uuid(), p_order_id, listing.id, listing.menu_name, requested.quantity, listing.rescue_price, true);
            total := total + requested.quantity * listing.rescue_price;
        end loop;

        update public.orders as o set total_price = total where o.id = p_order_id returning o.* into saved;
    end if;

    return jsonb_build_object(
        'id', saved.id, 'pickup_code', saved.pickup_code, 'total_price', saved.total_price,
        'status', saved.status, 'ordered_at', saved.ordered_at, 'completed_at', saved.completed_at,
        'preparation_minutes', saved.preparation_minutes,
        'items', (select jsonb_agg(jsonb_build_object(
            'id', i.id, 'listing_id', i.listing_id, 'menu_name', i.menu_name, 'quantity', i.quantity,
            'unit_price', i.unit_price, 'stock', s.quantity
        ) order by i.listing_id)
        from public.order_items as i left join public.surplus_listings as s on s.id = i.listing_id
        where i.order_id = saved.id)
    );
end;
$$;

revoke all on function public.replate_checkout(uuid, uuid, jsonb) from public, anon, authenticated;
grant execute on function public.replate_checkout(uuid, uuid, jsonb) to authenticated;

notify pgrst, 'reload schema';
commit;
