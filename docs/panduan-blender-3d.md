# Panduan desainer 3D: model Blender → sprite pixel 48px

Pakai template `art/blender/sprite_template.blend` supaya kamera, resolusi, dan "shader" render sama persis dengan sprite slime yang sudah ada. Model baru tinggal dimasukkan, lalu klik render. Hasilnya langsung berupa sprite sheet yang siap dipakai Unity.

Sudah dites: slime v06 yang di-append ke template menghasilkan sprite **identik pixel** dengan sprite di `art/sprites/slime/`.

---

## A. Cara pakai template (5 menit)

### 0. Sekali saja: aktifkan panel
Template membawa add-on `goonapped_sprite_addon.py`. Blender mematikan script otomatis demi keamanan, jadi pilih salah satu:
- **Disarankan (sekali seumur Blender):** Edit > Preferences > Add-ons > tombol ⌄ kanan atas > **Install from Disk** > pilih `art/tools/goonapped_sprite_addon.py` > centang.
- **Atau tiap buka file:** buka workspace *Scripting* > Text Editor > pilih `goonapped_sprite_addon.py` > **Run Script** (▶).

Panel muncul di **3D Viewport > tekan N > tab GooNapped**.

### 1. Buka template, simpan sebagai file baru
`File > Open > art/blender/sprite_template.blend`, lalu langsung `File > Save As > art/blender/<nama>_v01.blend`.
Simpan di folder `art/blender/`, karena folder output dihitung relatif dari lokasi file .blend.

### 2. Masukkan model
| Sumber | Caranya |
|---|---|
| File `.blend` lain | `File > Append` (atau drag file .blend ke viewport > Append) > masuk folder `Object` > pilih objeknya (ikutkan parent/rig/lattice-nya) |
| `.obj` / `.fbx` / `.glb` | Drag file ke viewport, atau `File > Import` |

### 3. Tandai Body dan Face
- Pilih objek badan (atau parent/root-nya) > **Jadikan Body**. Anak-anak objeknya ikut.
- Pilih mata, mulut, kilau > **Jadikan Face**.
- Klik **Pas-kan**: model berdiri di lantai (Z=0) dan berada di tengah dengan tinggi sesuai isian. Slime pemain = 1.0 m. Bandingkan dengan wireframe `Ref_Slime_1m`.
  > Pas-kan *sebelum* bikin animasi. Kalau objek sudah punya keyframe lokasi/skala, keyframe-nya yang menang.

### 4. Isi data aset
| Isian | Contoh | Catatan |
|---|---|---|
| Nama aset | `slime`, `golem`, `jar` | Huruf kecil, tanpa spasi/underscore. Jadi awalan nama file |
| Mode | Slime / Warna flat | Lihat bagian B.4 |
| Animasi | `idle:1-4, move:5-10, jelly:20-52/2` | `nama:frameAwal-frameAkhir`, `/2` = ambil tiap 2 frame. Prop diam: `pose:1` |
| Arah | front / side / back | Prop yang tidak berputar cukup `front` |

Animasi = rentang frame di **timeline**. Semua animasi dibuat berurutan di satu timeline (idle di frame 1–4, move di 5–10, dst.). FPS template = 8, sama dengan Unity, jadi preview timeline terasa sama dengan di game.

### 5. Tes, lalu render
- **Tes frame ini**: render frame sekarang di 3 arah, lalu tampilkan `GN_Preview` di Image Editor. Hasilnya juga disimpan sebagai `art/sprites/<nama>/_preview_<nama>_test.png`.
  Tiap arah ada 3 gambar: disinari dari **kiri**, dari **kanan**, dan **normal map**. Ini simulasi kasar lampu Unity. Yang dicek: sisi yang menghadap lampu harus terang.
- **Render semua**: semua animasi × arah × layer. Hasilnya:
  ```
  art/renders/raw/<nama>/...          frame mentah (tidak di-commit)
  art/sprites/<nama>/<nama>_body_idle_front.png     sheet horizontal, 48px per frame
  art/sprites/<nama>/<nama>_face_idle_front.png
  art/sprites/<nama>/<nama>_normal_idle_front.png
  art/sprites/<nama>/_preview_<nama>.png            cek sebelum masuk Unity
  ```
  Di mode Warna flat, `_body_` diganti `_albedo_`.

