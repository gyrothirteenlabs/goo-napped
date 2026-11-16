# Review: props level + rapih layout (16 Nov 2026)

Gambar `L00..L11.png` = peta penuh tiap level, **atas sebelum, bawah sesudah** (cahaya dinaikkan supaya layout terlihat; di game tetap gelap). `props_sheet.png` = semua sprite props baru (3x).

## Yang baru
- **Perabot penghalang** (peta `t`) dan **pilar** (peta `I`). Bertindak seperti tembok: menghalangi jalan, pandangan penjaga, dan cahaya lentera. Bayangannya bisa dipakai menyelinap.
  - Taman: sumur, bangku batu, bak bunga panjang, patung slime raja berlumut.
  - Penjara: meja, kandang besi kosong, meja panjang + bangku, pilar batu berantai.
  - Lab (L06): kuali hijau, rak tabung, meja kerja panjang (labu, buku, lumpang), tabung spesimen besar.
  - Kastil: meja bundar bertaplak, rak buku, meja makan panjang + kursi, tiang marmer.
  - Puncak: konsol aura, generator, saluran pipa, tiang pipa aura.
- **Serakan lantai** tanpa collider (jarang, di pinggir ruangan): tulang, ember, pecahan gerabah, jerami, kertas, pecahan kaca, kabel, batu, jamur, nisan kecil, daun.
- **Dekor tembok**: rantai (penjara), pipa (lab), tambahan ke rak ramuan/kandelabra.

## Layout
- Penempatan dicek skrip: tidak ada rute (spawn → esens → toples → ventilasi) yang bertambah panjang lebih dari 2 langkah, patroli penjaga tidak berubah, tidak menempel spawn/pintu/celah/toples/esens.
- L11: ruang kanan bawah (ada tempat sembunyi) dulu **tertutup total**, sekarang ada pintu.
- L06: tembok nyasar di pojok kanan bawah dihapus. Area L06 jadi `lab` (tile penjara + perabot lab).

## Untuk desainer
Semua sprite di sini SEMENTARA (AI). Timpa file dengan nama sama di `Assets/_Game/Art/Sprites/Props/<Area>/` untuk mengganti. Format frame ada di `art/CLAUDE.md` bagian "Props level tambahan". Posisi perabot diatur di peta teks `LevelBlockout.cs` (huruf `t` dan `I`).
