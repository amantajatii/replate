---
name: Replate Desktop
description: Restrained native workspace for surplus food reservation and restaurant operations.
colors:
  primary: "#245B43"
  ink: "#23382C"
  muted: "#59675D"
  canvas: "#FAFAF8"
  surface: "#FFFFFF"
  sidebar: "#F1F3EE"
  selection: "#E0E9DF"
  divider: "#DDE3DA"
  leaf: "#4FA37E"
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
rounded:
  control: "6px"
  card: "16px"
  full: "9999px"
elevation:
  card:
    offsetX: "0px"
    offsetY: "8px"
    blur: "24px"
    spread: "0px"
    color: "rgba(35, 56, 44, 0.12)"
    avaloniaBoxShadow: "0 8 24 0 #1F23382C"
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
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    padding: "11px 16px"
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
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    iconColor: "{colors.primary}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
    height: "96px"
  filter-card-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
  filter-card-selected:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
  filter-card-selected-hover:
    backgroundColor: "{colors.divider}"
    textColor: "{colors.primary}"
    rounded: "{rounded.card}"
    shadow: "{elevation.card}"
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
    textColor: "{colors.leaf}"
    rounded: "{rounded.control}"
    size: "20px"
  time-field:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.full}"
    height: "42px"
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
- One soft shadow, reserved for cards

## Colors

Forest green is a functional accent within a pale, warm-neutral workspace. Frontmatter values are normative; names below explain their use.

### Primary
- **Forest Green** (`primary`): primary actions, selected routes and filters, rescue prices, and meaningful operational emphasis.

### Neutral
- **Deep Leaf Ink** (`ink`): default readable content and secondary button text.
- **Muted Leaf** (`muted`): metadata, subtitles, supporting explanations, and inactive navigation.
- **Warm Canvas** (`canvas`): the main window background.
- **White Surface** (`surface`): secondary buttons, the search input, the profile menu, and, on the green top bar, the white logo, icons, cart badge, profile circle, and focus ring.
- **Pale Sage Sidebar** (`sidebar`): restaurant navigation rail.
- **Soft Sage Selection** (`selection`): selected navigation, collection cards and the back link on hover, profile menu items on hover and keyboard focus, the profile circle on hover, and the cart icon on hover (about 6.4:1 on `primary`).
- **Sage Divider** (`divider`): row rules and shell separators.
- **Light Leaf** (`leaf`): the hover color of the password-toggle icon only. About 3.1:1 on white, enough for an icon, not for body text.

The customer top bar is the one place `primary` is a surface rather than an accent: white text and icons on it reach about 7.9:1.

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

Jelajah centers its content at most (1200) wide (inset 32 on narrower windows) and scrolls vertically under the fixed top bar. Its vertical rhythm is one section gap (24) from the top bar to the banner, from the banner to the collection cards, and from the collection cards to the "Jelajahi Menu" title; (16) from the title to the first listing card, the same as between cards; and (32) below the page. Collection and search result pages use the same column and `Ui.Spacing` constants: (24) from the top bar to the back link, (16) from the back link to the title, (16) from the title to the first card, and (32) below the page. Full-page menu/listing forms are left-aligned (640 wide), food details use (680), and the production editor uses (760). Data grids combine flexible description columns with consistent fixed action, time, and price columns. Dashboard numeric columns align right.

### Auth surface

The sign-in (Masuk) and registration (Daftar) screens fill the window with two columns: a canvas column on the left and a primary panel on the right. The panel takes about 28% of the width; its left edge follows the plate.

