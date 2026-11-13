# Membuat docs/todo-tim/todo-tim.pdf (todo lengkap per divisi).
# Jalankan: python docs/todo-tim/make_pdf.py   (butuh: pip install reportlab)
# Isi diambil dari docs/desain-v2.md, docs/backlog.md, art/CLAUDE.md, dan kode (AudioDirector, prefab placeholder).
# Ubah isi di bagian DATA di bawah, lalu jalankan ulang.
import os
from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (KeepTogether, PageBreak, Paragraph, SimpleDocTemplate, Spacer, Table,
                                TableStyle)

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "todo-tim.pdf")

# ---------- font ----------
FONT_DIR = os.path.join(os.environ.get("WINDIR", "C:/Windows"), "Fonts")
try:
    pdfmetrics.registerFont(TTFont("Body", os.path.join(FONT_DIR, "segoeui.ttf")))
    pdfmetrics.registerFont(TTFont("Body-Bold", os.path.join(FONT_DIR, "segoeuib.ttf")))
    BODY, BOLD = "Body", "Body-Bold"
except Exception:
    BODY, BOLD = "Helvetica", "Helvetica-Bold"
from reportlab.lib.fonts import addMapping
addMapping(BODY, 0, 0, BODY); addMapping(BODY, 1, 0, BOLD); addMapping(BODY, 0, 1, BODY); addMapping(BODY, 1, 1, BOLD)

# ---------- warna ----------
INK = colors.HexColor("#1b2740")
MUTED = colors.HexColor("#5d6472")
ACCENT = colors.HexColor("#1f6f7a")
GOLD = colors.HexColor("#c2561f")
LINE = colors.HexColor("#b9c0cc")
HEAD_BG = colors.HexColor("#1b2740")
ROW_ALT = colors.HexColor("#f1f3f7")
P_COL = {"P1": colors.HexColor("#c2253a"), "P2": colors.HexColor("#c2561f"), "P3": colors.HexColor("#4d6a8c")}

ss = {
    "title": ParagraphStyle("title", fontName=BOLD, fontSize=30, leading=36, textColor=INK),
    "sub": ParagraphStyle("sub", fontName=BODY, fontSize=13, leading=18, textColor=MUTED),
    "h1": ParagraphStyle("h1", fontName=BOLD, fontSize=18, leading=23, textColor=INK, spaceBefore=4, spaceAfter=6),
    "h2": ParagraphStyle("h2", fontName=BOLD, fontSize=12.5, leading=16, textColor=ACCENT, spaceBefore=10, spaceAfter=4),
    "body": ParagraphStyle("body", fontName=BODY, fontSize=9.5, leading=13.5, textColor=INK, alignment=TA_LEFT),
    "small": ParagraphStyle("small", fontName=BODY, fontSize=8.3, leading=11, textColor=INK),
    "smallmuted": ParagraphStyle("smallmuted", fontName=BODY, fontSize=8.3, leading=11, textColor=MUTED),
    "cellh": ParagraphStyle("cellh", fontName=BOLD, fontSize=8.5, leading=11, textColor=colors.white),
    "bullet": ParagraphStyle("bullet", fontName=BODY, fontSize=9.5, leading=13.5, textColor=INK, leftIndent=12, bulletIndent=2),
    "note": ParagraphStyle("note", fontName=BODY, fontSize=9, leading=12.5, textColor=INK, backColor=colors.HexColor("#eef6f7"),
                           borderColor=ACCENT, borderWidth=0.6, borderPadding=6, spaceBefore=4, spaceAfter=8),
}


def P(text, style="body"):
    return Paragraph(text, ss[style])


def bullets(items):
    return [Paragraph(t, ss["bullet"], bulletText="•") for t in items]


def table(head, rows, widths, prio_col=None, font="small"):
    data = [[P(h, "cellh") for h in head]]
    for r in rows:
        cells = []
        for i, c in enumerate(r):
            if prio_col is not None and i == prio_col:
                col = "#" + P_COL.get(c, INK).hexval()[2:]
                cells.append(Paragraph(f'<font color="{col}"><b>{c}</b></font>', ss[font]))
            else:
                cells.append(P(c, font))
        data.append(cells)
    t = Table(data, colWidths=widths, repeatRows=1)
    style = [
        ("BACKGROUND", (0, 0), (-1, 0), HEAD_BG),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LINEBELOW", (0, 0), (-1, -1), 0.4, LINE),
        ("LEFTPADDING", (0, 0), (-1, -1), 4), ("RIGHTPADDING", (0, 0), (-1, -1), 4),
        ("TOPPADDING", (0, 0), (-1, -1), 3), ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
    ]
    for i in range(1, len(data)):
        if i % 2 == 0: style.append(("BACKGROUND", (0, i), (-1, i), ROW_ALT))
    t.setStyle(TableStyle(style))
    return t


W = A4[0] - 36 * mm   # lebar isi

# =====================================================================
# DATA
# =====================================================================
DATE = "13 November 2026"

STATUS_DONE = [
    "Semua sistem v2: gerak slime, massa 1–4, esens + aura, belah/gabung/Shift (maks 2 potongan), toples + tiruan + alarm penyadapan.",
    "Teman mengikuti jejak (F tunggu/ikut), sembunyi di objek, ventilasi (aman, checkpoint, exit, jaringan), lumpur + genangan.",
    "Penjaga v2: kejar, panggil penjaga lain, curiga dari transformasi terlihat, memeriksa tempat sembunyi. 9 jenis musuh (perilaku sudah jalan, sprite masih placeholder).",
    "Kekuatan teman dari ventilasi (Dentum, Padam, Umpan Kristal, Kabut), markas antar level, benda interaksi (vas, lantai berderit, peti dorong), pergantian shift jaga.",
    "11 level blockout (44 kolom) + adegan final L11 (3 katup aura, toples raja, Alkemis tersegel). Alur Intro → L01..L11 (lewat markas) → Ending.",
    "Main menu sementara, build Windows (GooNapped > Build) dengan log + smoke test otomatis. Validator level + bot QA menamatkan semua level.",
]
STATUS_TODO = [
    "Art final: slime + teman, sprite 9 musuh, tileset 4 area, props, tempat sembunyi, toples 2 ukuran, UI final.",
    "Audio: baru 13 SFX + 1 musik + 1 ambience placeholder. Sebagian besar suara belum ada (daftar lengkap di bagian Sound).",
    "Isi cerita: strip Intro/Ending, dialog level, kalimat markas, catatan (semua teks masih placeholder).",
    "Playtest manusia: belum pernah. Durasi level (target 2–4 menit) dan tingkat susah belum terbukti.",
]

