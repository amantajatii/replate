# Replate

<!-- impeccable:product-schema 1 -->

## Platform

Native desktop application built with Avalonia UI; currently verified on macOS.

## Stack

C# / .NET 10, Avalonia 12.1.4, existing Replate.Domain class library.

## Users

Customers reserve discounted surplus food for pickup from many restaurants.
Restaurant owners manage menus, production, surplus listings, orders, and pickup
verification for their restaurant.

In progress: users sign in with a local account as either Customer or
RestaurantOwner. Restaurant owners register and fill in their restaurant profile
in the same flow. For now the UI lets one owner manage one restaurant, although
the domain supports an owner having many.

## Product Purpose

Redistribute edible surplus while connecting per-menu production, regular sales,
rescued portions, and recommendations for future production.

## Operating Context

The user's practicum requires at least two representative screens. The user now
requires distinct full-page customer and restaurant areas and a mature prototype,
not both roles in one shared sidebar. Keep the previous anti-slop refinement and
remove the requested 'Praktikum UI / Data contoh' sidebar footer.

In progress: the prototype is moving from a single demo restaurant (Dapur Bumi)
to a multi-restaurant marketplace, and the area a user sees is determined by the
role of the signed-in account. A thorough UI refinement pass is underway and must
continue to follow DESIGN.md.

## Capabilities and Constraints

Customer workspace: marketplace, food detail, cart, orders, order detail.
Restaurant workspace: dashboard, menu/editor, listing/editor, production, orders,
confirmation/readiness and pickup verification. Existing domain rules remain the
source for checkout, stock, state transitions, and recommendations.

In progress (not yet complete):
- Multi-restaurant marketplace: surplus listings from many restaurants, not only
  Dapur Bumi.
- Accounts and roles: sign-in as Customer or RestaurantOwner; owner registration
  includes the restaurant profile; one owner manages one restaurant in the UI.
- Authentication: local accounts stored in JSON with passwords hashed using
  PBKDF2. This is prototype-grade authentication, not production authentication.
- Cart scope: a cart may contain items from only one restaurant (single pickup
  location); one checkout produces one order.
- State storage: the JSON state format moves to version 2, with migration from
  version 1.
- UI refinement across both workspaces, following DESIGN.md.

Working implementation choice, disclosed to the user: atomic local JSON persistence.
Storing accounts in PostgreSQL, payments, multi-user synchronization, and any
production-readiness claim are outside this prototype.

## Brand Commitments

Replate name, Indonesian task-oriented copy, restrained green/neutral identity.
The user explicitly requested Impeccable to remove AI slop. No promotional banners,
generic slogans, decorative food drawings, or repeated statistic-card scaffolding.

## Evidence on Hand

README.md, pembeda-replate.md, ERD and domain entities/services. Initial restaurant,
menu, production and order data are synthetic examples. Multi-restaurant support,
local accounts and roles, PBKDF2 password hashing, and the version 2 state format
with version 1 migration are planned and in progress; they are not yet verified.
No commercial/deployment or production-readiness claims are established.
