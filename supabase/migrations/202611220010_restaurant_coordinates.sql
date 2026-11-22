begin;

alter table public.restaurants
    add column if not exists latitude double precision,
    add column if not exists longitude double precision;

do $$
begin
    if not exists (
        select 1 from pg_catalog.pg_constraint
        where conrelid = 'public.restaurants'::regclass and conname = 'restaurants_coordinates_check'
    ) then
        alter table public.restaurants add constraint restaurants_coordinates_check check (
            (latitude is null and longitude is null)
            or (latitude is not null and longitude is not null
                and latitude between -90 and 90 and longitude between -180 and 180)
        );
    end if;
end;
$$;

drop function if exists public.replate_save_restaurant_profile(uuid, text, text, text, text, time, time);

create or replace function public.replate_save_restaurant_profile(p_restaurant_id uuid, p_name text,
    p_address text, p_phone text, p_description text, p_opening_time time, p_closing_time time,
    p_latitude double precision default null, p_longitude double precision default null)
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
    if (p_latitude is null) <> (p_longitude is null) or
        p_latitude not between -90 and 90 or p_longitude not between -180 and 180 then
        raise exception 'Koordinat restoran tidak valid.';
    end if;
    update public.restaurants as r
    set name = btrim(p_name), address = btrim(p_address), phone = btrim(p_phone),
        description = coalesce(p_description, ''), opening_time = p_opening_time, closing_time = p_closing_time,
        latitude = p_latitude, longitude = p_longitude
    where r.id = p_restaurant_id and r.owner_id = (select auth.uid());
    if not found then
        raise exception using errcode = '42501', message = 'Akun ini tidak boleh mengubah profil restoran tersebut.';
    end if;
end;
$$;

create or replace function public.replate_save_restaurant_location(p_restaurant_id uuid,
    p_latitude double precision, p_longitude double precision)
returns void
language plpgsql security definer set search_path = ''
as $$
begin
    if (p_latitude is null) <> (p_longitude is null) or
        p_latitude not between -90 and 90 or p_longitude not between -180 and 180 then
        raise exception 'Koordinat restoran tidak valid.';
    end if;
    update public.restaurants as r
    set latitude = p_latitude, longitude = p_longitude
    where r.id = p_restaurant_id and r.owner_id = (select auth.uid());
    if not found then
        raise exception using errcode = '42501', message = 'Akun ini tidak boleh mengubah lokasi restoran tersebut.';
    end if;
end;
$$;

revoke all on function public.replate_save_restaurant_profile(uuid, text, text, text, text, time, time, double precision, double precision),
    public.replate_save_restaurant_location(uuid, double precision, double precision)
from public, anon, authenticated;
grant execute on function public.replate_save_restaurant_profile(uuid, text, text, text, text, time, time, double precision, double precision),
    public.replate_save_restaurant_location(uuid, double precision, double precision)
to authenticated;

notify pgrst, 'reload schema';
commit;
