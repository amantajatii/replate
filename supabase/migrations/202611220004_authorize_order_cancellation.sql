begin;

create or replace function public.replate_cancel_order(p_order_id uuid)
returns table (listing_id uuid, quantity integer)
language plpgsql security definer set search_path = ''
as $$
declare
    account_id uuid := (select auth.uid());
    current_order public.orders%rowtype;
begin
    select o.* into current_order
    from public.orders as o
    where o.id = p_order_id
    for update;

    if not found or account_id is null then
        raise exception using errcode = '42501', message = 'Akun ini tidak boleh membatalkan pesanan tersebut.';
    end if;

    if current_order.customer_id <> account_id then
        perform 1
        from public.restaurants as r
        where r.id = current_order.restaurant_id and r.owner_id = account_id
        for share;
        if not found then
            raise exception using errcode = '42501', message = 'Akun ini tidak boleh membatalkan pesanan tersebut.';
        end if;
    end if;

    if current_order.status <> 'Cancelled' then
        if current_order.status not in ('Reserved', 'Confirmed', 'ReadyForPickup') then
            raise exception 'Pesanan ini sudah selesai atau tidak dapat dibatalkan.';
        end if;

        -- Use the same listing lock order as checkout.
        perform s.id
        from public.surplus_listings as s
        where s.id in (
            select i.listing_id from public.order_items as i
            where i.order_id = p_order_id and i.stock_reserved
        )
        order by s.id
        for update;

        if exists (
            select 1 from public.order_items as i
            left join public.surplus_listings as s on s.id = i.listing_id
            where i.order_id = p_order_id and i.stock_reserved and s.id is null
        ) then
            raise exception 'Penawaran tidak ditemukan. Pesanan belum dibatalkan.';
        end if;

        update public.surplus_listings as s
        set quantity = s.quantity + reserved.quantity
        from (
            select i.listing_id, sum(i.quantity)::integer as quantity
            from public.order_items as i
            where i.order_id = p_order_id and i.stock_reserved
            group by i.listing_id
        ) as reserved
        where s.id = reserved.listing_id;

        update public.orders as o set status = 'Cancelled' where o.id = p_order_id;
    end if;

    return query
        select s.id, s.quantity
        from public.surplus_listings as s
        where s.id in (select i.listing_id from public.order_items as i where i.order_id = p_order_id);
end;
$$;

revoke all on function public.replate_cancel_order(uuid) from public, anon, authenticated;
grant execute on function public.replate_cancel_order(uuid) to authenticated;

notify pgrst, 'reload schema';
commit;
