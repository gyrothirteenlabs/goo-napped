# Arsitektur: cara kerja game

Ringkasan sistem untuk programmer. Semua script runtime ada di `Assets/_Game/Scripts/Runtime/` (namespace `GooNapped`).

## Gambaran besar

```
                 Input System (InputSystem_Actions, map "Player")
                                 │
                           PlayerBrain  ── satu-satunya pembaca input
              (aksi: gerak, makan/tukar/telan, belah/gabung, Shift, nyamar)
                                 │ mengendalikan 1 potongan aktif
          ┌──────────────────────┼──────────────────────┐
     SlimePiece (badan)     SlimePiece (nyamar)    SlimePiece (nyamar)
       ├ SlimeMotor   gerak lompat-meluncur (Rigidbody2D)
       ├ SlimeJelly   squash/stretch + angkat (visual saja)
       ├ SlimeBody    menyimpan SlimeBodyState (ciri tubuh) + palet material
       └ SlimeSpriteAnimator  pilih frame sprite body/face
                                 │
     Level: Jar (toples) ─ RecipeData      IngredientPickup ─ IngredientData
            FriendSlime   LevelGoal ─ LevelExit   CameraFollow   LightExposure
                                 │
     Penjaga: Guard (Golem, Familiar, ...) ─ VisionCone ─ VisionMath ─ NoiseBus (suara)
                                 │
     AudioDirector (BGM, ambience, SFX)   GameFlow (urutan level, progres)   PauseMenu / MainMenuController   ProtoHud
```

## Pemain

| Script | Tugas |
|---|---|
| `Player/PlayerBrain` | Baca input, kendalikan **satu** `SlimePiece`. Skill: muntah (Q, `SlimePiece.TryVomit`) dan lempar gumpalan (F, `TryThrow` → `GooBlob` → `NoiseBus`). Belah (ancang + cooldown), gabung, Shift (cooldown + "bangun"), interaksi E (telan teman → tukar toples → makan bahan), intip toples. Jeda aksi lewat `lockTimer`. |
| `Slime/SlimePiece` | Satu potongan. Tidak membaca input. Hadap 4 arah, makan, transformasi nyamar bertahap (`disguiseT` 0 → 1), animasi belah (busur) dan terserap (gabung), daftar teman yang dibawa (`Carried`). |
| `Slime/SlimeMotor` | Gerak: siklus Ancang → Udara → Mendarat → Meluncur. Arah terkunci saat lepas landas (lengket). Event `PhaseEntered` (dipakai untuk suara langkah). `CyclePhase` 0..1 dipakai memilih frame sprite. |
| `Slime/SlimeJelly` | Pegas squash/stretch berporos di kaki + angkat badan + bayangan mengecil. `Hold()` untuk animasi khusus yang tidak boleh ditimpa gerak. |
| `Slime/SlimeBody` + `SlimeBodyState` | Ciri tubuh (logika murni, ada test). Makan menimpa ciri sejenis, jadi urutan penting. Efek samping (lambat, menyala, bau) aktif selama bahannya masih punya ciri. `Clone()` untuk belah, `Absorb()` untuk gabung (ciri potongan aktif menang), `MismatchCount()` untuk resep. |

## Data (ScriptableObject, `Assets/_Game/Data/`)
- **`IngredientData`**: ciri (`Trait` = jenis + nilai teks), pengali kecepatan, menyala, radius bau, memadamkan (`Extinguishes`), material palet body, warna placeholder.
- **`RecipeData`**: nama teman, 2–3 ciri, material palet teman.
- `Trait`: jenis `Color`, `Temperature`, `Weight`, `Shine`, `Smell`. Nilai teks bebas (huruf besar/spasi diabaikan saat dibandingkan).

## Level

