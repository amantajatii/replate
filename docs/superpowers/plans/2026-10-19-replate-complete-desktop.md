# Replate desktop areas and complete local flows

Goal: separate customer and restaurant workspaces, replace cart modal with full pages,
and deliver a coherent demonstrable local application in Avalonia 12.1.4 / .NET 10.

Confirmed: two distinct roles; full-page surfaces; retain restrained green/neutral
identity and remove practicum/footer slogans. Existing domain is business authority.
Working assumption: local JSON persistence, not PostgreSQL or real authentication/payment.

## Implementation

- [x] Introduce role entry, customer top navigation and dedicated restaurant sidebar.
- [x] Customer: browse, food details, full cart, reservations and order details.
- [x] Restaurant: dashboard, menu editor, surplus listing editor, daily production,
  order confirmation/readiness and pickup verification using existing domain services.
- [x] Persist DTO snapshots atomically without serializing cyclic domain graphs.
- [x] Validate fields and transitions, restore stock on cancellation, retain cart limits.
- [x] Exercise cross-role order flow, persistence, invalid quantities/codes, empty states.
- [x] Capture every screen at desktop and representative compact sizes in one batch;
  fix discovered issues together and confirm once. Update screenshots and run guide.

## Review focus

Expired listings after restart, cancellation restoring stock only once, completed
pickup increasing rescued production exactly once, invalid local file not being
silently overwritten, and minimum-size forms/tables without clipped actions.