TIMELINE = [
    ["Minggu 2 (10–16 Nov)", "Sistem v2 selesai (sudah). Tim desainer mulai aset P1, tim putuskan daftar keputusan (bagian 8).", "Semua"],
    ["Minggu 3 (17–23 Nov)", "Aset P1 masuk dan dipasang. Suara P1 masuk. Isi cerita ditulis. Playtest internal pertama (blockout + art yang sudah ada).", "Desainer, Sound, Programmer, QA"],
    ["Akhir minggu 3 (23 Nov)", "<b>Batas aset P1.</b> Setelah ini hanya perbaikan dan aset P2/P3 kalau sempat.", "Semua"],
    ["Minggu 4 (24 Nov – rilis)", "<b>Fitur dibekukan.</b> Playtest, bug fix, setel angka, halaman itch.io, build rilis. Cek tanggal tutup submit di halaman jam.", "Programmer, QA, UI"],
]

PRIO = [
    ["P1", "Wajib. Tanpa ini game tidak layak rilis atau tidak bisa dimainkan dengan benar."],
    ["P2", "Penting. Sangat menambah kualitas. Kerjakan setelah P1 aman."],
    ["P3", "Bonus. Hanya kalau ada waktu di minggu 3. Boleh dipotong."],
]

# ---------- PROGRAMMER ----------
PROG = [
    ["Pasang sprite final", "Import otomatis lewat PixelSpriteImporter (PPU 48, pivot bawah-tengah, normal map _normal). Ganti sprite di prefab placeholder: Slime_Player, FriendSlime, 9 musuh *_Placeholder, props Level/*, tile Placeholder. Animator per musuh (idle, jalan 3 arah, alert).", "P1", "Prefabs/, Art/Sprites/"],
    ["Tileset per area", "Tile asset per area + palet Global Light 2D per area. Blockout pakai tile placeholder; ganti ke tile final atau dekor ulang di Tilemap.", "P1", "Art/Tiles/, LevelBlockout.cs"],
    ["Sistem audio v2", "Perluas AudioDirector: daftar SFX baru (lihat bagian Sound), musik per area, lapisan musik kejar (crossfade saat dikejar), detak jantung sesuai jarak penjaga, sirene alarm, blip suara dialog per tokoh, mixer + volume musik/SFX.", "P1", "Scripts/Runtime/Audio/"],
    ["Pause + Options", "Pause menu masih OnGUI placeholder. Buat versi UI Toolkit (mengikuti desain UI): Resume, Retry, Options, Main Menu. Options: volume musik, volume SFX, fullscreen, resolusi. Simpan di PlayerPrefs.", "P1", "UI/, PauseMenu.cs"],
    ["Player Settings rilis", "Nama produk masih “My project”, company “DefaultCompany”. Isi nama game, ikon, splash, versi. ProjectSettings = bahas tim dulu.", "P1", "ProjectSettings"],
    ["Build Settings", "Daftar scene v2 belum di Build Settings. Build lewat GooNapped > Build sudah lengkap tanpa mengubahnya; putuskan apakah tetap begitu atau Build Settings diperbarui.", "P1", "ProjectSettings"],
    ["Bug fix harian + build rutin", "Build dev tiap ada perubahan besar, jalankan smoke_test.bat, bagikan ke QA. Test Runner (EditMode + PlayMode) sebelum tiap PR.", "P1", "Builds/Windows-Dev"],
    ["Setel angka dari playtest", "Alarm 90/30 dtk, kecepatan + jarak pandang penjaga, tabel massa, jatah kekuatan, waktu masuk/keluar sembunyi. Catat perubahan di backlog.", "P1", "Inspector / data"],
    ["Tempat sembunyi per area", "Sekarang hanya tong (muat 1) dan peti (muat 2). Tambah: semak (taman), toples kosong (bawah tanah), lemari, tudung saji, panci, tirai (kastil). Prefab + legenda blockout.", "P2", "HidingSpot, LevelPropBuilder"],
    ["Toples 2 ukuran", "Toples kecil/besar sesuai ukuran teman + pendar warna aura dari jauh (pilihan C di desain-v2).", "P2", "Jar.prefab"],
    ["Pasang isi cerita", "Strip Intro/Ending (StorySequence), dialog di level (StoryTrigger), kalimat markas (HubFriend), dialog final. Teks dari penulis.", "P2", "Data/Story, Markas"],
    ["Layout level final", "Terima layout dari desainer level, ubah peta teks di LevelBlockout.cs atau gambar langsung di Tilemap. Validator + bot harus tetap lolos.", "P2", "LevelBlockout.cs"],
    ["Ikon ? / ! final", "GuardMeter: isi Question/Exclaim Sprite dari desain UI.", "P2", "GuardMeter"],
    ["Koleksi opsional", "Catatan (jurnal alkemis, surat warga) + tetes kenangan 1 per level, mural di markas.", "P3", "baru"],
    ["Benda tambahan", "Lampu bersakelar (berisik), rak rahasia (galeri), semak/tirai yang menutup pandangan tapi bisa dilewati.", "P3", "baru"],
    ["Build WebGL", "Setelah Windows stabil.", "P3", "GameBuild"],
]