### 6. Masuk Unity
Copy sheet (tanpa `_preview`) ke `Assets/_Game/Art/Sprites/<Characters|Enemies|Props>/`. Slicing 48px, pivot, dan normal map terpasang otomatis. Material: body slime `Slime_*`, wajah `Slime_Face`, musuh/prop `Env_Lit`.

---

## B. Metodenya: kenapa render-nya terlihat seperti itu

Intinya: **Blender tidak menghitung cahaya sama sekali.** Blender cuma merekam *bentuk* (siluet + arah permukaan) dan *warna dasar*. Cahaya, bayangan, dan band toon dihitung real-time di Unity oleh shader `GooNapped/PixelToonLit`. Karena itu slime bisa terang di sisi yang menghadap obor, ke mana pun dia jalan.

### B.1 Kamera (dikunci)
| Setting | Nilai | Alasan |
|---|---|---|
| Tipe | **Orthographic** | Tanpa perspektif: ukuran pixel sama di depan dan belakang, sesuai game 2D |
| Rotasi | **X = 55°**, Y = 0, Z = 0 | Sudut top-down 3/4: atas kepala dan muka sama-sama kelihatan |
| Ortho Scale | **2.0** | 48 px / 2 m = **24 px per meter**. Slime 1 m ≈ 24 px tinggi badan |
| Posisi | (0, −8.19, 6.24), membidik titik (0, 0, 0.5) | Tengah badan setinggi 1 m ada di tengah frame |
| Resolusi | **48 × 48**, 100% | = 1 cell sprite = 1 tile Unity (PPU 48) |

Origin model (pusat kaki, Z=0) jatuh **±14 px di atas tepi bawah cell**, bukan tepat di tepi bawah. Tepi depan siluet slime turun sampai ±8 px karena lebih dekat ke kamera. Programmer perlu tahu ini untuk offset collider dan blob shadow di Unity (sudah diperhitungkan di DemoSlime).

### B.2 Render settings
| Setting | Nilai | Kalau salah |
|---|---|---|
| Engine | EEVEE, Samples **1** | Sample banyak = tepi halus (anti-aliasing), padahal pixel art harus tegas |
| Film > Filter Size | **0.00 px** | Tepi blur/abu-abu, lalu muncul pixel setengah transparan |
| Film > Transparent | **On** | Ada background di sprite |
| Color Management > View | **Standard**, Look None, Exposure 0, Gamma 1 | AgX/Filmic (default Blender) menggeser warna, sehingga putih jadi abu dan warna jadi pudar |
| Dither | **0** | Muncul noise ±1 di warna flat dan normal map |
| Output | PNG, RGBA, 8 bit | |

### B.3 "Shader": material flat (Emission)
Semua material render-nya **Emission**, bukan Principled/Diffuse. Emission berarti warna keluar apa adanya tanpa dipengaruhi lampu, sehingga hasilnya flat dan tidak ada gradasi cahaya di albedo.

Kamu **tidak perlu** bikin material emission sendiri. Saat render, add-on sementara mengganti tiap material jadi Emission, lalu mengembalikannya:
- Warna diambil dari **Base Color** (Principled), **Color** (Diffuse/Toon), atau Color node Emission yang sudah ada.
- Texture yang tersambung ke Base Color ikut terpakai.
- Material toon lama (Diffuse → Shader to RGB → ramp) diabaikan, karena hasilnya bergantung cahaya. Warna diambil dari BSDF-nya.

Jadi di viewport kamu bebas pakai material apa pun untuk preview. Yang penting **Base Color = warna flat yang diinginkan**.

### B.4 Tiga layer yang dirender
| Layer | Isi | Cara dibuat |
|---|---|---|
| **body** (mode Slime) | Siluet putih flat + outline abu `#282828` 1px | Semua material Body dipaksa putih. Warna slime ditentukan palet 4 warna di material Unity (gradient map), karena warna slime berubah terus sesuai bahan yang dimakan. Jadi warna slime di Blender **tidak berpengaruh** |
| **albedo** (mode Warna flat) | Warna dasar flat + outline versi gelap warna objek | Untuk musuh, toples, prop: warnanya tetap |
| **face** | Mata, mulut, kilau | Body dijadikan *holdout* (memotong tapi transparan), jadi mata yang tertutup badan tidak ikut. Tampak belakang = kosong |
| **normal** | Arah permukaan tiap pixel | Material override `GN_NormalPass`, View Transform **Raw** |

