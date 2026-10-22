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
---

# Design System: Replate Desktop

## Overview

**Creative North Star: "Established Replate workspace"**

Replate uses a restrained green and neutral workspace for reserving surplus food and managing restaurant operations. Native sans typography, direct Indonesian labels, and aligned lists keep information and actions easy to scan.

This records the implemented Avalonia desktop system, currently verified on macOS. It extends the established identity; role-specific navigation and full-page forms belong to the desktop surface contract rather than a new brand.

**Key Characteristics:**
- Quiet green actions and selection
- Native controls with visible field labels
- Flat surfaces, dividers, and structured rows

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

**Body and UI Font:** Helvetica Neue, Segoe UI, sans-serif, inherited from the main window. Avalonia uses normal and semibold weights; line-height and tracking are not customized.

### Hierarchy
- **Headline:** page headings; semibold, using the frontmatter headline role.
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

## Elevation & Depth

Custom application surfaces are flat: there are no custom shadow or motion tokens. Tonal backgrounds, whitespace, and thin dividers separate shell, rows, and content. Native Avalonia Fluent controls retain their theme behavior; this document does not override native popups, focus, or interaction states.

## Shapes

Buttons and text fields use gently rounded control corners. Lists and tables remain open rectangular rows separated by a single bottom rule (1). The build does not establish a reusable card component or decorative container silhouette.

## Components

### Buttons
Direct, compact action labels. Primary buttons use green with white text; secondary buttons use white with ink text. Both share the control radius, semibold text (13), and portable padding token. Inactive navigation and filters use transparent backgrounds. Hover, focus, pressed, and disabled behavior come from Avalonia FluentTheme, not custom state tokens.

### Inputs / Fields
Visible labels sit above native TextBox, NumericUpDown, ComboBox, DatePicker, and TimePicker controls. Text and numeric inputs have a minimum height (42); multiline text inputs use (88). The marketplace search field has its own height token and white background. Automation names mirror visible labels. Native controls own focus and disabled treatments.

### Navigation and Filters
Selected routes and category filters share pale sage fill and forest-green text; inactive items use muted text. Category filters are buttons, not a separate invented chip shape. Customer navigation is horizontal and restaurant navigation vertical.

### Lists and Tables
Marketplace food rows have generous vertical padding (22), with description, pickup, price, and action columns. Shared operational rows use minimum heights (40 header / 64 body), while compact dashboard tables use (36 / 48). All use the divider token. Avoid wrapping each datum in a separate card.

### Empty and Error States
Empty lists preserve a direct recovery path when applicable. Operational errors appear inline near the top of the content area; positive cart feedback uses green. Error colors vary between two source literals and are not promoted to a shared token until reconciled.

## Do's and Don'ts

### Do:
- **Do** use green for primary actions, selected navigation, and meaningful emphasis.
- **Do** preserve native focus and disabled affordances and pair fields with visible labels and automation names.
- **Do** use direct Indonesian task labels and recovery actions for empty or error states.

### Don't:
- **Don't** add promotional banners, generic slogans, decorative food drawings, or repeated statistic-card scaffolding.
- **Don't** restore the requested Praktikum UI / Data contoh sidebar footer.
- **Don't** replace native control states with undocumented custom behavior.

Source evidence: `src/Replate.Desktop/Ui.cs` (shared primitives), `MainWindow.cs` (font, palette, shell), `MarketplaceView.cs` (filters and rows), `DashboardView.cs` (aligned data tables), `CustomerViews.cs` and `RestaurantViews.cs` (full-page forms and states), and `Program.cs` (light FluentTheme). Product guardrails come from `PRODUCT.md`. The sidecar contains browser-panel translations of five native patterns; its HTML/CSS focus outlines, native-control approximations, and synthesized tonal ramps are preview metadata, not additional application tokens or verified native state styles. No HTML/CSS implementation or iOS/Android behavior is claimed.