# ---------- 3D ----------
SPEC_SPRITE = [
    "Ukuran cell <b>48×48 px</b>, kamera orthographic top-down 3/4 (tilt 55°).",
    "<b>3 arah</b>: front, side (hadap kanan), back. Kiri = flip di Unity.",
    "<b>Cahaya TIDAK di-bake</b>: albedo flat (warna saja) + normal map <i>&lt;nama&gt;_normal</i> dengan ukuran/layout frame sama persis. Cahaya dihitung di Unity.",
    "Albedo nada menengah (jangan terlalu gelap, lampu hanya bisa menggelapkan). Bagian yang menyala sendiri (api, kristal) dipisah sebagai bagian emisif.",
    "Slime pemain & teman: layer <b>body</b> (putih flat + outline abu, warna diatur palet di Unity), <b>normal</b>, <b>face</b> (mata/mulut). Jangan render varian warna.",
    "Bayangan jatuh tidak di-render (blob shadow terpisah). Wajib lolos <b>tes siluet</b> 48px hitam-putih.",
    "Template: <i>art/blender/sprite_template.blend</i> + add-on render 1 klik. Panduan: <i>docs/panduan-blender-3d.md</i>. Hasil ke <i>art/sprites/&lt;nama&gt;/</i>, lalu disalin ke <i>Assets/_Game/Art/Sprites/</i>.",
    "Desain harus original (Slime Rancher / Momo hanya acuan rasa). Branch git: <i>art/&lt;aset&gt;</i>.",
]
D3 = [
    ["Slime pemain (Blip)", "Desain final. Anim: idle (napas), move (lompat rendah, busur ±5 px, squash/stretch), eat, split, merge, jelly (goyang), masuk/keluar sembunyi. Model sekarang: slime_v06 (placeholder).", "P1"],
    ["Teman (1 desain dasar)", "Satu desain teman, warna dari palet aura di Unity, ukuran kecil/besar cukup diskalakan. Aksesori pembeda per tokoh (contoh: lonceng Tok, mahkota Raja, uban/kacamata Nenek). Anim: idle, move, takut (menunggu dekat toples), senang (diselamatkan).", "P1"],
    ["Golem Batu", "Generik. Lambat, langkah berat. Anim: idle, jalan, alert/menoleh. Placeholder ada.", "P1"],
    ["Anjing Lendir", "Generik. Mengendus jejak bau. Anim: idle, jalan, endus, alert.", "P1"],
    ["Monster Gerbang", "Unik (L01). Diam, sorot pandang menyapu lebar. Besar (tontonan). Anim: idle napas, sapu kepala/sorot, alert.", "P1"],
    ["Familiar Api", "Unik (lab). Membawa cahaya, melihat benda panas/menyala. Anim: melayang, jalan, alert. Api = bagian emisif.", "P1"],
    ["Mata Penyihir", "Unik (lab, galeri). Melayang, memeriksa toples dan tempat sembunyi. Anim: melayang, periksa, alert. Placeholder Blender ada.", "P1"],
    ["Zirah Hampa", "Unik (aula). Buta, memburu suara. Anim: diam, jalan berat, dengar (menoleh), alert.", "P1"],
    ["Pelayan Tudung", "Unik (ruang makan). Rutin membuka tudung saji/panci. Anim: jalan, buka tudung, alert.", "P1"],
    ["Lukisan Bermata", "Unik (galeri). Tidak bergerak, mata menyapu kiri-kanan. Bisa sprite 2D juga. Anim: mata kiri/tengah/kanan, alert.", "P1"],
    ["Alkemis (boss)", "Puncak. Pemburu ala Clock Tower. Anim: jalan, periksa tempat sembunyi, alert, menerkam, tersegel di toples (final + ending).", "P1/P2"],
    ["Toples raja + mesin + katup", "Boleh 3D kalau lebih cepat dari pixel manual. Katup 2 keadaan (tertutup merah / terbalik hijau).", "P2"],
    ["Pose untuk strip cerita", "Render pose besar untuk dipakai desainer 2D di strip Intro/Ending.", "P3"],
]

# ---------- 2D ----------
D2 = [
    ["Palet final 4 area", "Taman = biru bulan, bawah tanah = hijau aura, kastil atas = emas pudar + merah anggur, puncak = ungu roh. Dasar gelap dingin, cahaya hangat hanya dari sumber. art/palette.hex masih usulan.", "P1"],
    ["Tileset taman", "Rumput, jalan setapak, pagar, gerbang, pagar tanaman (semak tinggi), tembok kastil luar. Lantai + tembok (bagian atas + muka) + variasi + dekor.", "P1"],
    ["Tileset bawah tanah", "Batu penjara, jeruji sel, saluran/pipa aura bercahaya (petunjuk arah), lab (mesin, kabel).", "P1"],
    ["Tileset kastil atas", "Karpet, lantai kayu/ubin, dinding emas pudar, meja makan, bingkai lukisan.", "P1"],
    ["Tileset puncak", "Lantai mesin penyadap, ungu roh, pipa aura menuju toples raja.", "P1"],
    ["Props mekanik", "Ventilasi + ventilasi exit, toples kecil & besar (pendar aura), toples raja, katup aura (2 frame), pintu (tertutup/terbuka), tuas (2 frame), pelat ringan/berat (naik/turun), celah tikus, peti dorong, lentera/obor (nyala + padam).", "P1"],
    ["Tempat sembunyi", "Semak, tong, peti, toples kosong, lemari, tudung saji, panci, tirai. Tiap barang: normal + frame goyang (saat dicurigai).", "P1"],
    ["7 esens (kristal)", "Batu (abu), Pendar (hijau, menyala), Embun (biru), Bara (merah, menyala), Belerang (kuning), Beku (putih), Bayang (ungu). Harus beda bentuk juga, bukan cuma warna (buta warna).", "P1"],
    ["Benda interaksi", "Vas/piring/botol (utuh + pecah), lantai berderit, genangan air, lumpur.", "P1"],
    ["Strip cerita Intro", "Prolog: kerajaan slime damai → Alkemis datang dirasuki roh → penyadapan aura, warga dikurung di toples → Blip (aura bening) dibuang dan selamat. 6–10 panel.", "P2"],
    ["Strip cerita Ending", "Alkemis tersegel di toplesnya sendiri (peran tertukar) → kutukan hilang → aura kembali, yang mati hidup lagi → alat alkemis dipakai membangun ulang. 4–8 panel.", "P2"],
    ["Potret dialog", "Pip, Bolu, Tok, Lumi, Ayah, Ibu, Adik, Nenek, Raja, Alkemis. Minimal 1 ekspresi, ideal 2–3 (biasa, senang, takut).", "P2"],
    ["VFX", "Jejak lumpur, garis bau, uap panas, kabut (Kabut), kristal umpan pecah, gelombang dentum, efek padam, sihir segel raja, lampu alarm, percikan air.", "P2"],
    ["Mural markas + catatan", "Tetes kenangan (1 per level) jadi mural, ikon catatan koleksi, rak rahasia.", "P3"],
]

