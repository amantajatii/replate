begin;

create extension if not exists pg_cron with schema pg_catalog;
grant usage on schema cron to postgres;

create schema if not exists replate_internal;
revoke all on schema replate_internal from public, anon, authenticated;

create or replace function replate_internal.expire_overdue_orders()
returns integer
language plpgsql set search_path = ''
as $$
declare
    overdue_ids uuid[];
    cancelled_count integer;
begin
    select array_agg(batch.id) into overdue_ids
    from (
        select o.id
        from public.orders as o
        where o.status in ('Reserved', 'Confirmed', 'ReadyForPickup')
          and exists (
              select 1 from public.order_items as item
              join public.surplus_listings as listing on listing.id = item.listing_id
              where item.order_id = o.id and listing.pickup_end < statement_timestamp()
          )
          and not exists (
              select 1 from public.order_items as item
              left join public.surplus_listings as listing on listing.id = item.listing_id
              where item.order_id = o.id and listing.id is null
          )
        order by o.ordered_at, o.id
        limit 100
        for update of o skip locked
    ) as batch;

    if overdue_ids is null then
        return 0;
    end if;

    -- Use the same stock lock order as checkout and manual cancellation.
    perform listing.id
    from public.surplus_listings as listing
    where listing.id in (
        select item.listing_id from public.order_items as item
        where item.order_id = any(overdue_ids) and item.stock_reserved
    )
    order by listing.id
    for update;

    update public.surplus_listings as listing
    set quantity = listing.quantity + reserved.quantity
    from (
        select item.listing_id, sum(item.quantity)::integer as quantity
        from public.order_items as item
        where item.order_id = any(overdue_ids) and item.stock_reserved
        group by item.listing_id
    ) as reserved
    where listing.id = reserved.listing_id;

    update public.orders as o
    set status = 'Cancelled'
    where o.id = any(overdue_ids);
    get diagnostics cancelled_count = row_count;
    return cancelled_count;
end;
$$;

revoke all on function replate_internal.expire_overdue_orders() from public, anon, authenticated;

select cron.schedule(
    'replate-expire-overdue-orders',
    '* * * * *',
    'select replate_internal.expire_overdue_orders();'
);

commit;