- **Left column:** canvas background. The green logo (see Logo) sits at the top (inset 32, 44 high) on a band filled with `canvas`, aligned with the card's left edge and outside the scroll area; the scroll area starts below that band and clips there, so scrolled content never shows under or over the logo. Below it, a vertical-only scroll area holds a single auth-card. The demo-account line and the warning share the card's width and left edge. Masuk also shows the demo-account line under the card and, on a database startup error, one compact warning above it. No other containers, banners, or helper panels sit beside the card.
- **Right column:** a full-height panel filled with the primary color. The panel itself holds nothing.
- **Plate:** `src/Replate.Desktop/Assets/auth-hero.png` (a transparent round cut-out of a plate) sits in an overlay layer above both columns that does not scroll. It is centered vertically, and the panel's left edge passes exactly through the plate's center: half of the plate lies over the canvas and half over the green panel.
- **Placement:** the diameter is 0.56 × window height, at least (360) and never taller than the window. The panel edge (the plate center) sits at 0.72 × window width. The card starts at about 0.12 × window width (never under 48) and is 440 wide at most. The card and the plate keep a gap of 0.08 × window width; on a wide window the gap grows past that, since the card stays at 440. When the window is too small, the card narrows to (360) first, then the gap shrinks step by step to no less than (46: the scrollbar gap 16, the scrollbar 6, and 24 clear of the plate), then the left margin shrinks (to no less than 48), then the panel edge moves right, then the plate shrinks (to no less than 320). The 46 floor is reached only on narrow, tall windows (for example 1040 × 1080); the four reference sizes below keep their gaps.
- **Card and plate never touch:** the plate box and the card box never intersect and keep a gap of at least (24).
- **Scroll area:** the viewport hugs the card: it starts (24) left of the card and ends (16 + 6) right of it, so the scrollbar sits (16) beside the card's right edge and never over it. The 24 on the left is inner padding for the card shadow; the viewport does not clip horizontally, and an outer clip that runs to the plate's left edge keeps the shadow whole on both sides. The scrollbar stays at least (24) from the plate box. The mouse wheel also scrolls the form when the pointer is over the empty canvas beside it.
- **Scrollbar:** the scroll area uses a thin scrollbar (6 wide): one rounded thumb in `muted`, with no track and no arrow buttons. It is hidden at rest and shows only while the form scrolls or while the pointer is over the scroll area, with no transition. Mouse wheel, touchpad, and keyboard (PageUp, PageDown, arrows) scroll as usual, and a field reached with Tab is scrolled into view. Other screens keep the native scrollbar.
- **Photo content:** no text, slogan, logo, or overlay is placed on the plate or the panel. Both are decorative and hidden from assistive technology.

Reference sizes: at (1360 × 940) the plate is 526, the card 440 starting at 163 (12%), the gap about 113, and the panel 28% of the width; at (1283 × 780) the plate is 437, the card 440 starting at 154, and the gap about 111; at (1920 × 1080) the plate is 605, the card 440 starting at 230, and the gap about 410 (wide on purpose); at the minimum (1040 × 720) the plate is 403, the card 360 starting at 125, the gap about 62 (the gap gives way after the card reaches 360), the panel still 28%, and the owner registration form scrolls vertically. There is no horizontal scrolling at any size.

## Elevation & Depth

Custom application surfaces stay flat. Tonal backgrounds, whitespace, and thin dividers separate shell, rows, and content. The only exception is the card, which may carry one soft shadow: `elevation.card`. There are no other shadow tokens and no motion tokens.

**The One Shadow Rule.** `elevation.card` is offset (0, 8), blur (24), spread (0), in ink (`#23382C`) at about 12% alpha. In Avalonia this is the `BoxShadow` string `0 8 24 0 #1F23382C` (Avalonia colors are `#AARRGGBB`, and `1F` is 31 of 255, about 12%). The CSS equivalent is `0px 8px 24px 0px rgba(35, 56, 44, 0.12)`. Do not stack shadows, tint them with the primary color, or apply them to buttons, rows, or the shell.

The current build applies `elevation.card` to the auth-card, the profile menu, and the Jelajah collection cards and listing cards (also on collection and search result pages) only. The Jelajah banner has no shadow. The top bar stays flat with no rule and no shadow: its green fill separates it from the canvas. If a separator is ever needed, it is `elevation.card`, never a line.

Native Avalonia Fluent controls retain their theme behavior; this document does not override native popups or interaction states. The exceptions are the thin scrollbar of the auth scroll area, described under Auth surface, and keyboard focus on customer routes, described under Focus.

## Shapes

Buttons and text fields use gently rounded control corners (`rounded.control`). Cards use a larger radius (`rounded.card`, 16). Lists and tables remain open rectangular rows separated by a single bottom rule (1). The card is the only sanctioned container silhouette; no other decorative container shapes are established.

## Components

### Buttons
Direct, compact action labels. Primary buttons use green with white text; secondary buttons use white with ink text. Both share the control radius, semibold text (13), and portable padding token. Inactive navigation and filters use transparent backgrounds. Hover, focus, pressed, and disabled behavior come from Avalonia FluentTheme, not custom state tokens, except on the auth surface: there the primary button hovers to an ink fill with white text, the secondary button to a selection fill with primary text, and the auth controls below document their own states.

