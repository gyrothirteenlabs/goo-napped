# Game Off 2026: tema "SHIFT"

Game jam itch.io, 1 bulan, mulai 3 Nov 2026, dikerjakan bareng tim. Engine: Unity 6 (6000.3.28f1), URP 2D.
Nama game belum dipilih. Kandidat: Jarbreak, Slime Swap, Goo-napped, Body Double, Shiftling.

## Pembagian kerja (keputusan 9 Nov 2026)
- **Tim desainer:** semua desain visual (karakter, style, palet, UI look, level look), desain level/puzzle, cerita, audio.
- **Programmer tim:** arsitektur gameplay, review & merge.
- **AI (Claude):** kode, tooling render, setup scene/prefab, shader, struktur project, debug, dokumentasi teknis. Membantu programmer, bukan menggantikan keputusan desain.
- AI **tidak mengubah desain art** atas inisiatif sendiri. Kalau diminta "bikin lebih bagus" tanpa arahan, minta arahan konkret (sketsa, referensi, ukuran, warna).
- Detail: `docs/ai-vs-tim.md`. Panduan art desainer: `docs/panduan-art-desainer.md`. Desainer 3D: `docs/panduan-blender-3d.md` (template `art/blender/sprite_template.blend`). Pipeline art/Blender: `art/CLAUDE.md` (baca dulu sebelum kerja di Blender atau sprite).

## Status (10 Nov 2026, minggu 2)
- Sudah: konsep gameplay, lore, objektif. Pipeline sprite Blender → Unity (template + add-on v1.1). Lighting real-time (normal map + toon band). Struktur project + 18 scene placeholder. Placeholder 4 musuh (bukan desain final).
- Prototipe mekanik (branch `feat/prototipe-mekanik`, scene `Sandbox/ProtoMekanik`): gerak analog berdenyut, makan bahan + state tubuh, belah/gabung, Shift antar potongan.
- Toples + resep + exit juga sudah jalan di sandbox (alur intip → makan → belah → tukar → telan → exit).
- Penjaga Golem Batu (patroli, kerucut lurus, tingkat curiga, ketahuan = ulang level) jalan di sandbox.
- Nyamar jadi barang (tahan C, potongan yang ditinggal otomatis) + umpan (potongan nyamar yang menyala didatangi penjaga). Golem: datangi + sapu pandangan, radius sadar.
- Blockout chapter 1 lengkap (C1_L01 tutorial, L02 kenalan Golem, L03 urutan makan + 2 golem, L04 umpan + gerbang), bisa dimainkan berurutan; kamera ikut potongan aktif. Dibangun dari peta teks: menu GooNapped > Blockout (`Scripts/Editor/Level/LevelBlockout.cs`).
- Belum: desain slime final (tim desainer), musuh lain, tileset level final, art bahan/toples, UI (sekarang HUD teks sementara `ProtoHud`).
- Mood: **gelap** (disetujui 10 Nov, lihat `art/CLAUDE.md`). `art/palette.hex` masih USULAN.
- Usulan urutan chapter/level: `docs/rundown-level.md` (belum disepakati tim).

## Keputusan desain 10 Nov 2026
- **Gerak analog (bebas)** untuk slime dan musuh. Gerak per tile ala Pokemon sempat dicoba 10 Nov dan dibatalkan (terlalu kaku). Rasa "lengket dan lamban" datang dari akselerasi + kecepatan yang berdenyut mengikuti animasi jalan, dan dari animasi yang lebih ekspresif (frame boleh ditambah: sprite 48px ringan di engine, biayanya di waktu render/cek).
- **Bahan = barang alkemi** (ramuan, serbuk, minyak, esens), bukan bahan dapur. Contoh: Ramuan Api, bukan cabai.
- **Lokasi level bervariasi**, tidak hanya dalam ruangan. Musuh muncul di tempat yang masuk akal (Golem Batu di area bebatuan).