Normal map: Geometry > Normal → Vector Transform (World → Camera) → ×(0.5, 0.5, −0.5) + 0.5 → Emission. Hasilnya R = kanan, G = atas, B = ke arah penonton (Z dibalik karena ruang kamera Blender menghadap +Z). Permukaan menghadap layar = `(128, 128, 255)`. Unity memakai ini untuk tahu sisi mana yang terkena lampu.

### B.5 Pixelize (otomatis setelah render)
1. Alpha < 50% dibuang, sisanya dibuat 100%, supaya tidak ada pixel setengah transparan.
2. Outline 1px ditambahkan di luar siluet.
3. Normal map di-pad 1px supaya pixel outline juga punya arah permukaan (ikut terkena cahaya tepi).
4. Frame disusun jadi sheet horizontal, frame 0 di kiri.
5. Preview dibuat (lampu kiri/kanan).

Semua dikerjakan di Python bawaan Blender, tidak perlu install Python/Pillow.

---

## C. Aturan modeling supaya hasilnya bagus di 48px

- **Hadap −Y** (ke arah kamera) = arah `front`. Add-on memutar `Model_Root` 90° untuk `side` (hadap kanan) dan 180° untuk `back`. **Jangan animasikan rotasi `Model_Root`.**
- **Origin di tengah bawah**, berdiri di Z=0 (tombol Pas-kan mengurus ini).
- **Bentuk besar dan jelas.** Detail di bawah ±0.05 m (1 px) hilang. Uji siluet: lihat Tes frame ini, apakah bentuknya masih terbaca.
- **Smooth boleh** (Subdivision, Shade Smooth). Model tidak dipakai di game, hanya hasil render-nya. Shade Smooth bikin normal map halus, sehingga band cahaya di Unity lebih rapi.
- **Mata/detail wajah objek terpisah** dari badan, lalu masuk ke Face. Taruh sedikit di depan permukaan badan supaya tidak tenggelam.
- **Jangan** bikin bayangan jatuh atau lantai yang ikut di-render. Unity pakai blob shadow terpisah.
- **Warna (mode Warna flat):** nada menengah, flat per bagian. Aturannya sama dengan `docs/panduan-art-desainer.md` bagian 2.
- **Animasi:** shape key, lattice, armature, atau keyframe biasa, asal di timeline. Squash & stretch kuat terbaca bagus di 48px.

## D. Masalah umum
| Gejala | Penyebab / solusi |
|---|---|
| Panel GooNapped tidak ada | Add-on belum aktif. Lihat A.0 |
| "Simpan file .blend dulu" | Path output relatif ke file .blend. Save As ke `art/blender/` |
| Warna mata/badan dari OBJ jadi abu | OBJ/FBX tidak membawa warna Emission. Isi **Base Color** material di Blender |
| Model kepotong di frame | Terlalu besar. Kecilkan **Tinggi** lalu Pas-kan, atau diskusikan cell yang lebih besar dengan programmer (spek saat ini dikunci 48px) |
| Sheet berisi frame lama | Render semua menghapus frame mentah anim yang sama sebelum render. Kalau nama anim diganti, hapus sheet lama di `art/sprites/<nama>/` manual |
| Cahaya di Unity terbalik | Normal map salah format (jangan edit di Photoshop dengan profil warna). Render ulang dari template |
| Ingin ubah kamera/resolusi | Jangan diubah per file. Ubah konstanta di `goonapped_sprite_addon.py`, bahas dengan tim, lalu bangun ulang template: `blender -b --factory-startup --python art/tools/make_sprite_template.py` |

## E. File terkait
- `art/blender/sprite_template.blend`: template (jangan diedit langsung, bangun ulang lewat skrip).
- `art/tools/goonapped_sprite_addon.py`: add-on (panel, render, pixelize).
- `art/tools/make_sprite_template.py`: membangun ulang template.
- `art/tools/render_slime.py`: renderer lama khusus `slime_v06.blend` (pakai Action per anim). Model baru pakai template.
