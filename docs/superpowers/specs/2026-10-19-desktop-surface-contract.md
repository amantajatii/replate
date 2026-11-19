# Replate desktop surface contract

Mode: Operate. Extend the established restrained green/neutral Avalonia interface.
This is an application-flow extension, not a replacement brand or open visual-world exercise.

THESIS: Customers reserve food; restaurant staff process surplus and pickup. Each
workspace must expose only that role's working navigation.

OWN-WORLD: Existing Replate wordmark, native controls, pale neutral surfaces, green
actions/selection, precise tables and direct Indonesian labels. No invented claims.

STORY: Masuk as customer -> browse -> food detail/cart -> reserve -> inspect order code.
Keluar -> Masuk as owner -> restaurant order -> confirm -> ready -> verify customer
code -> production rescue counts and dashboard update. Menu -> production -> listing
is the supply workflow. Accounts are local and prototype-grade.

FIRST VIEWPORT: Customer pages fill the window below a horizontal customer nav,
with no owner dashboard link. Owner pages use a dedicated sidebar with Dashboard,
Menu, Penawaran surplus, Produksi, Pesanan. All forms and cart are full pages.

FORM: Inherit established world and specified workspace topology. Code-first native
app; no comp or concept seed: this is a specified extension, not new identity work.
Desktop 1360x940; compact desktop 1040x720; native scrolling when needed. Inputs have
visible labels and semantic automation names. Empty/error states preserve recovery.

