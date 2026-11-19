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

Quality bar: quiet, useful desktop UI. No glyph icons or marketing hero. Tables align
numeric columns; focus and disabled states use native affordances. Preserve domain
rules, action outcomes, local data visibility and realistic persistence boundaries.

ROUTES AND ROLES:
- Masuk (start screen): wordmark, Email, Kata sandi, primary Masuk, secondary
  Daftar akun baru, one demo-account hint line. Enter in Kata sandi submits; focus
  starts in Email. Failed sign-in shows the generic AccountService message inline at
  the top of the content and clears Kata sandi. No shell navigation is shown.
- Daftar: full page, 640-wide form. Role radio (Pelanggan / Pemilik restoran), Nama,
  Email, Kata sandi, Ulangi kata sandi. Pemilik restoran adds "Profil restoran":
  Nama restoran, Alamat, Telepon, Deskripsi, Jam buka, Jam tutup. Errors appear per
  field using AccountService keys plus confirmPassword. Success signs in, shows
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
