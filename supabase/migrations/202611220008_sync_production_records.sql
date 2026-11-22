begin;

create table if not exists public.production_records (
    menu_id uuid not null references public.menus(id),
    record_date date not null,
    produced_quantity integer not null check (produced_quantity >= 0),
    regular_sold_quantity integer not null check (regular_sold_quantity >= 0),
    rescued_quantity integer not null default 0 check (rescued_quantity >= 0),
    primary key (menu_id, record_date),
    check (regular_sold_quantity::bigint + rescued_quantity <= produced_quantity)
);

alter table public.production_records enable row level security;
revoke all on public.production_records from public, anon, authenticated;

create or replace function public.replate_allocated_portions(p_menu_id uuid, p_date date, p_exclude_listing uuid)
returns bigint
language sql stable set search_path = ''
as $$
    select coalesce((
        select sum(s.quantity)
        from public.surplus_listings as s
        where s.menu_id = p_menu_id and (s.pickup_start at time zone 'Asia/Jakarta')::date = p_date
          and not s.closed and s.pickup_end >= now() and s.id is distinct from p_exclude_listing
    ), 0) + coalesce((
        select sum(i.quantity)
        from public.order_items as i
        join public.orders as o on o.id = i.order_id
        join public.surplus_listings as s on s.id = i.listing_id
        where s.menu_id = p_menu_id and (s.pickup_start at time zone 'Asia/Jakarta')::date = p_date
          and o.status in ('Reserved', 'Confirmed', 'ReadyForPickup')
    ), 0);
$$;

create or replace function public.replate_owner_production(p_restaurant_id uuid)
returns jsonb
language sql stable security definer set search_path = ''
as $$
    select coalesce(jsonb_agg(to_jsonb(p) order by p.record_date desc, p.menu_id), '[]'::jsonb)
    from public.production_records as p
    join public.menus as m on m.id = p.menu_id
    join public.restaurants as r on r.id = m.restaurant_id
    where r.id = p_restaurant_id and r.owner_id = (select auth.uid());
$$;

create or replace function public.replate_save_production(
    p_menu_id uuid, p_record_date date, p_produced integer, p_regular_sold integer
)
returns jsonb
language plpgsql security definer set search_path = ''
as $$
declare
    saved public.production_records%rowtype;
    rescued integer;
    minimum bigint;
begin
    perform 1 from public.menus as m
    join public.restaurants as r on r.id = m.restaurant_id
    where m.id = p_menu_id and m.deleted_at is null and r.owner_id = (select auth.uid())
    for update of m for share of r;
    if not found then
        raise exception using errcode = '42501', message = 'Akun ini tidak boleh mencatat produksi untuk menu tersebut.';
    end if;
    if p_record_date is null or p_record_date > (now() at time zone 'Asia/Jakarta')::date
        or p_produced is null or p_regular_sold is null
        or p_produced < 0 or p_regular_sold < 0 or p_regular_sold > p_produced then
        raise exception 'Tanggal atau jumlah produksi tidak valid.';
    end if;

    select p.rescued_quantity into rescued from public.production_records as p
    where p.menu_id = p_menu_id and p.record_date = p_record_date for update;
    if not found then
        select coalesce(sum(i.quantity), 0)::integer into rescued
        from public.order_items as i
        join public.orders as o on o.id = i.order_id
        join public.surplus_listings as s on s.id = i.listing_id
        where s.menu_id = p_menu_id and (s.pickup_start at time zone 'Asia/Jakarta')::date = p_record_date
          and o.status = 'Completed';
    end if;

    minimum := p_regular_sold::bigint + rescued + public.replate_allocated_portions(p_menu_id, p_record_date, null);
    if p_produced < minimum then
        raise exception 'Jumlah produksi minimal % porsi.', minimum;
    end if;

    insert into public.production_records (menu_id, record_date, produced_quantity, regular_sold_quantity, rescued_quantity)
    values (p_menu_id, p_record_date, p_produced, p_regular_sold, rescued)
    on conflict (menu_id, record_date) do update
        set produced_quantity = excluded.produced_quantity, regular_sold_quantity = excluded.regular_sold_quantity
    returning * into saved;
    return to_jsonb(saved);
end;
$$;

create or replace function public.replate_record_rescued_portions()
returns trigger
language plpgsql set search_path = ''
as $$
declare
    pickup record;
    remaining integer;
begin
    for pickup in
        select s.menu_id, m.name as menu_name, (s.pickup_start at time zone 'Asia/Jakarta')::date as record_date,
            sum(i.quantity)::integer as quantity
        from public.order_items as i
        join public.surplus_listings as s on s.id = i.listing_id
        left join public.menus as m on m.id = s.menu_id
        where i.order_id = new.id
        group by s.menu_id, m.name, (s.pickup_start at time zone 'Asia/Jakarta')::date
        order by s.menu_id, record_date
    loop
        select p.produced_quantity - p.regular_sold_quantity - p.rescued_quantity into remaining
        from public.production_records as p
        where p.menu_id = pickup.menu_id and p.record_date = pickup.record_date
        for update;
        if not found then
            raise exception 'Catatan produksi % tanggal % belum tersimpan di Supabase. Simpan catatannya di halaman Produksi.',
                coalesce(pickup.menu_name, pickup.menu_id::text), pickup.record_date;
        end if;
        if remaining < pickup.quantity then
            raise exception 'Surplus % tanggal % tersisa % porsi, sedangkan pesanan membutuhkan % porsi.',
                coalesce(pickup.menu_name, pickup.menu_id::text), pickup.record_date, remaining, pickup.quantity;
        end if;
        update public.production_records as p
        set rescued_quantity = p.rescued_quantity + pickup.quantity
        where p.menu_id = pickup.menu_id and p.record_date = pickup.record_date;
    end loop;
    return new;
end;
$$;

revoke all on function public.replate_allocated_portions(uuid, date, uuid),
    public.replate_owner_production(uuid), public.replate_save_production(uuid, date, integer, integer),
    public.replate_record_rescued_portions() from public, anon, authenticated;
grant execute on function public.replate_owner_production(uuid),
    public.replate_save_production(uuid, date, integer, integer) to authenticated;

drop trigger if exists replate_record_rescued_portions on public.orders;
create trigger replate_record_rescued_portions before update of status on public.orders
    for each row when (old.status = 'ReadyForPickup' and new.status = 'Completed')
    execute function public.replate_record_rescued_portions();

notify pgrst, 'reload schema';
commit;