TEXT INPUTS (every screen): one shared app-level rule (`Ui.UseInputs`) gives every
single-line TextBox, including the one inside NumericUpDown, VerticalContentAlignment
Center and padding 10, 5.5, 6, 5.5 (top equals bottom), so typed text, placeholder,
password dots and the password-toggle share the field's vertical center (within 1 px).
Multiline TextBoxes (AcceptsReturn, e.g. Deskripsi) use VerticalContentAlignment Top
and padding 10, 10, 6, 5.5 (top equals left); text and placeholder start at the top
padding (within 1 px). Field heights stay 42 (multiline 88, marketplace search 44). Every TextBox has an Indonesian
PlaceholderText, "Masukkan ..." where it fits (search: "Cari menu atau restoran"), no
em dash and no exclamation mark. Placeholder color is `muted` (#59675D) at full
opacity (the Fluent 0.5 placeholder opacity is reset to 1); it disappears once the
field has content, with no other effect. `--verify-ui` (InputVerification) checks every
visible TextBox on each captured screen and on the auth surface at all four sizes.

Quality bar: quiet, useful desktop UI. No glyph icons or marketing hero. Tables align
numeric columns; focus and disabled states use native affordances. Preserve domain
rules, action outcomes, local data visibility and realistic persistence boundaries.

ROUTES AND ROLES:
- Masuk (start screen): Auth surface (below). Card holds the title Masuk, Email,
  Kata sandi, primary Masuk and secondary Daftar akun baru; one demo-account hint
  line sits below the card. Enter in Kata sandi submits; focus starts in Email.
  Failed sign-in shows the generic AccountService message inline under the card
  title, clears Kata sandi and returns it to hidden mode. No shell navigation is shown.
- Daftar: Auth surface (below). Card holds the title Daftar akun (no subtitle),
  the Daftar sebagai role buttons (Pelanggan / Pemilik restoran), Nama, Email, Kata
  sandi, Ulangi kata sandi. Enter in a single-line field submits; focus starts in Nama. Pemilik restoran adds "Profil restoran":
  Nama restoran, Alamat, Telepon, Deskripsi, then Jam buka and Jam tutup side by side,
  then the 24-hour helper line. Errors appear per field using AccountService keys plus
  confirmPassword (openingTime / closingTime for the hours). Submitting masks both
  password fields; an accepted submission also clears them. Success signs in, shows
  "Akun berhasil dibuat.", then routes by role. Kembali ke masuk returns to Masuk.
- After sign-in: Customer -> Jelajah (marketplace); RestaurantOwner -> Dashboard of
  the restaurant that owner manages.
- Keluar replaces Ganti area in the customer top nav and the restaurant rail. It
  signs out, clears the displayed page, and returns to Masuk.
- Every customer route (Jelajah, detail makanan, Keranjang, Pesanan saya, detail
  pesanan) requires a Customer; every restaurant route (Dashboard, Menu, Penawaran
  surplus, Produksi, Pesanan, editors, detail pesanan) requires the RestaurantOwner
  of the current restaurant. Order, menu and listing routes also require that the
  record belongs to the signed-in customer or restaurant. A signed-out user, a wrong
  role, or another owner's record is signed out and sent to Masuk without rendering
  the requested page.
- The restaurant rail shows the signed-in owner's restaurant name. A new restaurant
  with no menus, listings, production or orders renders every restaurant page with an
  empty state and a next step (Tambah menu, Buat penawaran).

AUTH SURFACE (Masuk and Daftar, implemented in `AuthViews.cs` as `AuthSurface`):
- A two-column Grid fills the page: the left canvas column and the right primary
  (#245B43) panel, full height. The first column width is set in pixels from the
  plate position (`AuthSurface.Layout`); the panel is about 28% of the width.
- Left column: canvas background. The wordmark replate. sits at the top (inset 32),
  left-aligned with the card, on a `canvas` band drawn above the scroll area. Below it
  a clip Border (ClipToBounds, from x 0 to plate left) holds the ScrollViewer, which
  scrolls vertically only (the owner registration form is taller than 720), so no
  scrolled content shows under the wordmark. The ScrollViewer starts 24 left of the
  card (inner padding for the shadow) and is 24 + card + 16 + 6 wide: the 6-wide
  scrollbar sits 16 beside the card's right edge. Its ScrollContentPresenter does not
  clip, so the card shadow is clipped only by the clip Border (at least 24 clear on
  both sides). The scrollbar stays at least 24 from the plate box. Wheel input over
  the empty canvas beside the form also scrolls it.
- Plate: `Assets/auth-hero.png` (1080x1080, transparent round cut-out) is loaded
  through `avares://Replate.Desktop/Assets/auth-hero.png` into a Canvas overlay that
  spans both columns, does not scroll and is not hit-testable. Centered vertically.
  The panel's left edge passes through the plate center: half over canvas, half
  over the panel (tolerance 0.02 of the diameter). No text over the photo; panel,
  overlay and image are hidden from assistive technology (AccessibilityView Raw).
- Placement: D = clamp(0.56 x height, 360, min(height, 0.56 x width)); boundary B =
  0.72 x width; plate left = B - D/2; card left L = max(48, 0.12 x width); target
  gap G = 0.08 x width; card = min(440, plate left - G - L), minimum 360, and the
  gap takes whatever the 440 card leaves (wide on large windows). If the card would
  be under 360, G shrinks first (minimum 46 = scrollbar gap 16 + scrollbar 6 + 24;
  reached only on narrow tall windows such as 1040x1080), then L (minimum 48), then B moves
  right, then D shrinks (minimum 320). The scroll area ends at plate left. The wordmark, the warning and the demo-account
  line share L, and the warning and demo line share the card width.
- Card: white surface, `rounded.card` (16), `elevation.card` shadow
  (`0 8 24 0 #1F23382C`), padding 32, fields 20 apart, no border. Hard rule: the
  plate box never intersects the card box and the gap is at least 24. At 1360x940
  the plate is 526, the card 440 at 163, the gap about 113 and the panel 28%; at
  1283x780 the plate is 437, the card 440 at 154 and the gap about 111; at 1920x1080
  the plate is 605, the card 440 at 230 and the gap about 410; at 1040x720 the plate
  is 403, the card 360 at 125, the gap about 62 and the panel 28%. The primary
  action spans the card; the secondary action (Daftar akun baru / Kembali ke masuk)
  spans it below. No Google sign-in, Ingat saya, or Lupa kata sandi.
- Titles Masuk and Daftar akun: 28, bold (`auth-title`).
- Kata sandi fields (Masuk, Kata sandi and Ulangi kata sandi on Daftar): a custom
  20x20 password-toggle button (16 icon, same 32 footprint via margin) inside the
  field, always visible, including when the field is empty. Eye and crossed-eye icons
  are Path geometry. Default ink icon, transparent, no border; hover (pointer over the
  icon only) turns the icon `leaf` (#4FA37E) with no fill, border or transition, and
  it returns to ink on leave; no pressed fill. Keyboard focus (Tab from the field) 2 px
  primary ring; Enter or Space toggles, and Enter on the toggle does not submit. Pressing it switches PasswordChar; its automation name switches
  between "Tampilkan kata sandi" and "Sembunyikan kata sandi". The native Fluent
  `revealPasswordButton` is not used.
- Daftar sebagai: two RadioButtons in one group, styled as role-buttons of equal
  width that together span the card (12 apart, `rounded.control`). Arrow keys move
  and select; automation names "Pelanggan" and "Pemilik restoran", group named
  "Daftar sebagai". Selected: primary fill, white text (hover ink fill). Unselected:
  sidebar fill, ink text, no border (hover selection fill, primary text).
- Jam buka / Jam tutup: time-field (white pill, native text field border, no shadow)
  holding a native hour ComboBox 00-23, ":", and a native minute ComboBox 00-59; no
  AM/PM and no TimePicker. Both sit in one row with visible labels above each.
  Automation names "Jam buka", "Jam buka, jam", "Jam buka, menit" (same for Jam
  tutup). The value stays a `TimeOnly`; validation keys stay openingTime /
  closingTime. The helper line "Untuk buka 24 jam, isi 00:00 sampai 23:59." stays.
  Hour and minute share the pill width and stay inside it at card width 360.
- Scrollbar (form scroll area of Masuk and Daftar only): a custom ScrollBar theme,
  6 wide, holding only a Track with one rounded `muted` Thumb; no track fill and no
  arrow RepeatButtons. Opacity 0 at rest, 1 while the ScrollViewer is pointer-over
  or during scrolling (class `auth-scrolling`, cleared 800 ms after the last
  offset change), with no transition. Wheel, touchpad, PageUp/PageDown and arrows
  scroll natively; `BringIntoViewOnFocusChange` keeps the field reached with Tab in
  view. Other screens keep the native Fluent scrollbar.
- Database warning (startup error only, Masuk): one compact block above the card at
  card width, existing warning colors, short message "Database belum tersambung.
  Pesanan belum dapat disimpan." with the error detail trimmed to two lines and the
  full detail in a tooltip.
- Hover: primary buttons change to ink background with white text; secondary
  buttons change to selection background with primary text. Pressed, focus and
  disabled states of those two remain native Fluent. No blur, glow, gradients or
  animation.
- Verified at 1360x940, 1040x720, 1283x780 and 1920x1080 by `--verify-ui` (AuthVerification):
  scrollbar within 24 of the card's right edge and not over it, scrollbar at least 24
  from the plate, shadow room, clean wordmark band, toggle hover color and keyboard.
