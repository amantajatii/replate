# Replate Desktop

Aplikasi Avalonia UI 12.1.4 / .NET 10 dengan area pelanggan dan pemilik restoran
terpisah. Seluruh layar, keranjang, detail pesanan, dan formulir memakai halaman
penuh; detail makanan tampil sebagai pop up di atas halaman. Pelanggan menggunakan bar atas berisi logo putih
(ke Jelajah), kolom cari, pilihan lokasi, ikon keranjang dengan jumlah item, dan lingkaran profil
yang membuka menu **Pesanan saya** dan **Keluar**; restoran memiliki sidebar operasional.

## Menjalankan

Dari root repository:

```bash
dotnet restore Replate.slnx
dotnet run --project src/Replate.Desktop
```

Aplikasi dibuka di layar **Masuk**. Masuk dengan akun demo (lihat tabel di bawah)
atau pilih **Daftar akun baru**. Layar Masuk dan Daftar memakai kolom kiri berlatar
terang dan panel hijau penuh tinggi di kanan. Batas kiri panel hijau tepat melewati
titik tengah foto piring `Assets/auth-hero.png` (tanpa teks): setengah piring di atas
latar terang dan setengah di atas panel hijau. Batas itu berada di sekitar 72% lebar
jendela, sehingga panel hijau sekitar 28% lebar. Diameter piring sekitar 56% tinggi
jendela. Kartu formulir mulai di sekitar 12% lebar jendela (minimal 48) dan berjarak
sekitar 8% lebar jendela dari piring; pada jendela kecil jarak itu mengecil sampai
minimal 24. Logo hijau
tetap di atas, sejajar dengan tepi kiri kartu, sedangkan satu kartu formulir di
bawahnya dapat digulir vertikal, tanpa teks tambahan di bawah kartu. Area gulir memakai scrollbar tipis tanpa
track dan tanpa tombol panah yang hanya tampil saat menggulir atau saat kursor berada
di atasnya; gulir tetap bekerja dengan roda mouse, touchpad, dan keyboard, dan kolom
yang dicapai dengan Tab otomatis tergulir ke tampilan. Setiap kolom kata sandi memiliki tombol mata yang
selalu terlihat untuk menampilkan atau menyembunyikan isi. Di Daftar, peran dipilih
dengan dua tombol **Pelanggan** / **Pemilik restoran** (bisa juga dengan tombol panah),
dan Jam buka serta Jam tutup dipilih dari daftar jam 00 sampai 23 dan menit 00 sampai
59 (format 24 jam). Jika database belum tersambung, peringatan ringkas tampil di atas kartu Masuk. Pelanggan diarahkan ke marketplace, pemilik
restoran ke dashboard restorannya. **Keluar** di menu profil pelanggan atau sidebar
restoran mengakhiri sesi dan kembali ke layar Masuk. Halaman pelanggan hanya untuk
pelanggan yang masuk; halaman restoran hanya untuk pemilik restoran tersebut. Peran
yang tidak sesuai atau belum masuk diarahkan kembali ke layar Masuk.

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
| Awal | Masuk | Kartu email dan kata sandi di samping foto; arah halaman mengikuti peran akun |
| Awal | Daftar | Kartu akun pelanggan atau pemilik restoran beserta profil restoran, di samping foto |
| Pelanggan | Jelajah | Banner sapaan, koleksi Terdekat, Termurah, Segera berakhir, Makanan utama, Roti & kue, serta Buah & sayur. Kartu makanan menampilkan harga, stok, jadwal, serta jarak dan estimasi perjalanan jika tersedia |
| Pelanggan | Terdekat | Penawaran diurutkan menurut jarak dari lokasi pelanggan. Penawaran tanpa jarak berada di akhir, dan batas pengambilan memecah urutan yang sama |
| Pelanggan | Koleksi lain | Kartu makanan terurut (harga atau batas pengambilan) atau tersaring per kategori |
| Pelanggan | Hasil pencarian | Dibuka dengan Enter atau ikon kaca pembesar di bar atas; judul "Hasil untuk {kata}", semua kata harus cocok dengan nama menu, deskripsi, restoran, atau kategori |
| Pelanggan | Pop up detail makanan | Foto, deskripsi, harga, stok, jadwal, pengatur porsi, alamat restoran, dan peta interaktif jika koordinat tersedia |
| Pelanggan | Keranjang | Tautan Kembali ke Jelajah, kartu item dikelompokkan per restoran (nama, subtotal, tombol Pesan) dengan pengatur jumlah dan ikon hapus, pop up Konfirmasi pesanan (item, jendela pengambilan, total, Batal dan Pesan sekarang), keadaan kosong dengan tombol Jelajahi menu |
| Pelanggan | Pesanan saya | Filter pesanan aktif, selesai, dibatalkan |
| Pelanggan | Detail pesanan | Status, rincian, kode pengambilan, pembatalan |
| Restoran | Dashboard | Periode 7/28 hari, surplus, penjualan selesai, rekomendasi |
| Restoran | Profil | Ubah nama, alamat, telepon, jam buka dan tutup, serta deskripsi. Peta menampilkan lokasi restoran |
| Restoran | Menu | Daftar menu, harga normal, aktif/nonaktif |
| Restoran | Tambah/ubah menu | Nama, deskripsi, kategori, harga, berat |
| Restoran | Penawaran surplus | Daftar penawaran, stok, jadwal, tutup penawaran |
| Restoran | Buat/ubah penawaran | Harga rescue, stok dan jadwal pengambilan |
| Restoran | Catatan produksi | Produksi dan penjualan reguler per menu/tanggal |
| Restoran | Pesanan | Daftar dan filter pesanan restoran |
| Restoran | Detail/pengambilan | Konfirmasi, estimasi persiapan, siap diambil, verifikasi kode |

