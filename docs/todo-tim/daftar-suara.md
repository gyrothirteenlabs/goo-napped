# Daftar suara Goo-napped (untuk pencari / pembuat suara)

Diperbarui 16 Nov 2026. Versi ringkas untuk WA: `daftar-suara-wa.txt`. Setelah file masuk, AI yang memasang dan mencocokkan di game.

**Status:** ✅ ada (final) · 🟡 ada placeholder (boleh diganti) · ⬜ belum ada
**Prioritas:** P1 = wajib untuk rilis jam · P2 = kalau sempat

## Aturan file (wajib)

| | |
|---|---|
| Format | WAV 44.1 kHz 16-bit (OGG boleh untuk musik) |
| Kanal | SFX **mono** (posisional di game). Musik & ambience **stereo** |
| Loop | Musik 60–120 dtk, ambience 30–60 dtk, **sambungan mulus** (tidak ada klik) |
| Level | Puncak maks −1 dBFS, tidak clipping. SFX dipotong rapat (tanpa hening di awal) |
| Variasi | Suara yang sering diulang (lompat, langkah) buat **3 variasi**: `_01`, `_02`, `_03` |
| Nama | Pakai nama di kolom pertama tabel di bawah, huruf kecil, tanpa spasi |
| Lisensi | **Wajib catat sumber + lisensi tiap file** (repo publik, aturan Game Off). Boleh: CC0, CC-BY, royalty-free yang boleh diredistribusi, buatan sendiri. **Jangan**: CC-BY-NC/ND, aset berbayar yang melarang dibagikan, potongan lagu/game lain |
| Mood | Dunia **gelap dan tegang**, slime tetap **lucu**: suara slime lembut, kenyal, imut. Dunia berat, dingin, bergema |

**Sudah dipasang (20 Nov 2026):** lihat `Assets/_Game/Audio/LISENSI.md`. Musik per scene diatur di `Assets/_Game/Resources/SceneMusic.asset` (ganti klip di Inspector).

**Tempat cari (aman lisensinya):** freesound.org (filter License: CC0 / Attribution), kenney.nl (CC0), opengameart.org (cek lisensi tiap file), Sonniss GDC Game Audio Bundle (royalty-free), itch.io "free sfx" (cek lisensi), pixabay.com/sound-effects.

**Cara kirim:** taruh di Google Drive / chat dengan nama file sesuai tabel + satu file `LISENSI.txt` berisi: nama file, link sumber, pembuat, lisensi. Atau langsung di repo: `Assets/_Game/Audio/Music` (mus_, amb_) dan `Assets/_Game/Audio/SFX` (sfx_, ui_, vo_).

---

## 1. Musik (stereo, loop kecuali stinger)

| Nama file | Dipakai di | Rasa / catatan | Kata kunci cari | Status | Prio |
|---|---|---|---|---|---|
| `mus_menu` | Main menu (sementara: Mystical Music Box, viramiller, CC BY; juga dipakai Intro) | Misterius, pelan, motif tema utama | dark fantasy menu loop, mysterious music box | ✅ | P1 |
| `mus_garden` | Taman kastil (L01–L03) (sementara pakai mus_dungeon) | Malam, tegang ringan, sedikit lucu | stealth sneaking loop night, sneaky pizzicato | 🟡 | P1 |
| `mus_dungeon` | Penjara + lab (L00 tutorial, L04–L06) (sementara: tusic99, CC0; juga dipakai L01–L10) | Lebih gelap, dengung, tetes | dungeon stealth loop dark ambient | ✅ | P1 |
| `mus_castle` | Kastil atas (L07–L10) (sementara pakai mus_dungeon) | Megah tapi suram (harpsichord/organ pelan) | gothic castle harpsichord loop | 🟡 | P1 |
| `mus_summit` | Puncak (L11, final) (sementara: Time Travel, victor_natas, CC BY) | Paling tegang, ritmis | boss tension loop dark | ✅ | P1 |
| `mus_chase` | Saat dikejar (lapisan di atas musik area) | Tempo naik, perkusi. Idealnya key sama dengan musik area | chase music loop tense percussion | ⬜ | P1 |
| `mus_hub` | Markas di ventilasi (sementara: Mysterious Merchant, code_box, CC0 (kurang hangat)) | Hangat, tenang, aman | cozy safe room loop, calm lullaby | 🟡 | P1 |
| `mus_intro` | Strip cerita pembuka (sementara pakai mus_menu) | Sedih-misterius, pelan (± 60 dtk, loop) | sad fantasy story theme music box | 🟡 | P2 |
| `mus_ending` | Strip cerita penutup (sementara pakai mus_hub (belum ceria)) | Lega, ceria, hangat | happy ending fantasy theme | 🟡 | P2 |
| `mus_win` | Stinger level selesai | 2–4 dtk, **bukan loop** | victory jingle short cute | ⬜ | P1 |
| `mus_caught` | Stinger tertangkap | 1–3 dtk, kalah tapi tetap lucu | fail stinger cartoon short | ⬜ | P1 |

