# Game Off 2026: tema "SHIFT"

Game jam itch.io, 1 bulan, mulai 3 Nov 2026, dikerjakan bareng tim. Engine: Unity 6 (6000.3.28f1), URP 2D.
Nama sementara: **Goo-napped** (nama final diputuskan setelah game jadi).

**Redesign v2 (11 Nov 2026):** lore dan mekanik baru dari jawaban tim. Sumber utama: `docs/desain-v2.md` (branch `feat/redesign-lore`). Bagian Lore, Objektif, Mekanik, dan Musuh di bawah sudah versi 2. Versi 1 (resep 2–3 ciri, telan teman, nyamar bebas, chapter 1–2) ada di `main` lewat PR #5 dan disimpan sebagai sandbox.

## Pembagian kerja (keputusan 9 Nov 2026)
- **Tim desainer:** semua desain visual (karakter, style, palet, UI look, level look), desain level/puzzle, cerita, audio.
- **Programmer tim:** arsitektur gameplay, review & merge.
- **AI (Claude):** kode, tooling render, setup scene/prefab, shader, struktur project, debug, dokumentasi teknis. Membantu programmer, bukan menggantikan keputusan desain.
- AI **tidak mengubah desain art** atas inisiatif sendiri. Kalau diminta "bikin lebih bagus" tanpa arahan, minta arahan konkret (sketsa, referensi, ukuran, warna).
- Detail: `docs/ai-vs-tim.md`. Panduan art desainer: `docs/panduan-art-desainer.md`. Desainer 3D: `docs/panduan-blender-3d.md` (template `art/blender/sprite_template.blend`). Pipeline art/Blender: `art/CLAUDE.md` (baca dulu sebelum kerja di Blender atau sprite).

