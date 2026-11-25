# Checklist submit: GitHub Game Off 2026 (itch.io)

Deadline: **1 Desember 2026, 13:37 PST** (21:37 UTC). Submit beberapa hari sebelumnya, jangan mepet.

## Sudah siap
- [x] Repo GitHub publik: https://github.com/gyrothirteenlabs/goo-napped
- [x] Deskripsi repo dalam bahasa Inggris
- [x] README.md dalam bahasa Inggris (kontrol, cara main, build, kredit)
- [x] Teks halaman itch.io: [itch-page.md](itch-page.md) (tagline, deskripsi, kontrol, kredit, tag)
- [x] Build Windows rilis terbaru (dari build ulang setelah PR #19 dan #20) lolos smoke test: 124 OK, 0 gagal
- [x] Zip build untuk upload: `Builds/submission/goo-napped-windows.zip` (50 MB, `GooNapped.exe` di dalam folder `GooNapped/`)
- [x] Screenshot 1360×768 dari build baru: [screenshots/](screenshots/)
- [x] Cover placeholder 630×500: [cover_placeholder_630x500.png](cover_placeholder_630x500.png) (pakai logo sementara, ganti dengan art final tim)
- [x] Kredit musik dan font: CREDITS.txt (ikut build)
- [x] Lisensi: MIT (file [LICENSE](../../LICENSE)), aset pihak ketiga tetap pakai lisensinya sendiri

## Masih perlu aksi tim
- [ ] **Cover final** dari tim desainer (ukuran 630×500, format PNG atau JPG).
- [ ] **Screenshot final** (opsional diganti dengan art final). Minimal 1 screenshot wajib.
- [ ] **GIF gameplay** (opsional, tapi sangat membantu di halaman itch). Bisa dibuat dari Unity Recorder atau OBS.
- [ ] **Akun itch.io**: daftar, lalu join jam Game Off 2026 (bisa login dengan GitHub).
- [ ] **Isi form submit** di itch.io: judul, tagline, deskripsi (copy dari itch-page.md), tag, cover, screenshot, upload zip Windows, centang "Windows" sebagai platform, tandai "Keyboard" sebagai input.
- [ ] **Link GitHub** di halaman game (sudah ada di bagian Credits itch-page.md).
- [ ] **Cek ulang** halaman itch setelah publish: unduh zip, ekstrak di folder kosong, jalankan, pastikan menu muncul.

## Cara cek build sebelum upload
1. Ekstrak `goo-napped-windows.zip` ke folder kosong.
2. Jalankan `GooNapped/GooNapped.exe`.
3. Mulai New Game, pastikan intro muncul, tutorial bisa dimainkan, dan Esc bisa membuka pause.
4. Kalau ada yang rusak, cek `docs/handoff.md` untuk cara build ulang.

## File penting
| File | Isi |
|---|---|
| [itch-page.md](itch-page.md) | Teks halaman itch.io |
| [screenshots/](screenshots/) | Screenshot 1360×768 (6 file) |
| [cover_placeholder_630x500.png](cover_placeholder_630x500.png) | Cover sementara |
| `Builds/submission/goo-napped-windows.zip` | Build Windows untuk upload (tidak di git) |