## 2. Ambience (stereo, loop)

| Nama file | Dipakai di | Isi | Kata kunci cari | Status | Prio |
|---|---|---|---|---|---|
| `amb_garden` | Taman malam (Lake Night, christmaskrumble666, CC0) | Angin, jangkrik, daun | night garden crickets wind loop | ✅ | P1 |
| `amb_dungeon` | Penjara | Tetes air, gema, rantai jauh | dungeon dripping water ambience | 🟡 | P1 |
| `amb_lab` | Lab penyadap | Dengung mesin, gelembung, desis pipa | laboratory machine hum bubbles | ⬜ | P1 |
| `amb_castle` | Aula / ruang makan / galeri | Ruang besar bergema, jam dinding, api perapian | castle hall room tone clock fireplace | ⬜ | P1 |
| `amb_summit` | Puncak | Angin kencang + mesin menyedot aura | tower wind howl machine drone | ⬜ | P1 |
| `amb_vent` | Di dalam ventilasi | Teredam, aman, desir udara | air duct ambience muffled | ⬜ | P2 |
| `sfx_torch_loop` | Dekat lentera / obor (posisional, mono) | Api kretek | torch fire crackle loop | 🟡 | P2 |

## 3. Slime pemain (mono)

| Nama file | Kapan bunyi | Rasa | Kata kunci cari | Status | Prio |
|---|---|---|---|---|---|
| `sfx_slime_hop_01..03` | Tiap lompatan (massa 1–2) (01–02 ada: qubodup, CC0; 03 belum) | Lembut, kenyal, imut | slime squish small, jelly bounce | ✅ | P1 |
| `sfx_slime_hop_big_01..03` | Massa 3–4 ("dug", terdengar penjaga) | Lebih berat, basah | heavy slime step, wet thud | ⬜ | P1 |
| `sfx_slime_land` | Mendarat setelah lompat | Squish kecil | slime land splat soft | ⬜ | P2 |
| `sfx_slime_drop` | **Baru:** jatuh dari pipa di awal tutorial | Jatuh + "plop" besar | slime fall splat, goo drop plop | ⬜ | P1 |
| `sfx_eat` | Makan esens (E) | Seruput + kristal larut | slurp gulp magic crystal | 🟡 | P1 |
| `sfx_split` | Belah (Space) (wubitog, CC0) | Lendir terbelah | slime split stretch tear | ✅ | P1 |
| `sfx_merge` | Gabung | Lendir menyatu | slime merge squelch | 🟡 | P1 |
| `sfx_vomit` | Muntah esens (Q) | Lucu, tidak menjijikkan | cartoon spit out, blorp | 🟡 | P1 |
| `sfx_switch` | Shift pindah kendali | Whoosh pendek | short magic whoosh swap | 🟡 | P1 |
| `sfx_hide_in` / `sfx_hide_out` | Masuk / keluar tempat sembunyi | Beda per bahan kalau sempat (semak, kayu, kain, logam) | hide in bush rustle, crate creak | 🟡 | P1 |
| `sfx_mud` / `sfx_wash` | Masuk lumpur / dicuci di genangan | Becek / percikan | mud squelch step, water splash puddle | ⬜ | P1 |
| `sfx_squeeze` | Lewat celah | Lendir diperas | squeeze through gap slime | ⬜ | P2 |

## 4. Teman

| Nama file | Kapan bunyi | Rasa | Kata kunci cari | Status | Prio |
|---|---|---|---|---|---|
| `sfx_jar_swap` | Tiruan masuk toples | Kaca + lendir masuk | glass jar slime pour | ⬜ | P1 |
| `sfx_friend_free` | Teman keluar dari toples | Pop tutup + lega | cork pop, cute relief | ⬜ | P1 |
| `sfx_friend_follow` / `sfx_friend_wait` | Perintah F: ikut / tunggu | Cicit pendek, nada beda | cute chirp yes, small squeak | ⬜ | P1 |
| `sfx_friend_hop_01..03` | **Baru:** teman melompat mengikuti | Lebih kecil dan pelan dari hop pemain | tiny jelly bounce soft | ⬜ | P2 |
| `sfx_friend_saved` | Teman masuk ventilasi (selamat) | Jingle kecil | cute success chime | ⬜ | P1 |
| `vo_<tokoh>_blip` | Blip suara saat bicara (gaya Animal Crossing / Undertale) | Satu bunyi pendek per tokoh, nada beda: narrator, dot, pip, bolu, tok, lumi, ayah, ibu, adik, nenek, raja, alkemis | dialogue blip voice beep | ⬜ | P2 |