### Inputs / Fields
Visible labels sit above native TextBox, NumericUpDown, ComboBox, DatePicker, and TimePicker controls (restaurant forms); the auth surface uses the time-field instead of TimePicker. Text and numeric inputs have a minimum height (42); multiline text inputs use (88). The top bar search field has its own height token, a pill shape, and a white background. Automation names mirror visible labels. Native controls own focus and disabled treatments.

- **Vertical centering:** every single-line TextBox (including the one inside NumericUpDown and every Kata sandi field) centers its content vertically with balanced padding (10, 5.5, 6, 5.5: top equals bottom). Typed text, the placeholder, password dots, and the password-toggle share the field's vertical center. One shared rule (`Ui.UseInputs` in `Ui.cs`) applies this app-wide; individual fields do not set it. Field heights are unchanged.
- **Multiline fields:** a TextBox that accepts returns (Deskripsi on Daftar and in the menu editor) aligns its placeholder and text to the top instead, with padding (10, 10, 6, 5.5): the top padding equals the left. The same shared rule applies it.
- **Placeholder:** every TextBox carries an Indonesian placeholder following "Masukkan ..." (for example "Masukkan email Anda", "Masukkan kata sandi Anda", "Masukkan nama restoran", "Masukkan harga"); the top bar search field reads "Mulai cari menu atau restoran". Placeholder text uses `muted` at full opacity (about 5.9:1 on white), disappears once the field has content, and has no other effect. No em dashes and no exclamation marks.

### Auth controls
These three components exist only on the auth surface. Their states are documented here because they replace the native Fluent look; they use only the color tokens above, with no blur, glow, gradient, or animation.

- **role-button:** the Daftar sebagai choice (Pelanggan / Pemilik restoran). Two equal-width buttons that together span the card (12 apart), `rounded.control`, no visible border. They keep RadioButton semantics: one group, arrow keys move and select, automation names match the labels. Unselected: sidebar fill with ink text; hover: selection fill with primary text. Selected: primary fill with white text; hover: ink fill with white text. Keyboard focus shows a 2 px ink ring.
- **password-toggle:** a 20 × 20 button hugging its 16 icon, inside the right edge of every Kata sandi field (Masuk, and Kata sandi and Ulangi kata sandi on Daftar), always visible, even when the field is empty. Its eye and crossed-eye icons are drawn as Path geometry (no icon package). Default: an ink icon with no fill or border. Hover, only while the pointer is over the icon: the icon turns `leaf`; there is still no fill, border, box, or transition, and the icon returns to ink when the pointer leaves. Pressing changes nothing else. Keyboard focus (Tab from the field): 2 px primary ring; Enter or Space toggles it, and Enter on the toggle does not submit the form. Pressing it switches the field between masked and plain text, and its automation name switches between "Tampilkan kata sandi" and "Sembunyikan kata sandi". Submitting the form returns every password field to masked mode; sign-in and an accepted registration also clear them.
- **time-field:** Jam buka and Jam tutup in the restaurant profile. A white pill (`rounded.full`, height 42) with the same border as native text fields, holding a native hour ComboBox (00 to 23), a ":" separator, and a native minute ComboBox (00 to 59). No AM/PM and no shadow. Picking an hour fills the minute with 00. The two fields share one row (16 apart) with visible labels above each; automation names follow the pattern "Jam buka, jam" and "Jam buka, menit". The value is a 24-hour `TimeOnly`. The hour and minute boxes share the pill width equally, so both stay inside the pill at the narrowest card (360).

### Focus
Customer routes (top bar, profile menu, Jelajah, Keranjang, Pesanan saya, details, and the customer cancellation dialog) share one focus style, `Ui.FocusStyles` in `Ui.cs`. It replaces the Fluent focus adorner (the black double box) with an empty adorner and shows focus only on `:focus-visible`, that is after Tab or arrow keys, never after a click.

- **Ring:** buttons and collection cards show a 2 px ring 2 outside their edge, following their corner radius: `primary` on light surfaces, white (`surface`) on the green top bar. The control's own border, fill, and size do not change. Primary buttons keep a visible ring because it sits outside the green fill.
- **Menu items:** profile menu items show the `selection` fill with `primary` text instead of a ring, the same as hover. Opened with the mouse, the menu focuses Pesanan saya without showing it; opened with Enter or Space, Pesanan saya shows the fill.
- **Other controls:** ComboBox uses a `primary` border on `:focus-visible`. Text fields keep their native focus border, except the top bar search, which shows the white ring while focused.
- The auth surface and restaurant routes keep their current focus treatment.

