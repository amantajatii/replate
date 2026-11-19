# Replate Desktop

Aplikasi Avalonia UI 12.1.4 / .NET 10 dengan area pelanggan dan pemilik restoran
terpisah. Seluruh layar, keranjang, detail pesanan, dan formulir memakai halaman
penuh. Pelanggan menggunakan navigasi atas; restoran memiliki sidebar operasional.

## Menjalankan

Dari root repository:

```bash
dotnet restore Replate.slnx
dotnet run --project src/Replate.Desktop
```

Pilih **Jelajahi makanan** atau **Buka area restoran**. **Ganti area** kembali ke
pemilihan area. Sebelum layar login tersedia, kedua tombol masuk otomatis dengan
akun demo: pelanggan demo untuk area pelanggan dan pemilik Dapur Bumi untuk area
restoran. Operasi keranjang dan reservasi hanya untuk pelanggan yang masuk;
operasi area restoran hanya untuk pemilik restoran tersebut.

## Akun demo

Autentikasi bersifat lokal tingkat prototype. Akun disimpan di `state.json` dengan
kata sandi yang di-hash PBKDF2-HMAC-SHA256; kata sandi polos tidak disimpan. Sesi
masuk tidak diingat antar peluncuran. Semua akun demo memakai kata sandi `replate123`.

| Peran | Email | Restoran |
| --- | --- | --- |
| Pelanggan | `pelanggan@replate.demo` | — |
| Pemilik restoran | `dapur-bumi@replate.demo` | Dapur Bumi |
| Pemilik restoran | `warung-sambal-ijo@replate.demo` | Warung Sambal Ijo |
| Pemilik restoran | `roti-pagi-kotabaru@replate.demo` | Roti Pagi Kotabaru |

Restoran tanpa pemilik (misalnya hasil migrasi data versi 1) otomatis mendapat akun
pemilik demo dengan email dari nama restoran, contoh `dapur-bumi@replate.demo`.
Akun demo tidak digandakan dan tidak menimpa akun yang sudah ada.

## Halaman

| Area | Halaman | Fungsi |
| --- | --- | --- |
| Awal | Pilih area | Memisahkan konteks pelanggan dan restoran |
| Pelanggan | Marketplace | Cari, filter, lihat harga, stok, jadwal pengambilan |
| Pelanggan | Detail makanan | Deskripsi, harga, jumlah porsi, tambah ke keranjang |
| Pelanggan | Keranjang | Ubah jumlah, hapus item, periksa total, reservasi |
| Pelanggan | Pesanan saya | Filter pesanan aktif, selesai, dibatalkan |
| Pelanggan | Detail pesanan | Status, rincian, kode pengambilan, pembatalan |
| Restoran | Dashboard | Periode 7/28 hari, surplus, penjualan selesai, rekomendasi |
| Restoran | Menu | Daftar menu, harga normal, aktif/nonaktif |
| Restoran | Tambah/ubah menu | Nama, deskripsi, kategori, harga, berat |
| Restoran | Penawaran surplus | Daftar penawaran, stok, jadwal, tutup penawaran |
| Restoran | Buat/ubah penawaran | Harga rescue, stok dan jadwal pengambilan |
| Restoran | Catatan produksi | Produksi dan penjualan reguler per menu/tanggal |
| Restoran | Pesanan | Daftar dan filter pesanan restoran |
| Restoran | Detail/pengambilan | Konfirmasi, estimasi persiapan, siap diambil, verifikasi kode |

## Alur demonstrasi

1. Pelanggan memilih makanan, membuka keranjang, lalu **Reservasi pesanan**.
2. Catat kode pengambilan di detail pesanan.
3. **Ganti area** → **Buka area restoran** → **Pesanan** → **Lihat pesanan**.
4. Isi estimasi persiapan → **Konfirmasi pesanan** → **Tandai siap diambil**.
5. Masukkan kode pelanggan → **Verifikasi dan selesaikan**.
6. Pesanan menjadi selesai, porsi terselamatkan bertambah pada produksi menu,
   dan dashboard memperbarui pendapatan yang dipulihkan.