## Alur demonstrasi

1. Di layar **Masuk**, masuk sebagai `pelanggan@replate.demo` / `replate123`.
   Klik **Atur lokasi** atau nama lokasi yang terdeteksi, masukkan alamat publik seperti
   `Jl. Malioboro, Yogyakarta`, lalu buka **Terdekat**. Buka detail makanan dan gulir ke peta.
2. Klik **Tambah** pada kartu makanan, atur jumlah porsi di pop up, lalu
   **Tambah ke keranjang**. Buka keranjang, klik **Pesan** pada kelompok restoran, lalu **Pesan sekarang** di pop up konfirmasi. Catat kode
   pengambilan di detail pesanan.
3. Lingkaran profil → **Keluar**, lalu masuk sebagai `dapur-bumi@replate.demo` / `replate123`
   (atau pemilik restoran tempat makanan dipesan) → **Pesanan** → **Lihat pesanan**.
4. Isi estimasi persiapan → **Konfirmasi pesanan** → **Tandai siap diambil**.
5. Masukkan kode pelanggan → **Verifikasi dan selesaikan**.
6. Pesanan menjadi selesai, porsi terselamatkan bertambah pada produksi menu,
   dan dashboard memperbarui pendapatan yang dipulihkan.
7. Sebagai pemilik restoran, pilih **Profil**, ubah alamat restoran, lalu klik
   **Simpan profil**. Peta diperbarui saat koordinat alamat ditemukan.

Restoran baru: **Keluar** → **Daftar akun baru** → pilih **Pemilik restoran**, isi
akun dan **Profil restoran** (untuk buka 24 jam, isi 00:00 sampai 23:59) → **Daftar**.
Dashboard restoran baru masih kosong dan menawarkan **Tambah menu**.

Untuk pasokan baru: **Menu** → **Tambah menu**, lalu **Produksi** → catat produksi
serta penjualan reguler. Setelah itu buat **Penawaran surplus** dengan stok yang
belum dialokasikan. Prototipe memakai kategori makanan utama, roti & kue, vegetarian.

## Integrasi API