### Logo
The brand is shown only as a bitmap logo; there is no text wordmark. `Assets/logo-white.webp` (white, transparent background) sits on `primary` surfaces, and `Assets/logo-green.webp` (green, transparent background) sits on light surfaces. Both are 1500 by 347 and shown with Uniform stretch at a fixed height; the width follows the bitmap ratio and is never set, so the logo is never distorted. Heights: 36 in the customer top bar, 32 in the restaurant rail (so it fits the 208 rail), and 44 on the auth surface. The logo carries the automation name "Replate".

- **Top bar:** white logo, 36 high, inside a plain button that opens Jelajah (name "Replate", tooltip "Jelajah") with the shared white focus ring on `:focus-visible`.
- **Restaurant rail:** green logo on `sidebar`, 32 high, not a button.
- **Auth surface:** green logo on `canvas`, 44 high, aligned with the card's left edge.

### Section Title
"Jelajahi Menu" heads the listing on Jelajah, between the collection cards and the first listing card: 32, bold (`auth-title` weight), `ink`, matching the largest banner greeting, left-aligned with the content edge, one line. It is always shown, also when the list is empty. The add-to-cart notice sits right-aligned on the same row. There is no result counter.

### Navigation and Filters
Selected routes share pale sage fill and forest-green text; inactive items use muted text. Jelajah has no filters: the collection cards below are links to their own pages, not a chip shape or a selection. Customer navigation is the top bar below and has no route tabs; restaurant navigation is the vertical rail.

### Top Bar
The customer shell (`CustomerTopBar.cs`) is one persistent bar on every customer page: `primary` fill, height (72), side padding (32). No bottom rule, no shadow, and no border. Text and icons on it are white (about 7.9:1). It holds three groups in an `Auto, *, Auto` grid:

- **Logo:** the white logo (`logo-white.webp`, 36 high, width from the bitmap ratio). It is a button that opens Jelajah, named "Replate"; see Logo.
- **Search:** the `search-input`, a white pill (`rounded.full`, height 44, no border) that fills the middle column with (32) on each side, at most (720) and at least (320), centered between the logo and the icons. A `muted` magnifier icon sits inside on the left; the placeholder reads "Mulai cari menu atau restoran" in `muted`, typed text is `ink`; automation name "Cari menu atau restoran". While focused, a 2 px white ring sits 2 outside the pill; the field never shows a dark border. It replaces the old search field and its label on Jelajah. The magnifier is a small button (40, `rounded.full`, transparent, `muted` icon that turns `primary` on hover, named "Cari", not a Tab stop). Typing alone changes nothing; Enter in the field or a click on the magnifier opens the search result page (see Collection Page). An empty or blank field does nothing. On the result page the field keeps the keyword, and searching again replaces the results. Every other customer page, including Jelajah, opens with an empty field; Keluar clears it too.
- **Cart:** a 44 by 44 icon button, transparent with no fill or box on hover. The icon is white and turns `selection` on hover. While Keranjang is open, a white line (20 by 2) sits under the icon. The `cart-badge` (18 high, `rounded.full`, white fill, `primary` supporting text) sits in the button's top-right corner and shows the number of portions in the cart ("99+" above 99); it is hidden at 0. Click opens Keranjang. Name and tooltip "Keranjang".
- **Profile:** a 44 circle (`rounded.full`) with a white fill and a `primary` user icon; hover changes the fill to `selection`. Name and tooltip "Akun". Click, Enter, or Space opens the profile menu.
- **Profile menu:** a popup anchored below the circle's right edge (8 below it), drawn in the window's overlay layer. A white card (`rounded.card`, `elevation.card`, padding 8, width 260, no border) holding the user's name (14 semibold) and email (12), both `muted` and not clickable, a divider, then "Pesanan saya" and "Keluar" as full-width items (14, ink, `rounded.control`; hover `selection` fill with `primary` text). Pesanan saya opens the orders page; Keluar runs the existing sign-out. Opening it moves focus to Pesanan saya (shown only when opened from the keyboard), the arrow keys move between the two items, and Esc or a click outside closes it (Esc returns focus to the circle). Keyboard focus on an item uses the `selection` fill with `primary` text, never a box.
- **Icons:** magnifier, cart, and user are hand-drawn Path geometry on a 24 grid, stroke 1.75 with round caps and joins, no fill, no icon package.
- **Focus:** every control is reached with Tab; buttons show the shared white focus ring on keyboard focus (see Focus).
- **Sizes:** at the minimum (1040 by 720) the search shrinks and stays at or above 320; at (1920 by 1080) it stops at 720. Nothing in the bar overlaps at any size.

