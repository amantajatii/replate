# Replate desktop surface contract

Mode: Operate. Extend the established restrained green/neutral Avalonia interface.
This is an application-flow extension, not a replacement brand or open visual-world exercise.

THESIS: Customers reserve food; restaurant staff process surplus and pickup. Each
workspace must expose only that role's working navigation.

OWN-WORLD: Replate bitmap logo (white on primary, green on light surfaces), native controls, pale neutral surfaces, green
actions/selection, precise tables and direct Indonesian labels. No invented claims.

STORY: Masuk as customer -> browse -> food detail/cart -> reserve -> inspect order code.
Keluar -> Masuk as owner -> restaurant order -> confirm -> ready -> verify customer
code -> production rescue counts and dashboard update. Menu -> production -> listing
is the supply workflow. Accounts are local and prototype-grade.

FIRST VIEWPORT: Customer pages fill the window below the customer top bar (below),
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
padding (within 1 px). Field heights stay 42 (multiline 88, top bar search 44). Every TextBox has an Indonesian
PlaceholderText, "Masukkan ..." where it fits (top bar search: "Mulai cari menu atau restoran"), no
em dash and no exclamation mark. Placeholder color is `muted` (#69736D) at full
opacity (the Fluent 0.5 placeholder opacity is reset to 1); it disappears once the
field has content, with no other effect. `--verify-ui` (InputVerification) checks every
visible TextBox on each captured screen and on the auth surface at all four sizes.

Quality bar: quiet, useful desktop UI. No marketing hero, except the Jelajah banner
(greeting and one call to action, below); icons only where they carry meaning (the
password-toggle eye, the top bar magnifier, cart and user icons, the five Jelajah
collection icons, and the back arrow). Tables align
numeric columns; disabled states use native affordances. Customer routes replace the
native focus adorner with the shared keyboard focus style (below); auth and restaurant
routes keep theirs. Preserve domain
rules, action outcomes, local data visibility and realistic persistence boundaries.

ROUTES AND ROLES:
- Masuk (start screen): Auth surface (below). Card holds the title Masuk, Email,
  Kata sandi, primary Masuk and secondary Daftar akun baru; nothing sits below the
  card. Enter in Kata sandi submits; focus starts in Email.
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
- Keluar sits in the customer profile menu and in the restaurant rail. It signs out,
  clears the displayed page and the top bar search, and returns to Masuk.
- Every customer route (Jelajah, detail makanan, Keranjang, Pesanan saya, detail
  pesanan) requires a Customer; every restaurant route (Dashboard, Menu, Penawaran
  surplus, Produksi, Pesanan, editors, detail pesanan) requires the RestaurantOwner
  of the current restaurant. Order, menu and listing routes also require that the
  record belongs to the signed-in customer or restaurant. A signed-out user, a wrong
  role, or another owner's record is signed out and sent to Masuk without rendering
  the requested page.
- The restaurant rail shows the green logo (32 high) at the top and the signed-in owner's restaurant name. A new restaurant
  with no menus, listings, production or orders renders every restaurant page with an
  empty state and a next step (Tambah menu, Buat penawaran).

CUSTOMER TOP BAR (every customer route, implemented in `CustomerTopBar.cs`):
- One persistent bar, 72 high, `primary` fill, side padding 32, no bottom rule and no
  shadow (the green against the canvas is the separation). No route tabs: Jelajah,
  Pesanan saya, Keranjang (n) and Keluar are no longer buttons in the bar.
- Left: white logo `Assets/logo-white.webp` (36 high, Uniform stretch, width from the
  bitmap ratio), a button named "Replate" that opens Jelajah. There is no text wordmark.
- Middle: search pill (`rounded.full`, 44 high, white, no border), 32 from the logo and from
  the icons, width between 320 and 720, centered in the remaining space; magnifier
  icon (`muted`) inside on the left; placeholder "Mulai cari menu atau restoran" in `muted`,
  typed text in `ink`. While focused it shows a 2 px white ring 2 outside the pill, never a
  dark border. It replaces the
  old Jelajah search field and label. The magnifier is a button named "Cari" (not a Tab
  stop). Typing alone changes nothing. Enter or the magnifier opens the search result
  page (`MainWindow.ShowSearch`); an empty or blank field does nothing. The field keeps
  the keyword on the result page; every other customer page opens with it empty.
- Right, 16 apart: cart icon button (44, transparent, white icon, `selection` tint on
  hover with no box; while Keranjang is open a white line 20 by 2 sits under the icon)
  with a white circular badge holding the portion count in `primary`, hidden at 0;
  profile circle (44, white fill, primary user icon, `selection` fill on hover).
  Automation names and tooltips "Keranjang" and "Akun". Contrast on the green: white
  about 6.4:1, `selection` about 5.3:1.
- Profile menu: Popup anchored to the circle (BottomEdgeAlignedRight, overlay layer,
  light dismiss). White card, `rounded.card`, `elevation.card`, padding 8, width 260, no
  border: muted name and email (not clickable), divider, Pesanan saya, Keluar. Click,
  Enter or Space on the circle opens it and focuses Pesanan saya; Up/Down move between
  items; Esc or a click outside closes it. Pesanan saya opens the orders page; Keluar
  signs out. Opened with the mouse, Pesanan saya takes focus without looking focused;
  opened with Enter or Space it shows the keyboard focus fill.
- Icons are Path geometry on a 24 grid, stroke 1.75, round caps and joins. The cart keeps
  its own line geometry (`CustomerTopBar.CartIcon`); the bold outline cart is only for the
  empty states. Every control is a Tab stop with the shared keyboard focus style (white ring on the bar).
- Verified by `--verify-ui` (TopBarVerification): primary fill without rule, white
  logo and icons with contrast at least 4.5 (logo) and 3 (icons), logo ratio equal to the
  bitmap ratio (within 1%), bar height unchanged at 72, search colors and
  focus ring, elements and names, removed tabs and old search, hover colors, badge
  colors and count, cart active line, typing without navigation, menu
  contents, keyboard and focus visibility, Keluar, and no overlap at 1040x720 and
  1920x1080 (search 320 to 720).

KEYBOARD FOCUS (customer routes, shared `Ui.FocusStyles` in `Ui.cs`):
- The native Fluent focus adorner (the black double box) is replaced by an empty
  adorner on every control in the customer shell, its profile menu and the customer
  cancellation dialog.
- Buttons show a 2 px ring, 2 outside the control, only on `:focus-visible` (Tab or
  arrow keys, never after a click): `primary` on light surfaces, white on the green top
  bar. Profile menu items show a `selection` fill with `primary` text instead of a ring.
  ComboBox uses a `primary` border on `:focus-visible`. Text fields keep their own
  focus border, except the top bar search (white ring above).
- No blur, glow, gradient or animation, except the Jelajah collection card hover
  (scale 1.03, 120 ms ease-out, see JELAJAH) and the compact primary and danger button
  hover (scale 1.03, 120 ms ease-out, see PRIMARY BUTTONS and DANGER BUTTON).

PRIMARY BUTTONS (whole app, shared style `Ui.UsePrimaryButtons` in `Ui.cs`):
- `Ui.Button(..., primary: true)` adds the `primary` and `compact` classes;
  `Ui.FullWidth` removes `compact` and stretches the button (Masuk, Daftar).
- Fill and text/icon color are set on `/template/ ContentPresenter#PART_ContentPresenter`
  for every enabled state, so the Fluent PointerOver/Pressed foreground (black text) never
  wins. Normal: `primary` fill, white text, flat. Hover: `selection` fill, `primary`
  text (about 5.3:1), shadow `elevation.button-primary-hover` (`0 4 12 0 #4D1F6B4F`).
  Pressed: `divider` fill, `primary` text (about 4.9:1), same shadow. Disabled: unchanged,
  no hover or press effect.
- Compact buttons (Tambah, Tambah ke keranjang, page actions, the owner's Tambah
  menu and Buat penawaran) scale to 1.03 from the center as a render transform on hover,
  ZIndex 1, without moving layout or being clipped. Full-width buttons do not scale. The
  Fluent press shrink is off for primary buttons. Fill, text, shadow and scale share
  120 ms ease-out. The `:focus-visible` ring is unchanged and stays on top.
- The restaurant owner screens use the same `Ui.Button` primary style, so their primary
  buttons change the same way. Secondary buttons are unchanged.
- Hover follows the shape: fills live on `PART_ContentPresenter` with the button's
  `CornerRadius`, and the shared button theme sets `ClipToBounds` false so the hover
  shadow and focus ring are not cut to the square layout box. `Ui.Round(button, size)`
  makes Width = Height and `CornerRadius` = size / 2 and drops `compact`.
- Round buttons (stepper plus and minus, class `round`) and icon buttons (dialog close,
  Keranjang trash, class `icon`) have no hover shadow and no scale: hover is a
  `selection` circle with a `primary` icon, press a `divider` circle. Only
  rounded-rectangle primary buttons carry the hover shadow (and, compact only, scale).
- Verified at 1040x720 and 1920x1080 by `--verify-ui` (UiVerification): normal, hover,
  pressed and disabled colors with contrast at least 4.5, the hover shadow, compact scale
  1.03 (within 0.005), no scale on full-width buttons, unchanged neighbour layout, no
  clipping and no horizontal scrolling. A pixel check (`UiVerification.HoverShape`) forces
  `:pointerover` on Tambah, Tambah ke keranjang, the dialog plus, minus and close, and the
  Keranjang plus, minus and trash, renders the window with `RenderTargetBitmap`, and
  requires the corners of each bounding box to match the surface behind the button
  (rendered with the button hidden), the inside to show the hover fill, round buttons
  to be square with half-size radius, no shadow or scale on round and icon buttons,
  and the compact shadow to show below the button inside every clipping container.
- Text link (`CollectionView.AddTextLinkStyles` and `CreateTextLink`, class `text-link`):
  every "Kembali ke Jelajah" (collection, search results, Keranjang, Pesanan saya), "Kembali
  ke Pesanan saya", and, without the arrow, "Lihat detail" on order cards. Transparent fill and
  border in every state, set on the Button and on PART_ContentPresenter so Fluent adds no
  fill; rest and pressed `primary`, hover `primary-hover` #2F7D5F (about 4.7:1 on the
  canvas); no shadow, scale (Fluent's pressed scale is reset), or underline; at least 32
  high; the shared focus ring. `UiVerification.TextLinks` checks it at 1040x720 and
  1920x1080 (CustomerOrdersVerification for the order links): the pixels around the arrow and text match the canvas (rendered with the
  link hidden) on hover and press, the label repaints on hover, the colors and contrast,
  the height, `:focus-visible`, and no horizontal scrolling.

JELAJAH (customer marketplace, `MarketplaceView.cs` and `MarketplaceBanner.cs`):
- One vertical ScrollViewer under the top bar, no horizontal scrolling. Content is at
  most 1200 wide, side inset 32, centered on wide windows. Order: banner, collection cards,
  the "Jelajahi Menu" title row, listing cards, sample-data note. No result counter, no
  "Makanan tersedia" or "Semua restoran" headings.
- Rhythm (shared constants `Ui.Spacing`): 24 from the top bar to the banner, from the
  banner to the collection cards and from the collection cards to the title; 16 from the title
  to the first listing card (equal to the gap between cards); 32 below the page. Side
  insets and the 16 between collection cards are unchanged. Jelajah always lists every
  available listing in the default order.
- Title: "Jelajahi Menu", 32 bold (at least 30, verified), `ink`, left-aligned with the
  content edge, always shown, including when the list is empty. The add-to-cart notice
  sits right-aligned on the same row. Jelajah has no restaurant picker.
- Banner: `rounded.card`, no border or shadow, centered in the content column,
  width = content width (left and right edges aligned with the collection row and listing
  cards at every size), height = width x bitmap height / bitmap width (4344 by 1280), so
  `Assets/marketplace-hero.webp` is shown whole with Uniform stretch, never cropped or
  distorted (about 288 high at 1040x720, 354 at the 1200 content maximum). Overlay text: "Halo, {name}."
  (bold primary, 0.0287 x width clamped 22 to 32, one line, ellipsis) and "Yuk,
  Selamatkan Makanan Lewat Setiap Pesanan!" (muted, 0.0184 x width clamped 15 to 20, at
  most two lines). Text inset 40, vertically centered above the cabinets (87.8% of the
  height), right edge at most 45% of the banner width (the nearest illustration object
  starts at 46.2%).