## 5. Penjaga (umum)

| Nama file | Kapan bunyi | Rasa | Kata kunci cari | Status | Prio |
|---|---|---|---|---|---|
| `sfx_suspicious` | Meter curiga naik (?) | Nada naik pelan | suspicion rising tone, hmm | ⬜ | P1 |
| `sfx_alert` | Ketahuan (!) | Stinger tajam | alert stinger exclamation | 🟡 | P1 |
| `sfx_call` | Penjaga memanggil penjaga lain | Teriakan/raungan (per jenis kalau sempat) | monster call roar short | ⬜ | P1 |
| `sfx_caught` | Tertangkap | Kalah tapi tetap lucu | caught grab cartoon | 🟡 | P1 |
| `sfx_heartbeat` (loop) | Penjaga dekat (makin dekat makin cepat) | 2 tempo kalau bisa | heartbeat loop tense | ⬜ | P1 |
| `sfx_inspect` | Penjaga memeriksa tempat sembunyi | Goyang/buka barang | rummage search object | ⬜ | P1 |
| `sfx_lost` | Penjaga kehilangan jejak | Gumam kecewa | monster grunt confused | ⬜ | P2 |

## 6. Per musuh

| Nama file | Musuh / kapan | Rasa | Kata kunci cari | Status | Prio |
|---|---|---|---|---|---|
| `sfx_golem_step_01..03` | Golem Batu jalan | Batu berat | stone golem footstep | 🟡 | P1 |
| `sfx_hound_sniff` / `sfx_hound_bark` | Anjing Lendir endus / gonggong | Basah, berlendir | dog sniff wet, creature bark | ⬜ | P1 |
| `sfx_gate_growl` / `sfx_gate_beam` (loop) | Monster Gerbang napas / sorot menyapu | Besar, dalam | large creature breathing, searchlight hum | ⬜ | P1 |
| `sfx_familiar_fire` (loop) | Familiar Api | Api kretek + desis | fire spirit crackle loop | ⬜ | P1 |
| `sfx_eye_hover` (loop) | Mata Penyihir melayang | Dengung sihir pelan | magic hum floating loop | ⬜ | P1 |
| `sfx_eye_scan` | Mata Penyihir memeriksa (animasi **search** baru: menoleh kiri-kanan) | Lensa "zoom" / klik monokel | lens focus zoom, magic scan | ⬜ | P1 |
| `sfx_skull_rattle` | **Baru:** tengkorak pendamping Mata Penyihir (sesekali) | Gigi gemeretak kecil | skull teeth chatter rattle | ⬜ | P2 |
| `sfx_armor_step` / `sfx_armor_listen` | Zirah Hampa langkah besi / berhenti mendengar | Logam kosong bergema | empty armor footstep metal clank | ⬜ | P1 |
| `sfx_butler_step` / `sfx_butler_lid` | Pelayan Tudung langkah / angkat tudung saji | Klonteng logam | cloche lid lift metal | ⬜ | P1 |
| `sfx_portrait_eyes` | Lukisan Bermata mata bergeser | Derit kayu/kanvas | painting creak eyes move | ⬜ | P1 |
| `sfx_alch_step` / `sfx_alch_whisper` (loop) / `sfx_alch_rage` / `sfx_alch_sealed` | Alkemis jalan / bisikan roh / mengamuk / tersegel di toples | Paling seram di game | evil whisper loop, creature rage, sealed magic jar | ⬜ | P1 |

## 7. Dunia & puzzle

