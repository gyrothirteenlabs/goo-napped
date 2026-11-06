---
name: slime-model
description: Modeling slime, musuh, atau prop di Blender (via Blender MCP) untuk game jam SHIFT. Pakai setiap kali diminta bikin/ubah model 3D yang nanti di-render jadi sprite pixel 48px.
---

# Slime model workflow

Baca `art/CLAUDE.md` dulu. Kerjakan tahap demi tahap. Di setiap **CHECK**, ambil screenshot dan nilai sendiri. Kalau gagal, perbaiki dulu sebelum lanjut.

## 0. Persiapan
1. `get_scene_info`. Catat objek yang sudah ada. Jangan hapus apa pun tanpa izin user.
2. Simpan file sebagai `art/blender/<nama>_v01.blend` (atau naikkan versinya).
3. Kalau scene belum punya `Render_Rig`, append dari file rig tim (atau dari `Knight_mc__1.blend`: camera + material `PixelArt_Simple`).
4. Kalau desainnya belum jelas, tanya user maksimal 3 hal: ciri utama, warna dasar, 1 referensi feel.

## 1. Blockout (bentuk besar saja)
- Body: UV sphere 16×8 atau cube + Subdivision. Skala sekitar (1.2, 1.1, 1.0) m. Rata-kan sedikit bagian bawah, origin di tengah bawah, Z=0.
- Mata: 2 placeholder oval di depan (-Y).
- Belum ada detail, aksesori, atau material.

**CHECK A: proporsi.** Screenshot view Front (numpad 1), Right (3), dan dari kamera render.
- Siluet seperti jeli, bukan bola sempurna atau telur?
- Mata di sekitar 45% tinggi, simetris, besar?
- Dari kamera 3/4, wajah masih terlihat jelas (tidak ketutup bagian atas kepala)?

## 2. Ciri & aksesori
- Satu ciri = satu bentuk dasar yang jelas (cone, wedge, cluster 3 kubus bevel, dll).
- Ukur dari kamera: tiap aksesori minimal sekitar 4px di 48px. Kalau lebih kecil, besarkan atau buang.
- Maksimal 2 aksesori per karakter. Lebih dari itu bikin siluet ramai.

**CHECK B: tes siluet.** Render preview 48×48 (lihat skill `sprite-render`, langkah 2) dengan material emission hitam polos.
- Karakter bisa dikenali dari siluet saja?
- Beda dengan karakter lain dari siluet saja?

## 3. Material
- Slime pemain/tiruan: body grayscale toon (Color Ramp Constant, 4 band: 0.25/0.5/0.75/1.0). Face di material terpisah.
- Musuh/prop: toon berwarna, 3–4 band, maksimal 4 hue per karakter.

**CHECK C:** render 48px dan lihat di zoom 400–800% (nearest). Band terbaca? Tidak ada noise atau pixel yatim?

## 4. Rig ringan & shape key
- Slime: shape key `squash`, `stretch`, `flat` (untuk berat/nyamar). Tanpa armature kecuali benar-benar perlu.
- Musuh: armature sederhana kalau ada anggota badan.

## 5. Serah terima
- Rapikan nama objek sesuai konvensi, apply scale (Ctrl+A), lalu simpan.
- Lapor singkat ke user: apa yang dibuat, screenshot CHECK terakhir, dan hal yang masih kurang.
- Lanjut ke skill `sprite-render` kalau user minta.

## Larangan
- Membangun semua bagian sekaligus tanpa CHECK.
- Aksesori berupa bola yang cuma di-scale.
- Gaya PSX/low-poly, tekstur realistis, gradasi halus.
- Menyalin desain Slime Rancher/Momo.