- Collection cards: Termurah, Segera berakhir, Makanan utama, Roti & kue, Buah & sayur;
  equal widths in one row, 16 apart, height 96, `rounded.card` with the
  `elevation.card-primary` shadow (`0 6 18 0 #4D1F6B4F`, primary at about 30%) and no
  border, filled `primary` with a white 24 line icon above a white 14 label (about
  6.4:1); no Semua and no selected state. Each card is a link to its collection page
  (`MainWindow.ShowCollection`). Hover: `selection` fill with `primary` icon and label
  (about 5.3:1), shadow `elevation.card-primary-hover` (`0 10 24 0 #591F6B4F`), scale
  1.03 from the center as a RenderTransform (layout unchanged) and ZIndex 1. Press:
  `divider` fill with `primary` text (about 4.9:1). Color, scale and shadow transition
  over 120 ms ease-out and back on leave; this is the only allowed animation. Reduced
  motion is not read. Tab, Enter and Space work with the shared 2 px primary focus
  ring, 2 outside the card on the canvas; it scales with the card and stays on top.
- Listing cards: card style, padding 20, 16 apart, same columns and content as the old
  rows; no row dividers (`ListingFeed.cs`, shared with collection pages). Tambah, the
  menu name, or a click anywhere else on the card opens the food dialog; Tambah never
  adds directly. Empty state "Belum ada menu
  tersedia", 16 under the title.