# ---------- UI ----------
UI = [
    ["Font + logo", "Font pixel dengan lisensi bebas (cek lisensi, repo publik). Logo judul (nama final menyusul, sementara GOO-NAPPED).", "P1"],
    ["Main menu final", "Placeholder sekarang: adegan lab gelap + menu kiri (Continue, New Game, Select Level, Quit). Desain ulang tampilan; susunan di MainMenu.uxml, gaya di MainMenu.uss.", "P1"],
    ["Pilih level", "Kartu 11 level (nomor, nama, area, terkunci). Sekarang placeholder 4 kolom.", "P1"],
    ["Pause + Options", "Resume, Retry, Options, Main Menu. Options: volume musik/SFX, fullscreen, resolusi.", "P1"],
    ["HUD", "Panel tubuh (ukuran 1–4, aura), jumlah potongan, objektif, status teman (ikut/tunggu), jatah kekuatan, timer alarm penyadapan, pesan singkat (toast). File: Hud.uxml/Hud.uss.", "P1"],
    ["Ikon penjaga", "? (curiga) dan ! (ketahuan) di atas penjaga, terbaca di tempat gelap.", "P1"],
    ["Dialog + strip", "Kotak dialog, papan nama, bingkai potret, petunjuk lanjut/skip. 3 layout strip (gambar kiri, atas, penuh). File: Story.uxml/Story.uss.", "P1"],
    ["Banner", "Tertangkap, checkpoint tersimpan, level selesai, alarm berbunyi.", "P1"],
    ["Petunjuk tombol", "Ikon keyboard: WASD, E, Space, Shift, C, F, Q, R, Esc. Dipakai di HUD dan tutorial.", "P1"],
    ["UI ventilasi", "Saat di dalam ventilasi: pilih kekuatan teman (Shift), pilih lubang (A/D), pakai (E), sisa jatah.", "P2"],
    ["Markas + kredit", "Prompt bicara dengan teman, layar kredit (nama tim + lisensi aset).", "P2"],
    ["Halaman itch.io", "Cover 630×500, banner, 5+ screenshot, GIF gameplay 10–20 dtk, ikon .ico, deskripsi + kontrol.", "P2"],
    ["Transisi", "Layar loading/fade antar scene, animasi kecil menu.", "P3"],
]
UI_SPEC = [
    "UI memakai <b>UI Toolkit</b>. Resolusi acuan 960×540 (PanelSettings). Game di-render Pixel Perfect 384×216 lalu diperbesar, jadi elemen piksel sebaiknya kelipatan rapi.",
    "Kirim <b>mockup PNG</b> (per layar, 1920×1080) atau langsung ubah file .uss. Jangan ganti <i>name</i> elemen di .uxml (dipakai kode).",
    "Teks di game <b>bahasa Inggris</b>. Mood gelap: kontras tinggi untuk teks, warna aksen dari palet.",
    "Gambar 9-slice untuk panel/tombol: sertakan ukuran border.",
]