| Script | Tugas |
|---|---|
| `Level/Jar` | Toples: resep, sudah diintip, isi (teman atau tiruan). `DecoyMismatch` = ciri tiruan yang salah (penjaga lebih cepat curiga). |
| `Level/FriendSlime` | Teman yang keluar dari toples, ditelan dengan E. |
| `Level/LevelGoal` | Menang: bawa semua teman ke exit. Kalah: `Fail()` dari penjaga → level diulang. `nextScene` = level berikutnya. |
| `Level/LevelExit` | Trigger exit. |
| `Level/CameraFollow` | Ikut potongan aktif, dijepit ke `CameraBounds`. |
| `Level/Extinguishable` | Lentera/obor yang bisa dipadamkan (E) oleh tubuh bersuhu `dingin`. Desisnya `NoiseBus.Emit` (pengalih yang berisiko). |
| `Level/LightExposure` | Seberapa terang sebuah titik oleh Light 2D titik (bukan global), memperhitungkan tembok. Dipakai penjaga. |

## Penjaga

`Enemies/Guard` (state machine, dipakai SEMUA musuh; jenisnya ditentukan setelan prefab):

| Prefab | Setelan |
|---|---|
| `Golem_Placeholder` | kerucut sempit & jauh, lambat, langkah berat |
| `Familiar_Placeholder` | melayang (`hoverHeight`), kerucut lebar, `needsLightToSee` (buta di gelap), `heatSightRange` 7 (melihat yang panas/menyala dari jauh), membawa Light 2D sendiri |
| `Anjing_Placeholder` | penglihatan pendek, `smellRange` 4.5: mencium slime berbau di dekatnya, menyusuri jejak bau (`ScentTrail`), terpancing umpan berbau dari segala arah |


| State | Perilaku |
|---|---|
| Patrol / Wait | Jalan antar titik patroli, berhenti sebentar di ujung. |
| Alert | Melihat/merasakan slime: berhenti, menoleh, curiga naik. |
| Search | Curiga cukup tinggi lalu slime hilang: datangi tempat terakhir slime terlihat (atau umpan). |
| LookAround | Diam di tempat itu, menyapu pandangan kiri-kanan. |
| Blocked | Ada potongan nyamar menghalangi jalan: menatap lalu putar balik. |
| Caught | Curiga penuh → `LevelGoal.Fail()`. |

Cara slime terdeteksi (laju curiga berbeda-beda):
1. **Kerucut pandang**: makin dekat makin cepat. Slime menyala terlihat 1.4× lebih jauh.
2. **Radius sadar**: terlalu dekat dari arah mana pun (anti menguntit dari belakang).
3. **Cahaya**: slime yang berdiri di cahaya lampu (atau menyala sendiri) terlihat dari segala arah dalam `lightSightRange`.
4. **Bau** (penjaga dengan `smellRange`): slime berbau tercium dari dekat tanpa butuh cahaya atau garis pandang. Slime berbau meninggalkan titik bau (`Level/ScentTrail`, pudar 10 detik); pencium menyusuri titik terbaru yang terjangkau langsung, jadi ia mengikuti jalan yang dilalui slime.

Potongan yang nyamar tidak terlihat. Potongan nyamar yang menyala/berbau = **umpan**, dan bunyi dari `NoiseBus.Emit` (gumpalan dilempar) = **pengalih**: penjaga mendatanginya tanpa menaikkan curiga.
`VisionMath` berisi hitungan murni (kerucut, pengali salah resep, faktor jarak) dan punya test.

## UI
- `UI/GameHud` (anak `Hud` di prefab LevelSystems, UI Toolkit): chip ciri tubuh, tag status (kena cahaya, menyala, berbau, lambat, nyamar), kartu resep tiap toples yang sudah diintip (titik hijau = ciri cocok dengan tubuh potongan aktif; setelah ditukar = dibandingkan dengan tiruan), statistik bawah, pesan, banner selesai/ketahuan.
- Layout `Assets/_Game/UI/Hud.uxml`, gaya `Hud.uss`, skala `HudPanelSettings` (referensi 960×540). Desainer mengubah tampilan lewat USS; nama elemen/kelas dipakai kode.
- `UI/GuardMeter` (di tiap prefab penjaga): bar curiga + ikon `?` (memeriksa) / `!` (melihat/mencium) di atas kepala, material unlit supaya terbaca di gelap.
- `Demo/ProtoHud` (HUD teks lama) tinggal dipakai di scene sandbox.