- Verified by `--verify-ui` (MarketplaceVerification) at 1040x720, 1360x940 and
  1920x1080: banner ratio equals the bitmap ratio (within 1%), banner left and right
  edges equal the collection row and listing cards (within 1 px), centered text inside 45%
  that does not intersect the illustration; no "menu tersedia" text; the bold,
  left-aligned title; the three 24 gaps equal (within 1 px) and the title to first card
  gap 16; collection cards in primary with white icon and label (contrast at least
  4.5 for text and 3 for icons) and the primary-tinted resting shadow; a selection
  hover fill that differs from primary with primary icon and label (at least 4.5), a
  divider press fill, hover scale 1.03 (within 0.005) with 120 ms ease-out
  transitions, unchanged position and size of the other cards, ZIndex above the
  neighbours, no clipping of the hovered card or its shadow and no overlap with
  neighbours, the deeper primary hover shadow, the focus ring on top while hovered,
  and the return to rest (checked on the first and last card in the window and at
  1040x720 and 1920x1080 with no horizontal scrolling); height 96, equal widths and
  16 gaps; focus rings on collection cards and Tambah without the native adorner; no
  pick-one state or Hapus filter.

FOOD DIALOG (customer only, `FoodDialog.cs`, opened by `MainWindow.ShowFoodDialog`):
- An overlay inside the main window (no OS window), one at a time, above the top bar:
  a flat scrim (`scrim`, ink at 40%, no blur) and a centered white card (760 wide,
  radius 16, card shadow, no border, at least 32 from every edge, internal vertical
  scroll when short, the bar always visible).