## Status (11 Nov 2026, minggu 2)
- **v1 lengkap dan lolos QA otomatis, sudah di `main`** (PR #2–#5): gerak, makan bahan, belah/gabung/Shift, toples + resep, Golem/Familiar/Anjing, pathfinding A*, HUD UI Toolkit, sistem cerita (strip + dialog), menu/pause/progres, props (pintu, tuas, pelat, celah), 8 level blockout, validator level + bot QA.
- **v2 dimulai 11 Nov** di `feat/redesign-lore`. Urutan kerja ada di `docs/desain-v2.md` bagian 17, progres di `docs/backlog.md`.
- Belum (tim desainer): desain slime final + teman, sprite musuh baru, tileset 3 area + puncak, props tempat sembunyi, toples 2 ukuran, isi cerita, audio final. Belum ada playtest manusia.
- Mood: **gelap**, palet beda per area (`art/CLAUDE.md`). `art/palette.hex` masih USULAN.
- `docs/rundown-level.md` adalah rundown v1 dan sudah usang. Daftar level v2 ada di `docs/desain-v2.md` bagian 3.

## Keputusan desain
- **10 Nov:**
  - Gerak analog (bebas). Gerak per tile sudah dicoba dan dibatalkan.
  - Bahan = barang alkemi, di v2 menjadi **esens**.
  - Lokasi level bervariasi, dan musuh muncul di tempat yang masuk akal.
- **11 Nov (v2):**
  - Nada: dunia gelap, slime tetap lucu.
  - Ketahuan = **dikejar**. Tertangkap = ulang dari **ventilasi terakhir**.
  - Satu tingkat kesulitan.
  - Teks game **bahasa Inggris**.
  - Rilis **build Windows**, kontrol keyboard dulu.
  - Teman **mengikuti** pemain, tidak lagi ditelan.
  - Sembunyi **hanya di objek**.
  - **Maksimal 2 potongan**, tubuh mengecil saat membelah.
  - Gerak diganti rasa slime Terraria, tapi lompatannya rendah.
  - Detail: `docs/desain-v2.md`.

## Struktur project
```
Assets/
  _Game/                      semua aset milik game (pisah dari pihak ketiga)
    Art/
      Sprites/                Characters/Slime, Enemies, Props, Environment, UI, VFX (import otomatis)
      Materials/              Slime_* (palet aura/esens), Slime_Face, Env_Lit
      Shaders/                PixelToonLit.shader (semua sprite lit)
      Animations/ Tiles/ Fonts/
    Audio/                    Music, SFX
    Data/                     ScriptableObject: Ingredients (esens), Recipes (data teman), Story
    Prefabs/                  Characters, Enemies, Props, Level, UI, Systems
    Scenes/
      Boot/Boot.unity         build index 0, init service global
      Menus/                  MainMenu, ChapterSelect, Credits
      Story/                  Intro, Ending
      Levels/                 level v2 (scene v1 C1/C2 jadi sandbox)
      Sandbox/                ruang uji (tidak masuk build)
    Scripts/
      Runtime/                asmdef GooNapped.Runtime, namespace GooNapped
        Core/ Player/ Slime/ Ingredients/ Enemies/ Level/ UI/ Audio/ Demo/
      Editor/                 asmdef GooNapped.Editor (PixelSpriteImporter, tool editor)
    Tests/EditMode, PlayMode  asmdef test (Test Runner)
  ThirdParty/                 aset/plugin pihak ketiga + lisensinya
  Settings/                   URP, Renderer2D, Input Actions
art/                          sumber art (Blender, sprite final, tools). Lihat art/CLAUDE.md
docs/                         panduan tim
```
Tiap scene placeholder punya objek `_SceneInfo` (komponen `SceneNote`) berisi tujuan scene dan TODO.

## Konvensi teknis
- **Kode:** C#, namespace `GooNapped` (sub-namespace per folder kalau perlu). PascalCase untuk tipe/method/properti, camelCase untuk field privat, `[SerializeField] private` daripada field publik untuk kode baru. Satu class MonoBehaviour per file, nama file = nama class.
- **Bahasa:** teks yang tampil di game = **bahasa Inggris**. Komentar kode dan dokumen tim = bahasa Indonesia.
- **Script baru** masuk `Assets/_Game/Scripts/Runtime/<Sistem>/`. Editor tool masuk `Scripts/Editor/`. Jangan taruh script di luar asmdef.
- **Input:** Input System (`Assets/Settings/InputSystem_Actions.inputactions`). `SlimeDemoMover` hanya prototipe.
- **Rendering:** semua sprite pakai `GooNapped/PixelToonLit`. Light 2D yang perlu efek bentuk: `Normal Map Quality = Accurate`. Pixel Perfect Camera 384×216, PPU 48. Sort sumbu Y (Renderer2D).
- **Aset:** sprite masuk `Assets/_Game/Art/Sprites/`, normal map `<nama>_normal` dipasang otomatis. Jangan ubah import setting manual.
- **Scene:** jangan edit scene yang sama bersamaan (konflik YAML). Kerja di prefab kalau bisa. Klaim scene di chat tim sebelum edit.
- **Test:** smoke test di `Tests/EditMode`. Tambah test untuk logika murni (massa, aura, state slime).

## Git
- `main` = stabil, hanya lewat PR + review. Jangan push langsung, jangan force-push.
- Branch: `feat/<fitur>`, `fix/<bug>`, `art/<aset>`, `level/<cx_lyy>`, `docs/<topik>`.
- Commit pendek, bahasa Indonesia/Inggris boleh, prefix area: `art:`, `unity:`, `feat:`, `fix:`, `docs:`, `level:`.
- AI: `git add` file spesifik (bukan `-A`), **tanpa atribusi AI** di commit/PR.
- `.blend`, `.png`, audio lewat Git LFS (`.gitattributes`). `Library/`, `Temp/`, `Assets/Screenshots/` di-ignore.
- `Packages/` dan `ProjectSettings/` (termasuk daftar Build Settings) dibahas tim dulu sebelum di-commit.
- Aturan Game Off: repo GitHub publik. Jangan commit aset berbayar yang lisensinya melarang redistribusi.

## Genre & feel
Puzzle stealth 2D top-down. Level pendek (2–4 menit), total 30–45 menit. Dunianya gelap dan tegang, slime-nya tetap lucu.
Referensi:
- Zelda: struktur level top-down.
- Robbery Bob: stealth lucu-kacau, tempat sembunyi.
- Helltaker: cerita lewat strip.
- Clock Tower SNES: dikejar, sembunyi yang bisa gagal.

## Lore (ringkas, lengkap di `docs/desain-v2.md` bagian 2)
- Kerajaan slime hidup tentram. Tiap slime punya **aura**: daya hidup sekaligus kekuatan tersembunyi.
- **Alkemis** (makhluk lain, bukan slime) ditumpangi **roh jahat kuno** yang butuh aura untuk menaklukkan dunia. Ia merebut kastil, lalu:
  - warga disadap, sebagian sampai mati;
  - warga yang auranya tersembunyi dikurung di **toples jahat**;
  - **raja** dikurung di puncak, dekat mesin penyadap.
- **Pemain** adalah slime biasa dengan **aura bening** (tidak terbaca). Ia dibuang dan selamat. Tubuh beningnya bisa **meniru aura lewat esens**. Pendiam tapi ekspresif.
- **Jalur:** gerbang (monster) → taman (golem, anjing, lumpur) → ventilasi → penjara bawah tanah → lab eksperimen → kastil atas (aula, ruang makan, galeri) → puncak.
- Teman yang diselamatkan **mengikuti** pemain, lalu dievakuasi ke **ventilasi** (markas). Beberapa teman memberi **kekuatan terbatas**.
- Pemain tidak bisa menyerang. Murni stealth.
- **Ending:**
  - alkemis tersegel di toplesnya sendiri (peran tertukar);
  - kutukan hilang dan alkemis lenyap;
  - aura kembali, dan yang mati hidup lagi;
  - alat alkemis dipakai membangun ulang kerajaan.

## Objektif per level
- **Level menyusup** (gerbang, taman): sampai ke ventilasi tujuan.
- **Level penyelamatan:**
  1. Temukan ruang eksperimen lewat petunjuk lingkungan (tanpa penanda). Toples berpendar sesuai aura teman, dan ukuran toples = ukuran teman.
  2. Makan esens (jumlahnya pas-pasan) sampai massa cukup. **Esens terakhir = aura teman.**
  3. Belah. Potongan dengan **massa = ukuran teman** masuk toples sebagai tiruan, dan teman keluar.
  4. Ajak teman mengikuti (F), lalu kawal ke ventilasi tanpa ketahuan. Alarm penyadapan berbunyi setelah beberapa detik.

## Mekanik (detail di `docs/desain-v2.md` bagian 4–11)
- **Gerak:** lompatan rendah ala slime Terraria (busur ±5 px, squash/stretch, goyang jeli), akselerasi halus.
- **Massa:** awal 1, tiap esens +1, maksimal 4.
  - Besar = lambat, lebih terlihat, berisik.
  - Kecil = muat lewat celah.
- **Esens:** memberi **aura** (dari esens terakhir) dan efek samping (menyala, berbau, berat, dingin, gelap). Muntah (Q) mengeluarkan esens terakhir.
- **Belah dan Shift:**
  - Belah (Space) maksimal 2 potongan, massa dibagi.
  - Gabung kalau bersentuhan.
  - **Shift** = pindah kendali antar potongan, lalu ke teman di ventilasi.
- **Transformasi yang terlihat penjaga** (belah, gabung, potongan bangun, masuk/keluar sembunyi) = curiga melonjak.
- **Potongan yang ditinggal** diam dan bisa terlihat atau tertangkap. Dipakai sebagai umpan dan penahan pelat.
- **Sembunyi (C)** hanya di objek.
  - Tidak bisa bergerak saat sembunyi.
  - Barang yang ditatap penjaga = curiga naik.
  - Pemeriksa membuka tempat sembunyi.
- **Teman yang dikawal:**
  - menyusuri jejak pemain, sedikit lebih lambat, dan diam saat pemain diam;
  - ikut sembunyi, tapi lebih cepat dicurigai;
  - bisa diperintah tunggu/ikut (F).
- **Ketahuan:**
  - Penjaga mengejar dan memanggil penjaga lain.
  - Putus pandangan lalu sembunyi = lolos.
  - Tertangkap (pemain, potongan, atau teman) = ulang dari ventilasi terakhir.
- **Ventilasi:** aman total, checkpoint, exit, jaringan jalan pintas, markas teman.
- **Lingkungan:**
  - Cahaya = bahaya (tetap).
  - Lumpur meninggalkan jejak bau selama beberapa detik dan bisa dicuci di genangan.
- **Kekuatan teman** (hanya beberapa, jatah per checkpoint): Dentum, Padam, Umpan Kristal, Kabut. Segel Raja dipakai di final.

## Musuh (detail di `docs/desain-v2.md` bagian 9)
Musuh dibuat dari aura curian. Pemain mengakalinya lewat sifat musuh itu sendiri.
- **Generik:** Golem Batu, Anjing Lendir.
- **Unik per tempat:**
  - Monster Gerbang.
  - Familiar Api.
  - Mata Penyihir: memeriksa toples dan tempat sembunyi.
  - Zirah Hampa: buta, memburu suara.
  - Pelayan Tudung: membuka tempat sembunyi.
  - Lukisan Bermata: diam, menyapu pandangan.
  - Alkemis: boss stealth, pemburu.

## Hubungan ke tema SHIFT
Urutan penonjolan:
1. Berubah bentuk: belah, tiruan, sembunyi.
2. Pindah kendali: tombol Shift, juga ke teman di ventilasi.
3. Tubuh berubah karena esens.
4. Peran tertukar di ending.
5. Pergantian shift jaga.
6. Dunia berubah.

## Scope
- 3 area + puncak, 11 level (usulan di `docs/desain-v2.md` bagian 3).
- 9 jenis musuh (2 generik, 7 unik), 7 esens, 4 kekuatan teman.
- Minggu 2–3: sistem v2 (urutan di desain-v2 bagian 17), level, art, cerita.
- Minggu 4: playtest, bug fix, halaman itch.io. TIDAK tambah fitur.
- Potong fitur: belum diputuskan (keputusan tim: siapkan semua dulu).

## Cara diskusi
- Bahasa Indonesia santai, singkat, langsung ke inti. Istilah teknis boleh bahasa Inggris.
- Kasih pendapat jujur, jangan cuma ikut opini user. Kalau ada yang lemah, bilang.
- Kalau kasih opsi, sertakan kecocokannya ke tema dan risikonya.
- Saat diskusi desain game, fokus satu topik dulu.
- Kerjaan teknis (kode, scene, shader, tooling, pipeline render) langsung kerjakan. Kerjaan desain art: bantu eksekusi kalau ada arahan dari tim desainer.

## Next step
- AI/programmer: kerjakan urutan di `docs/desain-v2.md` bagian 17, catat progres di `docs/backlog.md`.
- Tim: jawab daftar ❓ di `docs/desain-v2.md` bagian 18.
- Desainer: slime final + teman, sprite musuh v2, tileset per area, props tempat sembunyi, toples 2 ukuran, strip cerita.
