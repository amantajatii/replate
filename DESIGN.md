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
    rounded: "{rounded.control}"
    height: "44px"
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
    size: "32px"
  password-toggle-hover:
    backgroundColor: "{colors.selection}"
    textColor: "{colors.primary}"
    rounded: "{rounded.control}"
    size: "32px"
  password-toggle-pressed:
    backgroundColor: "{colors.divider}"
    textColor: "{colors.primary}"
    rounded: "{rounded.control}"
    size: "32px"
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
- **White Surface** (`surface`): customer navigation, secondary buttons, and search input.
- **Pale Sage Sidebar** (`sidebar`): restaurant navigation rail.
- **Soft Sage Selection** (`selection`): selected navigation and category filters.
- **Sage Divider** (`divider`): row rules and shell separators.

**The Functional Green Rule.** Use the accent to communicate action, selection, or useful emphasis.

## Typography

**Body and UI Font:** Helvetica Neue, Segoe UI, sans-serif, inherited from the main window. Avalonia uses normal and semibold weights, plus bold for the auth title only; line-height and tracking are not customized.

### Hierarchy
- **Headline:** page headings; semibold, using the frontmatter headline role.
- **Auth title:** the card titles Masuk and Daftar akun; bold (700) at the headline size, using the `auth-title` role. No subtitle sits under either title.
- **Section:** grouped content headings and status summaries.
- **Row title:** food/menu names and prominent row amounts.
- **Body:** descriptions, field labels, status, and supporting copy; visible field labels also use semibold.
- **Label:** buttons and compact labels; secondary metadata also uses normal at this size.
- **Supporting:** dates, caveats, and compact table headers.

The build also uses larger wordmark, food-detail title, price, and pickup-code text. These are local information treatments, not a decorative system-display font to reproduce across new surfaces.

## Layout

The native window starts at (1360 × 940) and has a minimum of (1040 × 720). These are desktop sizes, not web breakpoints. Content scrolls vertically; horizontal scrolling is disabled. No mobile or tablet behavior is established.

Shared pages use an inset of (32) and vertical section gaps of (24). Dashboard sections use (28). Related form fields use (20), labels sit (8) above controls, and compact stacks use (6–12). Measurements describe Avalonia device-independent units; the portable tokens express their equivalent px notation.

Customer navigation sits above the page with horizontal links. Restaurant navigation occupies a dedicated rail (208 wide). Role-specific route names and exact shell topology are defined in `docs/superpowers/specs/2026-10-19-desktop-surface-contract.md`.

Full-page menu/listing forms are left-aligned (640 wide), food details use (680), and the production editor uses (760). Data grids combine flexible description columns with consistent fixed action, time, and price columns. Dashboard numeric columns align right.

### Auth surface

The sign-in (Masuk) and registration (Daftar) screens fill the window with two columns: a canvas column on the left and a primary panel on the right. The panel width is not fixed; its left edge follows the plate.

- **Left column:** canvas background. The wordmark sits at the top left (inset 32) outside the scroll area, so it stays fully visible while the form scrolls. Below it, a vertical-only scroll area holds a single auth-card. Masuk also shows the demo-account line under the card and, on a database startup error, one compact warning above it. No other containers, banners, or helper panels sit beside the card.
- **Right column:** a full-height panel filled with the primary color. The panel itself holds nothing.
- **Plate:** `src/Replate.Desktop/Assets/auth-hero.png` (a transparent round cut-out of a plate) sits in an overlay layer above both columns that does not scroll. It is centered vertically, and the panel's left edge passes exactly through the plate's center: half of the plate lies over the canvas and half over the green panel.
- **Placement:** the diameter is 0.6 × window height, at least (360) and never taller than the window. The panel edge (the plate center) targets 0.6 × window width. The scroll area ends (24) before the plate, and the card is centered in it with (32) on either side, 440 wide at most. If that leaves the card under (360), the panel edge moves right until the card is 360; if the plate would then leave the window, the plate shrinks (to no less than 320).
- **Card and plate never touch:** the plate box and the card box never intersect and keep a gap of at least (24).
- **Scrollbar:** the scroll area uses a thin scrollbar (6 wide): one rounded thumb in `muted`, with no track and no arrow buttons. It is hidden at rest and shows only while the form scrolls or while the pointer is over the scroll area, with no transition. Mouse wheel, touchpad, and keyboard (PageUp, PageDown, arrows) scroll as usual, and a field reached with Tab is scrolled into view. Other screens keep the native scrollbar.
- **Photo content:** no text, slogan, logo, or overlay is placed on the plate or the panel. Both are decorative and hidden from assistive technology.

