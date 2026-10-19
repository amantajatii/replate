# Screenshot Replate Desktop

Dua screenshot utama untuk tugas praktikum:

- [Marketplace pelanggan](marketplace.png): navigasi pelanggan di bagian atas dan daftar makanan penuh.
- [Dashboard restoran](dashboard.png): sidebar restoran, analisis surplus, dan rekomendasi produksi.

Kedua gambar berasal dari halaman terpisah, bukan satu layar gabungan.

## Halaman dan alur tambahan

| Screenshot | Tampilan |
| --- | --- |
| [Pilih area](entry.png) | Masuk ke konteks pelanggan atau restoran |
| [Detail makanan](food-detail.png) | Informasi makanan dan jumlah porsi |
| [Keranjang](cart.png) | Rincian pilihan dan total reservasi |
| [Pesanan saya](customer-orders.png) | Daftar pesanan pelanggan |
| [Detail reservasi](reservation.png) | Status dan kode pengambilan |
| [Menu restoran](menus.png) | Pengelolaan menu |
| [Form menu](menu-editor.png) | Tambah/ubah menu |
| [Penawaran surplus](listings.png) | Stok dan jadwal penawaran |
| [Form penawaran](listing-editor.png) | Harga khusus dan pengambilan |
| [Produksi](production.png) | Produksi, penjualan, dan porsi terselamatkan |
| [Pesanan restoran](restaurant-orders.png) | Daftar pesanan untuk diproses |
| [Proses pesanan](restaurant-order-detail.png) | Konfirmasi dan estimasi persiapan |
| [Pengambilan selesai](pickup-completed.png) | Hasil verifikasi kode |

Gambar `*-compact.png` merekam jendela minimum 1040 × 720. Halaman panjang dapat
digulir. `marketplace-empty.png` menunjukkan pencarian/filter tanpa hasil.
Semua data dalam gambar merupakan data contoh pengujian terisolasi.

Screenshot dihasilkan dengan:

```bash
dotnet run --project src/Replate.Desktop -- --verify-ui /tmp/replate-ui-preview
```