# ---------- SOUND ----------
SOUND_SPEC = [
    "Format <b>WAV 44.1 kHz 16-bit</b>. SFX <b>mono</b> (suara posisional di game), musik & ambience <b>stereo</b>.",
    "Musik/ambience <b>loop mulus</b> (tidak ada klik di sambungan). Durasi loop musik 60–120 dtk, ambience 30–60 dtk.",
    "Puncak tidak lebih dari −1 dBFS, tidak clipping. SFX dipotong rapat (tanpa hening di awal).",
    "Variasi: suara yang sering diulang (langkah, lompat) buat <b>3 variasi</b> (_01, _02, _03).",
    "Nama file: <i>mus_&lt;nama&gt;</i>, <i>amb_&lt;nama&gt;</i>, <i>sfx_&lt;nama&gt;</i>, <i>ui_&lt;nama&gt;</i>, <i>vo_&lt;tokoh&gt;</i>. Taruh di Assets/_Game/Audio/Music atau SFX.",
    "<b>Wajib catat sumber + lisensi</b> tiap file (repo publik, aturan Game Off). Hanya CC0/CC-BY/buatan sendiri. Daftar di ThirdParty/ + kredit.",
    "Mood: dunia gelap dan tegang, slime tetap lucu (suara slime lembut, kenyal, imut; dunia berat dan dingin).",
]
# [nama file, kapan dipakai di game, catatan, status, prioritas]
SOUND = {
    "Musik": [
        ["mus_menu", "Main menu", "Misterius, pelan, motif tema utama", "baru", "P1"],
        ["mus_garden", "Area taman (L01–L03)", "Malam, tegang ringan", "baru (placeholder umum ada)", "P1"],
        ["mus_dungeon", "Bawah tanah (L04–L06)", "Lebih gelap, dengung, tetes", "baru", "P1"],
        ["mus_castle", "Kastil atas (L07–L10)", "Megah tapi suram (harpsichord/organ pelan)", "baru", "P1"],
        ["mus_summit", "Puncak (L11, final)", "Paling tegang, ritmis", "baru", "P1"],
        ["mus_chase", "Saat dikejar (lapisan di atas musik area)", "Tempo naik, perkusi. Harus nyambung (key sama) dengan musik area", "baru", "P1"],
        ["mus_hub", "Markas di ventilasi", "Hangat, tenang, aman", "baru", "P1"],
        ["mus_story", "Strip Intro/Ending", "Intro sedih-misterius, Ending lega-ceria (bisa 2 file)", "baru", "P2"],
        ["mus_win / mus_caught", "Stinger level selesai / tertangkap", "1–3 dtk, bukan loop", "baru", "P1"],
    ],
    "Ambience (loop)": [
        ["amb_garden", "Taman malam", "Angin, jangkrik, daun", "baru", "P1"],
        ["amb_dungeon", "Penjara", "Tetes air, gema, rantai jauh", "placeholder (cave)", "P1"],
        ["amb_lab", "Lab penyadap", "Dengung mesin, gelembung, desis pipa", "baru", "P1"],
        ["amb_castle", "Aula/ruang makan/galeri", "Ruang besar bergema, jam dinding, api perapian", "baru", "P1"],
        ["amb_summit", "Puncak", "Angin kencang + mesin menyedot aura", "baru", "P1"],
        ["amb_vent", "Di dalam ventilasi", "Teredam, aman, desir udara", "baru", "P2"],
        ["sfx_torch_loop", "Dekat lentera/obor", "Api kretek (posisional)", "placeholder ada", "P2"],
    ],
    "Slime pemain": [
        ["sfx_slime_hop_01..03", "Tiap lompatan (massa 1–2)", "Lembut, kenyal", "placeholder ada", "P1"],
        ["sfx_slime_hop_big_01..03", "Massa 3–4 (langkah “dug”)", "Lebih berat, terdengar penjaga", "baru", "P1"],
        ["sfx_slime_land", "Mendarat", "Squish kecil", "baru", "P2"],
        ["sfx_eat", "Makan esens", "Seruput + kristal larut", "placeholder ada", "P1"],
        ["sfx_split / sfx_merge", "Belah / gabung", "Lendir terbelah / menyatu", "placeholder ada", "P1"],
        ["sfx_vomit", "Muntah esens (Q)", "Lucu, tidak menjijikkan", "placeholder ada", "P1"],
        ["sfx_switch", "Shift pindah kendali", "Whoosh pendek", "placeholder ada", "P1"],
        ["sfx_hide_in / sfx_hide_out", "Masuk/keluar tempat sembunyi", "Beda per bahan bila sempat (semak, kayu, logam)", "placeholder ada (disguise/reveal)", "P1"],
        ["sfx_mud / sfx_wash", "Masuk lumpur / dicuci di genangan", "Becek / percikan", "baru", "P1"],
        ["sfx_squeeze", "Lewat celah tikus", "Lendir diperas", "baru", "P2"],
    ],
    "Teman": [
        ["sfx_jar_swap", "Tiruan masuk toples", "Kaca + lendir masuk", "baru", "P1"],
        ["sfx_friend_free", "Teman keluar dari toples", "Pop tutup + lega", "baru", "P1"],
        ["sfx_friend_follow / _wait", "Perintah F ikut / tunggu", "Cicit pendek, beda nada", "baru", "P1"],
        ["sfx_friend_saved", "Teman masuk ventilasi (selamat)", "Jingle kecil", "baru", "P1"],
        ["vo_&lt;tokoh&gt;_blip", "Blip suara saat bicara (gaya Animal Crossing)", "Pip, Bolu, Tok, Lumi, Ayah, Ibu, Adik, Nenek, Raja, Alkemis: nada beda per tokoh", "baru", "P2"],
    ],
    "Penjaga (umum)": [
        ["sfx_suspicious", "Meter curiga naik (?)", "Nada naik pelan", "baru", "P1"],
        ["sfx_alert", "Ketahuan (!)", "Stinger tajam", "placeholder ada", "P1"],
        ["sfx_call", "Penjaga memanggil penjaga lain", "Teriakan/raungan per jenis bila sempat", "baru", "P1"],
        ["sfx_caught", "Tertangkap", "Kalah, tapi tetap lucu", "placeholder ada", "P1"],
        ["sfx_heartbeat (loop)", "Penjaga dekat (makin cepat makin dekat)", "Bisa 2 tempo", "baru", "P1"],
        ["sfx_inspect", "Penjaga memeriksa tempat sembunyi", "Goyang/buka barang", "baru", "P1"],
        ["sfx_lost", "Penjaga kehilangan jejak", "Gumam kecewa", "baru", "P2"],
    ],
    "Per musuh": [
        ["sfx_golem_step_01..03", "Golem Batu jalan", "Batu berat", "placeholder ada", "P1"],
        ["sfx_hound_sniff / _bark", "Anjing Lendir endus / gonggong", "Basah, lendir", "baru", "P1"],
        ["sfx_gate_growl / _beam", "Monster Gerbang napas + sorot menyapu (loop)", "Besar, dalam", "baru", "P1"],
        ["sfx_familiar_fire (loop)", "Familiar Api", "Api kretek + desis", "baru", "P1"],
        ["sfx_eye_hover (loop) / _scan", "Mata Penyihir melayang / memeriksa", "Dengung sihir", "baru", "P1"],
        ["sfx_armor_step / _listen", "Zirah Hampa langkah besi / menoleh dengar", "Logam kosong bergema", "baru", "P1"],
        ["sfx_butler_step / _lid", "Pelayan Tudung langkah / angkat tudung saji", "Klonteng logam", "baru", "P1"],
        ["sfx_portrait_eyes", "Lukisan Bermata mata bergeser", "Derit kayu/kanvas", "baru", "P1"],
        ["sfx_alch_step / _whisper / _rage / _sealed", "Alkemis jalan / bisikan roh / mengamuk / tersegel", "Paling seram di game", "baru", "P1"],
    ],
    "Dunia & puzzle": [
        ["sfx_alarm (loop)", "Alarm penyadapan berbunyi", "Sirene sengau", "baru", "P1"],
        ["sfx_door_open / _close", "Pintu", "Kayu/besi", "baru", "P1"],
        ["sfx_lever", "Tuas", "Klak logam", "baru", "P1"],
        ["sfx_plate_down / _up", "Pelat tekan (ringan/berat)", "Klik batu", "baru", "P1"],
        ["sfx_vase_break", "Barang rapuh pecah", "Keras, pengalih perhatian", "placeholder (splat)", "P1"],
        ["sfx_creak", "Lantai berderit", "Kayu tua", "baru", "P1"],
        ["sfx_crate_push", "Peti didorong", "Seret kayu", "baru", "P1"],
        ["sfx_lamp_out", "Lentera padam", "Desis", "baru", "P1"],
        ["sfx_vent_in / _out / _travel", "Ventilasi masuk / keluar / pindah lubang", "Logam, teredam", "baru", "P1"],
        ["sfx_checkpoint", "Checkpoint tersimpan", "Chime lembut", "baru", "P1"],
        ["sfx_valve_turn", "Katup aura diputar (final)", "Keras, berderit (memang memancing Alkemis)", "baru", "P1"],
        ["sfx_kingjar_open", "Toples raja terbuka", "Kaca + energi lepas", "baru", "P1"],
        ["sfx_royal_seal", "Segel Raja", "Sihir megah", "baru", "P1"],
    ],
    "Kekuatan teman": [
        ["sfx_power_thump", "Dentum (Bolu)", "Ketukan keras di ventilasi", "baru", "P1"],
        ["sfx_power_snuff", "Padam (Tok)", "Lampu mati serentak + lonceng kecil", "baru", "P1"],
        ["sfx_power_decoy / _shatter", "Umpan Kristal (Lumi) dilempar / pecah", "Kristal berdenting", "baru", "P1"],
        ["sfx_power_mist", "Kabut (Ibu)", "Desis kabut", "baru", "P1"],
    ],
    "UI": [
        ["ui_move / ui_select / ui_back", "Navigasi menu", "Pendek, lembut", "placeholder ada (click)", "P1"],
        ["ui_locked", "Pilih yang terkunci", "Bunyi tolak", "baru", "P2"],
        ["ui_pause_open / _close", "Pause", "", "baru", "P2"],
        ["ui_dialog_next", "Lanjut dialog", "", "baru", "P1"],
    ],
}