## Alur game
- `Core/GameFlow`: daftar level (`Levels`, urut), progres (PlayerPrefs `gn_unlocked`), `Load`, `ReloadCurrent`, `LoadMainMenu`. **Level baru wajib ditambahkan di `GameFlow.Levels` dan Build Settings.**
- `Core/BootLoader` (scene Boot) → menu utama. `UI/MainMenuController` (scene MainMenu): Mulai/Lanjut, Pilih level (terkunci sampai level sebelumnya selesai), Reset progres.
- `UI/PauseMenu` (di LevelSystems): Esc jeda, R ulang. Tampilan OnGUI = placeholder sampai UI desainer.
- `LevelGoal`: selesai → buka level berikutnya → muat; level terakhir → menu utama.

## Audio
`Audio/AudioDirector` ada di prefab `LevelSystems`: BGM + ambience (2D), SFX posisional (pool 12 AudioSource). Panggil `AudioDirector.Play(AudioDirector.Sfx.X, posisi)` dari mana saja (aman walau tidak ada director). Pendengar dipindah ke bidang permainan (z = 0) supaya jarak suara = jarak di layar. Lentera punya AudioSource loop sendiri (kretek obor).

## Rendering
- Semua sprite memakai shader `GooNapped/PixelToonLit`: cahaya real-time Light 2D + normal map, dipotong jadi band (pixel art). Body slime memakai gradient map palet (`_UseGradient`) yang ikut digelapkan cahaya (`_ShadeGradient`).
- Tembok punya `ShadowCaster2D` (dari TilemapCollider2D), lentera ber-bayangan lembut.
- Sort sumbu Y (Renderer2D). Pixel Perfect Camera 384×216, PPU 48.

## Prefab penting
| Prefab | Isi |
|---|---|
| `Characters/Slime_Player` | SlimePiece + Motor + Jelly + Body + Animator + collider; anak Body, Face, Shadow, Disguise |
| `Systems/LevelSystems` | PlayerBrain + LevelGoal + ProtoHud + AudioDirector (satu per level) |
| `Enemies/Golem_Placeholder` | GolemGuard + animator sprite placeholder |
| `Props/Jar`, `Props/IngredientPickup`, `Characters/FriendSlime`, `Level/Lantern` | objek level |

## Tool editor (`Scripts/Editor/`)
| Menu | Fungsi |
|---|---|
| GooNapped > Blockout > … | Bangun ulang level dari peta teks (`Level/LevelBlockout.cs`) |
| GooNapped > Audio > Buat suara placeholder | Sintesis SFX/BGM placeholder (`Audio/ProtoAudioGenerator.cs`) |
| (otomatis) `Art/PixelSpriteImporter` | Import sprite: grid 48, pivot, normal map otomatis |

## Test
`Tests/EditMode`: state tubuh, resep, urutan makan, gabung, hitungan penglihatan, smoke test proyek. Jalankan lewat Window > General > Test Runner. Tambah test untuk setiap logika murni baru.

## Menambah musuh baru (pola)
1. Kalau cukup beda setelan (jarak, sudut, buta gelap, panas, melayang): duplikat prefab `Familiar_Placeholder`/`Golem_Placeholder`, ubah field `Guard`. Kalau butuh perilaku baru (misal mengendus jejak): tambah opsi di `Guard` atau komponen pendamping, jangan menyalin state machine.
2. Cara diakali harus lewat sifat musuh itu (CLAUDE.md bagian Musuh), bukan menyerang.
3. Prefab di `Prefabs/Enemies/`, sprite placeholder dari `art/tools/make_placeholder_enemies.py`.
4. Tambah huruf di legenda `LevelBlockout` kalau musuhnya dipakai di blockout.
