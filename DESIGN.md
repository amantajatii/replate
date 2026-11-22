---
name: Replate Desktop
description: Restrained native workspace for surplus food reservation and restaurant operations.
colors:
  primary: "#1F6B4F"
  primary-hover: "#2F7D5F"
  primary-500: "#59AC87"
  accent: "#F4A261"
  ink: "#202522"
  muted: "#69736D"
  canvas: "#FFFFFF"
  workspace-fill: "#FFFFFF"
  surface: "#FFFFFF"
  sidebar: "#EEF4F1"
  selection: "#DDEDE5"
  divider: "#D7E4DF"
  scrim: "rgba(32, 37, 34, 0.40)"
  danger: "#B3261E"
  danger-tint: "#FBE4E1"
  danger-pressed: "#F6D3CE"
typography:
  headline:
    fontFamily: "Helvetica Neue, Segoe UI, sans-serif"
    fontSize: "28px"
    fontWeight: 600
  auth-title:
    fontFamily: "Helvetica Neue, Segoe UI, sans-serif"
    fontSize: "28px"
    fontWeight: 700
  section:
    fontFamily: "Helvetica Neue, Segoe UI, sans-serif"
    fontSize: "20px"
    fontWeight: 600
  row-title:
    fontFamily: "Helvetica Neue, Segoe UI, sans-serif"
    fontSize: "18px"
    fontWeight: 600
  body:
    fontFamily: "Helvetica Neue, Segoe UI, sans-serif"
    fontSize: "14px"
    fontWeight: 400
  label:
    fontFamily: "Helvetica Neue, Segoe UI, sans-serif"
    fontSize: "13px"
    fontWeight: 600
  supporting:
    fontFamily: "Helvetica Neue, Segoe UI, sans-serif"
    fontSize: "12px"
    fontWeight: 400
  pickup-code:
    fontFamily: "Cascadia Mono, Consolas, Menlo, monospace"
    fontSize: "36px"
    fontWeight: 700
  pickup-code-compact:
    fontFamily: "Cascadia Mono, Consolas, Menlo, monospace"
    fontSize: "18px"
    fontWeight: 700
rounded:
  control: "6px"
  field: "12px"
  card: "16px"
  full: "9999px"
elevation:
  card:
    offsetX: "0px"
    offsetY: "8px"
    blur: "24px"
    spread: "0px"
    color: "rgba(32, 37, 34, 0.12)"
    avaloniaBoxShadow: "0 8 24 0 #1F202522"
  card-primary:
    offsetX: "0px"
    offsetY: "6px"
    blur: "18px"
    spread: "0px"
    color: "rgba(31, 107, 79, 0.30)"
    avaloniaBoxShadow: "0 6 18 0 #4D1F6B4F"
  card-primary-hover:
    offsetX: "0px"
    offsetY: "10px"
    blur: "24px"
    spread: "0px"
    color: "rgba(31, 107, 79, 0.35)"
    avaloniaBoxShadow: "0 10 24 0 #591F6B4F"
  button-primary-hover:
    offsetX: "0px"
    offsetY: "4px"
    blur: "12px"
    spread: "0px"
    color: "rgba(31, 107, 79, 0.30)"
    avaloniaBoxShadow: "0 4 12 0 #4D1F6B4F"
  button-danger-hover:
    offsetX: "0px"
    offsetY: "4px"
    blur: "12px"
    spread: "0px"
    color: "rgba(179, 38, 30, 0.30)"
    avaloniaBoxShadow: "0 4 12 0 #4DB3261E"
motion:
  collection-card-hover:
    scale: 1.03
    origin: "center"
    duration: "120ms"
    easing: "ease-out (Avalonia CubicEaseOut)"
    properties: "background, foreground, render transform, shadow"
  primary-button-hover:
    scale: "1.03 (compact primary buttons and the compact danger button only; full-width primary buttons do not scale)"
    origin: "center"
    duration: "120ms"
    easing: "ease-out (Avalonia CubicEaseOut)"
    properties: "background, foreground, render transform, shadow"
spacing:
  tight: "6px"
  field: "8px"
  default: "12px"
  related: "16px"
  form: "20px"
  section: "24px"
  page: "32px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    rounded: "{rounded.control}"
    padding: "11px 16px"
  button-primary-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    shadow: "{elevation.button-primary-hover}"
    motion: "{motion.primary-button-hover}"
  button-primary-pressed:
    backgroundColor: "{colors.divider}"
    textColor: "{colors.primary}"
    shadow: "{elevation.button-primary-hover}"
  button-danger:
    backgroundColor: "{colors.danger}"
    textColor: "{colors.surface}"
    rounded: "{rounded.control}"
    padding: "11px 16px"
  button-danger-hover:
    backgroundColor: "{colors.danger-tint}"
    textColor: "{colors.danger}"
    shadow: "{elevation.button-danger-hover}"
    motion: "{motion.primary-button-hover}"
  button-danger-pressed:
    backgroundColor: "{colors.danger-pressed}"
    textColor: "{colors.danger}"
    shadow: "{elevation.button-danger-hover}"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    padding: "11px 16px"
  card-action-tonal-hover:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
  card-action-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
  card-action-danger-hover:
    backgroundColor: "{colors.danger-tint}"
    textColor: "{colors.danger}"
  navigation-selected:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    rounded: "{rounded.control}"
    padding: "11px 16px"
  filter-selected:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    rounded: "{rounded.control}"
    padding: "11px 16px"
  search-input:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    placeholderColor: "{colors.muted}"
    iconColor: "{colors.muted}"
    rounded: "{rounded.full}"
    height: "44px"
    maxWidth: "720px"
    minWidth: "320px"
  search-input-focus:
    ring: "2px {colors.surface}, offset 2px"
  top-bar:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    height: "72px"
    padding: "0px 32px"
  cart-icon:
    backgroundColor: "transparent"
    textColor: "{colors.surface}"
    size: "44px"
  cart-icon-hover:
    backgroundColor: "transparent"
    textColor: "{colors.selection}"
    size: "44px"
  cart-icon-active:
    backgroundColor: "transparent"
    textColor: "{colors.surface}"
    indicator: "20px by 2px {colors.surface} line under the icon"
    size: "44px"
  cart-badge:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.primary}"
    rounded: "{rounded.full}"
    height: "18px"
  profile-button:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.primary}"
    rounded: "{rounded.full}"
    size: "44px"
  profile-button-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    rounded: "{rounded.full}"
    size: "44px"
  section-title:
    typography: "{typography.section}"
    textColor: "{colors.ink}"
  text-link:
    backgroundColor: "transparent"
    textColor: "{colors.primary}"
    typography: "14px semibold"
    minHeight: "32px"
    padding: "6px 8px"
  text-link-hover:
    backgroundColor: "transparent"
    textColor: "{colors.primary-hover}"
  text-link-pressed:
    backgroundColor: "transparent"
    textColor: "{colors.primary}"
  focus-ring:
    width: "2px"
    offset: "2px"
    color: "{colors.primary}"
    colorOnPrimary: "{colors.surface}"
  focus-menu-item:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
  marketplace-banner:
    rounded: "{rounded.card}"
    width: "content width"
    aspectRatio: "bitmap width / bitmap height"
    padding: "40px"
  filter-card:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    iconColor: "{colors.surface}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card-primary}"
    height: "96px"
  filter-card-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    iconColor: "{colors.primary}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card-primary-hover}"
    motion: "{motion.collection-card-hover}"
  filter-card-pressed:
    backgroundColor: "{colors.divider}"
    textColor: "{colors.primary}"
    iconColor: "{colors.primary}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card-primary-hover}"
    motion: "{motion.collection-card-hover}"
  listing-card:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
    padding: "20px"
  profile-menu:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
    padding: "8px"
    width: "260px"
  card:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
  auth-card:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
    padding: "32px"
    width: "440px"
    minWidth: "360px"
  role-button:
    backgroundColor: "{colors.sidebar}"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    padding: "11px 16px"
  role-button-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    rounded: "{rounded.control}"
    padding: "11px 16px"
  role-button-selected:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    rounded: "{rounded.control}"
    padding: "11px 16px"
  role-button-selected-hover:
    backgroundColor: "{colors.ink}"
    textColor: "{colors.surface}"
    rounded: "{rounded.control}"
    padding: "11px 16px"
  password-toggle:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    size: "20px"
  password-toggle-hover:
    backgroundColor: "transparent"
    textColor: "{colors.primary-500}"
    rounded: "{rounded.control}"
    size: "20px"
  food-dialog:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
    width: "760px"
    margin: "32px"
  food-dialog-bar:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.ink}"
    padding: "16px 24px"
  dialog-scrim:
    backgroundColor: "{colors.scrim}"
  stepper-button:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    rounded: "{rounded.full}"
    size: "36px"
  stepper-button-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    rounded: "{rounded.full}"
    size: "36px"
  stepper-button-disabled:
    backgroundColor: "{colors.divider}"
    textColor: "{colors.muted}"
    rounded: "{rounded.full}"
    size: "36px"
  cart-item-card:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
    padding: "20px"
  checkout-dialog:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
    padding: "24px"
    width: "520px"
    margin: "32px"
  cart-remove:
    backgroundColor: "transparent"
    textColor: "{colors.muted}"
    rounded: "{rounded.full}"
    size: "36px"
  cart-remove-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    rounded: "{rounded.full}"
    size: "36px"
  time-field:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.full}"
    height: "42px"
  order-filter:
    backgroundColor: "transparent"
    textColor: "{colors.muted}"
    rounded: "{rounded.full}"
    height: "36px"
    padding: "0px 16px"
  order-filter-hover:
    backgroundColor: "{colors.sidebar}"
    textColor: "{colors.ink}"
    rounded: "{rounded.full}"
    height: "36px"
  order-filter-selected:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    typography: "14px semibold"
    rounded: "{rounded.full}"
    height: "36px"
  status-filter:
    backgroundColor: "{colors.surface}"
    borderColor: "{colors.divider}"
    textColor: "{colors.ink}"
    rounded: "{rounded.full}"
    height: "36px"
    padding: "0px 16px"
  status-filter-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    rounded: "{rounded.full}"
    height: "36px"
  status-filter-selected:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    typography: "14px semibold"
    rounded: "{rounded.full}"
    height: "36px"
  order-card:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
    padding: "20px"
  order-detail-card:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
    padding: "24px"
  field:
    backgroundColor: "{colors.surface}"
    borderColor: "{colors.divider}"
    textColor: "{colors.ink}"
    placeholderColor: "{colors.muted}"
    iconColor: "{colors.muted}"
    rounded: "{rounded.field}"
    height: "48px"
    padding: "0px 16px"
  field-hover:
    borderColor: "{colors.primary-500}"
  field-focus:
    borderColor: "{colors.primary}"
    ring: "{components.focus-ring} (keyboard focus only)"
  field-invalid:
    borderColor: "{colors.danger}"
  select-list:
    backgroundColor: "{colors.surface}"
    borderColor: "{colors.divider}"
    rounded: "{rounded.field}"
    padding: "4px"
  select-option-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
  select-option-selected:
    typography: "semibold"
  calendar-day-selected:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    rounded: "{rounded.full}"
    size: "36px"
  calendar-day-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    typography: "medium"
    rounded: "{rounded.full}"
  calendar-month-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    typography: "medium"
    rounded: "8px"
  restaurant-form-card:
    backgroundColor: "{colors.surface}"
    borderColor: "{colors.divider}"
    rounded: "{rounded.card}"
    padding: "24px"
---

# Design System: Replate Desktop

## Overview