Reference sizes: at (1360 × 940) the plate is 564, the card 440, and the panel about 40% of the width; at the minimum (1040 × 720) the plate is 432, the card 360, and the panel about 36%, and the owner registration form scrolls vertically. There is no horizontal scrolling at any size.

## Elevation & Depth

Custom application surfaces stay flat. Tonal backgrounds, whitespace, and thin dividers separate shell, rows, and content. The only exception is the card, which may carry one soft shadow: `elevation.card`. There are no other shadow tokens and no motion tokens.

**The One Shadow Rule.** `elevation.card` is offset (0, 8), blur (24), spread (0), in ink (`#23382C`) at about 12% alpha. In Avalonia this is the `BoxShadow` string `0 8 24 0 #1F23382C` (Avalonia colors are `#AARRGGBB`, and `1F` is 31 of 255, about 12%). The CSS equivalent is `0px 8px 24px 0px rgba(35, 56, 44, 0.12)`. Do not stack shadows, tint them with the primary color, or apply them to buttons, rows, or the shell.

The current build applies `elevation.card` to the auth-card only.

Native Avalonia Fluent controls retain their theme behavior; this document does not override native popups, focus, or interaction states. The one exception is the thin scrollbar of the auth scroll area, described under Auth surface.

## Shapes

Buttons and text fields use gently rounded control corners (`rounded.control`). Cards use a larger radius (`rounded.card`, 16). Lists and tables remain open rectangular rows separated by a single bottom rule (1). The card is the only sanctioned container silhouette; no other decorative container shapes are established.

## Components

### Buttons
Direct, compact action labels. Primary buttons use green with white text; secondary buttons use white with ink text. Both share the control radius, semibold text (13), and portable padding token. Inactive navigation and filters use transparent backgrounds. Hover, focus, pressed, and disabled behavior come from Avalonia FluentTheme, not custom state tokens, except on the auth surface: there the primary button hovers to an ink fill with white text, the secondary button to a selection fill with primary text, and the auth controls below document their own states.

### Inputs / Fields
Visible labels sit above native TextBox, NumericUpDown, ComboBox, DatePicker, and TimePicker controls (restaurant forms); the auth surface uses the time-field instead of TimePicker. Text and numeric inputs have a minimum height (42); multiline text inputs use (88). The marketplace search field has its own height token and white background. Automation names mirror visible labels. Native controls own focus and disabled treatments.

### Auth controls
These three components exist only on the auth surface. Their states are documented here because they replace the native Fluent look; they use only the color tokens above, with no blur, glow, gradient, or animation.