# ---------- QA ----------
QA_PROCESS = [
    "<b>Ambil build</b>: programmer membuat build lewat <i>GooNapped &gt; Build &gt; Windows Dev</i>, hasil di <i>Builds/Windows-Dev/</i>. Beri nama/tanggal build di chat.",
    "<b>Cek cepat (2 menit)</b>: jalankan <i>smoke_test.bat</i>. Harus “SMOKE SELESAI: … 0 GAGAL”. Screenshot tiap scene di <i>smoke_shots/</i> tidak boleh hitam.",
    "<b>Main</b>: <i>GooNapped.exe</i> atau <i>main_dengan_log.bat</i>. <b>F1</b> = lihat log. Penanda merah di pojok = ada error.",
    "<b>Lapor bug</b> dengan format di bawah, selalu lampirkan <i>goo-napped_log.txt</i>. Satu laporan = satu bug.",
    "Tempat lapor: GitHub Issues repo (label: bug, art, audio, level) atau satu sheet bersama. Programmer menandai “fixed di build X”, QA mengetes ulang lalu menutup.",
]
SEVERITY = [
    ["Crash", "Game tertutup/hang, harus Alt+F4.", "Hari itu juga"],
    ["Blocker", "Tidak bisa lanjut (level tidak bisa selesai, stuck, layar hitam).", "Hari itu juga"],
    ["Mayor", "Salah besar tapi bisa diakali (penjaga tembus tembok, checkpoint salah, suara hilang).", "1–2 hari"],
    ["Minor", "Kosmetik (typo, sprite geser, sorting salah).", "Kalau sempat"],
]
QA_FLOW = [
    "New Game → Intro (Space/E lanjut, Esc skip) → otomatis masuk L01 (bukan layar hitam).",
    "Tiap level selesai → Markas → level berikutnya. L11 → Ending → kembali ke menu.",
    "Continue melanjutkan level terakhir yang terbuka. Select Level: level terkunci tidak bisa dipilih.",
    "Reset Progress + New Game minta konfirmasi. Quit menutup game.",
    "Esc pause (Resume, Retry, Main Menu). R = ulang dari ventilasi terakhir.",
]
QA_LEVEL = [
    "Objektif tercapai (menyusup: sampai ventilasi exit; penyelamatan: semua teman selamat).",
    "Esens cukup dan urutan makan masuk akal; muntah (Q) bisa memperbaiki salah makan.",
    "Belah ke toples: tiruan masuk hanya kalau massa = ukuran teman; aura salah = alarm lebih cepat.",
    "Teman: mengikuti jejak, F tunggu/ikut, ikut sembunyi, masuk ventilasi = selamat. Tidak menumpuk/stuck.",
    "Masuk ventilasi = checkpoint. Tertangkap / R = mulai lagi di ventilasi itu dengan tubuh, toples, teman, pintu sama.",
    "Tiap musuh berperilaku sesuai jenisnya (lihat tabel musuh). Dikejar → putus pandangan + sembunyi = lolos.",
    "Kekuatan teman dari ventilasi (Shift, A/D, E) bekerja dan jatah terisi ulang di checkpoint.",
    "Tidak ada tempat stuck, tembus tembok, atau jatuh keluar peta. Kamera tidak keluar batas.",
    "Catat <b>waktu selesai</b> (target 2–4 menit) dan <b>berapa kali tertangkap</b>.",
]
QA_FINAL = [
    "Putar 3 katup (E): berisik, toples raja terbuka setelah katup ketiga, Alkemis mengamuk.",
    "Tinggalkan potongan di toples raja, sembunyi: Alkemis menerkam di toples = tersegel → dialog → Ending.",
    "Tertangkap di tengah final: kembali ke ventilasi, katup yang sudah terbalik tetap terbalik.",
]
QA_NONFUNC = [
    ["Resolusi & layar", "1280×720, 1920×1080, layar lebar; fullscreen ↔ windowed; Alt+Tab keluar-masuk."],
    ["Performa", "FPS stabil di PC spek rendah (contoh GPU GT 730). Catat FPS kalau patah-patah."],
    ["Audio", "Volume musik vs SFX seimbang, tidak ada suara hilang/terlalu keras, loop tanpa klik."],
    ["Teks", "Bahasa Inggris benar (typo, grammar), teks tidak terpotong."],
    ["Keterbacaan", "Di tempat gelap: slime, penjaga, kerucut pandang, esens, tempat sembunyi masih terbaca."],
    ["Pemain baru", "Minta orang yang belum pernah main. Tonton tanpa membantu. Catat di mana bingung atau mati berulang."],
]
QC_ASSET = [
    ["Sprite", "48×48 per frame, 3 arah lengkap, ada file _normal dengan layout sama, cahaya tidak di-bake, lolos tes siluet, nama file sesuai."],
    ["Tile", "48×48, menyambung rapi di semua sisi, versi tembok atas + muka, palet area benar."],
    ["Audio", "WAV 44.1 kHz 16-bit, mono/stereo sesuai jenis, loop mulus, tidak clipping, nama file sesuai, lisensi tercatat."],
    ["UI", "Terbaca di 1280×720, teks Inggris, 9-slice benar, cocok dengan palet."],
    ["Lisensi", "Semua aset pihak ketiga: sumber + lisensi dicatat di ThirdParty/ dan kredit. Tidak ada aset berbayar yang melarang redistribusi."],
]
RELEASE = [
    "Semua bug Crash/Blocker tertutup, smoke test 0 GAGAL di build rilis.",
    "Test Runner EditMode + PlayMode lolos.",
    "Nama produk, ikon, versi, kredit benar. Build rilis (GooNapped > Build > Windows Rilis) dites di PC lain.",
    "Zip build, upload ke itch.io, halaman lengkap (cover, screenshot, GIF, kontrol, kredit). Repo GitHub publik (aturan Game Off).",
    "Tes unduh dari itch.io di PC bersih: ekstrak → jalan tanpa error.",
]

DECISIONS = [
    "Nama game final (sementara Goo-napped) dan nama pemain (usulan Blip).",
    "Siapa pemegang kata akhir kalau beda pendapat desain.",
    "Daftar 11 level + musuh per level (desain-v2 bagian 3 dan 9).",
    "Isian lore yang masih usulan: asal roh, lensa aura, Inti Mesin, Segel Raja.",
    "Jawaban 27: aura toples terlihat dari jauh + adegan singkat saat mendekat (pilihan C)?",
    "Build Settings + Player Settings (ProjectSettings).",
    "Siapa penulis isi cerita (strip, dialog level, kalimat markas, catatan). Teks bahasa Inggris.",
    "Siapa desainer level (layout final 11 level) bila bukan desainer 2D.",
]