## Struktur project
```
Assets/
  _Game/                      semua aset milik game (pisah dari pihak ketiga)
    Art/
      Sprites/                Characters/Slime, Enemies, Props, Environment, UI, VFX (import otomatis)
      Materials/              Slime_* (palet state bahan), Slime_Face, Env_Lit
      Shaders/                PixelToonLit.shader (semua sprite lit)
      Animations/ Tiles/ Fonts/
    Audio/                    Music, SFX
    Data/                     ScriptableObject: Ingredients, Recipes, Levels
    Prefabs/                  Characters, Enemies, Props, Level, UI, Systems
    Scenes/
      Boot/Boot.unity         build index 0, init service global
      Menus/                  MainMenu, ChapterSelect, Credits
      Story/                  Intro, Ending
      Levels/Chapter1..3/     C<ch>_L<nn>.unity (12 level, chapter 3 kandidat dipotong)
      Sandbox/                DemoSlime, SampleScene (tidak masuk build)
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
- **Script baru** masuk `Assets/_Game/Scripts/Runtime/<Sistem>/`. Editor tool masuk `Scripts/Editor/`. Jangan taruh script di luar asmdef.
- **Input:** Input System (`Assets/Settings/InputSystem_Actions.inputactions`). `SlimeDemoMover` hanya prototipe.
- **Rendering:** semua sprite pakai `GooNapped/PixelToonLit`. Light 2D yang perlu efek bentuk: `Normal Map Quality = Accurate`. Pixel Perfect Camera 384×216, PPU 48. Sort sumbu Y (Renderer2D).
- **Aset:** sprite masuk `Assets/_Game/Art/Sprites/`, normal map `<nama>_normal` dipasang otomatis. Jangan ubah import setting manual.
- **Scene:** jangan edit scene yang sama bersamaan (konflik YAML). Kerja di prefab kalau bisa. Klaim scene di chat tim sebelum edit.
- **Test:** smoke test di `Tests/EditMode`. Tambah test untuk logika murni (resep, state slime).

## Git
- `main` = stabil, hanya lewat PR + review. Jangan push langsung, jangan force-push.
- Branch: `feat/<fitur>`, `fix/<bug>`, `art/<aset>`, `level/<cx_lyy>`, `docs/<topik>`.
- Commit pendek, bahasa Indonesia/Inggris boleh, prefix area: `art:`, `unity:`, `feat:`, `fix:`, `docs:`, `level:`.
- AI: `git add` file spesifik (bukan `-A`), **tanpa atribusi AI** di commit/PR.
- `.blend`, `.png`, audio lewat Git LFS (`.gitattributes`). `Library/`, `Temp/`, `Assets/Screenshots/` di-ignore.
- `Packages/` dan `ProjectSettings/` dibahas tim dulu sebelum di-commit.
- Aturan Game Off: repo GitHub publik. Jangan commit aset berbayar yang lisensinya melarang redistribusi.

## Genre & feel
Puzzle stealth 2D top-down, level pendek (2–4 menit). Referensi feel: Robbery Bob (aktif, lucu, chaotic), cerita ringan ala Helltaker.

## Lore
- Kerajaan slime. Tiap warga punya kekuatan (api, kristal, bayangan, dll).
- Alkemis jahat menculik hampir semua slime dan menyedot kekuatannya untuk menguasai dunia.
- Slime yang diculik disimpan di TOPLES SEGEL di markas alkemis.
- Pemain adalah satu slime yang lolos dan ingin menyelamatkan warga serta keluarganya.
- Pemain tidak bisa menyerang/membunuh. Murni stealth.
- Slime yang diselamatkan hanya untuk lore, TIDAK jadi skill.
- Ending: alkemis disegel di toplesnya sendiri (peran tertukar), kekuatan slime kembali, dan alat alkemis dipakai untuk membangun ulang kerajaan.

## Objektif per level
1. Intip toples untuk melihat ciri teman yang dikurung.
2. Kumpulkan bahan sambil menghindari penjaga.
3. Makan bahan sesuai "resep" supaya tubuh punya ciri yang sama.
4. Belah diri: potongan masuk toples menyamar jadi teman, lalu teman ditelan untuk dibawa kabur.
5. Kabur ke exit tanpa ketahuan.

Alkemis tidak sadar karena toples masih berisi slime (hanya slime yang bisa menggantikan slime).

## Mekanik
- Gerak analog berupa lompatan kecil (ancang, lompat, mendarat, meluncur) + squash/stretch berpegas di engine. Bahan berat = lompatan pendek dan lambat. Spek frame sprite: `docs/panduan-blender-3d.md` C.1.
- Makan bahan alkemi: tubuh berubah sesuai bahan (Ramuan Api = merah + panas + menyala, Serbuk Batu = abu + berat, dll). Daftar final oleh tim.
- Belah diri: potongan mewarisi sifat tubuh saat itu. Bisa dipakai jadi tiruan di toples ATAU umpan.
- Nyamar jadi barang untuk ngumpet dari penjaga.
- Tombol Shift untuk ganti kontrol antar potongan. Potongan yang ditinggal otomatis nyamar.
- Gabung lagi hanya bisa kalau potongan bersentuhan langsung.
- Resep tiruan: tiap slime di toples punya 2–3 ciri (warna, suhu, berat, kilau, bau).
- Sifat bahan memengaruhi stealth: habis minum Ramuan Api jadi menyala dan gampang terlihat, habis makan Serbuk Batu jadi lambat.
- Bahan dipakai untuk resep ATAU umpan, jadi pemain harus memilih.
- Salah resep tidak langsung kalah. Penjaga cuma jadi lebih cepat curiga.

## Musuh
Musuh dibuat dari kekuatan slime curian. Pemain mengakalinya lewat sifat musuh itu sendiri, bukan melawan.
- Golem Batu: patroli, lambat, melihat lurus ke depan.
- Familiar Api: melihat benda panas/menyala.
- Anjing Lendir: mengendus bau bahan yang dimakan.
- Mata Penyihir: memeriksa toples, bisa membongkar tiruan.

Cara mengakali: nyamar, potongan umpan, urutan makan bahan, umpan pembawa sifat (misal potongan berbau).

## Hubungan ke tema SHIFT
Shapeshift (jadi barang dan jadi teman), tubuh yang terus berubah karena bahan, shift kontrol antar potongan (tombol Shift), dan peran tertukar di ending.

## Scope
- 3 chapter, 10–12 level, 3–4 musuh, 6–8 bahan.
- Minggu 1: prototipe mekanik dasar (gerak, makan, belah, gabung, 1 penjaga, 1 toples).
- Minggu 2: sistem resep, musuh, level kasar.
- Minggu 3: semua level, art, sound, cerita.
- Minggu 4: playtest, bug fix, halaman itch.io. TIDAK tambah fitur.
- Kalau molor, potong musuh ke-4 dan chapter 3.

## Cara diskusi
- Bahasa Indonesia santai, singkat, langsung ke inti. Istilah teknis boleh bahasa Inggris.
- Kasih pendapat jujur, jangan cuma ikut opini user. Kalau ada yang lemah, bilang.
- Kalau kasih opsi, sertakan kecocokannya ke tema dan risikonya.
- Saat diskusi desain game, fokus satu topik dulu.
- Kerjaan teknis (kode, scene, shader, tooling, pipeline render) langsung kerjakan. Kerjaan desain art: bantu eksekusi kalau ada arahan dari tim desainer.

## Next step
- Programmer: musuh ke-2 (Familiar Api) + blockout chapter 2. Sudah ada: `GolemGuard` + `VisionCone` (penjaga), `Jar`/`RecipeData`/`LevelGoal` (objektif). Sudah ada: `PlayerBrain` (input), `SlimePiece` + `SlimeMotor` (badan), `SlimeBodyState` (ciri), `IngredientData`.
- Desainer: desain final slime + palet state bahan, tileset chapter 1 (albedo flat + normal), toples, Golem Batu.
- Tim: daftar bahan + ciri, slime teman per chapter, daftar level.
