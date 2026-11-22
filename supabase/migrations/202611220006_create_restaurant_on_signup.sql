begin;

alter table public.restaurants
    add column if not exists phone text,
    add column if not exists description text,
    add column if not exists opening_time time,
    add column if not exists closing_time time;

create or replace function public.replate_create_restaurant_on_signup()
returns trigger
language plpgsql security definer set search_path = ''
as $$
declare
    profile jsonb := new.raw_user_meta_data -> 'replate_restaurant';
    restaurant_name text;
    address text;
    phone text;
    description text;
    opens text;
    closes text;
begin
    -- Signup metadata describes a new restaurant. The owner always comes from auth.users.
    if profile is null then
        return new;
    end if;

    restaurant_name := btrim(coalesce(profile ->> 'name', ''));
    address := btrim(coalesce(profile ->> 'address', ''));
    phone := btrim(coalesce(profile ->> 'phone', ''));
    description := btrim(coalesce(profile ->> 'description', ''));
    opens := coalesce(profile ->> 'opening_time', '');
    closes := coalesce(profile ->> 'closing_time', '');
    if jsonb_typeof(profile) <> 'object'
        or char_length(restaurant_name) not between 1 and 80
        or char_length(address) not between 1 and 500
        or phone !~ '^[0-9 +\-]+$'
        or char_length(regexp_replace(phone, '[^0-9]', '', 'g')) not between 8 and 15
        or char_length(description) > 500
        or opens !~ '^([01][0-9]|2[0-3]):[0-5][0-9]$'
        or closes !~ '^([01][0-9]|2[0-3]):[0-5][0-9]$'
        or opens = closes then
        raise exception 'Profil restoran tidak valid.';
    end if;

    insert into public.restaurants (id, owner_id, name, address, phone, description, opening_time, closing_time)
    values (gen_random_uuid(), new.id, restaurant_name, address, phone, description, opens::time, closes::time);
    return new;
end;
$$;

revoke all on function public.replate_create_restaurant_on_signup() from public, anon, authenticated;
drop trigger if exists replate_create_restaurant_on_signup on auth.users;
create trigger replate_create_restaurant_on_signup
    after insert on auth.users
    for each row execute function public.replate_create_restaurant_on_signup();

commit;