| Nama file | Kapan bunyi | Rasa | Kata kunci cari | Status | Prio |
|---|---|---|---|---|---|
| `sfx_alarm` (loop) | Alarm penyadapan | Sirene sengau | alarm siren loop old | ⬜ | P1 |
| `sfx_door_open` / `sfx_door_close` | Pintu | Kayu / besi | dungeon door open creak | ⬜ | P1 |
| `sfx_lever` | Tuas | Klak logam | lever pull metal | ⬜ | P1 |
| `sfx_plate_down` / `sfx_plate_up` | Pelat tekan | Klik batu | stone pressure plate click | ⬜ | P1 |
| `sfx_vase_break` | Barang rapuh pecah (pengalih perhatian) | Keras | vase shatter | 🟡 | P1 |
| `sfx_creak` | Lantai berderit | Kayu tua | wood floor creak | ⬜ | P1 |
| `sfx_crate_push` | Peti didorong | Seret kayu | wooden crate drag | ⬜ | P1 |
| `sfx_lamp_out` | Lentera padam | Desis | candle blow out hiss | ⬜ | P1 |
| `sfx_vent_in` / `sfx_vent_out` / `sfx_vent_travel` | Ventilasi masuk / keluar / pindah lubang | Logam, teredam | metal vent crawl, duct | ⬜ | P1 |
| `sfx_checkpoint` | Checkpoint tersimpan | Chime lembut | soft save chime | ⬜ | P1 |
| `sfx_valve_turn` | Katup aura diputar (final) | Keras, berderit (memang memancing Alkemis) | heavy valve turn squeak | ⬜ | P1 |
| `sfx_kingjar_open` | Toples raja terbuka | Kaca + energi lepas | glass break magic release | ⬜ | P1 |
| `sfx_royal_seal` | Segel Raja | Sihir megah | magic seal holy spell | ⬜ | P1 |

## 8. Kekuatan teman

| Nama file | Kekuatan | Rasa | Kata kunci cari | Status | Prio |
|---|---|---|---|---|---|
| `sfx_power_thump` | Dentum (Bolu) | Ketukan keras di ventilasi | loud thump metal bang | ⬜ | P1 |
| `sfx_power_snuff` | Padam (Tok) | Lampu mati serentak + lonceng kecil | lights out whoosh | ⬜ | P1 |
| `sfx_power_decoy` / `sfx_power_shatter` | Umpan Kristal (Lumi) dilempar / pecah | Kristal berdenting | crystal throw, crystal shatter | ⬜ | P1 |
| `sfx_power_mist` | Kabut (Ibu) | Desis kabut | fog hiss steam | ⬜ | P1 |

## 9. UI, cerita & sinematik

| Nama file | Kapan bunyi | Rasa | Kata kunci cari | Status | Prio |
|---|---|---|---|---|---|
| `ui_move` / `ui_select` / `ui_back` | Navigasi menu | Pendek, lembut | ui click soft, menu select | 🟡 | P1 |
| `ui_locked` | Pilih yang terkunci | Bunyi tolak | ui error deny | ⬜ | P2 |
| `ui_slider` | Geser volume di Options | Tik kecil | ui tick | ⬜ | P2 |
| `ui_pause_open` / `ui_pause_close` | Pause | Whoosh kecil | ui whoosh open | ⬜ | P2 |
| `ui_goo_drip` | Transisi lendir menetes (pindah scene) | Lendir menetes / menutup layar | slime drip transition | ⬜ | P2 |
| `ui_slash` | Transisi lembar miring | Sabetan kain | swoosh cloth slash | ⬜ | P2 |
| `ui_title_stamp` | Kartu judul level dicap | "Dug" stempel | stamp hit | ⬜ | P2 |
| `ui_tip_show` / `ui_tip_done` | **Baru:** tip tutorial muncul / selesai | Pop kecil / ceklis | ui pop, success tick | ⬜ | P2 |
| `ui_dialog_next` | Lanjut dialog / halaman cerita | Klik lembut | dialog advance click | ⬜ | P1 |
| `ui_text_blip` | **Baru:** ketik huruf di kotak cerita (narator) | Tik sangat pendek, tidak mengganggu | typewriter blip retro | ⬜ | P1 |
| `ui_page_fade` | **Baru:** ganti gambar strip cerita | Desir lembut | soft page turn whoosh | ⬜ | P2 |
| `sfx_cine_whoosh` | **Baru:** kamera sinematik bergerak di pembuka level | Desir udara pelan | cinematic whoosh soft | ⬜ | P2 |

---

**Total:** ± 95 file (musik 11, ambience 7, sisanya SFX/UI). Placeholder 🟡 yang sekarang ada di game hanya 13 SFX + 1 musik + 1 ambience (`Assets/_Game/Audio/*/proto_*.wav`, buatan generator, boleh diganti semua).

**Urutan kalau waktu sempit:** musik area + `mus_chase` → SFX slime (hop, eat, split, merge) → penjaga umum (curiga, ketahuan, tertangkap, detak jantung) → toples & teman → UI dasar → sisanya.
