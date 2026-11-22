# Replate

<!-- impeccable:product-schema 1 -->

## Platform

Native desktop application built with Avalonia UI; verified on macOS and tested
manually on Windows.

## Stack

C# / .NET 10, Avalonia 12.1.4, existing Replate.Domain class library.

## Users

Customers reserve discounted surplus food for pickup from many restaurants.
Restaurant owners manage menus, production, surplus listings, orders, and pickup
verification for their restaurant.

Users sign in with a local account as either Customer or RestaurantOwner.
Restaurant owners register and fill in their restaurant profile in the same flow.
For now the UI lets one owner manage one restaurant, although the domain supports
an owner having many.

## Product Purpose

Redistribute edible surplus while connecting per-menu production, regular sales,
rescued portions, and recommendations for future production.

## Operating Context

The user's practicum requires at least two representative screens. The user now
requires distinct full-page customer and restaurant areas and a mature prototype,
not both roles in one shared sidebar. Keep the previous anti-slop refinement and
remove the requested 'Praktikum UI / Data contoh' sidebar footer.

The prototype is a multi-restaurant marketplace rather than a single demo
restaurant (Dapur Bumi), and the area a user sees is determined by the role of the
signed-in account.

In progress: a thorough UI refinement pass is underway and must continue to follow
DESIGN.md.

## Capabilities and Constraints

Account flow: sign in (Masuk), register (Daftar), and sign out (Keluar).
Customer workspace: marketplace, food detail, cart, orders, order detail.
Restaurant workspace: dashboard, menu/editor, listing/editor, production, orders,
confirmation/readiness and pickup verification. Existing domain rules remain the
source for checkout, stock, state transitions, and recommendations.

- Multi-restaurant marketplace: surplus listings from many restaurants, not only
  Dapur Bumi.
- Accounts and roles: local Customer and RestaurantOwner accounts; owner
  registration includes the restaurant profile; one owner manages one restaurant
  in the UI.
- Authentication: local accounts stored in JSON with passwords hashed using
  PBKDF2. This is prototype-grade authentication, not production authentication.
- Access guards: each workspace is guarded by role, and restaurant data can only
  be changed by the owner of that restaurant.
- Cart scope: each customer has one cart, which may hold menus from several
  restaurants. Adding the same offer again merges it into one line; the quantity
  never exceeds the remaining stock and only available offers can be added. The
  cart groups its items by restaurant in the order the first item of each
  restaurant was added, with a subtotal per group.
- Checkout: each restaurant in the cart is ordered separately from Keranjang
  (one order per restaurant), after a confirmation pop up. Ordering one
  restaurant creates exactly one order with its own id, pickup code, total, and
  pickup at that restaurant; the items of other restaurants stay in the cart.
  Checkout is atomic: only that restaurant's items are validated (available,
  enough stock, active menu; an unavailable item of another restaurant does not
  block it), the order is built without touching stock, and it is saved to
  PostgreSQL in one transaction. Only after that save succeeds is that
  restaurant's stock reduced, the order added to its restaurant, its items
  removed from the cart, and the JSON state written once. If the save fails,
  stock and cart stay unchanged and no order is kept. Each restaurant owner sees
  only the orders of their own restaurant.
- State storage: the JSON state format is version 2, with migration from
  version 1.
- Empty states: a newly registered restaurant starts with no menus, listings, or
  orders and shows empty states instead of sample data.

In progress: UI refinement across both workspaces, following DESIGN.md.

Working implementation choice, disclosed to the user: atomic local JSON persistence.
Storing accounts in PostgreSQL, payments, multi-user synchronization, and any
production-readiness claim are outside this prototype.

## Brand Commitments

Replate name, Indonesian task-oriented copy, restrained green/neutral identity.
The user explicitly requested Impeccable to remove AI slop. No promotional banners,
generic slogans, hand-drawn decorative food images, or repeated statistic-card
scaffolding.
Real food photography is allowed on the sign-in (Masuk) and registration (Daftar)
screens, with no text placed over the photo. No slogans appear anywhere in the
product.

## Evidence on Hand

README.md, pembeda-replate.md, ERD and domain entities/services. Initial restaurant,
menu, production and order data are synthetic examples. Multi-restaurant support,
local accounts and roles with sign-in, registration and sign-out, PBKDF2 password
hashing, role and ownership guards, per-customer single-restaurant carts, the
version 2 state format with version 1 migration, and empty states for new
restaurants are verified through Replate.Domain.Checks, `--verify-state`,
`--verify-ui`, and manual testing on Windows.
No commercial/deployment or production-readiness claims are established.