**Creative North Star: "Established Replate workspace"**

Replate uses a restrained green and neutral workspace for reserving surplus food and managing restaurant operations. Native sans typography, direct Indonesian labels, and aligned lists keep information and actions easy to scan.

This records the implemented Avalonia desktop system, currently verified on macOS. It extends the established identity; role-specific navigation and full-page forms belong to the desktop surface contract rather than a new brand.

**Key Characteristics:**
- Quiet green actions and selection
- Native controls with visible field labels
- Flat application surfaces, dividers, and structured rows
- One soft shadow, reserved for cards, plus a primary-tinted pair for the green collection cards

## Colors

Forest green is a functional accent within a pale, warm-neutral workspace. Frontmatter values are normative; the table maps them to the team palette and the names below explain their use.

| Palette name | Token | Value | Role |
| --- | --- | --- | --- |
| Primary 700 | `primary` | `#1F6B4F` | Primary buttons, the customer top bar, the dark Jelajah collection cards, link text, focus rings |
| Primary 500 | `primary-500` | `#59AC87` | Non-text only: the password-toggle icon on hover and the shared field border on hover |
| Primary 100 | `selection` | `#DDEDE5` | Primary button hover fill (with `primary` text), selected navigation, hover fills |
| Accent | `accent` | `#F4A261` | The 1 px border of the Masuk database warning only |
| Background | `canvas` | `#FFFFFF` | Page canvas. Deliberately deviates from the team palette. |
| Workspace | `workspace-fill` | `#FFFFFF` | Restaurant workspace area, same as `canvas` |
| Surface | `surface` | `#FFFFFF` | Cards, panels, dialogs, input fields |
| Text Primary | `ink` | `#202522` | Default readable text |
| Text Secondary | `muted` | `#69736D` | Supporting text, placeholders, inactive navigation |

Derived from Primary 700: `primary-hover` `#2F7D5F` (Primary 700 mixed 28% toward Primary 500), `sidebar` `#EEF4F1` (Primary 700 at about 8% over white), `divider` `#D7E4DF` (Primary 700 at about 18% over white), and the `elevation.card-primary`, `elevation.card-primary-hover`, and `elevation.button-primary-hover` shadows. `scrim` and `elevation.card` derive from Text Primary.

Contrast: white on `primary` 6.4:1; `primary` on `selection` 5.3:1; `primary` on `divider` 4.9:1; `primary-hover` 5.0:1 on `surface` and `canvas`; `muted` 4.9:1 on `surface` and `canvas`; `ink` 15.6:1 on `surface` and `canvas`.

### Primary
- **Primary 700** (`primary`): primary actions, selected routes and filters, rescue prices, and meaningful operational emphasis.
- **Primary hover** (`primary-hover`): the text and icon color of the shared text link on hover only. About 5.0:1 on `canvas` and `surface`. Never a fill. Primary 500 is never used for text.
- **Primary 500** (`primary-500`): the hover color of the password-toggle icon and of the shared field border (see Replate fields). About 2.7:1 on white, so it never carries text; the icon is `ink` at rest and the field border is `divider` at rest.
- **Accent** (`accent`): the border of the Masuk database warning only. No other element uses it.

### Neutral
- **Text Primary** (`ink`): default readable content and secondary button text; also the hover fill of the selected role-button, where white text reaches about 15.6:1.
- **Text Secondary** (`muted`): metadata, subtitles, supporting explanations, and inactive navigation.
- **Background** (`canvas`): the main window background.
- **Workspace** (`workspace-fill`): the restaurant screens' work area background, the same white as `canvas`.
- **Surface** (`surface`): cards, panels, dialogs, secondary buttons, the search input, the profile menu, and, on the green top bar, the white logo, icons, cart badge, profile circle, and focus ring.
- **Pale Sage Sidebar** (`sidebar`): restaurant navigation rail.
- **Primary 100** (`selection`): selected navigation, profile menu items on hover and keyboard focus, the profile circle on hover, the cart icon on hover (about 5.3:1 on `primary`), and the Jelajah collection cards and every primary button on hover, where `primary` icon and label reach about 5.3:1.
- **Sage Divider** (`divider`): row rules and shell separators; also the pressed fill of the Jelajah collection cards and primary buttons, one step under the `selection` hover (`primary` text about 4.9:1).
- **Ink Scrim** (`scrim`): `ink` at 40% alpha (Avalonia `#66202522`), the flat dimmed layer behind the food dialog and the checkout confirmation only. No blur.

### Danger
- **Danger Red** (`danger`): the fill of the danger button (white text about 6.5:1) and its text and icon color on hover and press. Used only for actions that cancel or destroy something.
- **Danger Tint** (`danger-tint`): the danger button hover fill (`danger` text about 5.4:1).
- **Danger Pressed** (`danger-pressed`): the danger button pressed fill, one step under `danger-tint` (`danger` text about 4.7:1).

The customer top bar and the Jelajah collection cards are the places `primary` is a surface rather than an accent: white text and icons on it reach about 6.4:1.

**The Functional Green Rule.** Use the accent to communicate action, selection, or useful emphasis.

## Typography

**Body and UI Font:** Helvetica Neue, Segoe UI, sans-serif, inherited from the main window. Avalonia uses normal and semibold weights, plus bold for the auth title, the Jelajah banner greeting, and the "Jelajahi Menu" title; line-height and tracking are not customized.

### Hierarchy
- **Headline:** page headings; semibold, using the frontmatter headline role.
- **Auth title:** the card titles Masuk and Daftar akun; bold (700) at the headline size, using the `auth-title` role. No subtitle sits under either title.
- **Section:** grouped content headings and status summaries.
- **Row title:** food/menu names and prominent row amounts.
- **Body:** descriptions, field labels, status, and supporting copy; visible field labels also use semibold.
- **Label:** buttons and compact labels; secondary metadata also uses normal at this size.
- **Supporting:** dates, caveats, and compact table headers.

The build also uses larger food-detail title, price, and pickup-code text, the "Jelajahi Menu" title (see Section Title), and the 16 collection intro (see Collection Page). These are local information treatments, not a decorative system-display font to reproduce across new surfaces.

## Layout

The native window starts at (1360 × 940) and has a minimum of (1040 × 720). These are desktop sizes, not web breakpoints. Content scrolls vertically; horizontal scrolling is disabled. No mobile or tablet behavior is established.

Shared pages use an inset of (32) and vertical section gaps of (24). The rhythm values live once in `Ui.Spacing` (`Related` 16, `Section` 24, `Page` 32). Dashboard sections use (28). Related form fields use (20), labels sit (8) above controls, and compact stacks use (6–12). Measurements describe Avalonia device-independent units; the portable tokens express their equivalent px notation.

Customer navigation is a fixed top bar (72 high) above the page: logo, search, cart, and profile; see Top Bar under Components. Restaurant navigation occupies a dedicated rail (208 wide). Role-specific route names and exact shell topology are defined in `docs/superpowers/specs/2026-10-19-desktop-surface-contract.md`.

Jelajah centers its content at most (1200) wide (inset 32 on narrower windows) and scrolls vertically under the fixed top bar. Its vertical rhythm is one section gap (24) from the top bar to the banner, from the banner to the collection cards, and from the collection cards to the "Jelajahi Menu" title; (16) from the title to the first listing card, the same as between cards; and (32) below the page. Collection and search result pages use the same column and `Ui.Spacing` constants: (24) from the top bar to the back link, (16) from the back link to the title, (16) from the title to the first card, and (32) below the page. Full-page menu/listing forms are left-aligned (640 wide), food details use (680), the production editor is one form card at most (860) wide (see Catatan produksi), and the restaurant profile is one form card at most (720) wide (see Profil restoran). Data grids combine flexible description columns with consistent fixed action, time, and price columns. Dashboard numeric columns align right.

### Auth surface

The sign-in (Masuk) and registration (Daftar) screens fill the window with two columns: a canvas column on the left and a primary panel on the right. The panel takes about 28% of the width; its left edge follows the plate.

- **Left column:** canvas background. The green logo (see Logo) sits at the top left (inset 32 from the top, 44 high, at the left margin below) on a band filled with `canvas`, outside the scroll area, and never moves the card; the scroll area starts below that band and clips there, so scrolled content never shows under or over the logo. Below it, a vertical-only scroll area holds a single auth-card. The warning shares the card's width and left edge. On a database startup error, Masuk shows one compact warning above the card; nothing sits below the card. No other containers, banners, or helper panels sit beside the card.
- **Right column:** a full-height panel filled with the primary color. The panel itself holds nothing.
- **Plate:** `src/Replate.Desktop/Assets/auth-hero.png` (a transparent round cut-out of a plate) sits in an overlay layer above both columns that does not scroll. It is centered vertically, and the panel's left edge passes exactly through the plate's center: half of the plate lies over the canvas and half over the green panel.
- **Card centering:** the card is centered horizontally on the canvas between the window's left edge and the plate's left edge (at least 32 from either side), and vertically on the window height, not on the space below the logo. When the card (with the Masuk warning above it) does not fit with at least (24) between the logo band and its top, it sits (24) below the band and the form scrolls; it never covers the logo.
- **Placement:** the diameter is 0.56 × window height, at least (360) and never taller than the window. The panel edge (the plate center) sits at 0.72 × window width. The left margin is about 0.12 × window width (never under 48); the logo starts there, and it sizes the card, which is 440 wide at most. The card and the plate keep a gap of 0.08 × window width; on a wide window the gap grows past that, since the card stays at 440. When the window is too small, the card narrows to (360) first, then the gap shrinks step by step to no less than (46: the scrollbar gap 16, the scrollbar 6, and 24 clear of the plate), then the left margin shrinks (to no less than 48), then the panel edge moves right, then the plate shrinks (to no less than 320). The 46 floor is reached only on narrow, tall windows (for example 1040 × 1080); the four reference sizes below keep their gaps.
- **Card and plate never touch:** the plate box and the card box never intersect and keep a gap of at least (24).
- **Scroll area:** the viewport hugs the card: it starts (24) left of the card and ends (16 + 6) right of it, so the scrollbar sits (16) beside the card's right edge and never over it. The 24 on the left is inner padding for the card shadow; the viewport does not clip horizontally, and an outer clip that runs to the plate's left edge keeps the shadow whole on both sides. The scrollbar stays at least (24) from the plate box. The mouse wheel also scrolls the form when the pointer is over the empty canvas beside it.
- **Scrollbar:** the scroll area uses a thin scrollbar (6 wide): one rounded thumb in `muted`, with no track and no arrow buttons. It is hidden at rest and shows only while the form scrolls or while the pointer is over the scroll area, with no transition. Mouse wheel, touchpad, and keyboard (PageUp, PageDown, arrows) scroll as usual, and a field reached with Tab is scrolled into view. Other screens keep the native scrollbar.
- **Photo content:** no text, slogan, logo, or overlay is placed on the plate or the panel. Both are decorative and hidden from assistive technology.

