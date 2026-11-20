# Lisensi audio (repo publik, aturan Game Off)

Sumber: zip tim "Gamedev Ekskul - Game Off 2026" (`03_Audio`), dicek & dipasang 20 Nov 2026. Lisensi dicek di halaman
Freesound masing-masing. Kredit untuk pemain: `CREDITS.txt` di root repo (disalin otomatis ke folder build).

| File di repo | Sumber | Pembuat | Lisensi | Dipakai di | Olahan |
|---|---|---|---|---|---|
| `Music/mus_menu.ogg` | https://freesound.org/s/742116/ | viramiller | **CC BY 4.0 (wajib kredit)** | Main menu, Intro | OGG 44.1 kHz stereo, dinormalisasi |
| `Music/mus_dungeon.ogg` | https://freesound.org/s/648900/ | tusic99 | CC0 | Tutorial, L01–L10 | OGG, normalisasi, fade 6 ms di sambungan loop (dulu klik) |
| `Music/mus_summit.ogg` | https://freesound.org/s/657346/ | victor_natas | **CC BY 4.0 (wajib kredit)** | L11 Puncak | OGG, normalisasi |
| `Music/mus_hub.ogg` | https://freesound.org/s/505166/ | code_box | CC0 | Markas, Ending | OGG, normalisasi |
| `Music/amb_garden.ogg` | https://freesound.org/s/744098/ | christmaskrumble666 | CC0 | Ambience taman L01–L03 | OGG, normalisasi |
| `SFX/sfx_slime_hop_01.wav` | https://freesound.org/s/751341/ | qubodup | CC0 | Langkah/lompat slime | mono 44.1 kHz 16-bit, dipotong rapat, puncak −1 dBFS |
| `SFX/sfx_slime_hop_02.wav` | https://freesound.org/s/751342/ | qubodup | CC0 | Langkah/lompat slime | sama |
| `SFX/sfx_split.wav` | https://freesound.org/s/188384/ | wubitog | CC0 | Belah, umpan | mono, dinormalisasi (asli sangat pelan) |

## Tidak dipakai dari zip
- **6 mp3 `...-128-ytshorts.savetube.me.mp3`** (antent, oneheart, reidenshi, diedlonely): rip dari YouTube, lagu berhak
  cipta. **Tidak boleh** masuk repo / game.
- `202400__mattiagreyfox__...` dan `587388__wolfowi__tearing-flesh-slime-owi`: lisensi **CC BY-NC 4.0** (non-komersial,
  dilarang di `docs/todo-tim/daftar-suara.md`) dan isinya suara daging robek (terlalu gore untuk slime lucu).
- Duplikat: 5 SFX ada juga di folder `musik/`.

`proto_*.wav` = placeholder sintesis (Editor/Audio/ProtoAudioGenerator.cs), buatan sendiri.