### Jelajah Banner
The first block on Jelajah: `rounded.card` corners, no border, no shadow, centered in the content column. `Assets/marketplace-hero.webp` (a wide kitchen illustration, 4344 by 1280, empty top-left, objects on the right, cabinets along the bottom) is shown whole with Uniform stretch: never cropped or distorted. The banner is exactly as wide as the content column, so its left and right edges line up with the filter row and the listing cards at every window size; its height follows the bitmap ratio (width × 1280 / 4344): about 288 at the minimum window and 354 at the 1200 content maximum. The image is decorative and hidden from assistive technology.

- **Greeting:** "Halo, {nama pengguna}." from the signed-in account, bold (`auth-title` weight) in `primary`, one line, trimmed with an ellipsis when too long. Size scales with the banner width (0.0287 × width, 22 to 32): 28 at the minimum window, 32 at the 1200 content maximum.
- **Call to action:** "Yuk, Selamatkan Makanan Lewat Setiap Pesanan!" in `muted`, at most two lines. Size 0.0184 × width, 15 to 20.
- **Text block:** overlaid on the empty top-left of the image, left inset (40), centered vertically in the area above the cabinets (which start at 87.8% of the height). Its right edge stays at or left of 45% of the banner width; the illustration's nearest object starts at 46.2% and the empty area reaches about 55% at the top, so the text never covers the illustration at any size.

### Collection Card
Five equal cards in one row, (24) under the banner and (24) above the section title, (16) apart: Termurah (tag icon), Segera berakhir (clock), Makanan utama (fork and spoon), Roti & kue (bread), Buah & sayur (leaf). Each is a button styled as a card: `surface`, `rounded.card`, `elevation.card`, no border, height (96); a 24 line icon in `primary` (Path geometry, stroke 1.75, round caps) above a 14 label. Hover: `selection` fill with `primary` text. There is no selected state. Keyboard focus: the shared 2 px `primary` ring outside the card; Enter or Space opens it. Each card is a link: it opens that collection's page (see Collection Page), and Jelajah itself always lists every available listing in the default order. There is no Semua card. At (1040) the cards narrow and stay in one row.

### Collection Page
One reused view (`CollectionView.cs`) for the five collections and for search results, under the same top bar and in the same content column as Jelajah (at most 1200, inset 32), with no banner. From top to bottom:

- **Back link:** "Kembali ke Jelajah" with a 16 left arrow (Path on a 16 grid, stroke 1.75, round caps), 8 from the 14 semibold `primary` text and vertically centered with it, transparent, at least 32 high; hover `selection` fill; padding 8 by 6 with the arrow on the content edge; the shared `primary` focus ring. It opens Jelajah.
- **Title:** 32, bold, `ink`, one line, the same size as "Jelajahi Menu". A collection uses its card label exactly; search results read "Hasil untuk {kata}" with the keyword as typed (outer spaces trimmed), cut with an ellipsis when too long. The add-to-cart notice sits right-aligned on the same row.
- **Intro:** on a collection page with listings, one sentence sits (8) under the title, 16 in `muted`, wrapping; the first card follows (16) under it. Search results and empty states have no intro. Termurah: "Temukan berbagai makanan lezat, pas, dan ramah di kantong!"; Segera berakhir: "Nikmati hidangan favorit sebelum terlambat!"; Makanan utama: "Temukan hidangan mengenyangkan untuk menemani harimu!"; Roti & kue: "Maniskan harimu dengan roti dan kue lezat yang menggugah selera!"; Buah & sayur: "Nikmati pilihan hidangan sehat yang lezat, segar, dan penuh kebaikan!".
- **Listings:** the same listing cards as Jelajah, (16) apart, with the same Tambah behavior and notice. Termurah lists every available listing by rescue price ascending, Segera berakhir by pickup end ascending, and Makanan utama, Roti & kue, and Buah & sayur keep their category in the default order.
- **Search:** the keyword is split on spaces and every word must appear (case-insensitive) in the menu name, description, restaurant name, or category; "vegetarian" also matches Buah & sayur.
- **Categories:** the menu categories are Makanan utama, Roti & kue, and Buah & sayur (`MenuCategories.cs`), shown the same on the cards, collection titles, listing lines, food details, and the owner's menu form. Buah & sayur replaces the old Vegetarian label: a saved state file that still holds "Vegetarian" loads as Buah & sayur in memory without being rewritten on load.
- **Empty states:** a collection shows "Belum ada menu di sini" with "Menu di koleksi ini sedang habis. Coba lagi nanti."; a search shows "Tidak ada menu yang cocok" with "Coba kata kunci lain.". Both end with a secondary "Kembali ke Jelajah" button and sit (16) under the title.