LEVELS = [
    ["L01", "The Gate", "Taman", "Monster Gerbang", "–", "Gerak, cahaya = bahaya, celah pagar"],
    ["L02", "Hedge Garden", "Taman", "Golem", "–", "Kerucut pandang, timing patroli"],
    ["L03", "Mud Path", "Taman", "Golem, Anjing", "–", "Lumpur = jejak bau, cuci di genangan"],
    ["L04", "Empty Cells", "Bawah tanah", "Golem", "Pip", "Esens → belah → tiruan → kawal"],
    ["L05", "Cell Block", "Bawah tanah", "Golem, Anjing", "Bolu", "Sembunyi, urutan makan (teman besar)"],
    ["L06", "Drain Lab", "Bawah tanah", "Familiar Api, Mata Penyihir", "Tok, Lumi", "Alarm, pemeriksa toples, kekuatan pertama"],
    ["L07", "Great Hall", "Kastil atas", "Zirah Hampa, Golem", "Ayah", "Bergerak senyap, shift jaga"],
    ["L08", "Dining Hall", "Kastil atas", "Pelayan Tudung, Anjing", "Ibu", "Tempat sembunyi diperiksa, barang jatuh"],
    ["L09", "Gallery", "Kastil atas", "Lukisan Bermata, Mata Penyihir", "Adik", "Penjaga diam menyapu, ventilasi"],
    ["L10", "Tower Stairs", "Kastil atas", "Campuran", "Nenek", "Ujian semua mekanik"],
    ["L11", "The Summit", "Puncak", "Alkemis + penjaga dipanggil", "Raja", "Boss stealth: 3 katup, segel"],
]


# =====================================================================
# SUSUN DOKUMEN
# =====================================================================
def header_footer(canvas, doc):
    canvas.saveState()
    canvas.setFont(BODY, 8)
    canvas.setFillColor(MUTED)
    if doc.page > 1:
        canvas.drawString(18 * mm, A4[1] - 12 * mm, "Goo-napped · Todo per divisi")
        canvas.drawRightString(A4[0] - 18 * mm, A4[1] - 12 * mm, DATE)
        canvas.setStrokeColor(LINE); canvas.setLineWidth(0.4)
        canvas.line(18 * mm, A4[1] - 14 * mm, A4[0] - 18 * mm, A4[1] - 14 * mm)
    canvas.drawRightString(A4[0] - 18 * mm, 10 * mm, f"hal. {doc.page}")
    canvas.restoreState()


def section(story, title, intro=None):
    story.append(PageBreak())
    story.append(P(title, "h1"))
    if intro: story.append(P(intro, "note"))


