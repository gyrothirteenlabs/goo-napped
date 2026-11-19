# Panduan aset dari luar (itch.io dll)

Untuk tim yang mengunduh aset gratis (sprite, tileset, UI, efek, audio). Taruh zip-nya, nanti AI yang membongkar, memilih yang cocok, menyesuaikan ukuran, dan memasang ke game.

## 1. Cara kirim
1. Taruh **zip apa adanya** di `art/incoming/` (folder ini tidak masuk git).
2. Di chat, sebutkan **link halaman itch** tiap zip (untuk cek lisensi dan kredit) dan, kalau ada, mau dipakai untuk apa.
3. AI akan:
   - membongkar tiap zip ke folder sendiri dan membaca file lisensinya;
   - memilih sprite yang cocok, menyesuaikan ukuran dan palet;
   - memasang ke `Assets/ThirdParty/<nama-pack>/` lengkap dengan file lisensi;
   - mencatat kredit di `Assets/ThirdParty/README.md`.

## 2. Ukuran sprite (paling penting)
Game ini: **1 tile = 48×48 px**, PPU 48, kamera Pixel Perfect 384×216 (terlihat ±8×4,5 tile). Semua art kita digambar di grid 48, jadi 1 pixel art = 1 pixel layar.

| Grid pack | Cocok? | Catatan |
|---|---|---|
| **48 px** | ✅ paling cocok | Langsung pakai. |
| **16 px** | 🟡 bisa, diperbesar 3× | Pixel-nya jadi 3× lebih gemuk dari art kita. Pakai **satu pack untuk satu area penuh** (mis. seluruh tileset taman), jangan dicampur per objek di samping art 48 px. |
| **24 px** | 🟡 bisa, diperbesar 2× | Sama seperti 16 px, pixel 2× lebih gemuk. |
| **32 px** | ⚠️ hindari | Butuh skala 1,5×, jadi pixel tidak rata dan goyang. Hanya kalau digambar ulang. |
| **64 px ke atas** | ⚠️ hindari | Harus diperkecil, jadi detail rusak. Kecuali objek besar yang memang 1–2 tile (pohon, mesin) dan pas di kelipatan 48 (96, 144). |
| Bukan pixel art (HD, vektor) | ❌ | Gaya tidak cocok. |

Ukuran objek di dunia (kira-kira):
- slime pemain ±30 px tinggi (dalam sel 48);
- penjaga 40–48 px;
- prop kecil (vas, ember) 16–30 px;
- perabot 1 tile (meja, peti) ±40 px;
- pohon/pilar 2 tile (96 px).

## 3. Gaya dan sudut pandang
- **Top-down 3/4** (seperti Zelda SNES / Stardew): tampak atas sedikit miring, tembok kelihatan mukanya. **Bukan** platformer tampak samping, **bukan** isometrik.
- Karakter idealnya punya **3 arah**: depan, samping, belakang (kiri = dibalik). Kalau pack cuma 1 arah, hanya cocok untuk prop diam.
- Mood **gelap**, tapi tidak masalah kalau pack-nya cerah: AI bisa mengubah palet ke palet area (taman biru malam, penjara batu dingin, kastil anggur-emas, puncak ungu).
- Bayangan yang sudah digambar di sprite boleh. Cahaya di game dihitung engine, jadi sprite tanpa cahaya terarah hasilnya paling bagus.
- Prop dekor tidak boleh mirip tempat sembunyi (tong, peti, semak, lemari) atau musuh, supaya pemain tidak tertipu. AI memperhatikan ini saat memilih.
- Desain dari tim desainer tetap prioritas. Aset luar mengisi yang belum dibuat tim.

## 4. Format
- **PNG** latar transparan. Sheet = grid rata (semua frame sama besar). GIF/Aseprite boleh, AI yang mengubah.
- Animasi: sebutkan frame mana untuk animasi apa kalau tidak jelas dari nama file.
- Audio: WAV atau OGG, aturan lengkapnya di `docs/todo-tim/daftar-suara.md`.

## 5. Lisensi (wajib dicek, repo kita publik)
"Free for commercial use" **belum tentu boleh masuk repo**. Repo Game Off publik, jadi menaruh file asli di GitHub = **membagikan ulang** aset itu.

| Lisensi | Boleh masuk repo? |
|---|---|
| CC0 / public domain | ✅ |
| CC-BY | ✅ wajib kredit (nama + link) |
| MIT / OFL (font) | ✅ sertakan file lisensi |
| "Free commercial, credit appreciated" tanpa larangan redistribusi | ✅ biasanya, cek teks lengkapnya |
| "Free commercial, **tidak boleh redistribute/resell aset**" | ❌ jangan di repo. Boleh dipakai hanya kalau tim setuju menyimpannya di luar repo (orang lain tidak bisa build ulang) |
| CC-BY-NC / ND, "personal use only" | ❌ |
| Potongan dari game/film lain | ❌ |

Kalau ragu, kirim link-nya saja, AI bacakan teks lisensinya dan bilang boleh/tidak.
