# Replate

<!-- impeccable:product-schema 1 -->

## Platform

Native desktop application built with Avalonia UI; currently verified on macOS.

## Stack

C# / .NET 10, Avalonia 12.1.4, existing Replate.Domain class library.

## Users

Customers reserve discounted surplus food for pickup. Restaurant owners manage
menus, production, surplus listings, orders, and pickup verification.

## Product Purpose

Redistribute edible surplus while connecting per-menu production, regular sales,
rescued portions, and recommendations for future production.

## Operating Context

The user's practicum requires at least two representative screens. The user now
requires distinct full-page customer and restaurant areas and a mature prototype,
not both roles in one shared sidebar. Keep the previous anti-slop refinement and
remove the requested 'Praktikum UI / Data contoh' sidebar footer.

## Capabilities and Constraints

Customer workspace: marketplace, food detail, cart, orders, order detail.
Restaurant workspace: dashboard, menu/editor, listing/editor, production, orders,
confirmation/readiness and pickup verification. Existing domain rules remain the
source for checkout, stock, state transitions, and recommendations.

Working implementation choice, disclosed to the user: atomic local JSON persistence.
PostgreSQL integration, real authentication, payments, and multi-user synchronization
are outside this prototype. Area selection is demo navigation, not authentication.

## Brand Commitments

Replate name, Indonesian task-oriented copy, restrained green/neutral identity.
The user explicitly requested Impeccable to remove AI slop. No promotional banners,
generic slogans, decorative food drawings, or repeated statistic-card scaffolding.

## Evidence on Hand

README.md, pembeda-replate.md, ERD and domain entities/services. Initial restaurant,
menu, production and order data are synthetic examples. No commercial/deployment
or production-readiness claims are established.