- Content: photo 300 by 300 or the large "Foto belum tersedia" box; name, "Restoran ·
  Kategori", description, "Pengambilan HH:mm–HH:mm", "N porsi tersisa", rescue price
  with "/porsi", struck normal price, "Hemat N%"; round primary minus and plus with the
  quantity (1 to stock minus cart quantity for that listing). At the limit plus is
  disabled with "Maksimal N porsi"; nothing left: "Sudah di keranjang dengan jumlah
  maksimal" and Tambah ke keranjang disabled; expired or sold out: "Penawaran tidak
  tersedia" and disabled. Bottom bar on `selection`: total (rescue price x quantity)
  and the primary "Tambah ke keranjang".
- Tambah ke keranjang calls `DemoSession.AddToCart` with the quantity (through
  `ListingFeed.Add`). Success closes the dialog, updates the badge and the existing
  notice, and stays on the page. Another restaurant in the cart: the existing message
  shows inside the dialog above the bar, nothing is added, the dialog stays open.
- Closes with the X button, Esc, or a click on the scrim, without touching the cart.
  Opening and closing never write state. Focus moves into the dialog, Tab cycles
  inside it, and closing returns focus to the trigger; `:focus-visible` rings only.
- Any navigation (Render or a public page) dismisses an open dialog.
- Verified by `--verify-ui` (FoodDialogVerification): Tambah opens without adding;
  content; plus/minus and total; limit and disabled plus; adding the chosen quantity
  and closing; a menu from another restaurant joins the same cart; expired offer; Esc, X and
  scrim close without adding; no state write on open/close; focus trap and return
  (pointer and keyboard); collection and search pages; centered and not cut off at
  1040x720 and 1920x1080.

KERANJANG (customer only, `CartView.cs`, opened by `MainWindow.ShowCart` with the
customer role guard):
- Same top bar; in the Jelajah content column (at most 1200, centered, inset 32, edges
  aligned with Jelajah). Rhythm from `Ui.Spacing`: 24 from the top bar, then 16 between
  the "Kembali ke Jelajah" link (the collection page link, `CollectionView.CreateBackLink`),
  the header ("Keranjang", 32 bold ink; "Pesanan dari {restoran}" 14 muted, 8 under it,
  only with items; "{n} restoran" when the cart holds several restaurants) and the
  content.
