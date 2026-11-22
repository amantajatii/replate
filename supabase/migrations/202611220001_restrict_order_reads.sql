begin;

alter table public.restaurants
    add column if not exists owner_id uuid references auth.users(id);

alter table public.restaurants enable row level security;
alter table public.orders enable row level security;
alter table public.order_items enable row level security;

revoke all on public.restaurants, public.orders, public.order_items
    from public, anon, authenticated;
grant select (id, owner_id) on public.restaurants to authenticated;
grant select on public.orders, public.order_items to authenticated;

-- The restrictive rules also apply when another permissive policy exists.
drop policy if exists replate_restaurants_read on public.restaurants;
create policy replate_restaurants_read on public.restaurants
    for select to authenticated using (true);
drop policy if exists replate_restaurants_owner on public.restaurants;
create policy replate_restaurants_owner on public.restaurants
    as restrictive for select to authenticated
    using (owner_id = (select auth.uid()));

drop policy if exists replate_orders_read on public.orders;
create policy replate_orders_read on public.orders
    for select to authenticated using (true);
drop policy if exists replate_orders_participant on public.orders;
create policy replate_orders_participant on public.orders
    as restrictive for select to authenticated
    using (
        customer_id = (select auth.uid())
        or exists (
            select 1 from public.restaurants as restaurant
            where restaurant.id = orders.restaurant_id
              and restaurant.owner_id = (select auth.uid())
        )
    );

drop policy if exists replate_order_items_read on public.order_items;
create policy replate_order_items_read on public.order_items
    for select to authenticated using (true);
drop policy if exists replate_order_items_participant on public.order_items;
create policy replate_order_items_participant on public.order_items
    as restrictive for select to authenticated
    using (
        exists (
            select 1 from public.orders as parent
            where parent.id = order_items.order_id
        )
    );

create index if not exists restaurants_owner_id_idx on public.restaurants (owner_id);
create index if not exists orders_customer_id_idx on public.orders (customer_id);
create index if not exists orders_restaurant_id_idx on public.orders (restaurant_id);
create index if not exists order_items_order_id_idx on public.order_items (order_id);

commit;
