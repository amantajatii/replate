begin;

-- The desktop still reads the catalog through PostgreSQL. Keep these tables
-- private when the Data API is enabled for order history.
alter table public.menus enable row level security;
alter table public.surplus_listings enable row level security;
revoke all on public.menus, public.surplus_listings from public, anon, authenticated;

commit;