- Restaurant groups (`CartView.Group`, from `Cart.GroupByRestaurant`): one per
  restaurant in the order its first item was added, 32 apart (`Ui.Spacing.Page`, from
  one group's footer to the next heading). Each group: a heading aligned with the card
  edges (only the restaurant name 18 bold ink; no subtotal, no button), its item cards
  16 under it and 16 apart, then 16
  under the last card a 1 px `divider` line as wide as the group, and 16 under the line
  the footer row: on the left, aligned with the card edge, "Total" (12 muted) above the
  group total (28 bold primary, Rupiah); on the right, aligned with the card edge and
  vertically centered with the total, the compact primary "Pesan" button (standard
  height). When the group is not valid (`CartRestaurantGroup.Validate()` fails) only its
  own Pesan is disabled (divider fill, muted text) and a 12 muted line, 8 under the
  footer row and aligned left, reads "Hapus penawaran yang tidak tersedia untuk
  melanjutkan." for unavailable items, otherwise "Periksa jumlah pesanan untuk
  melanjutkan.".
- One column: the groups use the full content width (edges aligned with Jelajah),
  vertical scroll only, 24 inner inset so shadows are not cut; the list ends 32 under
  the last group (`Ui.Spacing.Page`). No summary card; no horizontal scroll at 1040x720.
- Item card (white, radius 16, card shadow, no border, padding 20, 16 apart): photo or
  placeholder (72), name (18 semibold), "Restoran · Kategori", "Pengambilan
  HH:mm–HH:mm", "Rp… / porsi"; the shared `QuantityStepper` (same component as the food
  dialog; minimum 1, maximum the listing's remaining stock, plus disabled at the limit);
  subtotal (18 bold); a 36 trash icon button (24 Path, stroke 1.75, transparent and
  muted at rest, a selection circle with a primary icon on hover, no box, shadow or
  scale, name and tooltip "Hapus {menu}", shared focus ring).
  Quantity changes go through `DemoSession.ChangeQuantity` and update the subtotal, the
  group total, the Pesan state and the badge in place; Hapus calls `ChangeQuantity(id, 0)` and rebuilds.
- Unavailable item (expired, sold out, inactive menu): the card stays, the unit price
  line reads "Penawaran tidak tersedia. Hapus untuk melanjutkan.", the stepper is
  disabled, Hapus stays active.
- Checkout confirmation (`CheckoutDialog.cs`, on the shared `ModalDialog` overlay with
  the food dialog: scrim, centered white card radius 16, card shadow, no border, width
  520, at least 32 from every window edge; Esc, a click on the scrim, the X, or "Batal"
  closes it; Tab cycles inside the card; closing returns focus to the Pesan that opened
  it). Content, 16 apart: "Konfirmasi pesanan" (22 bold) with the X; "Pesan dari
  {restoran}?"; one row per item of that restaurant only (menu name, "{jumlah} x
  {harga satuan}" 13 muted, item subtotal right); "Pengambilan HH:mm–HH:mm"; divider;
  "Total" with the group total (24 bold primary, Rupiah); bottom row: secondary "Batal" and
  primary "Pesan sekarang". Pesan sekarang calls `DemoSession.CheckoutAsync(restaurantId)`
  and is disabled while it runs (no double order). On success the pop up closes: with
  items left Keranjang is rebuilt (groups, badge) with "Pesanan dari {restoran} berhasil
  dibuat."; with an empty cart the new order opens with "Pesanan berhasil dibuat.". On
  failure the pop up stays open with "Pesanan belum dapat dibuat. Periksa koneksi
  PostgreSQL lalu coba lagi." above the buttons and nothing changes.
- Empty cart: back link and title only, then centered: 80 primary bold outline cart icon
  (`Ui.EmptyIcon`, stroke 8% of the width, round caps and joins, no fill),
  "Keranjangmu masih kosong", "Mulai jelajahi menu dan selamatkan makanan hari ini." and
  the primary "Jelajahi menu" button (opens Jelajah). No groups.
- Verified by `--verify-ui` (CartVerification): header, back link and rhythm; restaurant
  groups in first-added order, 32 apart from footer to heading; a heading with only the
  name and note, 16 above the cards; a divider 16 under the last card as wide as the
  group and the footer 16 under it, with the large primary total (the sum of the item
  subtotals) on the left card edge and Pesan on the right card edge, vertically
  centered; the reason left under the footer; every footer fully visible; no summary card; the payment note under the list; item card content and
  style; plus and minus update the subtotal, group total and badge; plus disabled at
  the stock limit; Hapus; an unavailable item disables only the Pesan of its group with
  the reason; invalid quantity reason; the confirmation lists only that restaurant's
  items and total; Batal, Esc, X and the scrim close it without an order; focus trapped
  and returned to Pesan; a failed save keeps it open with the message; Pesan sekarang is
  disabled while saving and creates one order, reduces only that restaurant's stock and
  keeps the other items with the notification; empty state and its button; edges
  aligned with Jelajah, no overlap, no horizontal scroll and the confirmation fully
  visible at 1040x720 and 1920x1080. UiVerification checks that ordering the only
  restaurant opens the new order with "Pesanan berhasil dibuat." and an empty badge, and
  that two restaurants are ordered one at a time, staying on Keranjang after the first,
  and that Pesan and the total sit in the group footer, not the heading, with the shared
  primary button states.

PESANAN SAYA AND ORDER DETAIL (customer only, `OrdersView` and `OrderDetailView`, customer
parts in `CustomerOrdersView.cs`, opened by `MainWindow.ShowCustomerOrders` and
`MainWindow.ShowOrder(order, false)` with the unchanged customer role and ownership guards; the
restaurant order pages keep their table and detail layout; see PESANAN RESTORAN):
- Pesanan saya: same top bar; in the Keranjang content column (at most 1200, centered,
  inset 32). Rhythm from `Ui.Spacing`: 24 from the top bar, then 16 between the shared
  "Kembali ke Jelajah" link, the title "Pesanan saya" (32 bold ink), the status filter
  and the list. Only the list scrolls (vertical only, 24 shadow inset, 32 under the last
  card).
- Status filter: four pill buttons in one row, 8 apart: Semua (default), Aktif, Selesai,
  Dibatalkan; pick one. 36 high, radius 18, no border; rest transparent with muted text,
  hover sidebar fill with ink text, pressed divider; selected selection fill with semibold
  primary text. Fills are painted on the pill only: no box outside it, no shadow, scale or
  transition; shared focus ring. Mapping of `OrderStatus`: Aktif = Reserved, Confirmed,
  ReadyForPickup; Selesai = Completed; Dibatalkan = Cancelled. Hidden with no orders.
- Order card (white, radius 16, card shadow, no border, padding 20, 16 apart), one per
  order from `DemoSession.CustomerOrders` (newest first): restaurant name (18 bold) left
  and `OrderStatusBadge` right; "Dipesan dd MMM yyyy, HH:mm · Pengambilan dd MMM,
  HH:mm–HH:mm" (13 muted); up to three rows "menu" with "{jumlah} x" right, then
  "+{N} menu lainnya" (13 muted); on Aktif orders only "Kode ambil" with the code (18
  bold, fixed-width font), no copy button; a divider; "Total" (12 muted) above the order
  total (22 bold primary) on the left and the "Lihat detail" text link on the right (its
  text on the content edge, centered with Total), which opens `ShowOrder(order, false)`.
- Empty: no orders at all shows an 80 primary bold outline receipt icon
  (`Ui.EmptyIcon`, the same size and stroke as the empty cart icon), "Belum ada pesanan",
  "Pesananmu akan muncul di sini setelah kamu memesan." and the primary "Jelajahi menu"
  (opens Jelajah), with no filter row; a filter without orders shows "Tidak ada pesanan
  di sini" and, 24 under it and centered, the compact primary "Lihat semua pesanan" (the
  same shared button as "Jelajahi menu" on the empty cart; selects Semua).