Untuk pasokan baru: **Menu** → **Tambah menu**, lalu **Produksi** → catat produksi
serta penjualan reguler. Setelah itu buat **Penawaran surplus** dengan stok yang
belum dialokasikan. Prototipe memakai kategori makanan utama, roti & kue, vegetarian.

## Aturan dan data lokal

- Data awal adalah contoh Dapur Bumi, bukan transaksi restoran nyata.
- Perubahan disimpan otomatis dalam JSON di direktori data lokal pengguna:
  `Path.Combine(Environment.GetFolderPath(LocalApplicationData), "Replate", "state.json")`.
  Pada macOS, lokasi umumnya `~/Library/Application Support/Replate/state.json`.
- Snapshot terpisah dari graf navigasi domain; penulisan berkas memakai penggantian atomik.
- Keranjang, menu, penawaran, produksi, dan status pesanan bertahan setelah restart.
- Penawaran yang sudah melewati waktu pengambilan tidak muncul di marketplace.
  Item keranjang yang kedaluwarsa masih dapat dihapus, tetapi tidak dapat dipesan.
- Stok dibatasi oleh surplus setelah penjualan reguler, porsi terselamatkan, dan
  alokasi penawaran/pesanan lain. Pembatalan pesanan mengembalikan stok satu kali.
- Pengambilan harus berstatus siap dan kode harus cocok; penyelesaian tidak dapat
  diulang untuk menggandakan porsi terselamatkan.
- Berkas yang tidak dapat dibaca dipertahankan. Aplikasi menampilkan peringatan dan
  menolak perubahan agar berkas asli tidak tertimpa data baru.
- Data lokal hanya untuk satu pengguna/satu aplikasi. Belum ada sinkronisasi antar
  perangkat, koneksi PostgreSQL untuk akun, atau pembayaran. Autentikasi memakai
  akun lokal tingkat prototype, bukan autentikasi produksi.

Dashboard menghitung rekomendasi dari rata-rata penjualan reguler pada periode
terpilih ditambah buffer dua porsi, mengikuti model domain; bukan machine learning.
Terbuang dan estimasi nilai terbuang hanya memakai hari yang sudah berakhir.
Surplus hari ini masih dapat diselamatkan. Pendapatan berdasarkan pesanan selesai,
dikelompokkan menurut tanggal pemesanan. Tidak ada rekomendasi tanpa riwayat produksi.

## Screenshot

Screenshot native jendela 1360 × 940 dan ukuran minimum 1040 × 720 tersedia di
[`docs/ui`](../../docs/ui). Marketplace dan dashboard adalah halaman terpisah.
Pada halaman yang panjang, gulir untuk bagian berikutnya.

![Marketplace pelanggan](../../docs/ui/marketplace.png)

![Dashboard restoran](../../docs/ui/dashboard.png)

![Keranjang](../../docs/ui/cart.png)

![Form penawaran](../../docs/ui/listing-editor.png)

## Verifikasi

```bash
dotnet build Replate.slnx
dotnet run --project tests/Replate.Domain.Checks
dotnet run --project src/Replate.Desktop -- --verify-state
dotnet run --project src/Replate.Desktop -- --verify-ui /tmp/replate-ui-preview
```

Normal startup initializes PostgreSQL before opening the window. Configure the connection, including the password,
with `REPLATE_DB_CONNECTION_STRING` when the local PostgreSQL server requires authentication, for example:

`REPLATE_DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=replate_db;Username=postgres;Password=..."`

Alternatively, keep the connection string without a password and set only `REPLATE_DB_PASSWORD`.

If PostgreSQL is unavailable, the application still opens and displays a warning; checkout reports the database
failure instead of terminating the desktop process.

Pemeriksaan state memakai berkas sementara terisolasi: muat ulang keranjang/status,
timestamp, stok, pickup salah/benar, pembatalan berulang, kapasitas surplus, tanggal,
harga, menu nonaktif, kedaluwarsa, perlindungan berkas rusak, serta akun: pendaftaran,
login, akun demo, isolasi antar pemilik, dan keranjang per pelanggan. Pemeriksaan UI
membuka jendela native, menjalankan alur lintas area, menyimpan screenshot, lalu
menutup otomatis. Kedua pemeriksaan tidak menggunakan berkas data aplikasi normal.