Reference sizes: at (1360 × 940) the plate is 526, the card 440 centered at 138 (gap about 138), and the panel 28% of the width; at (1283 × 780) the plate is 437, the card 440 centered at 133 (gap about 133); at (1920 × 1080) the plate is 605, the card 440 centered at 320 (gap about 320); at the minimum (1040 × 720) the plate is 403, the card 360 centered at 94 (gap about 94 (the gap gives way after the card reaches 360), the panel still 28%, and the owner registration form scrolls vertically. There is no horizontal scrolling at any size.

## Elevation & Depth

Custom application surfaces stay flat. Tonal backgrounds, whitespace, and thin dividers separate shell, rows, and content. The only exception is the card, which may carry one soft shadow: `elevation.card`, or, on the green Jelajah collection cards only, `elevation.card-primary` (and `elevation.card-primary-hover` on hover and press). The non-card shadows are `elevation.button-primary-hover`, on primary buttons while hovered or pressed, and `elevation.button-danger-hover`, the same shadow tinted with `danger`, on the danger button. There are no other shadow tokens and two motion tokens, `motion.collection-card-hover` and `motion.primary-button-hover`.

**The One Shadow Rule.** `elevation.card` is offset (0, 8), blur (24), spread (0), in ink (`#202522`) at about 12% alpha. In Avalonia this is the `BoxShadow` string `0 8 24 0 #1F202522` (Avalonia colors are `#AARRGGBB`, and `1F` is 31 of 255, about 12%). The CSS equivalent is `0px 8px 24px 0px rgba(32, 37, 34, 0.12)`. Do not stack shadows, tint them with the primary color, or apply them to rows or the shell; the only button shadow is the primary button hover shadow below.

**The primary shadow exception.** A white card's ink shadow disappears under a green fill, so the `primary` collection cards use a shadow tinted with `primary` (`#1F6B4F`) instead: `elevation.card-primary` is offset (0, 6), blur (18), spread (0), at about 30% alpha (Avalonia `0 6 18 0 #4D1F6B4F`, CSS `0px 6px 18px 0px rgba(31, 107, 79, 0.30)`); on hover and press it deepens to `elevation.card-primary-hover`, offset (0, 10), blur (24), about 35% alpha (Avalonia `0 10 24 0 #591F6B4F`, CSS `0px 10px 24px 0px rgba(31, 107, 79, 0.35)`). Both stay a single soft shadow with no border, glow, or blur filter. No other surface uses them.

**The primary button shadow.** Primary buttons are flat at rest. On hover and press they carry `elevation.button-primary-hover`, the same `primary` tint at about 30% alpha but smaller: offset (0, 4), blur (12), spread (0) (Avalonia `0 4 12 0 #4D1F6B4F`, CSS `0px 4px 12px 0px rgba(31, 107, 79, 0.30)`). It is the only shadow a primary button may carry, and only rounded-rectangle primary buttons carry it; round buttons, icon buttons, and secondary buttons stay flat.

**The danger button shadow.** The danger button carries `elevation.button-danger-hover` on hover and press: offset (0, 4), blur (12), spread (0), `danger` (`#B3261E`) at about 30% alpha (Avalonia `0 4 12 0 #4DB3261E`, CSS `0px 4px 12px 0px rgba(179, 38, 30, 0.30)`). It is flat at rest and follows the button's corner radius.

The current build applies `elevation.card` to the auth-card, the profile menu, the Jelajah listing cards (also on collection and search result pages), the Keranjang item cards, the Pesanan saya order cards and the order detail cards, the food dialog, and the checkout confirmation, and `elevation.card-primary` to the Jelajah collection cards only. The Jelajah banner has no shadow. The top bar stays flat with no rule and no shadow: its green fill separates it from the canvas. If a separator is ever needed, it is `elevation.card`, never a line.

**The one motion exception.** New animations and transitions are not allowed. The single exception is the hover motion of the Jelajah collection cards (`motion.collection-card-hover`) and, extended from it, of the compact primary buttons and the compact danger button (`motion.primary-button-hover`): scale 1.03 from the center plus the fill, text, icon, and shadow change, all over 120 ms ease-out, and back the same way. Full-width primary buttons share the color and shadow change over the same 120 ms but never scale. Reduced-motion system settings are not read (Avalonia exposes no portable setting), so the transition always runs; it is short and never moves layout. The restaurant dashboard metric cards keep their earlier 180 ms hover transition, which predates this rule; nothing else animates.

Native Avalonia Fluent controls retain their theme behavior; this document does not override native popups or interaction states. The exceptions are the thin scrollbar of the auth scroll area, described under Auth surface, keyboard focus on customer routes, described under Focus, and the Replate fields with their select list and calendar popups, described under Inputs / Fields.

## Shapes

Buttons and text fields use gently rounded control corners (`rounded.control`). Cards use a larger radius (`rounded.card`, 16). Lists and tables remain open rectangular rows separated by a single bottom rule (1). The card is the only sanctioned container silhouette; no other decorative container shapes are established.

## Components

### Buttons
Direct, compact action labels. Primary buttons use green with white text; secondary buttons use white with ink text. Both share the control radius, semibold text (13), and portable padding token. Inactive navigation and filters use transparent backgrounds.

**Primary button states.** One shared style (`Ui.Button(..., primary: true)`, registered for the whole app) sets the fill and the text and icon color explicitly on the button's content presenter in every state, so the Fluent hover and pressed colors (which turned the text black) never show through:
- **Normal:** `primary` fill, white text and icon, flat (no shadow), scale 1.
- **Hover:** `selection` fill, `primary` text and icon (about 5.3:1), `elevation.button-primary-hover`.
- **Pressed:** `divider` fill, one step under hover, `primary` text and icon (about 4.9:1), the same shadow.
- **Disabled:** unchanged; no hover, press, shadow, or scale effect.
- **Compact vs full width:** compact primary buttons (Tambah on listing cards, Tambah ke keranjang in the food dialog, Pesan on each Keranjang restaurant group, Pesan sekarang in the checkout confirmation, page actions such as Jelajahi menu, Konfirmasi pesanan, and the restaurant owner's Tambah menu and Buat penawaran) also scale to 1.03 from the center as a render transform on hover, so the layout never moves, and rise above their neighbours (ZIndex 1). Full-width primary buttons (Masuk and Daftar on the auth surface) get the color and shadow change only, without scale. The Fluent press shrink does not apply to primary buttons.
- **Motion:** fill, text, shadow, and (compact only) scale share the 120 ms ease-out `motion.primary-button-hover`.
- **Round buttons:** the stepper plus and minus (`Ui.Round`, class `round`) are primary but neither compact nor shadowed: hover and press only change the fill and icon color inside the circle (`selection` or `divider` with a `primary` icon), with no shadow and no scale.
- **Focus:** the shared `:focus-visible` ring stays as is; on customer routes it is part of the button, so it scales and rises with it and is never covered.

**Hover follows the shape.** Every hover, press, and rest fill is painted on the button's `PART_ContentPresenter` (or the face Border of a custom template) with the same `CornerRadius` as the button, never on a rectangle behind it, so no tint shows outside a rounded or round shape. The shared button theme sets `ClipToBounds` to false: Fluent clips a button to its rectangular layout box, which cut the hover shadow (and the focus ring) into a square. A round button is a circle by construction: `Ui.Round` sets Width = Height and `CornerRadius` to half the size (never a `9999` placeholder). Shadow and scale belong only to rounded-rectangle buttons (compact primary buttons scale 1.03 and carry `elevation.button-primary-hover`; full-width primary buttons carry the shadow without scale); their shadow follows the same corner radius and every container leaves room for it (the listing card padding 20, the dialog bar padding 16, the 24 list inset on Keranjang, the checkout confirmation padding 24). Round buttons and small icon buttons (stepper plus and minus, the dialog close button, the Keranjang trash button, the top bar magnifier, cart, and profile circle) never carry a hover shadow or scale; they only change color inside their shape.
- **Icon buttons** (`Ui.Round(..., icon: true)`, class `icon`): transparent at rest with an `ink` icon (the trash icon rests `muted`); hover fills the circle with `selection` and turns the icon `primary`; press uses `divider`. Used by the dialog close button and the Keranjang trash button.

**Danger button.** One shared variant (`Ui.DangerButton`, classes `danger` and `compact`, registered with the primary style for the whole app) for actions that cancel an order. It has the compact primary shape (`rounded.control`, padding 11 by 16, semibold 13) and, like the primary button, sets fill and text on `PART_ContentPresenter` in every state:
- **Normal:** `danger` fill, white text and icon (about 6.5:1), flat, scale 1.
- **Hover:** `danger-tint` fill, `danger` text and icon (about 5.4:1), `elevation.button-danger-hover`, scale 1.03 from the center, ZIndex 1, over the 120 ms ease-out `motion.primary-button-hover`.
- **Pressed:** `danger-pressed` fill, one step under hover, `danger` text (about 4.7:1), the same shadow and no extra one.
- **Disabled:** the existing primary disabled treatment; no hover, press, shadow, or scale.
- **Shape and focus:** hover follows the shape (fill and shadow stay inside the rounded rectangle and no container clips them); the shared `:focus-visible` ring is unchanged and stays on top.
- **Used by:** "Batalkan pesanan" on the customer order detail and on the restaurant owner's order detail (the same shared button). The "Ya, batalkan pesanan" confirmation and every Hapus action keep their own style.

**Card action buttons.** The action buttons on the Menu restoran and Penawaran surplus cards (`RestaurantListCard.Action`, class `card-action` with `compact` and one role class) take the shared compact button hover: the fill and text change, the button scales to 1.03 with a soft shadow over one 120 ms ease-out transition, and the 1 px `divider` border keeps its color and width in every state. Fill, text, and shadow are set on `PART_ContentPresenter` with the button's corner radius, so the Fluent hover fill never shows through, and the button does not clip:
- **Ubah** (`card-action-tonal`): rest `selection` fill with `primary` text; hover and pressed `primary` fill with `surface` text (about 6.4:1) and the primary hover shadow.
- **Buka kembali** (`RestaurantListCard.PrimaryAction`, class `primary` with `card-action` and `compact`): the shared primary button at the card action size (same height, padding, and radius as Ubah, same place in the card's button row). Rest `primary` fill with white text (about 6.4:1); hover the pale green inversion, `selection` fill with `primary` text, scale 1.03 and `elevation.button-primary-hover`; pressed `divider` fill, one step darker; disabled uses the shared primary disabled treatment. The 1 px border is transparent so the height matches the other card actions.
- **Closed listings:** a listing closed with Tutup keeps its card and its "Dibatalkan" badge, and has no Ubah or Tutup. It shows Buka kembali only while it has stock left and its pickup window has not ended; otherwise the button is replaced by a 12 `muted` note, "Jendela pengambilan sudah berakhir" or "Stok habis". Buka kembali reverses only the close (JSON state and the PostgreSQL `closed` column), changes no other data, and is refused when the stock no longer fits the unallocated surplus of the production record; the listing then shows on the marketplace again under the usual availability rules.
- **Nonaktifkan, Aktifkan, and Tutup** (`card-action-neutral`): rest `surface` fill with `ink` text; hover `selection` fill with `primary` text (about 5.3:1); pressed `divider` fill with `primary` text; the primary hover shadow.
- **Hapus** (`card-action-danger`): rest `surface` fill with `danger` text; hover and pressed follow the shared danger button ("Batalkan pesanan"): `danger-tint` then `danger-pressed` fill with `danger` text and the danger hover shadow.
- **Layout:** the scale is a render transform, so size and position at rest never move. The shared `:focus-visible` ring is unchanged.

Secondary buttons keep their FluentTheme hover, focus, pressed, and disabled behavior, except on the auth surface, where the secondary button hovers to a selection fill with primary text, and the auth controls below document their own states.

### Inputs / Fields
Visible labels sit above native TextBox, NumericUpDown, and ComboBox controls where a screen does not use the Replate fields; the auth surface uses the time-field instead of TimePicker. Text and numeric inputs have a minimum height (42); multiline text inputs use (88). The top bar search field has its own height token, a pill shape, and a white background. Automation names mirror visible labels. Native controls own focus and disabled treatments.

- **Vertical centering:** every single-line TextBox (including the one inside NumericUpDown and every Kata sandi field) centers its content vertically with balanced padding (10, 5.5, 6, 5.5: top equals bottom). Typed text, the placeholder, password dots, and the password-toggle share the field's vertical center. One shared rule (`Ui.UseInputs` in `Ui.cs`) applies this app-wide; individual fields do not set it. Field heights are unchanged.
- **Multiline fields:** a TextBox that accepts returns (Deskripsi on Daftar) aligns its placeholder and text to the top instead, with padding (10, 10, 6, 5.5): the top padding equals the left. The same shared rule applies it.
- **Placeholder:** every TextBox carries an Indonesian placeholder following "Masukkan ..." (for example "Masukkan email Anda", "Masukkan kata sandi Anda", "Masukkan nama restoran", "Masukkan harga"); the top bar search field reads "Mulai cari menu atau restoran". Placeholder text uses `muted` at full opacity (about 4.9:1 on white), disappears once the field has content, and has no other effect. No em dashes and no exclamation marks.

### Replate fields
One shared field style (`Ui.cs`, registered app-wide by `Ui.UseInputs`) replaces the native Fluent chrome where a screen opts in. It is used on Catatan produksi, Profil restoran, Tambah menu and Ubah menu, Buat penawaran and Ubah penawaran, and for the Dashboard restoran period; other restaurant forms can adopt it without new styles.

- **Frame** (`FieldFrame`, class `field` on its face Border): `surface` fill, 1 px `divider` border, `rounded.field` (12), height 48, the text inset 16 from each side, `ink` text, `muted` placeholder. The inner native control (TextBox, NumericUpDown, or ComboBox, class `bare` or the select theme) draws no border, fill, or focus visual of its own.
- **States:** hover turns the border `primary-500`; focus from the keyboard or a click turns it `primary` (Primary 700); keyboard focus (Tab or arrow keys) also shows the shared focus ring (2 px `primary`, 2 outside the field, radius 16), never after a click. There is no native black focus box. An invalid field keeps a `danger` border in every state, with the inline error under it. No shadow, no transition.
- **Factories:** `Ui.TextField(TextBox)`, `Ui.NumberField(NumericUpDown)`, `Ui.SelectField(ComboBox)`, plus the `DateField` and `NumberStepper` components below, and `FieldGrid` for the two-column layout.
- **Select** (`Ui.SelectField`, `Ui.SelectTheme`): the selected value or the `muted` placeholder on the left and a thin outline chevron on the right (20, Path on a 24 grid, stroke 1.75, round caps, `muted`). The list opens 4 under the field, as wide as the field: a `surface` sheet with a 1 px `divider` border, `rounded.field`, padding 4, at most 280 high and scrolling vertically. Options (`Ui.OptionTheme`) are 14 `ink` rows, padding 12 by 10, radius 8; hover and keyboard focus use the `selection` fill with `primary` text, press uses `divider`, and the selected option is semibold. While the list is open the field keeps the `primary` border. Keyboard behavior is the native ComboBox (arrows, Enter, Esc, Alt+Down).
- **Compact select** (`Ui.CompactSelectField`): the same select field, frame, states, list, and options at height 44, for a single dropdown outside a form. It is as wide as its longest option (text, the 16 insets, the 8 gap, and the chevron) and at least 180, aligned left, never stretched. Used by the Dashboard restoran period ("7 hari terakhir", "28 hari terakhir"), which sits 8 under the banner subtitle as before.
- **Date** (`DateField`): one field showing the date in Indonesian ("22 November 2026", format `d MMMM yyyy`, culture `id-ID`) with a 20 outline calendar icon in `muted` on the right. Click, Enter, Space, F4, or Alt+Down opens a small calendar (the native Avalonia Calendar) 4 under the field, in the same sheet as the select list with padding 8. The whole calendar is `surface` white inside the sheet: the native grey day-grid layer, header band, and navigation hover are replaced, so no grey remains. Day names are Indonesian (Sen, Sel, Rab, Kam, Jum, Sab, Min, Monday first) at 12 in `muted`, and the month header always reads in Indonesian (`MMMM yyyy`, `id-ID`). Days are 36 circles with nothing clipping them: `ink` text, adjacent months `muted` at 60% (paler, with the same hover as other days), today (when not selected) a thin `primary` ring without fill and semibold `primary` text, hover `selection` with medium `primary` text (about 5.3:1) and no border, the circle keeping its size and position, press `divider`, the selected day a `primary` fill with semibold white text that stays the same on hover; keyboard focus (`:focus-visible`) draws a `primary` ring on the circle. Months (in Indonesian: Jan, Feb, Mar, Apr, Mei, Jun, Jul, Agu, Sep, Okt, Nov, Des) and years use the same states on 60 by 48 rounded cells with radius 8: hover `selection` with medium `primary` text and no border, the cell keeping its size and position, the shown month or year a `primary` fill with white text and no outline, and the current month or year (when not shown) a thin `primary` ring without fill. Day, month, and year cells have no border at rest; only the today or current ring and keyboard focus draw one. The month title is transparent with `ink` text and hover `selection`; the previous and next arrows are 32 round icon buttons (`Ui.Round`) with a round `selection` hover and no square behind them. Picking a day closes the calendar and returns focus to the field; arrow keys move the date, Enter or Esc closes. The value and the latest selectable date (31 December of the current year, as the old DatePicker year limit) are unchanged.
- **Stepper** (`NumberStepper`): the shared round `stepper-button` minus and plus (36, from `QuantityStepper.StepButton`) 12 left and right of a centered number field. The native spinner is hidden. Minus is disabled at the field minimum and plus at its maximum; typed numbers and the inline validation are unchanged.
- **Layout** (`FieldGrid`): two equal columns 16 apart with rows 16 apart, so second-row fields are exactly as wide as the fields above them; when the window is 900 wide or less (below the 1040 minimum, so only on a future smaller window) the fields stack in one column. Labels sit 8 above the field (14 semibold `ink`).

### Auth controls
These three components exist only on the auth surface. Their states are documented here because they replace the native Fluent look; they use only the color tokens above, with no blur, glow, gradient, or animation.

- **role-button:** the Daftar sebagai choice (Pelanggan / Pemilik restoran). Two equal-width buttons that together span the card (12 apart), `rounded.control`, no visible border. They keep RadioButton semantics: one group, arrow keys move and select, automation names match the labels. Unselected: sidebar fill with ink text; hover: selection fill with primary text. Selected: primary fill with white text; hover: ink fill with white text. Keyboard focus shows a 2 px ink ring.
- **password-toggle:** a 20 × 20 button hugging its 16 icon, inside the right edge of every Kata sandi field (Masuk, and Kata sandi and Ulangi kata sandi on Daftar), always visible, even when the field is empty. Its eye and crossed-eye icons are drawn as Path geometry (no icon package). Default: an ink icon with no fill or border. Hover, only while the pointer is over the icon: the icon turns `primary-500`; there is still no fill, border, box, or transition, and the icon returns to ink when the pointer leaves. Pressing changes nothing else. Keyboard focus (Tab from the field): 2 px primary ring; Enter or Space toggles it, and Enter on the toggle does not submit the form. Pressing it switches the field between masked and plain text, and its automation name switches between "Tampilkan kata sandi" and "Sembunyikan kata sandi". Submitting the form returns every password field to masked mode; sign-in and an accepted registration also clear them.
- **time-field:** Jam buka and Jam tutup on Daftar (Profil restoran uses `ProfileTimeField`, see Profil restoran). A white pill (`rounded.full`, height 42) with the same border as native text fields, holding a native hour ComboBox (00 to 23), a ":" separator, and a native minute ComboBox (00 to 59). No AM/PM and no shadow. Picking an hour fills the minute with 00. The two fields share one row (16 apart) with visible labels above each; automation names follow the pattern "Jam buka, jam" and "Jam buka, menit". The value is a 24-hour `TimeOnly`. The hour and minute boxes share the pill width equally, so both stay inside the pill at the narrowest card (360).

### Focus
Customer routes (top bar, profile menu, Jelajah, Keranjang, Pesanan saya, details, the food dialog, the checkout confirmation, and the customer cancellation dialog) share one focus style, `Ui.FocusStyles` in `Ui.cs`. It replaces the Fluent focus adorner (the black double box) with an empty adorner and shows focus only on `:focus-visible`, that is after Tab or arrow keys, never after a click.

- **Ring:** buttons and collection cards show a 2 px ring 2 outside their edge, following their corner radius: `primary` on light surfaces, white (`surface`) on the green top bar. The control's own border, fill, and size do not change. Primary buttons keep a visible ring because it sits outside the green fill.
- **Menu items:** profile menu items show the `selection` fill with `primary` text instead of a ring, the same as hover. Opened with the mouse, the menu focuses Pesanan saya without showing it; opened with Enter or Space, Pesanan saya shows the fill.
- **Other controls:** ComboBox uses a `primary` border on `:focus-visible`. Text fields keep their native focus border, except the top bar search, which shows the white ring while focused.
- The auth surface and restaurant routes keep their current focus treatment, except Menu restoran, Penawaran surplus, Catatan produksi, Profil restoran, and Dashboard restoran, which use the shared focus style; on Catatan produksi, Profil restoran, and the Dashboard restoran period the fields show the field ring described under Replate fields. On Dashboard restoran, Ubah profil is a shared solid pill (`Ui.Pill` with `Ui.SolidPills`, class `dashboard-action`): a `surface` fill with a 1 px `divider` border and `ink` text, hover `selection` with `primary` text, pressed `divider`, and the shared `:focus-visible` ring.

### Logo
The brand is shown only as a bitmap logo; there is no text wordmark. `Assets/logo-white.webp` (white, transparent background) sits on `primary` surfaces, and `Assets/logo-green.webp` (green, transparent background) sits on light surfaces. Both are 1500 by 347 and shown with Uniform stretch at a fixed height; the width follows the bitmap ratio and is never set, so the logo is never distorted. Heights: 36 in the customer top bar, 32 in the restaurant rail (so it fits the 208 rail), and 44 on the auth surface. The logo carries the automation name "Replate".

- **Top bar:** white logo, 36 high, inside a plain button that opens Jelajah (name "Replate", tooltip "Jelajah") with the shared white focus ring on `:focus-visible`.
- **Restaurant rail:** green logo on `sidebar`, 32 high, not a button.
- **Auth surface:** green logo on `canvas`, 44 high, aligned with the card's left edge.

### Section Title
"Jelajahi Menu" heads the listing on Jelajah, between the collection cards and the first listing card: 32, bold (`auth-title` weight), `ink`, matching the largest banner greeting, left-aligned with the content edge, one line. It is always shown, also when the list is empty. The add-to-cart notice sits right-aligned on the same row. There is no result counter.

### Navigation and Filters
Selected routes share pale sage fill and forest-green text; inactive items use muted text. The only customer filter is the status filter on Pesanan saya (see Pesanan saya). Jelajah has no filters: the collection cards below are links to their own pages, not a chip shape or a selection. Customer navigation is the top bar below and has no route tabs; restaurant navigation is the vertical rail.

### Top Bar
The customer shell (`CustomerTopBar.cs`) is one persistent bar on every customer page: `primary` fill, height (72), side padding (32). No bottom rule, no shadow, and no border. Text and icons on it are white (about 6.4:1). It holds three groups in an `Auto, *, Auto` grid:

- **Logo:** the white logo (`logo-white.webp`, 36 high, width from the bitmap ratio). It is a button that opens Jelajah, named "Replate"; see Logo.
- **Search:** the `search-input`, a white pill (`rounded.full`, height 44, no border) that fills the middle column with (32) on each side, at most (720) and at least (320), centered between the logo and the icons. A `muted` magnifier icon sits inside on the left; the placeholder reads "Mulai cari menu atau restoran" in `muted`, typed text is `ink`; automation name "Cari menu atau restoran". While focused, a 2 px white ring sits 2 outside the pill; the field never shows a dark border. It replaces the old search field and its label on Jelajah. The magnifier is a small button (40, `rounded.full`, transparent, `muted` icon that turns `primary` on hover, named "Cari", not a Tab stop). Typing alone changes nothing; Enter in the field or a click on the magnifier opens the search result page (see Collection Page). An empty or blank field does nothing. On the result page the field keeps the keyword, and searching again replaces the results. Every other customer page, including Jelajah, opens with an empty field; Keluar clears it too.
- **Cart:** a 44 by 44 icon button, transparent with no fill or box on hover. The icon is white and turns `selection` on hover. While Keranjang is open, a white line (20 by 2) sits under the icon. The `cart-badge` (18 high, `rounded.full`, white fill, `primary` supporting text) sits in the button's top-right corner and shows the number of portions in the cart ("99+" above 99); it is hidden at 0. Click opens Keranjang. Name and tooltip "Keranjang".
- **Profile:** a 44 circle (`rounded.full`) with a white fill and a `primary` user icon; hover changes the fill to `selection`. Name and tooltip "Akun". Click, Enter, or Space opens the profile menu.
- **Profile menu:** a popup anchored below the circle's right edge (8 below it), drawn in the window's overlay layer. A white card (`rounded.card`, `elevation.card`, padding 8, width 260, no border) holding the user's name (14 semibold) and email (12), both `muted` and not clickable, a divider, then "Pesanan saya" and "Keluar" as full-width items (14, ink, `rounded.control`; hover `selection` fill with `primary` text). Pesanan saya opens the orders page; Keluar runs the existing sign-out. Opening it moves focus to Pesanan saya (shown only when opened from the keyboard), the arrow keys move between the two items, and Esc or a click outside closes it (Esc returns focus to the circle). Keyboard focus on an item uses the `selection` fill with `primary` text, never a box.
- **Icons:** magnifier, cart, and user are hand-drawn Path geometry on a 24 grid, stroke 1.75 with round caps and joins, no fill, no icon package. The top bar cart keeps its own line geometry (`CustomerTopBar.CartIcon`); the bold outline cart below is only for the empty states.

**Outline icons.** The two empty-state icons share one style and one helper (`Ui.OutlineIcon`, with `Ui.EmptyIcon`): hand-drawn Path geometry on a 24 grid (`Ui.CartOutline`, `Ui.ReceiptOutline`), scaled to the display size, a bold outline with round caps and round joins, no fill, no icon package. The cart has a curved handle at the top left, a trapezoid basket with rounded corners and a grid of two rows and four slanted columns, a lower frame, and two small round wheels. The receipt is an upright sheet with rounded top corners, a curled paper roll at the top right, three horizontal text lines, and a zigzag bottom edge. On the empty states both icons are 80 square with a stroke of 8% of the width (6.4), in `primary`.
- **Focus:** every control is reached with Tab; buttons show the shared white focus ring on keyboard focus (see Focus).
- **Sizes:** at the minimum (1040 by 720) the search shrinks and stays at or above 320; at (1920 by 1080) it stops at 720. Nothing in the bar overlaps at any size.

### Jelajah Banner
The first block on Jelajah: `rounded.card` corners, no border, no shadow, centered in the content column. `Assets/marketplace-hero.webp` (a wide kitchen illustration, 4344 by 1280, empty top-left, objects on the right, cabinets along the bottom) is shown whole with Uniform stretch: never cropped or distorted. The banner is exactly as wide as the content column, so its left and right edges line up with the filter row and the listing cards at every window size; its height follows the bitmap ratio (width × 1280 / 4344): about 288 at the minimum window and 354 at the 1200 content maximum. The image is decorative and hidden from assistive technology.

- **Greeting:** "Halo, {nama pengguna}." from the signed-in account, bold (`auth-title` weight) in `primary`, one line, trimmed with an ellipsis when too long. Size scales with the banner width (0.0287 × width, 22 to 32): 28 at the minimum window, 32 at the 1200 content maximum.
- **Call to action:** "Yuk, Selamatkan Makanan Lewat Setiap Pesanan!" in `muted`, at most two lines. Size 0.0184 × width, 15 to 20.
- **Text block:** overlaid on the empty top-left of the image, left inset (40), centered vertically in the area above the cabinets (which start at 87.8% of the height). Its right edge stays at or left of 45% of the banner width; the illustration's nearest object starts at 46.2% and the empty area reaches about 55% at the top, so the text never covers the illustration at any size.

### Collection Card
Five equal cards in one row, (24) under the banner and (24) above the section title, (16) apart: Termurah (tag icon), Segera berakhir (clock), Makanan utama (fork and spoon), Roti & kue (bread), Buah & sayur (leaf). Each is a button styled as a card: `primary` fill, `rounded.card`, `elevation.card-primary`, no border, height (96); a 24 line icon in white (`surface`, Path geometry, stroke 1.75, round caps) above a 14 white label (about 6.4:1 on `primary`). The icon stroke follows the card's text color. Hover: the fill changes to `selection` and the icon and label to `primary` (about 5.3:1), the shadow deepens to `elevation.card-primary-hover`, and the card scales to 1.03 from its center as a render transform, so no other card moves or resizes; the hovered card sits above its neighbours (ZIndex 1), and the 16 gap keeps the grown card clear of them. Pressing changes the fill to `divider` (`primary` text about 4.9:1). Leaving returns to `primary`, white, scale 1.0, and the resting shadow. Color, scale, and shadow share the 120 ms ease-out `motion.collection-card-hover`, the only animation in the customer app. There is no selected state. Keyboard focus: the shared 2 px `primary` ring 2 outside the card, drawn on the canvas (about 6.4:1), never white on green; the ring is part of the card, so it scales and rises with it and is never covered. Enter or Space opens it. Each card is a link: it opens that collection's page (see Collection Page), and Jelajah itself always lists every available listing in the default order. There is no Semua card. At (1040) the cards narrow and stay in one row.

### Collection Page
One reused view (`CollectionView.cs`) for the five collections and for search results, under the same top bar and in the same content column as Jelajah (at most 1200, inset 32), with no banner. From top to bottom:

- **Back link:** "Kembali ke Jelajah" (or another label passed to `CollectionView.CreateBackLink`, such as "Kembali ke Pesanan saya") with a 16 left arrow (Path on a 16 grid, stroke 1.75, round caps), 8 from the 14 semibold `primary` text and vertically centered with it, transparent in every state, at least 32 high; the shared text link (`text-link`): hover only changes the arrow and text to `primary-hover`, pressed returns them to `primary`, with no fill, border, shadow, scale, or underline; padding 8 by 6 with the arrow on the content edge; the shared `primary` focus ring. It opens Jelajah.
- **Title:** 32, bold, `ink`, one line, the same size as "Jelajahi Menu". A collection uses its card label exactly; search results read "Hasil untuk {kata}" with the keyword as typed (outer spaces trimmed), cut with an ellipsis when too long. The add-to-cart notice sits right-aligned on the same row.
- **Intro:** on a collection page with listings, one sentence sits (8) under the title, 16 in `muted`, wrapping; the first card follows (16) under it. Search results and empty states have no intro. Termurah: "Temukan berbagai makanan lezat, pas, dan ramah di kantong!"; Segera berakhir: "Nikmati hidangan favorit sebelum terlambat!"; Makanan utama: "Temukan hidangan mengenyangkan untuk menemani harimu!"; Roti & kue: "Maniskan harimu dengan roti dan kue lezat yang menggugah selera!"; Buah & sayur: "Nikmati pilihan hidangan sehat yang lezat, segar, dan penuh kebaikan!".
- **Listings:** the same listing cards as Jelajah, (16) apart, with the same Tambah behavior (the food dialog) and notice. Termurah lists every available listing by rescue price ascending, Segera berakhir by pickup end ascending, and Makanan utama, Roti & kue, and Buah & sayur keep their category in the default order.
- **Search:** the keyword is split on spaces and every word must appear (case-insensitive) in the menu name, description, restaurant name, or category; "vegetarian" also matches Buah & sayur.
- **Categories:** the menu categories are Makanan utama, Roti & kue, and Buah & sayur (`MenuCategories.cs`), shown the same on the cards, collection titles, listing lines, food details, and the owner's menu form. Buah & sayur replaces the old Vegetarian label: a saved state file that still holds "Vegetarian" loads as Buah & sayur in memory without being rewritten on load.
- **Empty states:** a collection shows "Belum ada menu di sini" with "Menu di koleksi ini sedang habis. Coba lagi nanti."; a search shows "Tidak ada menu yang cocok" with "Coba kata kunci lain.". Both end with a secondary "Kembali ke Jelajah" button and sit (16) under the title.

### Listing Card
Each Jelajah listing is one card (`surface`, `rounded.card`, `elevation.card`, padding 20, no border), (16) apart, with photo, description, pickup, price, and action columns: photo box (or "Foto belum tersedia"), menu name, description, "Restoran · Kategori", Pengambilan with time and remaining portions, rescue price, struck-through normal price, "Hemat N%", and Tambah. The first card sits (16) under the "Jelajahi Menu" title; there is no result counter. The sample-data note stays under the list. If nothing is available, Jelajah shows "Belum ada menu tersedia" (16) under the title. Tambah does not add to the cart: it opens the food dialog (see Food Dialog), and so does a click anywhere else on the card (photo, name, text). The card shows a hand cursor. One shared card (`ListingFeed.cs`) serves Jelajah, collection pages, and search results.

### Food Dialog
The pop up detail for one listing (`FoodDialog.cs`), customer only. It is drawn in an overlay layer inside the main window (never a separate OS window), above everything including the top bar, and only one is open at a time.

- **Scrim:** a full-window layer in `scrim` (ink at 40%), flat, no blur. A click on it closes the dialog.
- **Card:** `surface`, `rounded.card`, `elevation.card`, no border, width (760), centered horizontally and vertically with at least (32) to every window edge. The height follows the content; when the window is too short, the upper area scrolls inside the card (vertical only) while the bar stays visible. Resizing keeps it centered; it fits at (1040 × 720) and (1920 × 1080).
- **Content** (padding 24, columns 300 and the rest, 24 apart): left, the menu photo at 300 by 300 (`MenuPhotoView`, radius 8), or the same "Foto belum tersedia" box at that size with 14 text. Right, top to bottom: the menu name (28, bold, `ink`) with the close button on the same row; "Restoran · Kategori" (14, `muted`); the full description (14); "Pengambilan HH:mm–HH:mm" (14) and "N porsi tersisa" (13, `muted`); the rescue price (26, semibold, `primary`) with "/porsi" (14, `muted`); the struck-through normal price (13, `muted`) and "Hemat N%" (13, semibold, `primary`); then "Jumlah porsi" (14 semibold) above the stepper.
- **Stepper:** the shared `QuantityStepper` (`QuantityStepper.cs`, also used on Keranjang): a minus button, the quantity (18 semibold, centered), and a plus button, 12 apart. The buttons are `stepper-button`: 36 circles in `primary` with a white filled minus or plus icon (Path geometry, no icon package), never red; disabled, they use `divider` with a `muted` icon. The quantity is at least 1 and at most the listing stock minus what the cart already holds of that listing. At the limit the plus button is disabled and a 12 `muted` note reads "Maksimal N porsi". When nothing more may be added the note reads "Sudah di keranjang dengan jumlah maksimal" and Tambah ke keranjang is disabled. When the offer is no longer available (expired, sold out, or the menu turned inactive while the dialog is open; re-checked every 15 seconds and on every action) the note reads "Penawaran tidak tersedia" and the stepper and Tambah ke keranjang are disabled.
- **Bar:** the bottom strip of the card in `selection` (padding 16 by 24, bottom corners `rounded.card`): left, "Total" (12, `muted`) above the rescue price times the quantity (20 semibold, Rupiah format); right, the primary button "Tambah ke keranjang". Disabled, it uses `divider` with `muted` text.
- **Adding:** Tambah ke keranjang uses the existing cart logic with the chosen quantity, added to any quantity already in the cart. On success the dialog closes, the cart badge updates, the existing "{menu} ditambahkan ke keranjang." notice shows next to the page title, and the page does not change. When the cart holds another restaurant, the existing one-restaurant message appears inside the dialog in 14 text right above the bar, nothing is added, and the dialog stays open.
- **Closing:** the close button (an icon button: 36 circle, transparent, `ink` X drawn as a 24 Path, stroke 1.75, `selection` circle with a `primary` X on hover, name and tooltip "Tutup") in the top-right of the details, Esc, or a click on the scrim. Closing never changes the cart, and opening or closing never writes state.
- **Focus:** opening moves focus to the close button (with the ring only when the dialog was opened from the keyboard); Tab and Shift+Tab cycle inside the card; closing returns focus to the control that opened it (Tambah or the menu name, rebuilt after an add). All buttons use the shared focus style on `:focus-visible`.
- No animation other than the shared primary button hover (Tambah ke keranjang, and the color change of the stepper buttons, see Buttons); no blur, glow, or gradient. No em dashes in its text.

### Keranjang
The customer cart page (`CartView.cs`), under the same top bar and in the same content column as Jelajah and the collection pages (at most 1200, centered, inset 32, top 24), so its left and right edges line up with Jelajah. From top to bottom, (16) apart:

- **Back link:** the collection page's "Kembali ke Jelajah" link (`CollectionView.CreateBackLink`), opening Jelajah.
- **Header:** "Keranjang" (32, bold, `ink`, the size of "Jelajahi Menu") and, (8) under it while the cart holds items, "Pesanan dari {restoran}" for one restaurant or "{N} restoran" for several (14, `muted`).
- **List:** the restaurant groups use the full content width, so card edges line up with Jelajah. Only the list scrolls (vertical only); it keeps a 24 inset inside its scroll area so card shadows and the hover scale are not cut. The list ends (32) under the last group (`Ui.Spacing.Page`). There is no summary card and no combined order button. At (1040 × 720) nothing scrolls sideways.

**Restaurant group:** the list holds one group per restaurant, in the order the first item of each restaurant was added, (32) apart (`Ui.Spacing.Page`), measured from one group's footer to the next group's heading. A group starts with its heading, then its item cards, then its footer, all (16) apart. The heading is aligned with the card edges, without a card of its own, and holds only the restaurant name (18, bold, `ink`).

**Group footer:** (16) under the last item card, a thin rule as wide as the group (`divider`, 1, no border); (16) under it, one row. On the left, aligned with the card edge: "Total" (12, `muted`) above the group total (28, bold, `primary`, Rupiah format), the sum of the group's item subtotals. On the right, aligned with the card edge and vertically centered with the total block: the compact primary button "Pesan" (shared button style and standard height, named "Pesan dari {restoran}"). The total and the button state update in place with the quantities. When an item of the group is unavailable, only that group's Pesan is disabled (`divider` fill, `muted` text) and a 12 `muted` line, (8) under the footer row and aligned left, reads "Hapus penawaran yang tidak tersedia untuk melanjutkan."; any other invalid group (for example a quantity above the remaining stock) reads "Periksa jumlah pesanan untuk melanjutkan." in the same place. Other groups stay orderable. Pesan opens the checkout confirmation for that restaurant.

**Item card** (`cart-item-card`: `surface`, `rounded.card`, `elevation.card`, padding 20, no border, (16) apart, one per item). Columns: the photo or "Foto belum tersedia" box (72, radius 8); the menu name (18 semibold), "Restoran · Kategori" (13, `muted`), "Pengambilan HH:mm–HH:mm" (13, `muted`), and the unit price "Rp16.000 / porsi" (14); the shared stepper (at least 1, at most the remaining stock; plus disabled at the limit, minus disabled at 1); the item subtotal (18, bold, right-aligned); and the remove button. The remove button (`cart-remove`) is a 36 circle holding a 24 line trash icon (Path, stroke 1.75, round caps), an icon button: transparent at rest with a `muted` icon, a `selection` circle with a `primary` icon on hover (`divider` on press), never a box, shadow, or scale, named and tooltipped "Hapus {menu}", with the shared focus ring. Changing the quantity updates the subtotal, the group total and Pesan, and the cart badge in place; removing an item rebuilds the page. An item whose offer is no longer available (expired, sold out, or the menu turned inactive) keeps its card, reads "Penawaran tidak tersedia. Hapus untuk melanjutkan." (14, `muted`) in place of the unit price, and has its stepper disabled; its remove button stays active.

**Checkout confirmation** (`checkout-dialog`, `CheckoutDialog.cs`): the pop up that orders one restaurant, on the same overlay as the food dialog (`ModalDialog.cs`): the `scrim` layer, a centered card (`surface`, `rounded.card`, `elevation.card`, no border, width 520, at least (32) to every window edge; the upper area scrolls inside the card when the window is short while the buttons stay visible). Content, padding 24 and (16) apart: "Konfirmasi pesanan" (22, bold, `ink`) with the shared close button on the same row; "Pesan dari {restoran}?" (15); one row per item of that restaurant only, 12 apart, with the menu name (14 semibold), "{jumlah} x {harga satuan}" (13, `muted`) under it and the item subtotal (14 semibold) on the right; "Pengambilan HH:mm–HH:mm" (13, `muted`); a divider; "Total" (16 semibold) with the restaurant total (24, bold, `primary`, Rupiah format); and, on the bottom row aligned right, 12 apart, the secondary "Batal" and the compact primary "Pesan sekarang". Esc, a click on the scrim, the X, or Batal close it without an order; opening moves focus to the close button, Tab and Shift+Tab cycle inside the card, and closing returns focus to the Pesan that opened it. Pesan sekarang uses the existing atomic checkout for that restaurant only and is disabled (`divider` fill) while it runs, with no extra animation. On success the pop up closes: if the cart still holds other restaurants, Keranjang stays open with the remaining groups, the badge updates, and the shared notification reads "Pesanan dari {restoran} berhasil dibuat."; if the cart is now empty, the new order opens with "Pesanan berhasil dibuat." and the badge is hidden. On failure the pop up stays open and the existing message "Pesanan belum dapat dibuat. Periksa koneksi PostgreSQL lalu coba lagi." (14, the dialog error color) shows right above the buttons; nothing changes.

**Empty state:** with no items, the page keeps the back link and title (no subtitle, no groups) and centers under them the 80 bold outline cart icon (`Ui.EmptyIcon(Ui.CartOutline)`, stroke 6.4, `primary`, see Outline icons), "Keranjangmu masih kosong" (22 semibold, `ink`), "Mulai jelajahi menu dan selamatkan makanan hari ini." (15, `muted`), and the compact primary button "Jelajahi menu", which opens Jelajah.

No animation other than the shared primary button hover; no blur, glow, or gradient. No em dashes in its text.

### Pesanan saya
The customer orders page (`OrdersView`, customer part in `CustomerOrdersView.cs`), under the same top bar and in the same content column as Keranjang (at most 1200, centered, inset 32, top 24). From top to bottom, (16) apart (`Ui.Spacing.Related`): the shared "Kembali ke Jelajah" text link (`CollectionView.CreateBackLink`), the title "Pesanan saya" (32, bold, `ink`, the size of "Keranjang" and "Jelajahi Menu"), the status filter, and the list. Only the list scrolls (vertical only), with the Keranjang 24 inset so card shadows are not cut; it ends (32) under the last card. The restaurant orders page keeps its own table; its status filter is pills too (see Pesanan restoran).

**Status filter** (`order-filter`): one row of four pill buttons, 8 apart: Semua, Aktif, Selesai, Dibatalkan; pick one, Semua by default. Each pill is 36 high with `CornerRadius` 18 (half its height), padding 0 by 16, 14 text, no border. At rest: transparent with `muted` text; hover: `sidebar` fill with `ink` text; pressed: `divider` fill. Selected (`order-filter-selected`): `selection` fill with semibold `primary` text, also on hover; pressed `divider`. Every fill is painted on the pill itself (hover follows the shape): no box outside the pill, no shadow, no scale, no transition. The shared `:focus-visible` ring follows the pill. The groups map the existing `OrderStatus` values: Aktif is Reserved (Menunggu Konfirmasi), Confirmed (Diproses), and ReadyForPickup (Siap Diambil); Selesai is Completed; Dibatalkan is Cancelled; Semua shows all five. The filter row is hidden while the customer has no orders at all.

**Order card** (`order-card`: `surface`, `rounded.card`, `elevation.card`, padding 20, no border, (16) apart), one per order, newest first. Every restaurant ordered at checkout is its own order, so it is its own card. Content, (16) apart:
- **Top:** the restaurant name (18, bold, `ink`) on the left; the existing status badge (`OrderStatusBadge`, its tint and Indonesian label) on the right. Under them (4), in 13 `muted`: "Dipesan dd MMM yyyy, HH:mm · Pengambilan dd MMM, HH:mm–HH:mm" (the pickup window runs from the earliest item start to the latest item end).
- **Items:** up to three rows, 8 apart: the menu name (14) on the left and "{jumlah} x" (14) right-aligned; with more items a 13 `muted` line reads "+{N} menu lainnya".
- **Pickup code:** only on Aktif orders: "Kode ambil" (12, `muted`) above the code (`pickup-code-compact`: 18, bold, fixed-width font, `ink`). There is no copy button on the card; copying lives on the detail page.
- **Footer:** a thin rule (`divider`, 1); under it, on the left, "Total" (12, `muted`) above the order total (22, bold, `primary`, Rupiah format); on the right, vertically centered, the text link "Lihat detail" (`CollectionView.CreateTextLink`, the shared `text-link` without an arrow: 14 semibold `primary`, hover only changes the text to `primary-hover`, pressed returns it to `primary`, transparent in every state with no fill, border, shadow, scale, or underline, at least 32 high, the shared `primary` focus ring; named "Lihat detail pesanan dari {restoran}"), its text ending on the card content edge, which opens the detail page of that order.

**Empty states:** both are centered under the header with the 80 bold outline receipt icon (`Ui.EmptyIcon(Ui.ReceiptOutline)`, stroke 6.4, `primary`, see Outline icons) and a 22 semibold `ink` title. With no orders at all: "Belum ada pesanan", "Pesananmu akan muncul di sini setelah kamu memesan." (15, `muted`), and the compact primary "Jelajahi menu" (opens Jelajah); the filter row is hidden. When the chosen filter has no orders: "Tidak ada pesanan di sini" and, 24 under the title and centered, the compact primary "Lihat semua pesanan" (the same shared button as "Jelajahi menu" on the empty cart), which selects Semua again.

Checkout is unchanged: ordering the last restaurant in the cart still opens the detail page of the new order with "Pesanan berhasil dibuat.", and the new orders sit at the top of Pesanan saya. At (1040 × 720) and (1920 × 1080) nothing scrolls sideways or is cut off. No animation; no blur, glow, or gradient. No em dashes in its text.

### Pesanan restoran
The restaurant orders page (`OrdersView`, restaurant part in `CustomerViews.cs`) keeps its heading and order table. Under the heading, the label "Status pesanan" (14 semibold, `ink`) and, (8) under it, the status filter (`status-filter`); the table follows (24) under the filter. The filter is one pill per status, in this order: Aktif (default), Semua, Selesai, Dibatalkan; pick one. The pills come from the same shared helper as the Pesanan saya filter (`Ui.Pill` and `Ui.PillStyles` in `Ui.cs`): 36 high, `CornerRadius` 18, padding 0 by 16, 14 text, 8 apart, with no shadow, scale, or transition, and the shared `:focus-visible` ring. Only the palette differs (`Ui.SolidPills`): at rest a `surface` fill with a 1 px `divider` border and `ink` text; hover `selection` fill with `primary` text; pressed `divider` fill; selected (`status-filter-selected`) a `primary` fill with semibold white text, also on hover and press. The pills sit in a wrap panel: when the width is short they move to the next line (8 apart), so nothing scrolls sideways at (1040 × 720) and (1920 × 1080). Each pill carries its label as its automation name and is reached with Tab and activated with Enter or Space. The mapping is unchanged: Aktif is every order that is neither Completed nor Cancelled, Selesai is Completed, Dibatalkan is Cancelled, Semua shows all. Other restaurant screens keep their native ComboBox controls, except the Menu field on Catatan produksi, the hour and minute selects on Profil restoran, Kategori on Tambah menu and Ubah menu, Menu and the hour and minute selects on Buat penawaran and Ubah penawaran, and the Dashboard restoran period (see Replate fields).

### Catatan produksi
The restaurant production page (`ProductionView` in `RestaurantViews.cs`) keeps its heading, subtitle, and the "Ringkasan tanggal terpilih" table. Under the heading (24), one `restaurant-form-card` in the form area (at most 860 wide, left-aligned with the heading): `surface`, 1 px `divider` border, `rounded.card` (16), padding 24, no shadow. Inside, 24 apart:

- **Fields:** the `FieldGrid` with Menu and Tanggal produksi on the first row and Diproduksi (porsi) and Terjual reguler (porsi) on the second, all Replate fields (select, date, and two steppers). Inline errors stay under their field in 14 `danger` text, with the field border in `danger`.
- **Divider:** a thin `divider` rule.
- **Footer:** on the left the summary block: "Terselamatkan" (12, `muted`), the rescued number (28, bold, `primary`) followed by " porsi" (14, `muted`) on one line, and "Bertambah setelah kode pengambilan diverifikasi." (14, `muted`); on the right, aligned with the card's bottom and right content edges, the shared compact primary button "Simpan catatan".

Saving, validation, the default date and menu, and the summary numbers are unchanged. No horizontal scroll and no cut text at (1040 × 720) and (1920 × 1080). No animation other than the shared primary button hover; no blur, glow, or gradient. No em dashes in its text.

### Profil restoran
The restaurant profile page (`RestaurantProfileView` in `RestaurantViews.cs`) keeps the title "Profil restoran" with the subtitle "Atur informasi restoran yang tampil kepada pelanggan." (14, `muted`). Under the heading (24), one `restaurant-form-card` in the form area (at most 720 wide, left-aligned with the heading): `surface`, 1 px `divider` border, `rounded.card` (16), padding 24, no shadow. Inside, 24 apart:

- **Fields:** all Replate fields, 16 apart, each with its 14 semibold label 8 above. Nama restoran and Telepon share a `FieldGrid` row; Alamat and Deskripsi span the card; Jam buka and Jam tutup share a second `FieldGrid` row. Both rows stack to one column at 900 or less. Nama restoran, Alamat, and Telepon are single-line text fields (`Ui.TextField`, no wrap). Deskripsi is a multiline text field 88 high (about 3 lines), wrapping, with its text inset 16 by 12 from the top left.
- **Time** (`ProfileTimeField`): an hour select (00 to 23) and a minute select (00 to 59), each a 104 wide `Ui.SelectField` with the shared list and options, 8 from a ":" (15 semibold `ink`) between them, aligned left. Picking an hour fills the minute with 00. Automation names follow "Jam buka, jam" and "Jam buka, menit". The value is a 24-hour `TimeOnly`.
- **Actions:** on the right, aligned with the card's bottom and right content edges, the shared text link "Kembali" (`CollectionView.CreateTextLink`, no arrow) 16 left of the shared compact primary button "Simpan profil". Kembali opens Dashboard restoran; Simpan profil saves and opens Dashboard restoran with "Profil restoran tersimpan.".

Loading, validation, saving, and navigation are unchanged. No horizontal scroll and no cut text at (1040 × 720) and (1920 × 1080). No animation other than the shared primary button hover; no blur, glow, or gradient. No em dashes in its text.

### Tambah menu and Ubah menu
The menu form (`MenuEditorView` in `RestaurantViews.cs`) serves both Tambah menu and Ubah menu and keeps its title and subtitle. Under the heading (24), in the form area (at most 860 wide, left-aligned), two `restaurant-form-card` cards 16 apart (`surface`, 1 px `divider` border, `rounded.card` (16), padding 24, no shadow): the details card fills the width and the Foto menu column is 284 wide.

- **Details:** all Replate fields, 16 apart, each with its 14 semibold label 8 above. Nama menu is a single-line text field (no wrap); Kategori is the shared select with its list and options; Harga normal (Rp) and Berat per porsi (kg) sit in two equal columns 16 apart, each a `NumberStepper` (round minus and plus, native spinner hidden). Start values, steps (1000 and 0.05), limits, and formats (`0` and `0.000`) are unchanged and numbers can still be typed. Deskripsi is a multiline text field 112 high, wrapping, with its text inset 16 by 12 from the top left. Inline errors stay under their field in 14 `danger` text, with the field border in `danger`.
- **Foto menu:** the title "Foto menu" (18 semibold), the unchanged preview (`selection` fill, radius 8, "Foto belum tersedia" in `muted` when empty) filling the card width, the note "Opsional. JPG, JPEG, atau PNG, maksimal 5 MB." (13, `muted`), then the buttons 8 apart, all 16 apart. Both buttons are 44 high with padding 0 by 16, the control radius, and a 1 px border, and take the card action hover (scale 1.03 with the soft shadow). "Pilih foto" is tonal (`card-action-tonal`): `selection` fill with `primary` text, hover `primary` fill with `surface` text, no icon. "Hapus foto" is outlined (`card-action-neutral`): `surface` fill, `divider` border, `ink` text, hover `selection` fill with `primary` text. Without a photo it is disabled: `sidebar` fill and border with `muted` text at 60%, and no hover, press, shadow, or scale.
- **Actions:** 16 under the Foto menu card, right-aligned with its right edge (and so with the bottom of the details card): the shared text link "Batal" (`CollectionView.CreateTextLink`) 16 left of the shared compact primary button "Simpan menu". Batal opens Menu restoran; Simpan menu validates and saves as before.

Validation, saving, photo picking, and navigation are unchanged. No horizontal scroll and no cut text at (1040 × 720) and (1920 × 1080). No animation other than the shared button hovers; no blur, glow, or gradient. No em dashes in its text.

### Buat penawaran and Ubah penawaran
The listing form (`ListingEditorView` in `RestaurantViews.cs`) serves both Buat penawaran and Ubah penawaran and keeps its title and subtitle. Under the heading (24), one `restaurant-form-card` in the form area (at most 720 wide, left-aligned with the heading): `surface`, 1 px `divider` border, `rounded.card` (16), padding 24, no shadow. Inside, 24 apart:

- **Fields:** all Replate fields, 16 apart, each with its 14 semibold label 8 above. Menu is the shared select spanning the card (disabled on Ubah penawaran, as before). Harga penawaran (Rp) and Stok tersedia (porsi) share a `FieldGrid` row, each a `NumberStepper`; start values, steps (1000 and 1), limits (0 and 1 to 10000000 and 100000), and the `0` format are unchanged, numbers can still be typed, and minus at the minimum uses the shared `stepper-button-disabled`. Tanggal pengambilan is the shared `DateField` spanning the card, with no latest date. Mulai and Selesai share a second `FieldGrid` row, each a `ProfileTimeField` (104 wide hour and minute selects with ":" between, the `00` format). Both rows stack to one column at 900 or less.
- **Note:** "Stok tidak boleh melebihi surplus dalam catatan produksi. Jadwal pesanan aktif tidak dapat diubah." (13, `muted`), 16 under Mulai and Selesai.
- **Actions:** on the right, aligned with the card's bottom and right content edges, the shared text link "Batal" 16 left of the shared compact primary button "Simpan penawaran". Batal opens Penawaran surplus; Simpan penawaran saves as before.

Validation (stock within the production surplus, schedules of active orders), saving, and navigation are unchanged. No horizontal scroll and no cut text at (1040 × 720) and (1920 × 1080). No animation other than the shared button hovers; no blur, glow, or gradient. No em dashes in its text.

### Detail pesanan
The customer order detail (`OrderDetailView`, customer part in `CustomerOrdersView.cs`), in the same content column (at most 1200, inset 32, top 24, 32 below), scrolling vertically as one page. From top to bottom, (16) apart: the shared text link "Kembali ke Pesanan saya" (opens Pesanan saya); the title "Detail pesanan" (32, bold, `ink`) with the status badge (16) beside it, vertically centered; (8) under it the existing status sentence (14, `muted`); then two columns, (24) apart, about 60% and 40% of the column (`3*,2*`). The minimum window (1040) already fits both, so the layout never stacks and never scrolls sideways. All cards are `order-detail-card` (`surface`, `rounded.card`, `elevation.card`, padding 24, no border), (16) apart.

- **Left, Item pesanan:** "Item pesanan" (18, bold); one row per item, 12 apart: the menu name (14 semibold) above "{jumlah} x {harga satuan}" (13, `muted`), with the item subtotal (14 semibold) right-aligned; a thin `divider` rule; then, as in the Keranjang footer, "Total" (12, `muted`) above the order total (28, bold, `primary`), left-aligned.
- **Right, Kode ambil:** "Kode ambil" (14 semibold, `muted`); the code (`pickup-code`: 36, bold, fixed-width font, `primary`) with the existing copy button 12 beside it; and "Tunjukkan kode ini di restoran." (14, `muted`). The copy button is the shared icon button (`Ui.Round(..., icon: true)`: a 44 circle, transparent, the 18 `primary` copy icon, `selection` circle on hover, no shadow or scale) with the same tooltip and name ("Salin kode pengambilan"), the same copy behavior, and the same "Kode berhasil disalin." or "Kode belum berhasil disalin. Silakan coba lagi." notification. Its rule is unchanged: it shows only when the order is not cancelled and the code is not empty. Selesai and Dibatalkan orders still show the code; a cancelled order has no copy button and no sentence.
- **Right, restaurant:** the restaurant name (18, bold) and address (13, `muted`), then label and value pairs (12 `muted` label above 14 `ink` value): Pengambilan, Dipesan, and Nomor pesanan.
- **Cancellation:** on Aktif orders the danger button "Batalkan pesanan" (see Buttons) sits under the restaurant card, with the unchanged confirmation window.

No animation other than the shared primary and danger button hover; no blur, glow, or gradient. No em dashes in its text.

### Lists and Tables
Shared operational rows use minimum heights (40 header / 64 body), while compact dashboard tables use (36 / 48). All use the divider token. Avoid wrapping each datum in a separate card.

### Cards
Implemented on the auth surface, the profile menu, Jelajah (collection cards and listing cards), collection and search result pages (listing cards), Keranjang (item cards), Pesanan saya (order cards), the order detail page (items, pickup code, and restaurant cards), and the food dialog and checkout confirmation cards. A card is a white surface (`surface`) with `rounded.card` corners (the Jelajah collection cards are the one exception: a `primary` fill and the `elevation.card-primary` shadow, see Collection Card) and the `elevation.card` shadow. It has no border of any kind: no colored border, no accent stripe, and no thin outline. Separation comes from the shadow and the canvas around it. Restaurant workspace cards (the menu and listing rows, the menu editor panels, and the Catatan produksi `restaurant-form-card`) are the exception: flat, with a 1 px `divider` border instead of the shadow. Use a card to hold one coherent unit of work, not to wrap each datum. Table and list row dividers are unchanged and stay as described under Lists and Tables.

### Auth Card
The auth-card is the card used on the auth surface. It holds the sign-in or registration form: the bold auth title, visible field labels, inputs, inline errors, the primary action, and the link to switch between Masuk and Daftar. It uses the card treatment with padding (32) and a width of (440), narrowing to no less than (360) to keep its gap from the plate. It is the only card on the auth surface.

### Empty and Error States
Empty lists preserve a direct recovery path when applicable. Operational errors appear inline near the top of the content area; positive cart feedback uses green. Error colors vary between two source literals and are not promoted to a shared token until reconciled.

## Do's and Don'ts

### Do:
- **Do** use green for primary actions, selected navigation, and meaningful emphasis.
- **Do** keep a visible keyboard focus on every control (the shared focus style on customer routes, native elsewhere), preserve disabled affordances, and pair fields with visible labels and automation names.
- **Do** use direct Indonesian task labels and recovery actions for empty or error states.
- **Do** keep real food photography to the auth surface, and to other surfaces only once this document names them explicitly. Menu photos uploaded by restaurants are content, not decoration, and this rule does not affect them.

### Don't:
- **Don't** add promotional banners, generic slogans, hand-drawn food images, decorative illustrations, or repeated statistic-card scaffolding. **The one exception** is the Jelajah banner: its greeting with the user's name, its single call to action, and its kitchen illustration are allowed there and nowhere else. The auth surface stays without slogans.
- **Don't** place text, slogans, or logos over the auth photo panel.
- **Don't** restore the requested Praktikum UI / Data contoh sidebar footer.
- **Don't** replace native control states with undocumented custom behavior.
- **Don't** use purple-to-blue gradients.
- **Don't** use gradient text in headings or hero text.
- **Don't** put emojis in headings.
- **Don't** apply Inter everywhere; use the documented font stack.
- **Don't** give cards colored borders.
- **Don't** use glassmorphism cards (blur, frosted translucent panels).
- **Don't** ship a low-contrast dark mode; keep readable contrast in any theme.
- **Don't** place three icon boxes in a row.
- **Don't** put a badge above the headline.
- **Don't** apply icon sets (such as Lucide) everywhere; use icons only where they carry meaning.
- **Don't** use untouched stock component kits (such as shadcn UI); adapt them to the tokens.
- **Don't** add cursor-following beams or glows.
- **Don't** make buttons that only fade on hover; hover must change background or contrast visibly.
- **Don't** add animations or transitions; the one exception is the collection card and compact primary button hover (scale 1.03, 120 ms), see Elevation & Depth.
- **Don't** space inconsistently; use only the spacing tokens.
- **Don't** scatter em dashes through copy; prefer commas, periods, and colons.
- **Don't** write generic buzzword copy; labels state the task directly.
- **Don't** use serif italic accents.
- **Don't** pair Space Grotesk with Instrument Serif.
- **Don't** add grain or noise overlays on gradients.

Source evidence: `src/Replate.Desktop/Ui.cs` (shared primitives), `MainWindow.cs` (font, palette, shell), `CustomerTopBar.cs` (customer top bar and profile menu), `MarketplaceView.cs` and `MarketplaceBanner.cs` (Jelajah banner, collection cards), `ListingFeed.cs` (listing cards, Tambah), `CollectionView.cs` (collection and search result pages and the shared back link), `ModalDialog.cs` (the shared overlay, scrim, close button, and focus cycle), `FoodDialog.cs` (the food dialog), `CheckoutDialog.cs` (the checkout confirmation), `QuantityStepper.cs` (the shared stepper), `CartView.cs` (Keranjang), `DashboardView.cs` (aligned data tables), `CustomerViews.cs` and `RestaurantViews.cs` (full-page forms and states), `AuthViews.cs` (auth surface, role-button, password-toggle, and time-field), and `Program.cs` (light FluentTheme). The photo asset `src/Replate.Desktop/Assets/auth-hero.png` is loaded by `AuthViews.cs`; the logos `logo-green.webp` and `logo-white.webp` are loaded once by `Ui.cs` and verified (source, height, bitmap ratio within 1%) in the top bar, the restaurant rail, and the auth surface. The `elevation.card`, `elevation.card-primary`, `elevation.card-primary-hover`, `elevation.button-primary-hover`, `motion.collection-card-hover`, `motion.primary-button-hover`, `rounded.card`, and `rounded.full` tokens, the auth-card, role-button, password-toggle, and time-field components, and the auth surface layout and thin scrollbar are implemented and verified by `--verify-ui` (`AuthVerification.cs`; input centering and placeholders by `InputVerification.cs`) at (1360 × 940), (1040 × 720), (1283 × 780), and (1920 × 1080). The top bar, profile menu, and their focus behavior are verified by `TopBarVerification.cs`, the Jelajah banner, collection cards, section title, rhythm, and listing cards by `MarketplaceVerification.cs`, at (1040 × 720), (1360 × 940), and (1920 × 1080), the collection and search result pages by `CollectionVerification.cs` at (1040 × 720) and (1920 × 1080), the food dialog by `FoodDialogVerification.cs` at (1040 × 720) and (1920 × 1080), and Keranjang (header, restaurant group headings with only the name and note, the group divider and footer with the total on the left and Pesan on the right aligned with the card edges, 32 between groups, no summary card, item cards, stepper and stock limit, remove, unavailable items per group, the checkout confirmation content, closing, focus, failed save and single-restaurant order, empty state, alignment with Jelajah, no overlap or horizontal scroll, the confirmation fully visible) by `CartVerification.cs` at (1040 × 720) and (1920 × 1080). The primary button states (normal, hover, pressed, disabled, compact scale, unmoved layout, no horizontal scroll) are verified by `UiVerification.cs` on Masuk, Daftar, Tambah, Tambah ke keranjang, and the owner's Tambah menu, at (1040 × 720) and (1920 × 1080); the hover shape (no tint outside the rounded or round shape, the hover fill inside, half-size radius on round buttons, no shadow or scale on round and icon buttons, an unclipped compact shadow) is verified by a `RenderTargetBitmap` pixel check (`UiVerification.HoverShape`) on Tambah, Tambah ke keranjang, the dialog plus, minus, and close, and the Keranjang plus, minus, trash, and Pesan. The shared text link (`UiVerification.TextLinks`) is verified on the collection page, search results, and Keranjang at (1040 × 720) and (1920 × 1080): transparent fill, border, and pixels behind the link in hover and pressed (each sampled pixel outside the arrow and text matches the canvas with the link hidden), `primary-hover` text and arrow on hover at 4.5:1 or more on the canvas, `primary` at rest and pressed, no shadow, scale, or underline, at least 32 high, the `:focus-visible` ring, and no horizontal scroll. The empty-state outline icons (`UiVerification.EmptyIcon`) are verified on the empty Keranjang and the empty Pesanan saya at (1040 × 720) and (1920 × 1080): 80 square, a stroke of 8% with round caps and joins and no fill, the `primary` token, strokes inside the box and no clipping, centered in the empty state, rendered pixels in `primary`, and no horizontal scroll; the top bar cart keeps its original line icon (`CustomerTopBar.CartIcon`, 24, stroke 1.75) and its 44 hit area (`UiVerification.NavbarCart`). The Catatan produksi fields (`UiVerification.ProductionFields`) are verified at (1040 × 720) and (1920 × 1080): white fields with a 1 px `divider` border, radius 12 and height 48, the `primary-500` hover border, the `primary` focus border with the 2 px ring on keyboard focus only and no native focus box, the select chevron, list sheet, semibold selected option and `selection` hover, the Indonesian date text, the calendar opening on the selected date and picking a day, the calendar look (`UiVerification.CalendarLook`: a white calendar and header with no default grey pixels, Indonesian day names and month header, a round `selection` day hover with `primary` text at 4.5:1 or more, no border, an unmoved circle, and white square corners, the paler adjacent-month days with the same hover, the unchanged selected day on hover with no outline, the today ring, the round arrow hovers, and in the month and year views no grey pixels, Indonesian month names, and a borderless rounded hover that keeps the cell size), both steppers changing the number and holding their minimum and maximum, the unchanged sold-over-produced error with the `danger` border, two equal columns 16 apart with labels 8 above, one column at 900 or less, the form card, the summary block, Simpan catatan in the bottom right, 24 under the heading, no horizontal scroll, and no cut text. The Profil restoran fields (`UiVerification.RestaurantProfileFields`) are verified at (1040 × 720) and (1920 × 1080): every field is a Replate field with a white face and a 1 px `divider` border (not black), radius 12, height 48 (Deskripsi 88), the `primary-500` hover border, the `primary` focus border with the ring on keyboard focus only and no native focus box, the saved values loaded, the hour and minute selects 104 wide side by side with the ":" inside the card and their values shown in full before the chevron, the shared list sheet with `selection` option hover, the two-column rows and their one-column fallback, labels 8 above, the form card at most 720 wide, the muted subtitle and 24 rhythm, Kembali and Simpan profil in the bottom right 24 under the last field, the shared text link and primary button states, Kembali and Simpan profil still navigating and saving, no horizontal scroll, and no cut text. The Dashboard restoran period (`UiVerification.DashboardPeriod`) is verified at (1040 × 720) and (1920 × 1080): the shared select theme with `ink` text, a white field with a 1 px `divider` border (not black), radius 12 and height 44, the outline chevron, the `primary-500` hover border, the `primary` focus border with the ring on keyboard focus only and no native focus box, the rounded list sheet, the semibold selected option, `selection` hover and keyboard focus with `primary` text and no outline on the options, each option shown in full before the chevron, 8 under the subtitle, Ubah profil as the shared solid pill with its hover, and no horizontal scroll. The card action buttons (`UiVerification.CardActions`) are verified on Menu restoran and Penawaran surplus: in hover and pressed the fill differs from rest and takes the documented role color, the text takes its documented color at 4.5:1 or more, the border color, width, and radius are unchanged, hover uses the shared 1.03 scale and shadow, the layout bounds never move, the button returns to its rest colors, and the hover shape check passes. The filter-empty "Lihat semua pesanan" (`UiVerification.FilterEmptyButton`) is verified at (1040 × 720) and (1920 × 1080): the shared compact primary button (filled, not transparent), at least 40 high, 24 under the title and centered, the primary hover, pressed, and focus states, the hover shape, and no horizontal scroll. Product guardrails come from `PRODUCT.md`. The sidecar contains browser-panel translations of five native patterns; its HTML/CSS focus outlines, native-control approximations, and synthesized tonal ramps are preview metadata, not additional application tokens or verified native state styles. No HTML/CSS implementation or iOS/Android behavior is claimed.