- Checkout is unchanged: ordering the last restaurant opens the new order with
  "Pesanan berhasil dibuat."; every restaurant order is its own card at the top of the
  list.
- Detail pesanan: same content column, one vertical scroll. The shared text link
  "Kembali ke Pesanan saya" (opens Pesanan saya); "Detail pesanan" (32 bold) with the
  status badge 16 beside it and the status sentence 8 under it; two columns 24 apart
  (`3*,2*`, about 60/40, never stacked: 1040 already fits). Cards: white, radius 16, card
  shadow, no border, padding 24, 16 apart. Left "Item pesanan": per item the name,
  "{jumlah} x {harga satuan}" and the subtotal right, a divider, then "Total" above the
  total (28 bold primary, left, as in Keranjang). Right top "Kode ambil": the code (36
  bold, fixed-width font, primary), the existing copy button beside it (now the shared 44
  icon button; same tooltip, name, clipboard text and notifications) and "Tunjukkan kode
  ini di restoran."; the copy rule is unchanged (not cancelled and a non-empty code). A
  cancelled order still shows the code, without the copy button or the sentence. Right
  below: restaurant name and address, Pengambilan, Dipesan, Nomor pesanan. Aktif orders
  keep "Batalkan pesanan" (DANGER BUTTON) with the unchanged confirmation window under it.
- Verified by `--verify-ui` (CustomerOrdersVerification): rhythm, title and content column;
  the empty state and its button; filter-empty states and the primary Lihat semua pesanan
  (`UiVerification.FilterEmptyButton`: filled, at least 40 high, primary hover, 24 under
  the title, no horizontal scroll at 1040x720 and 1920x1080); one card per
  order with name, badge, three item rows and "+N menu lainnya", the right total and Lihat
  detail; the pickup code only on Aktif cards; each filter follows the mapping, the
  selected pill uses the selection token, the pills share one row and the focus ring;
  newest first; two restaurants as two cards; hover follows the shape on the pills, Lihat
  detail and the copy button; the shared text links; the detail columns, code card, copy
  rule, item rows, subtotals and total; no horizontal scroll and nothing cut off at
  1040x720 and 1920x1080. UiVerification checks that a two-restaurant checkout lists both
  orders as separate cards on top; PickupCopyVerification keeps the clipboard checks.

PESANAN RESTORAN (restaurant only, `OrdersView` restaurant part in `CustomerViews.cs`,
opened by `MainWindow.ShowRestaurantOrders`):
- "Status pesanan" (14 semibold) above the status filter, 8 apart; the order table 24 under
  the filter. The filter is pill buttons, one per status in the former dropdown order:
  Aktif (default), Semua, Selesai, Dibatalkan; pick one, same mapping as before.
- Pills share `Ui.Pill` and `Ui.PillStyles` with Pesanan saya: 36 high, radius 18, padding
  0 by 16, 14 text, 8 apart, no shadow, scale or transition, shared focus ring. Palette
  `Ui.SolidPills`: rest white with a 1 px divider border and ink text; hover selection fill
  with primary text; pressed divider; selected primary fill with semibold white text.
- A wrap panel moves pills to the next line when narrow; no horizontal scroll at 1040x720
  and 1920x1080. Each pill is named by its label; Tab, Enter and Space work.
- Verified by `--verify-ui` (`UiVerification.RestaurantStatusPills`): one named pill per
  option in order, Aktif selected first, at least 36 high and fully rounded, selected and
  rest colors, 8 between label and pills, every pill filters the list to the mapping with
  one selected pill, no horizontal scroll at 1040x720 and 1920x1080. Other restaurant
  screens (Dashboard period, Menu category, Penawaran surplus menu) keep their native
  ComboBox; the Produksi menu is a ComboBox restyled as a Replate field (CATATAN PRODUKSI).

CATATAN PRODUKSI (restaurant only, `ProductionView` in `RestaurantViews.cs`):
- Heading, subtitle, the "Ringkasan tanggal terpilih" table, saving, validation, the
  default menu and date, and every number are unchanged; only the editor changes.
