# Backlog fitur

Status: ✅ selesai (prototipe) · 🔧 sedang dikerjakan · ⬜ belum. Prioritas: **P1** wajib untuk jam, **P2** penting, **P3** kalau sempat.
Fokus minggu 2–3: **perbanyak fitur & mekanik dulu, bug menyusul** (keputusan 10 Nov).

## Mekanik slime
| | Fitur | Prio | Catatan |
|---|---|---|---|
| ✅ | Gerak lompat-merayap + jeli | P1 | `SlimeMotor`, `SlimeJelly` |
| ✅ | Makan bahan, ciri tubuh, urutan makan | P1 | `SlimeBodyState` |
| ✅ | Belah / gabung / Shift + jeda anti-spam | P1 | `PlayerBrain` |
| ✅ | Nyamar jadi barang (tahan C) | P1 | sprite samaran per chapter nanti dari desainer |
| ✅ | Umpan (potongan nyamar menyala/berbau didatangi penjaga) | P1 | |
| ⬜ | Muntahkan bahan (buang ciri terakhir) | P2 | koreksi resep salah tanpa ulang level |
| ⬜ | Lempar potongan kecil sebagai pengalih suara | P2 | "skill" utama selain belah |
| ⬜ | Suara langkah terdengar penjaga (berat = lebih berisik) | P2 | |
| ⬜ | Ciri tubuh terlihat di badan (uap panas, garis bau, kilau) | P2 | VFX Unity |

## Musuh
| | Fitur | Prio | Catatan |
|---|---|---|---|
| ✅ | Golem Batu: patroli, kerucut lurus, cari, sapu pandangan, radius sadar, cahaya | P1 | |
| ⬜ | Familiar Api: melayang, melihat yang PANAS/menyala dari jauh, buta di gelap | P1 | chapter 2 |
| ⬜ | Anjing Lendir: mengendus jejak bau, mengikuti jejak | P2 | chapter 2 |
| ⬜ | Mata Penyihir: memeriksa toples, membongkar tiruan salah resep | P3 | chapter 3 (kandidat dipotong) |

## Bahan (chapter 1–2)
| | Bahan | Ciri | Efek |
|---|---|---|---|
| ✅ | Serbuk Batu | abu, berat | lambat |
| ✅ | Lumut Pendar | hijau, berkilau | menyala |
| ✅ | Air Embun | biru, dingin | – |
| ✅ | Ramuan Api | merah, panas | menyala |
| ⬜ | Minyak Belerang | kuning, bau | radius bau (Anjing) |
| ⬜ | Garam Beku | putih, dingin | memadamkan menyala |

## Level
| | Fitur | Prio |
|---|---|---|
| ✅ | Blockout chapter 1 (4 level) + tool peta teks | P1 |
| ⬜ | Blockout chapter 2 (kastil bawah) | P1 |
| ⬜ | Pintu / tuas / lorong tikus (hanya potongan kecil bisa lewat) | P2 |
| ⬜ | Lampu yang bisa dipadamkan (Garam Beku / potongan dingin) | P2 |

## Alur game & UI
| | Fitur | Prio |
|---|---|---|
| ⬜ | Pause + restart + menu utama + pilih level | P1 |
| ⬜ | UI asli: kartu resep, meter curiga di atas penjaga, ikon ciri tubuh | P1 |
| ⬜ | Simpan progres level | P2 |
| ⬜ | Cerita: intro, dialog ringan per chapter, ending peran tertukar | P2 |

## Audio & art (tim desainer)
| | Fitur | Prio |
|---|---|---|
| ✅ | SFX/ambience/BGM placeholder + AudioDirector | P1 |
| ⬜ | Desain final slime + render sprite 8 frame `move` | P1 |
| ⬜ | Tileset chapter 1, toples, Golem final | P1 |
| ⬜ | Audio final | P2 |

## Bug / poles yang diketahui (menyusul)
- Sprite slime lama + squash engine bisa dobel sampai model baru dirender.
- Angka rasa gerak dan kesulitan penjaga belum disetel lewat playtest.
- Air Embun belum punya palet warna.