### Listing Card
Each Jelajah listing is one card (`surface`, `rounded.card`, `elevation.card`, padding 20, no border), (16) apart, with photo, description, pickup, price, and action columns: photo box (or "Foto belum tersedia"), menu name, description, "Restoran · Kategori", Pengambilan with time and remaining portions, rescue price, struck-through normal price, "Hemat N%", and Tambah. The first card sits (16) under the "Jelajahi Menu" title; there is no result counter. The sample-data note stays under the list. If nothing is available, Jelajah shows "Belum ada menu tersedia" (16) under the title. Tambah adds one portion; when the cart already holds another restaurant's menu, the notice explains that a cart holds one restaurant.

### Lists and Tables
Shared operational rows use minimum heights (40 header / 64 body), while compact dashboard tables use (36 / 48). All use the divider token. Avoid wrapping each datum in a separate card.

### Cards
Implemented on the auth surface, the profile menu, Jelajah (collection cards and listing cards), and collection and search result pages (listing cards). A card is a white surface (`surface`) with `rounded.card` corners and the `elevation.card` shadow. It has no border of any kind: no colored border, no accent stripe, and no thin outline. Separation comes from the shadow and the canvas around it. Use a card to hold one coherent unit of work, not to wrap each datum. Table and list row dividers are unchanged and stay as described under Lists and Tables.

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
- **Don't** space inconsistently; use only the spacing tokens.
- **Don't** scatter em dashes through copy; prefer commas, periods, and colons.
- **Don't** write generic buzzword copy; labels state the task directly.
- **Don't** use serif italic accents.
- **Don't** pair Space Grotesk with Instrument Serif.
- **Don't** add grain or noise overlays on gradients.

Source evidence: `src/Replate.Desktop/Ui.cs` (shared primitives), `MainWindow.cs` (font, palette, shell), `CustomerTopBar.cs` (customer top bar and profile menu), `MarketplaceView.cs` and `MarketplaceBanner.cs` (Jelajah banner, collection cards), `ListingFeed.cs` (listing cards, Tambah), `CollectionView.cs` (collection and search result pages), `DashboardView.cs` (aligned data tables), `CustomerViews.cs` and `RestaurantViews.cs` (full-page forms and states), `AuthViews.cs` (auth surface, role-button, password-toggle, and time-field), and `Program.cs` (light FluentTheme). The photo asset `src/Replate.Desktop/Assets/auth-hero.png` is loaded by `AuthViews.cs`; the logos `logo-green.webp` and `logo-white.webp` are loaded once by `Ui.cs` and verified (source, height, bitmap ratio within 1%) in the top bar, the restaurant rail, and the auth surface. The `elevation.card`, `rounded.card`, and `rounded.full` tokens, the auth-card, role-button, password-toggle, and time-field components, and the auth surface layout and thin scrollbar are implemented and verified by `--verify-ui` (`AuthVerification.cs`; input centering and placeholders by `InputVerification.cs`) at (1360 × 940), (1040 × 720), (1283 × 780), and (1920 × 1080). The top bar, profile menu, and their focus behavior are verified by `TopBarVerification.cs`, the Jelajah banner, collection cards, section title, rhythm, and listing cards by `MarketplaceVerification.cs`, at (1040 × 720), (1360 × 940), and (1920 × 1080), and the collection and search result pages by `CollectionVerification.cs` at (1040 × 720) and (1920 × 1080). Product guardrails come from `PRODUCT.md`. The sidecar contains browser-panel translations of five native patterns; its HTML/CSS focus outlines, native-control approximations, and synthesized tonal ramps are preview metadata, not additional application tokens or verified native state styles. No HTML/CSS implementation or iOS/Android behavior is claimed.