- One form card under the heading (24): white, 1 px divider border, radius 16, padding
  24, no shadow, in the form area (at most 860, left-aligned with the heading). Inside,
  24 apart: the fields, a divider rule, and the footer.
- Fields use the shared Replate field (`FieldFrame` in `Ui.cs`, class `field`): white,
  1 px divider border, radius 12, height 48, text inset 16, ink text, muted placeholder.
  Hover: primary-500 border. Focus (keyboard or click): primary border; keyboard focus
  also shows the shared 2 px primary ring 2 outside. No native black focus box. An
  invalid field keeps a danger border with its inline error under it.
- `FieldGrid`: Menu and Tanggal produksi on row one, Diproduksi (porsi) and Terjual
  reguler (porsi) on row two; two equal columns 16 apart, rows 16 apart; one column when
  the window is 900 wide or less. Labels 14 semibold, 8 above the field.
- Menu (`Ui.SelectField`): own ComboBox theme with a thin outline chevron (muted, stroke
  1.75 on a 24 grid); the list is a white sheet with a 1 px divider border, radius 12,
  padding 4, at most 280 high; options hover and keyboard focus selection fill with
  primary text, selected option semibold.
- Tanggal produksi (`DateField`): one field reading "22 November 2026" (`d MMMM yyyy`,
  id-ID) with an outline calendar icon on the right; click, Enter, Space, F4 or Alt+Down
  opens the native Calendar in the same sheet style (36 round days, selected day primary
  with white text, today primary outline, hover selection). Picking a day closes it;
  Enter or Esc closes it. Latest selectable day: 31 December of the current year (the
  old DatePicker year limit). Default today.
- Diproduksi and Terjual reguler (`NumberStepper`): round 36 stepper buttons
  (`QuantityStepper.StepButton`) 12 left and right of a centered number field; native
  spinner hidden; minus disabled at the minimum and plus at the maximum. Typed text and
  the existing validation (including sold not above produced) are unchanged.
- Footer: left "Terselamatkan" (12 muted), the number (28 bold primary) and " porsi"
  (14 muted), and "Bertambah setelah kode pengambilan diverifikasi." (14 muted); right,
  bottom-aligned with the card content edge, the compact primary "Simpan catatan".
- Verified by `--verify-ui` (`UiVerification.ProductionFields`) at 1040x720 and 1920x1080:
  field border, radius and height, hover border, focus border and keyboard-only ring, no
  native focus box, select chevron, sheet, option states, Indonesian date text, calendar
  open and pick, stepper changes and limits, inline sold error with danger border, equal
  columns and gaps, one column at 900, form card, summary, button position, 24 under the
  heading, no horizontal scroll, no cut text.

