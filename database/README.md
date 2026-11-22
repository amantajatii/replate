# Database Replate — Modul 5

Database PostgreSQL berdasarkan [ERD Replate](../erd-replate.mmd), berisi 11 tabel.

## Membuat database

Jalankan dari direktori utama proyek, menggunakan akun PostgreSQL lokal:

```sh
psql -d postgres -v ON_ERROR_STOP=1 -c 'CREATE DATABASE replate;'
psql -d replate -v ON_ERROR_STOP=1 -f database/schema.sql
```

Tambahkan opsi `-h localhost -U nama_user` jika diperlukan untuk koneksi lokal.
Pembuatan database hanya dilakukan sekali. Script schema ditujukan untuk database
kosong; tidak menghapus atau menimpa tabel yang sudah ada. Transaksi membatalkan
seluruh pembuatan tabel jika terjadi kesalahan.

## Bukti homework

Pemeriksaan sederhana keberadaan 11 tabel serta penolakan role dan foreign key
yang tidak valid (tanpa menyimpan data pemeriksaan):

```sh
psql -d replate -v ON_ERROR_STOP=1 -f database/check.sql
```

Buka `psql -d replate`, lalu screenshot hasil perintah berikut:

```psql
\l replate
\conninfo
\dt public.*
\d public.users
\d public.restaurants
\d public.menus
\d public.production_records
\d public.production_recommendations
\d public.surplus_listings
\d public.carts
\d public.cart_items
\d public.orders
\d public.order_items
\d public.pickup_verifications
```

Kumpulkan file `schema.sql`, ERD, dan screenshot sesuai format worksheet.
Alternatif: tampilkan database, daftar tabel, kolom, dan constraint melalui pgAdmin.

| Tabel | Fungsi |
| --- | --- |
| users | Akun customer dan pemilik restoran |
| restaurants | Profil restoran milik pengguna |
| menus | Menu yang disediakan restoran |
| production_records | Riwayat produksi dan penjualan per menu per tanggal |
| production_recommendations | Riwayat rekomendasi produksi per menu per tanggal |
| surplus_listings | Penawaran makanan surplus dan waktu pengambilan |
| carts | Keranjang customer dengan restoran yang dipilih |
| cart_items | Rincian makanan dalam keranjang |
| orders | Pesanan, status, dan kode pengambilan |
| order_items | Rincian pesanan dengan snapshot nama dan harga menu |
| pickup_verifications | Hasil verifikasi pengambilan pesanan |

## Pemetaan ERD

- Nama tabel menggunakan bentuk plural dan kolom menggunakan `snake_case`.
- UUID memiliki default `gen_random_uuid()`. Uang menggunakan `numeric`, tanggal
  dan waktu kejadian menggunakan `timestamptz`, durasi menggunakan `interval`.
- Enum ERD diterapkan sebagai `text` dengan `CHECK` untuk membatasi nilai.
- Primary key, foreign key, serta seluruh keunikan tunggal dan gabungan pada ERD
  diterapkan. `cart.restaurant_id`, `orders.completed_at`, dan
  `pickup_verifications.verified_at` boleh kosong sesuai ERD.
- `updated_at` memiliki default saat insert; aplikasi harus memperbaruinya saat update.
- Sesuai catatan ERD, pemeriksaan role pemilik/customer menjadi tanggung jawab
  aplikasi. Foreign key memastikan keberadaan pengguna, bukan role pengguna.
- Database ini belum dihubungkan ke class library C#; acuannya adalah ERD target.
