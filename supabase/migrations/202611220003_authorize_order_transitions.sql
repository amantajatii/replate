begin;

-- The pickup code is visible only to the customer who placed the order.
revoke select on public.orders from public, anon, authenticated;
revoke select (pickup_code) on public.orders from public, anon, authenticated;
grant select (id, customer_id, restaurant_id, total_price, status, ordered_at, preparation_minutes, completed_at)
    on public.orders to authenticated;

create or replace function public.replate_customer_pickup_codes(p_order_ids uuid[])
returns table (order_id uuid, pickup_code text)
language sql stable security definer set search_path = ''
as $$
    select o.id, o.pickup_code::text
    from public.orders as o
    where o.id = any(p_order_ids) and o.customer_id = (select auth.uid());
$$;

revoke all on function public.replate_customer_pickup_codes(uuid[]) from public, anon, authenticated;
grant execute on function public.replate_customer_pickup_codes(uuid[]) to authenticated;

create or replace function public.replate_update_order_status(
    p_order_id uuid, p_next_status text, p_preparation_minutes integer, p_pickup_code text
)
returns timestamptz
language plpgsql security definer set search_path = ''
as $$
declare
    current_order public.orders%rowtype;
    finished_at timestamptz;
begin
    select o.* into current_order
    from public.orders as o
    join public.restaurants as r on r.id = o.restaurant_id
    where o.id = p_order_id and r.owner_id = (select auth.uid())
    for update of o for share of r;

    if not found then
        raise exception using errcode = '42501', message = 'Akun ini tidak boleh mengubah status pesanan tersebut.';
    end if;

    if p_next_status = 'Confirmed' then
        if p_preparation_minutes is null or p_preparation_minutes not between 1 and 240 then
            raise exception 'Waktu persiapan harus antara 1 dan 240 menit.';
        end if;
        if current_order.status = 'Confirmed' and current_order.preparation_minutes = p_preparation_minutes then
            return null;
        end if;
        if current_order.status <> 'Reserved' then
            raise exception 'Pesanan sudah berubah. Buka ulang daftar pesanan.';
        end if;
    elsif p_next_status = 'ReadyForPickup' then
        if current_order.status = 'ReadyForPickup' then
            return null;
        end if;
        if current_order.status <> 'Confirmed' then
            raise exception 'Pesanan sudah berubah. Buka ulang daftar pesanan.';
        end if;
    elsif p_next_status = 'Completed' then
        if current_order.pickup_code is distinct from upper(btrim(p_pickup_code)) then
            raise exception 'Kode pengambilan tidak cocok. Periksa kode dari pelanggan.';
        end if;
        if current_order.status = 'Completed' then
            return current_order.completed_at;
        end if;
        if current_order.status <> 'ReadyForPickup' then
            raise exception 'Pesanan sudah berubah. Buka ulang daftar pesanan.';
        end if;
        finished_at := clock_timestamp();
    else
        raise exception 'Perubahan status ini tidak didukung.';
    end if;

    if not exists (
        select 1 from public.order_items as i where i.order_id = current_order.id
    ) or exists (
        select 1 from public.order_items as i
        left join public.surplus_listings as s on s.id = i.listing_id
        where i.order_id = current_order.id
          and (s.id is null or s.pickup_end < clock_timestamp())
    ) then
        raise exception 'Jadwal pengambilan sudah berakhir atau tidak ditemukan.';
    end if;

    update public.orders
    set status = p_next_status,
        preparation_minutes = case when p_next_status = 'Confirmed' then p_preparation_minutes else preparation_minutes end,
        completed_at = case when p_next_status = 'Completed' then finished_at else completed_at end
    where id = current_order.id;

    return finished_at;
end;
$$;

revoke all on function public.replate_update_order_status(uuid, text, integer, text) from public, anon, authenticated;
grant execute on function public.replate_update_order_status(uuid, text, integer, text) to authenticated;

notify pgrst, 'reload schema';
commit;
