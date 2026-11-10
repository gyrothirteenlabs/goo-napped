# Goo-napped (nama kerja)

Game puzzle stealth 2D top-down untuk **GitHub Game Off 2026** (tema **SHIFT**). Kamu slime kecil yang lolos dari alkemis jahat. Makan bahan alkemi untuk mengubah tubuh, belah diri, menyamar jadi barang, dan tukar dirimu dengan teman yang dikurung di toples segel, tanpa ketahuan penjaga.

Engine **Unity 6 (6000.3.28f1)**, URP 2D. Art dibuat di **Blender** lalu dirender jadi sprite pixel 48 px.

---

## Mulai cepat

1. Install **Unity 6000.3.28f1** (Unity Hub) dan **Git LFS** (`git lfs install`). Sprite, `.blend`, dan audio disimpan lewat LFS.
2. Clone repo, buka folder ini di Unity Hub.
3. Buka scene `Assets/_Game/Scenes/Boot/Boot.unity`, tekan **Play** (masuk menu utama → Mulai / Pilih level). Atau buka scene level langsung, misal `Scenes/Levels/Chapter1/C1_L01.unity`. Level bersambung otomatis: C1_L01 → C1_L04 → C2_L01 → C2_L04.

### Kontrol
| Tombol | Aksi |
|---|---|
| WASD / panah | Gerak (lompatan kecil yang merayap) |
| E | Makan bahan / tukar dengan isi toples / telan teman / padamkan lentera (butuh tubuh dingin) |
| Space | Belah diri (ke arah hadap) / gabung kalau menyentuh potongan lain |
| Shift | Pindah kendali ke potongan lain (yang ditinggal otomatis nyamar) |
| Tahan C | Nyamar jadi barang di tempat |
| Q | Muntahkan bahan terakhir (tubuh kembali seperti sebelumnya, bahan jatuh ke lantai) |
| F | Lempar gumpalan lendir: bunyinya memancing penjaga (cooldown 5 detik) |
| R | Ulang level |
| Esc | Jeda (lanjut / ulang / menu utama) |

### Alur satu level
Intip toples (lihat resep) → makan bahan sesuai resep (urutan penting: ciri sejenis tertimpa) → belah → potongan masuk toples jadi tiruan → telan teman → kabur ke exit (kotak hijau). Ketahuan penjaga = level diulang.

---

## Dokumentasi

| Dokumen | Isi | Untuk |
|---|---|---|
| [docs/arsitektur.md](docs/arsitektur.md) | Cara kerja game: sistem, script, alur data | Programmer |
| [docs/cara-edit-unity.md](docs/cara-edit-unity.md) | Resep kerja di Unity: bikin level, bahan, resep, penjaga, suara | Semua |
| [docs/panduan-blender-3d.md](docs/panduan-blender-3d.md) | Model Blender → sprite 48 px (template + add-on 1 klik) | Desainer 3D |
| [docs/panduan-art-desainer.md](docs/panduan-art-desainer.md) | Aturan sprite 2D: albedo flat, normal map, tile | Desainer 2D |
| [docs/rundown-level.md](docs/rundown-level.md) | Usulan urutan chapter & level | Desainer level |
| [docs/backlog.md](docs/backlog.md) | Daftar fitur: selesai / dikerjakan / berikutnya | Semua |
| [docs/ai-vs-tim.md](docs/ai-vs-tim.md) | Pembagian kerja tim dan AI | Semua |
| [CLAUDE.md](CLAUDE.md), [art/CLAUDE.md](art/CLAUDE.md) | Keputusan desain + aturan teknis (juga dibaca asisten AI) | Semua |

---

## Struktur folder (standar)

```
Assets/
  _Game/                  SEMUA aset milik game. Jangan taruh aset game di luar folder ini.
    Art/
      Sprites/            sprite import otomatis (PPU 48, Point, grid 48, pivot tengah-bawah)
        Characters/  Enemies/  Props/  Environment/  UI/  VFX/
        Tiles/            sprite untuk Tilemap: pivot otomatis TENGAH
      Materials/          Slime_* (palet per state bahan), Slime_Face, Env_Lit, Enemy_*
      Shaders/            PixelToonLit.shader (semua sprite lit)
      Tiles/              aset Tile (Placeholder/ = blockout)
      Animations/  Fonts/
    Audio/  Music/  SFX/  (proto_* = placeholder hasil sintesis)
    UI/                   UI Toolkit: Hud.uxml (layout), Hud.uss (gaya), HudPanelSettings, tema
    Data/                 ScriptableObject: Ingredients/, Recipes/, Levels/
    Prefabs/              Characters/  Enemies/  Props/  Level/  Systems/  UI/
    Scenes/
      Boot/  Menus/  Story/
      Levels/Chapter1..3/ C<ch>_L<nn>.unity
      Sandbox/            scene uji (tidak masuk build)
    Scripts/
      Runtime/            asmdef GooNapped.Runtime, namespace GooNapped
        Audio/ Core/ Demo/ Enemies/ Ingredients/ Level/ Player/ Slime/ UI/
      Editor/             asmdef GooNapped.Editor: Art/ Audio/ Level/ (tool editor)
    Tests/EditMode/  PlayMode/
  ThirdParty/             aset pihak ketiga + lisensinya (wajib boleh diredistribusi: repo publik)
  Settings/               URP, Renderer2D, Input Actions
art/                      sumber art di luar Unity
  blender/                .blend (template, model, placeholder)
  sprites/<nama>/         hasil render siap copy ke Assets/_Game/Art/Sprites/
  renders/raw/            frame mentah (tidak di-commit)
  tools/                  add-on Blender + skrip render/pixelize
  palette.hex  ref/
docs/                     dokumentasi tim
```

### Aturan nama
| Jenis | Pola | Contoh |
|---|---|---|
| Scene level | `C<chapter>_L<nomor>` | `C1_L03.unity` |
| Script | PascalCase, 1 MonoBehaviour per file, nama file = nama class | `GolemGuard.cs` |
| Prefab | `<Jenis>_<Nama>` atau nama benda | `Golem_Placeholder`, `Jar`, `Lantern` |
| Data | `Ingredient_<Nama>`, `Recipe_<NamaTeman>` | `Ingredient_SerbukBatu.asset` |
| Sprite sheet | `<aset>_<layer>_<anim>_<arah>.png` | `slime_body_move_side.png`, `golem_albedo_idle_front.png` |
| Normal map | ganti `_body_`/`_albedo_` jadi `_normal_`, atau tambah `_normal` | `golem_normal_idle_front.png`, `crate_normal.png` |
| Objek Blender | `<Karakter>_<Bagian>` | `Slime_Body`, `Golem_Head` |
| File Blender | `art/blender/<nama>_v01.blend` (naik versi sebelum perubahan besar) | `golem_v02.blend` |
| Placeholder | awalan `proto_` atau akhiran `_placeholder` | `proto_bgm_stealth.wav`, `golem_placeholder_v01.blend` |

---

## Git
- `main` stabil, hanya lewat PR + review. Branch: `feat/…`, `fix/…`, `art/…`, `level/c1_l03`, `docs/…`.
- Commit pendek dengan awalan area: `feat:`, `fix:`, `art:`, `level:`, `unity:`, `docs:`.
- **Jangan edit scene yang sama bersamaan** (konflik YAML). Klaim scene di chat tim dulu. Kerja di prefab kalau bisa.
- `Packages/` dan `ProjectSettings/` dibahas tim dulu sebelum di-commit.
- Aset berbayar/berlisensi tertutup **tidak boleh** masuk repo (aturan Game Off: repo publik).