- **role-button:** the Daftar sebagai choice (Pelanggan / Pemilik restoran). Two equal-width buttons that together span the card (12 apart), `rounded.control`, no visible border. They keep RadioButton semantics: one group, arrow keys move and select, automation names match the labels. Unselected: sidebar fill with ink text; hover: selection fill with primary text. Selected: primary fill with white text; hover: ink fill with white text. Keyboard focus shows a 2 px ink ring.
- **password-toggle:** a 32 × 32 button inside the right edge of every Kata sandi field, always visible, even when the field is empty. Its eye and crossed-eye icons are drawn as Path geometry (no icon package). Default: transparent with an ink icon; hover: selection fill with a primary icon; pressed: divider fill with a primary icon; keyboard focus: 2 px primary ring. Pressing it switches the field between masked and plain text, and its automation name switches between "Tampilkan kata sandi" and "Sembunyikan kata sandi". Submitting the form returns every password field to masked mode; sign-in and an accepted registration also clear them.
- **time-field:** Jam buka and Jam tutup in the restaurant profile. A white pill (`rounded.full`, height 42) with the same border as native text fields, holding a native hour ComboBox (00 to 23), a ":" separator, and a native minute ComboBox (00 to 59). No AM/PM and no shadow. Picking an hour fills the minute with 00. The two fields share one row (16 apart) with visible labels above each; automation names follow the pattern "Jam buka, jam" and "Jam buka, menit". The value is a 24-hour `TimeOnly`. The hour and minute boxes share the pill width equally, so both stay inside the pill at the narrowest card (360).

### Navigation and Filters
Selected routes and category filters share pale sage fill and forest-green text; inactive items use muted text. Category filters are buttons, not a separate invented chip shape. Customer navigation is horizontal and restaurant navigation vertical.

### Lists and Tables
Marketplace food rows have generous vertical padding (22), with description, pickup, price, and action columns. Shared operational rows use minimum heights (40 header / 64 body), while compact dashboard tables use (36 / 48). All use the divider token. Avoid wrapping each datum in a separate card.

### Cards
Implemented on the auth surface. A card is a white surface (`surface`) with `rounded.card` corners and the `elevation.card` shadow. It has no border of any kind: no colored border, no accent stripe, and no thin outline. Separation comes from the shadow and the canvas around it. Use a card to hold one coherent unit of work, not to wrap each datum. Table and list row dividers are unchanged and stay as described under Lists and Tables.

### Auth Card
The auth-card is the card used on the auth surface. It holds the sign-in or registration form: the bold auth title, visible field labels, inputs, inline errors, the primary action, and the link to switch between Masuk and Daftar. It uses the card treatment with padding (32) and a width of (440), narrowing to no less than (360) to keep its gap from the plate. It is the only card on the auth surface.

### Empty and Error States
Empty lists preserve a direct recovery path when applicable. Operational errors appear inline near the top of the content area; positive cart feedback uses green. Error colors vary between two source literals and are not promoted to a shared token until reconciled.

## Do's and Don'ts

### Do:
- **Do** use green for primary actions, selected navigation, and meaningful emphasis.
- **Do** preserve native focus and disabled affordances and pair fields with visible labels and automation names.
- **Do** use direct Indonesian task labels and recovery actions for empty or error states.
- **Do** keep real food photography to the auth surface, and to other surfaces only once this document names them explicitly. Menu photos uploaded by restaurants are content, not decoration, and this rule does not affect them.

### Don't:
- **Don't** add promotional banners, generic slogans, hand-drawn food images, decorative illustrations, or repeated statistic-card scaffolding.
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

Source evidence: `src/Replate.Desktop/Ui.cs` (shared primitives), `MainWindow.cs` (font, palette, shell), `MarketplaceView.cs` (filters and rows), `DashboardView.cs` (aligned data tables), `CustomerViews.cs` and `RestaurantViews.cs` (full-page forms and states), `AuthViews.cs` (auth surface, role-button, password-toggle, and time-field), and `Program.cs` (light FluentTheme). The photo asset `src/Replate.Desktop/Assets/auth-hero.png` is loaded by `AuthViews.cs`. The `elevation.card`, `rounded.card`, and `rounded.full` tokens, the auth-card, role-button, password-toggle, and time-field components, and the auth surface layout and thin scrollbar are implemented and verified by `--verify-ui` (`AuthVerification.cs`) at (1360 × 940), (1040 × 720), and (1283 × 780). Product guardrails come from `PRODUCT.md`. The sidecar contains browser-panel translations of five native patterns; its HTML/CSS focus outlines, native-control approximations, and synthesized tonal ramps are preview metadata, not additional application tokens or verified native state styles. No HTML/CSS implementation or iOS/Android behavior is claimed.
