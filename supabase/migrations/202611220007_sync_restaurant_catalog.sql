begin;

alter table public.menus
    add column if not exists description text,
    add column if not exists category text,
    add column if not exists normal_price numeric,
    add column if not exists weight_per_portion numeric,
    add column if not exists is_active boolean not null default true,
    add column if not exists deleted_at timestamptz;

update public.menus as m
set description = s.description, category = s.category,
    normal_price = s.normal_price, weight_per_portion = s.weight_per_portion
from (
    select distinct on (menu_id) menu_id, description, category, normal_price, weight_per_portion
    from public.surplus_listings order by menu_id, pickup_start desc, id
) as s
where m.id = s.menu_id and m.normal_price is null;

create or replace function public.replate_owner_menus(p_restaurant_id uuid)
returns table (id uuid, name text, description text, category text, normal_price numeric,
    weight_per_portion numeric, is_active boolean, deleted_at timestamptz)
language sql stable security definer set search_path = ''
as $$
    select m.id, m.name, m.description, m.category, m.normal_price,
        m.weight_per_portion, m.is_active, m.deleted_at
    from public.menus as m join public.restaurants as r on r.id = m.restaurant_id
    where r.id = p_restaurant_id and r.owner_id = (select auth.uid())
    order by lower(m.name), m.id;
$$;

create or replace function public.replate_save_menu(p_restaurant_id uuid, p_menu_id uuid, p_name text,
    p_description text, p_category text, p_price numeric, p_weight numeric, p_is_active boolean)
returns void
language plpgsql security definer set search_path = ''
as $$
begin
    perform 1 from public.restaurants as r
    where r.id = p_restaurant_id and r.owner_id = (select auth.uid()) for share;
    if not found then
        raise exception using errcode = '42501', message = 'Akun ini tidak boleh mengubah menu restoran tersebut.';
    end if;

    if char_length(btrim(coalesce(p_name, ''))) not between 1 and 80
        or char_length(coalesce(p_description, '')) > 500 or nullif(btrim(p_category), '') is null
        or p_price is null or p_price <= 0 or p_weight is null or p_weight <= 0 or p_is_active is null then
        raise exception 'Data menu tidak valid.';
    end if;

    if exists (select 1 from public.surplus_listings as s
        where s.menu_id = p_menu_id and not s.closed and s.quantity > 0
            and s.pickup_end >= now() and s.rescue_price > p_price) then
        raise exception 'Harga normal tidak boleh lebih rendah dari harga penawaran aktif.';
    end if;

    insert into public.menus as m (id, restaurant_id, name, description, category, normal_price, weight_per_portion, is_active)
    values (p_menu_id, p_restaurant_id, btrim(p_name), coalesce(p_description, ''), p_category, p_price, p_weight, p_is_active)
    on conflict (id) do update set name = excluded.name, description = excluded.description,
        category = excluded.category, normal_price = excluded.normal_price,
        weight_per_portion = excluded.weight_per_portion, is_active = excluded.is_active
    where m.restaurant_id = excluded.restaurant_id and m.deleted_at is null;
    if not found then
        raise exception 'Menu sudah dihapus atau tidak tersedia untuk restoran ini.';
    end if;
end;
$$;

create or replace function public.replate_delete_menu(p_restaurant_id uuid, p_menu_id uuid)
returns void
language plpgsql security definer set search_path = ''
as $$
begin
    perform 1 from public.restaurants as r
    where r.id = p_restaurant_id and r.owner_id = (select auth.uid()) for share;
    if not found then
        raise exception using errcode = '42501', message = 'Akun ini tidak boleh menghapus menu restoran tersebut.';
    end if;
    perform 1 from public.menus as m where m.id = p_menu_id and m.restaurant_id = p_restaurant_id for update;
    if not found then
        return;
    end if;
    if exists (select 1 from public.surplus_listings as s where s.menu_id = p_menu_id) then
        raise exception 'Menu masih dipakai oleh penawaran. Gunakan Nonaktifkan.';
    end if;
    update public.menus as m
    set is_active = false, deleted_at = coalesce(m.deleted_at, now())
    where m.id = p_menu_id;
end;
$$;

create or replace function public.replate_set_menu_active(p_restaurant_id uuid, p_menu_id uuid, p_is_active boolean)
returns void
language plpgsql security definer set search_path = ''
as $$
begin
    perform 1 from public.restaurants as r
    where r.id = p_restaurant_id and r.owner_id = (select auth.uid()) for share;
    if not found then
        raise exception using errcode = '42501', message = 'Akun ini tidak boleh mengubah menu restoran tersebut.';
    end if;
    update public.menus as m set is_active = p_is_active
    where m.id = p_menu_id and m.restaurant_id = p_restaurant_id and m.deleted_at is null;
    if not found then
        raise exception 'Menu tidak tersedia di Supabase. Simpan menu terlebih dahulu.';
    end if;
end;
$$;

create or replace function public.replate_save_restaurant_profile(p_restaurant_id uuid, p_name text,
    p_address text, p_phone text, p_description text, p_opening_time time, p_closing_time time)
returns void
language plpgsql security definer set search_path = ''
as $$
begin
    if char_length(btrim(coalesce(p_name, ''))) not between 1 and 80
        or char_length(btrim(coalesce(p_address, ''))) not between 1 and 500
        or coalesce(p_phone, '') !~ '^[0-9 +\-]+$'
        or char_length(regexp_replace(p_phone, '[^0-9]', '', 'g')) not between 8 and 15
        or char_length(coalesce(p_description, '')) > 500
        or p_opening_time is null or p_closing_time is null or p_opening_time = p_closing_time then
        raise exception 'Profil restoran tidak valid.';
    end if;
    update public.restaurants as r
    set name = btrim(p_name), address = btrim(p_address), phone = btrim(p_phone),
        description = coalesce(p_description, ''), opening_time = p_opening_time, closing_time = p_closing_time
    where r.id = p_restaurant_id and r.owner_id = (select auth.uid());
    if not found then
        raise exception using errcode = '42501', message = 'Akun ini tidak boleh mengubah profil restoran tersebut.';
    end if;
end;
$$;

revoke all on function public.replate_owner_menus(uuid),
    public.replate_save_menu(uuid, uuid, text, text, text, numeric, numeric, boolean),
    public.replate_delete_menu(uuid, uuid),
    public.replate_set_menu_active(uuid, uuid, boolean),
    public.replate_save_restaurant_profile(uuid, text, text, text, text, time, time)
from public, anon, authenticated;
grant execute on function public.replate_owner_menus(uuid),
    public.replate_save_menu(uuid, uuid, text, text, text, numeric, numeric, boolean),
    public.replate_delete_menu(uuid, uuid),
    public.replate_set_menu_active(uuid, uuid, boolean),
    public.replate_save_restaurant_profile(uuid, text, text, text, text, time, time)
to authenticated;

create or replace function public.replate_check_order_menu_active()
returns trigger
language plpgsql set search_path = ''
as $$
begin
    if new.stock_reserved and exists (
        select 1 from public.surplus_listings as s join public.menus as m on m.id = s.menu_id
        where s.id = new.listing_id and (not m.is_active or m.deleted_at is not null)
    ) then
        raise exception 'Menu sudah tidak aktif. Buka kembali Jelajah dan periksa keranjang.';
    end if;
    return new;
end;
$$;

revoke all on function public.replate_check_order_menu_active() from public, anon, authenticated;
drop trigger if exists replate_check_order_menu_active on public.order_items;
create trigger replate_check_order_menu_active before insert on public.order_items
    for each row execute function public.replate_check_order_menu_active();

notify pgrst, 'reload schema';
commit;
