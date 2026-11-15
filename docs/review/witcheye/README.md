# Witch Eye: bahan review desainer (15 Nov 2026)

Model: `art/blender/witcheye_v02.blend` (salinan file desainer, desain tidak diubah).

| File | Isi |
|---|---|
| `model_blender.png` | Model di Blender: depan, kanan, belakang, 3/4 |
| `sprite_sheet.png` | Semua frame sprite 48 px (idle 8, move 6, search 8) x 3 arah, diperbesar 4x |
| `anim_idle.gif`, `anim_move.gif`, `anim_search.gif` | Animasi 8 fps, 3 arah berdampingan (depan, samping, belakang) |
| `ingame_*.png` | Witch Eye di level (cahaya game asli) |

Dipakai di: **L06 Drain Lab**, **L09 Gallery**, dan sandbox **ProtoEnemies** (semua lewat prefab `Prefabs/Enemies/WitchEye_Placeholder`).

Yang diubah dari file asli (teknis/keterbacaan saja):
- objek masuk koleksi template + `Model_Root` (supaya arah samping/belakang ikut berputar), posisi ditengahkan dan melayang;
- tengkorak diberi warna tulang, lubang mata/hidung digelapkan (dulu bolong, di 48 px jadi gumpalan krem);
- lensa monokel = layer yang menyala di game;
- animasi baru: idle (tengkorak mengorbit), move (condong, tengkorak tertinggal), search (menoleh memeriksa).

Kalau mau ubah bentuk/warna: edit `witcheye_v02.blend`, lalu render ulang lewat panel GooNapped (nama `witcheye`, animasi `idle:1-8, move:11-16, search:21-28`).