def build():
    doc = SimpleDocTemplate(OUT, pagesize=A4, leftMargin=18 * mm, rightMargin=18 * mm, topMargin=20 * mm, bottomMargin=18 * mm,
                            title="Goo-napped - Todo per divisi", author="Tim Goo-napped")
    s = []
    # sampul
    s.append(Spacer(1, 40 * mm))
    s.append(P("GOO-NAPPED", "title"))
    s.append(Spacer(1, 4))
    s.append(P("Todo lengkap per divisi · Game Off 2026 (tema SHIFT)", "sub"))
    s.append(P(DATE + " · nama game masih sementara", "sub"))
    s.append(Spacer(1, 14 * mm))
    s.append(P("Isi", "h2"))
    for t in ["1. Status & jadwal", "2. Programmer", "3. Desainer 3D", "4. Desainer 2D pixel", "5. Desainer UI",
              "6. Sound design (daftar suara lengkap)", "7. QA / QC", "8. Keputusan tim & referensi (level, tokoh, musuh)"]:
        s.append(P(t))
    s.append(Spacer(1, 10 * mm))
    s.append(P("Sumber: docs/desain-v2.md, docs/backlog.md, art/CLAUDE.md, dan kode game. Versi singkat untuk WhatsApp: "
               "docs/todo-tim/todo-tim-wa.txt. Dokumen ini dibuat ulang dengan <i>python docs/todo-tim/make_pdf.py</i>.", "note"))

    # 1 status
    section(s, "1. Status & jadwal")
    s.append(P("Sudah jalan di game", "h2")); s += bullets(STATUS_DONE)
    s.append(P("Yang kurang", "h2")); s += bullets(STATUS_TODO)
    s.append(P("Jadwal usulan", "h2"))
    s.append(table(["Waktu", "Fokus", "Siapa"], TIMELINE, [38 * mm, W - 38 * mm - 40 * mm, 40 * mm]))
    s.append(P("Prioritas", "h2"))
    s.append(table(["Kode", "Arti"], PRIO, [16 * mm, W - 16 * mm], prio_col=0))
    s.append(P("Alur kerja aset", "h2"))
    s += bullets([
        "Desainer kirim file ke folder sumber (<i>art/sprites/</i>, <i>art/blender/</i>, audio mentah) lewat branch git <i>art/&lt;aset&gt;</i>, atau kirim ke programmer kalau belum pakai git.",
        "Programmer/AI memasang ke Unity (<i>Assets/_Game/...</i>), cek di game, lalu build dev untuk dicek desainer dan QA.",
        "Ketergantungan: programmer menunggu sprite + suara untuk dipasang; QA menunggu build; UI menunggu font/logo untuk layar lain. Kirim yang sudah jadi sedikit-sedikit, jangan ditumpuk di akhir.",
        "Jangan edit scene yang sama bersamaan (konflik). Klaim scene di chat sebelum edit.",
    ])

    # 2 programmer
    section(s, "2. Programmer", "Fokus minggu 3: pasang aset yang masuk, sistem audio, pause/options, setel angka. Minggu 4: hanya bug fix dan setelan, tidak tambah fitur.")
    s.append(table(["Tugas", "Detail", "Prio", "Lokasi"], PROG, [34 * mm, W - 34 * mm - 12 * mm - 32 * mm, 12 * mm, 32 * mm], prio_col=2))

    # 3 3D
    section(s, "3. Desainer 3D (Blender → sprite 48px)")
    s.append(P("Spesifikasi wajib", "h2")); s += bullets(SPEC_SPRITE)
    s.append(P("Daftar model + animasi", "h2"))
    s.append(table(["Aset", "Detail & animasi", "Prio"], D3, [36 * mm, W - 36 * mm - 14 * mm, 14 * mm], prio_col=2))
    s.append(P("Semua animasi musuh minimal: idle, jalan (3 arah), alert. Gerak khas tiap musuh (sapu sorot, periksa, dengar, buka tudung, menerkam) "
               "membuat perilakunya terbaca pemain; prioritaskan itu setelah idle/jalan.", "note"))

    # 4 2D
    section(s, "4. Desainer 2D pixel")
    s += bullets([
        "Tile dan prop <b>48×48 px</b> (prop besar boleh kelipatan 48). Albedo flat + normal map <i>_normal</i> (cahaya dari Unity). Palet per area.",
        "Dasar gelap dingin; cahaya hangat hanya dari sumber (obor, api, kristal). Slime pemain harus paling menonjol.",
        "Sprite masuk <i>Assets/_Game/Art/Sprites/&lt;kategori&gt;/</i>; import diatur otomatis (jangan ubah import setting manual).",
    ])
    s.append(table(["Aset", "Detail", "Prio"], D2, [36 * mm, W - 36 * mm - 14 * mm, 14 * mm], prio_col=2))

    # 5 UI
    section(s, "5. Desainer UI")
    s.append(P("Aturan", "h2")); s += bullets(UI_SPEC)
    s.append(P("Daftar layar & elemen", "h2"))
    s.append(table(["Layar / elemen", "Detail", "Prio"], UI, [34 * mm, W - 34 * mm - 14 * mm, 14 * mm], prio_col=2))

    # 6 sound
    section(s, "6. Sound design", "Saat ini game baru punya 13 SFX + 1 musik + 1 ambience placeholder (proto_*.wav). "
            "Kolom Status: <b>placeholder ada</b> = sudah ada suara sementara dan sudah tersambung di kode (tinggal diganti); "
            "<b>baru</b> = belum ada, programmer perlu menyambungkannya juga.")
    s.append(P("Spesifikasi file", "h2")); s += bullets(SOUND_SPEC)
    widths = [40 * mm, 44 * mm, W - 40 * mm - 44 * mm - 26 * mm - 11 * mm, 26 * mm, 11 * mm]
    for cat, rows in SOUND.items():
        s.append(P(cat, "h2"))
        s.append(table(["File", "Kapan di game", "Catatan", "Status", "Prio"], rows, widths, prio_col=4))
    total = sum(len(r) for r in SOUND.values())
    s.append(P(f"Total {total} baris suara (beberapa baris berisi lebih dari satu file).", "smallmuted"))

    # 7 QA
    section(s, "7. QA / QC")
    s.append(P("Proses", "h2")); s += bullets(QA_PROCESS)
    s.append(P("Format lapor bug (salin ke chat/issue)", "h2"))
    s.append(P("<b>Build/tanggal:</b> …<br/><b>Level:</b> …<br/><b>Langkah:</b> 1. … 2. … 3. …<br/><b>Harusnya:</b> …<br/>"
               "<b>Yang terjadi:</b> …<br/><b>Parah:</b> Crash / Blocker / Mayor / Minor<br/><b>Lampiran:</b> goo-napped_log.txt + screenshot/video", "note"))
    s.append(P("Tingkat keparahan", "h2"))
    s.append(table(["Tingkat", "Contoh", "Target fix"], SEVERITY, [22 * mm, W - 22 * mm - 30 * mm, 30 * mm]))
    s.append(P("Checklist alur game", "h2")); s += bullets(["[  ] " + t for t in QA_FLOW])
    s.append(P("Checklist per level (ulang untuk L01–L11)", "h2")); s += bullets(["[  ] " + t for t in QA_LEVEL])
    s.append(P("Checklist final L11", "h2")); s += bullets(["[  ] " + t for t in QA_FINAL])
    s.append(P("Tabel catatan playtest per level", "h2"))
    s.append(table(["Level", "Waktu", "Tertangkap", "Susah (1–5)", "Bug / catatan"],
                   [[l[0] + " " + l[1], "", "", "", ""] for l in LEVELS],
                   [34 * mm, 18 * mm, 20 * mm, 20 * mm, W - 92 * mm]))
    s.append(P("Non-fungsional", "h2"))
    s.append(table(["Area", "Yang dicek"], QA_NONFUNC, [32 * mm, W - 32 * mm]))
    s.append(P("QC aset (sebelum dipasang)", "h2"))
    s.append(table(["Jenis", "Syarat lolos"], QC_ASSET, [22 * mm, W - 22 * mm]))
    s.append(P("Checklist rilis (minggu 4)", "h2")); s += bullets(["[  ] " + t for t in RELEASE])

    # 8 referensi
    section(s, "8. Keputusan tim & referensi")
    s.append(P("Perlu diputuskan tim", "h2")); s += bullets(DECISIONS)
    s.append(P("Daftar level", "h2"))
    s.append(table(["#", "Level", "Area", "Musuh", "Teman", "Puzzle utama"], LEVELS,
                   [11 * mm, 25 * mm, 22 * mm, 40 * mm, 18 * mm, W - 116 * mm]))
    s.append(P("Tokoh", "h2"))
    s.append(table(["Tokoh", "Ukuran", "Aura (warna)", "Level", "Peran"], [
        ["Blip (pemain)", "1", "bening", "–", "Diam tapi ekspresif"],
        ["Pip", "kecil", "embun (biru)", "4", "Sahabat, tuan rumah markas, cerewet"],
        ["Bolu", "besar", "batu (abu)", "5", "Kekuatan Dentum"],
        ["Tok", "kecil", "bayang (ungu)", "6", "Penjaga lonceng tua, kekuatan Padam"],
        ["Lumi", "kecil", "pendar (hijau)", "6", "Kekuatan Umpan Kristal"],
        ["Ayah", "besar", "bara (merah)", "7", "Keluarga"],
        ["Ibu", "besar", "embun (biru)", "8", "Keluarga, kekuatan Kabut"],
        ["Adik", "kecil", "belerang (kuning)", "9", "Keluarga"],
        ["Nenek", "besar", "beku (putih)", "10", "Tahu sejarah roh dan toples segel"],
        ["Raja", "–", "segel", "11", "Segel Raja"],
        ["Alkemis", "–", "–", "11", "Boss, dirasuki roh jahat kuno"],
    ], [26 * mm, 16 * mm, 30 * mm, 14 * mm, W - 86 * mm]))
    s.append(P("Kontrol", "h2"))
    s.append(table(["Tombol", "Aksi"], [
        ["WASD", "Gerak"], ["E", "Makan esens, tukar di toples, tuas, katup, pakai kekuatan"], ["Space", "Belah / gabung"],
        ["Shift", "Pindah kendali antar potongan / ke teman di ventilasi"], ["C", "Masuk / keluar tempat sembunyi"],
        ["F", "Teman: tunggu / ikut"], ["Q", "Muntahkan esens terakhir"], ["R / Esc", "Ulang dari checkpoint / pause"],
    ], [24 * mm, W - 24 * mm]))

    doc.build(s, onFirstPage=header_footer, onLaterPages=header_footer)
    print("ok:", OUT)


if __name__ == "__main__":
    build()