DANGER BUTTON (whole app, registered with the primary style in `Ui.cs`):
- `Ui.DangerButton(text, action)` adds the `danger` and `compact` classes, with the
  compact primary shape (radius 6, padding 11 by 16, semibold 13). Used only by
  "Batalkan pesanan" (customer detail and, through the same `CancelButton`, the
  restaurant owner's order detail). Text and cancellation logic are unchanged.
- Tokens: `danger` `#B3261E`, `danger-tint` `#FBE4E1`, `danger-pressed` `#F6D3CE`,
  `elevation.button-danger-hover` `0 4 12 0 #4DB3261E`.
- Fill and text set on `PART_ContentPresenter` in every enabled state. Normal: `danger`
  fill, white text (about 6.5:1), flat. Hover: `danger-tint` fill, `danger` text (about
  5.4:1), the red hover shadow, scale 1.03 from the center, ZIndex 1, 120 ms ease-out.
  Pressed: `danger-pressed` fill, `danger` text (about 4.7:1), the same shadow. Disabled:
  the primary disabled treatment, no hover, press, shadow or scale. Hover follows the
  shape; the `:focus-visible` ring is unchanged and stays on top.
- Not applied to "Ya, batalkan pesanan", Kembali, Batal or any Hapus action.
- Verified at 1040x720 and 1920x1080 by `--verify-ui` (UiVerification.DangerButtons on
  the customer order detail): class and shape, normal, hover and pressed colors with
  contrast at least 4.5, the red hover shadow, scale 1.03 (within 0.005), unchanged
  neighbour layout, no clipping of the button or its shadow, the focus ring on top,
  disabled without effect, the `HoverShape` pixel check, and no horizontal scrolling.

COLLECTION AND SEARCH PAGES (customer only, `CollectionView.cs`):
- One view for the five collections (`ShowCollection(name)`) and for search results
  (`ShowSearch(query)`). Both use the customer role guard: any other role is signed out
  and sent to Masuk; an unknown collection name is ignored. Cart and session survive.
- Same top bar; in the Jelajah content column (at most 1200, inset 32), no banner:
  "Kembali ke Jelajah" link (16 left arrow, stroke 1.75, 8 from the 14 text, centered,
  at least 32 high, primary, transparent in every state; hover only turns the arrow and
  text primary-hover #2F7D5F, about 4.7:1 on the canvas, pressed returns to primary; no
  fill, border, shadow, scale or underline; primary focus ring), then the title
  (32 bold ink, one line), then on collection pages with listings an intro sentence 8
  under it (16, muted, wrapping; exact copy in DESIGN.md), then listing cards 16 below.
  Search results and empty states have no intro. Rhythm from
  `Ui.Spacing`: 24 from the top bar, 16 to the title, 16 to the first card, 32 below.
- Titles: the card label exactly, or "Hasil untuk {kata}" with the keyword as typed
  (trimmed), cut with an ellipsis when too long.
- Termurah: every available listing by RescuePrice ascending; Segera berakhir: by
  PickupEnd ascending; Makanan utama, Roti & kue, Buah & sayur: that category in the
  default order. Search splits on spaces; every word must match the menu name,
  description, restaurant name or category, case-insensitive; "vegetarian" also finds
  Buah & sayur.
- Category Buah & sayur replaces Vegetarian everywhere in the UI and the menu form. The
  category is a string in state.json: "Vegetarian" loads as Buah & sayur in memory, load
  never writes, the next save stores the new name, and the seed uses it.
- Empty: collection "Belum ada menu di sini" with a short line; search "Tidak ada menu
  yang cocok" with "Coba kata kunci lain."; both with a "Kembali ke Jelajah" button.
- The logo, Kembali ke Jelajah, Keranjang and Pesanan saya clear the search field.
- Verified by `--verify-ui` (CollectionVerification): each card opens its page with the
  right title; ordering and category filtering; search by menu name, restaurant name
  and words out of order; unknown words empty state; blank search stays; back link,
  logo, cart and orders clear the field; Tambah opens the food dialog, which adds, also
  from a second restaurant into the same cart; rhythm
  and alignment; no horizontal scrolling at 1040x720 and 1920x1080; role guards.

AUTH SURFACE (Masuk and Daftar, implemented in `AuthViews.cs` as `AuthSurface`):
- A two-column Grid fills the page: the left canvas column and the right primary
  (#1F6B4F) panel, full height. The first column width is set in pixels from the
  plate position (`AuthSurface.Layout`); the panel is about 28% of the width.
- Left column: canvas background. The green logo `Assets/logo-green.webp` (44 high, Uniform
  stretch, width from the bitmap ratio) sits at the top left (inset 32 from the top, at the left margin L), on a `canvas` band drawn above the scroll area; it does not affect the card position. Below it
  a clip Border (ClipToBounds, from x 0 to plate left) holds the ScrollViewer, which
  scrolls vertically only (the owner registration form is taller than 720), so no
  scrolled content shows under the logo. The ScrollViewer starts 24 left of the
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
  right, then D shrinks (minimum 320). The scroll area ends at plate left. The logo starts
  at L. The card (with the warning, which shares its width and left edge) is then centered
  horizontally between x 0 and plate left (at least 32 from both), and vertically on the
  window height: its center is the window center while its top block stays at least 24
  below the logo band; otherwise it sits 24 below the band and the form scrolls.
  UiVerification checks both centers within 2 px at 1920x1080 and 1283x780 and that the
  card never overlaps the logo at 1040x720.
- Card: white surface, `rounded.card` (16), `elevation.card` shadow
  (`0 8 24 0 #1F202522`), padding 32, fields 20 apart, no border. Hard rule: the
  plate box never intersects the card box and the gap is at least 24. At 1360x940
  the plate is 526, the card 440 at 138, the gap about 138 and the panel 28%; at
  1283x780 the plate is 437, the card 440 at 133 and the gap about 133; at 1920x1080
  the plate is 605, the card 440 at 320 and the gap about 320; at 1040x720 the plate
  is 403, the card 360 at 94, the gap about 94 and the panel 28%. The primary
  action spans the card; the secondary action (Daftar akun baru / Kembali ke masuk)
  spans it below. No Google sign-in, Ingat saya, or Lupa kata sandi.
- Titles Masuk and Daftar akun: 28, bold (`auth-title`).
- Kata sandi fields (Masuk, Kata sandi and Ulangi kata sandi on Daftar): a custom
  20x20 password-toggle button (16 icon, same 32 footprint via margin) inside the
  field, always visible, including when the field is empty. Eye and crossed-eye icons
  are Path geometry. Default ink icon, transparent, no border; hover (pointer over the
  icon only) turns the icon `primary-500` (#59AC87) with no fill, border or transition, and
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
- Hover: primary buttons follow PRIMARY BUTTONS (selection background with primary
  text and the soft primary shadow, full width so no scale); secondary buttons change
  to selection background with primary text, and their pressed, focus and disabled
  states remain native Fluent. No blur, glow or gradients.
- Verified at 1360x940, 1040x720, 1283x780 and 1920x1080 by `--verify-ui` (AuthVerification):
  scrollbar within 24 of the card's right edge and not over it, scrollbar at least 24
  from the plate, shadow room, clean logo band, green logo with bitmap ratio, toggle hover color and keyboard.