| Layanan | Fungsi | Saat layanan gagal |
| --- | --- | --- |
| [Photon](https://github.com/komoot/photon) | Menyarankan alamat di Indonesia setelah tiga karakter, dengan jeda 350 ms dan maksimal lima saran. Hasil diprioritaskan menurut lokasi tersimpan jika ada | Ketik alamat lengkap dan klik **Gunakan alamat** |
| [Nominatim](https://operations.osmfoundation.org/policies/nominatim/) | Mencari koordinat setelah alamat lengkap dikirim. Pencarian balik memberi nama jalan atau kota pada koordinat perangkat | Profil restoran tetap tersimpan dengan peringatan. Jika pencarian balik gagal, koordinat perangkat tersimpan dengan label **Lokasi saya** |
| [ipapi.co](https://ipapi.co/api/) | Memperkirakan kota dari IP saat pelanggan belum menetapkan lokasi atau lokasi browser tidak tersedia | Pilih lokasi manual dari bar atas |
| [OSRM](https://project-osrm.org/docs/v5.24.0/api/#table-service) | Menghitung jarak dan perkiraan waktu berkendara ke restoran | Estimasi jalan yang masih tersimpan dipertahankan. Jika belum ada, kartu menunjukkan jarak garis lurus tanpa waktu tempuh |
| [Tile OpenStreetMap](https://operations.osmfoundation.org/policies/tiles/) via [Mapsui](https://www.nuget.org/packages/Mapsui.Avalonia12/5.2.0) | Memuat peta interaktif untuk detail makanan dan profil restoran | Penanda lokasi masih dapat tampil jika gambar peta belum tersedia |

**Gunakan lokasi saya** membuka konfirmasi di Replate sebelum membuka tab
browser. Setelah memilih **Lanjutkan**, izinkan browser mengakses lokasi dan
kembali ke Replate. Browser modern mengizinkan geolokasi pada alamat loopback
`http://127.0.0.1`, dan macOS mungkin meminta agar Layanan Lokasi diaktifkan untuk
browser. **Batalkan** menghentikan permintaan di Replate. Jika browser menolak,
lokasi gagal, atau sesi mencapai 90 detik, Replate mencoba perkiraan IP dan
menandainya di bar atas.

Replate menyimpan koordinat pelanggan di perangkat. Browser mengirim hasil ke
listener lokal sekali pakai. Replate memakai koordinat pada permintaan Nominatim
untuk nama lokasi dan OSRM untuk estimasi perjalanan. Saat mengetik alamat,
Photon menerima teks dan lokasi tersimpan jika tersedia untuk memprioritaskan
hasil yang lebih dekat.

Pencarian alamat dan pencarian balik Nominatim berbagi jeda minimal 1,1 detik
antarpermintaan dalam satu proses dan menyimpan hasil di memori. Batas layanan publik berlaku
untuk keseluruhan aplikasi. Gunakan alamat publik yang dapat dibagikan untuk
pencarian dan hindari data pribadi atau rahasia.
Peta menampilkan atribusi **© OpenStreetMap contributors**, mengirim
`User-Agent` Replate, dan menyimpan tile di cache lokal selama tujuh hari.
`--verify-state` dan `--verify-ui` memakai penyedia lokasi, saran alamat,
geocoder, dan rute palsu. Tes listener memakai HTTP loopback tanpa membuka
browser. Verifikasi UI menampilkan penanda tanpa mengunduh tile.

## Aturan dan data lokal

- Data awal adalah contoh Dapur Bumi, bukan transaksi restoran nyata.
- Perubahan disimpan otomatis dalam JSON di direktori data lokal pengguna:
  `Path.Combine(Environment.GetFolderPath(LocalApplicationData), "Replate", "state.json")`.
  Pada macOS, lokasi umumnya `~/Library/Application Support/Replate/state.json`.
- Snapshot terpisah dari graf navigasi domain; penulisan berkas memakai penggantian atomik.
- Keranjang, menu, penawaran, produksi, dan status pesanan bertahan setelah restart.
- Satu keranjang boleh berisi menu dari beberapa restoran. Setiap restoran dipesan
  terpisah: **Pesan** pada kelompok restoran membuka pop up konfirmasi, dan **Pesan
  sekarang** membuat tepat satu pesanan untuk restoran itu dengan kode pengambilan
  sendiri, hanya terlihat oleh pemilik restoran tersebut. Item restoran lain tetap di
  keranjang. Pesanan disimpan ke PostgreSQL dalam satu transaksi; bila gagal, stok dan
  keranjang tidak berubah dan tidak ada pesanan yang tersimpan.
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
failure instead of terminating the desktop process, and leaves stock, cart, and orders unchanged.

Pemeriksaan state memakai berkas sementara terisolasi: muat ulang keranjang/status,
timestamp, stok, pickup salah/benar, pembatalan berulang, kapasitas surplus, tanggal,
harga, menu nonaktif, kedaluwarsa, perlindungan berkas rusak, serta akun: pendaftaran,
login, akun demo, isolasi antar pemilik, dan keranjang per pelanggan. Pemeriksaan UI
membuka jendela native, menjalankan alur lintas area, menyimpan screenshot, lalu
menutup otomatis. Untuk layar Masuk dan Daftar, pemeriksaan UI juga memastikan pada
1360 × 940, 1040 × 720, 1283 × 780, dan 1920 × 1080: batas panel hijau berada di
tengah piring (setengah di atas latar terang, toleransi 0,02 diameter) dan di sekitar
72% lebar (panel sekitar 28%), tepi kiri kartu di sekitar 12% lebar dan minimal 48,
piring tidak berpotongan dengan kartu (jarak minimal 24, dan minimal 8% lebar pada
1360 × 940, 1283 × 780, dan 1920 × 1080), kartu tidak terpotong,
logo sejajar tepi kiri kartu, tidak ada teks akun demo di bawah kartu, logo tidak terpotong saat
digulir, tidak ada gulir horizontal, tidak ada scrollbar bertrack atau berpanah yang
tampil, dan setiap kolom yang difokuskan dengan Tab tergulir ke tampilan. Pemeriksaan
juga mencakup judul tebal tanpa subjudul, tombol mata yang terlihat saat kolom kosong
dan mengganti mode, tombol peran yang eksklusif dan bisa dipakai dengan keyboard,
kolom jam yang menghasilkan `TimeOnly` 24 jam tanpa AM/PM, serta hover tombol.
Langkah salin kode pengambilan memakai clipboard sistem dan gagal bila clipboard
tidak tersedia di lingkungan pengujian. Kedua pemeriksaan tidak menggunakan berkas data aplikasi normal.
