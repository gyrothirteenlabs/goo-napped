# Art FONDASI per area (AI, 13 Nov 2026) - SEMENTARA, bukan desain final tim desainer.
# Gaya mengacu gambar konsep lab gelap (outline gelap, batu biru-abu dingin, kayu hangat, tekstur halus).
# Aturan pipeline (art/CLAUDE.md): albedo flat tanpa cahaya terarah + normal map <nama>_normal dari height map.
#
# Jalankan:  python art/tools/gen_area_art.py      (butuh pillow + numpy)
# Output:    Assets/_Game/Art/Sprites/Tiles/<Area>/*.png   (pivot tengah, untuk Tilemap)
#            Assets/_Game/Art/Sprites/Props/<Area>/*.png   (pivot bawah-tengah)
#            art/sprites/_preview_area_<area>.png           (lembar pratinjau 3x, tidak masuk game)
# Sheet = beberapa frame 48 px berjejer horizontal (PixelSpriteImporter memotong grid 48).
import os
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SPR = os.path.join(ROOT, "Assets", "_Game", "Art", "Sprites")
C = 48


def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


class Canvas:
    def __init__(self, w, h, seed=1):
        self.w, self.h = w, h
        self.col = np.zeros((h, w, 4), np.uint8)
        self.hgt = np.zeros((h, w), np.float32)
        self.rng = np.random.default_rng(seed)

    def px(self, x, y, c, ht=None):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.col[y, x] = c
            if ht is not None: self.hgt[y, x] = ht

    def rect(self, x, y, w, h, c, ht=None):
        x0, y0, x1, y1 = max(0, x), max(0, y), min(self.w, x + w), min(self.h, y + h)
        if x1 <= x0 or y1 <= y0: return
        self.col[y0:y1, x0:x1] = c
        if ht is not None: self.hgt[y0:y1, x0:x1] = ht

    def ellipse(self, cx, cy, rx, ry, c, ht=None, dome=0.0):
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                d = ((x + 0.5 - cx) / max(rx, 0.1)) ** 2 + ((y + 0.5 - cy) / max(ry, 0.1)) ** 2
                if d <= 1.0 and 0 <= x < self.w and 0 <= y < self.h:
                    self.col[y, x] = c
                    if ht is not None: self.hgt[y, x] = ht + dome * (1 - d) ** 0.5

    def speckle(self, mask, colors, density, ht_jitter=0.0):
        ys, xs = np.nonzero(mask)
        for i in range(len(xs)):
            if self.rng.random() < density:
                self.col[ys[i], xs[i]] = colors[self.rng.integers(len(colors))]
                if ht_jitter: self.hgt[ys[i], xs[i]] += (self.rng.random() - 0.5) * ht_jitter

    def outline(self, c, region=None):
        a = self.col[:, :, 3] > 0
        edge = np.zeros_like(a)
        pad = np.pad(a, 1)
        nb = pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:]
        edge = a & ~nb
        if region is not None: edge &= region
        self.col[edge] = c

    def alpha_mask(self):
        return self.col[:, :, 3] > 0


def normal_map(hgt, alpha, strength=2.2, wrap=False):
    h = hgt.astype(np.float32)
    if wrap:
        dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
        dy = (np.roll(h, 1, 0) - np.roll(h, -1, 0)) * 0.5   # atas = +G
    else:
        p = np.pad(h, 1, mode="edge")
        dx = (p[1:-1, 2:] - p[1:-1, :-2]) * 0.5
        dy = (p[:-2, 1:-1] - p[2:, 1:-1]) * 0.5
    nx, ny, nz = -dx * strength, -dy * strength, np.ones_like(h)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.stack([nx / ln, ny / ln, nz / ln], -1)
    out = np.zeros(h.shape + (4,), np.uint8)
    out[..., :3] = np.clip((n * 0.5 + 0.5) * 255, 0, 255).astype(np.uint8)
    out[..., 3] = np.where(alpha, 255, 0)
    return out


def save(canvas, folder, name, wrap=False, strength=2.2):
    os.makedirs(folder, exist_ok=True)
    Image.fromarray(canvas.col, "RGBA").save(os.path.join(folder, name + ".png"))
    Image.fromarray(normal_map(canvas.hgt, canvas.alpha_mask(), strength, wrap), "RGBA").save(os.path.join(folder, name + "_normal.png"))
    PREVIEW.append((name, canvas.col.copy()))


PREVIEW = []

# ---------------------------------------------------------------- palet
# penjara bawah tanah: batu biru-abu dingin (acuan gambar konsep), lumut hijau aura
ST_TOP = [hexc("20262f"), hexc("232a35"), hexc("1f242e")]   # atas tembok gelap (tidak terbaca sebagai lantai)
ST_TOP_GAP = hexc("12151c")
ST_TRIM = [hexc("5b6884"), hexc("66748f"), hexc("525e78")]  # batu terang untuk bingkai pintu/jendela
ST_FACE = [hexc("3f4a62"), hexc("46526c"), hexc("3a445a")]
ST_FACE_HI = hexc("56637e")
ST_MORTAR = hexc("232a3a")
ST_FLOOR = [hexc("474f63"), hexc("4f586d"), hexc("434b5e"), hexc("4b546a")]
ST_FLOOR_GAP = hexc("262c38")
MOSS = [hexc("2f5a46"), hexc("3a6e52"), hexc("487f5c")]
OUT = hexc("12151d")
WOOD = [hexc("6b4630"), hexc("74503a"), hexc("5f3e2b")]
WOOD_D = hexc("4d3324")
WOOD_L = hexc("8a5d3e")
WOOD_OUT = hexc("1f1512")
IRON = hexc("3a3f4a")
IRON_L = hexc("5d6472")
STRAW = [hexc("8a7440"), hexc("a08a4c"), hexc("6e5c34")]
# taman kastil malam: rumput biru bulan, pagar tanaman, tanah
GR = [hexc("2c4a44"), hexc("31514a"), hexc("2a453f"), hexc("345850")]
GR_BLADE = [hexc("3d655a"), hexc("447060"), hexc("26403a")]
HEDGE = [hexc("234038"), hexc("2a4b41"), hexc("1f3932")]
HEDGE_L = [hexc("35604f"), hexc("3f6d59")]
HEDGE_OUT = hexc("0f1c19")
DIRT = [hexc("4f4236"), hexc("574a3d"), hexc("483c31")]
MUD = [hexc("3a2e25"), hexc("42352a"), hexc("33291f")]
MUD_WET = hexc("5a4a3c")
WATER = [hexc("2f5a78"), hexc("356582"), hexc("2a5270")]
WATER_L = hexc("5c8fae")
FLOWER = [hexc("9fb4d8"), hexc("c9d4ea"), hexc("7d8fc4")]
BARK = [hexc("4d3324"), hexc("573a29"), hexc("432c1f")]
MUSH = hexc("b9c0cc")


def sheet(frames, seed):
    return Canvas(C * frames, C, seed)


# ================================================================ PENJARA
def dungeon():
    tiles = os.path.join(SPR, "Tiles", "Dungeon")
    props = os.path.join(SPR, "Props", "Dungeon")

    # lantai: batu lempeng tidak beraturan, 4 variasi (tileable)
    cv = sheet(4, 11)
    for f in range(4):
        ox = f * C
        cv.rect(ox, 0, C, C, ST_FLOOR_GAP, 0.0)
        # grid batu 2x2 dengan geseran per variasi
        r = np.random.default_rng(100 + f)
        cuts_x = [0, 22 + r.integers(-4, 5), C]
        for row in range(2):
            y0, y1 = (0, 23 + (f % 2) * 2) if row == 0 else (23 + (f % 2) * 2, C)
            cx = cuts_x if row == 0 else [0, 12 + r.integers(-3, 4), 34 + r.integers(-3, 4), C]
            for i in range(len(cx) - 1):
                x0, x1 = cx[i], cx[i + 1]
                base = ST_FLOOR[r.integers(len(ST_FLOOR))]
                cv.rect(ox + x0 + 1, y0 + 1, x1 - x0 - 2, y1 - y0 - 2, base, 1.0)
                # tepi sedikit miring (bevel) di height saja
                cv.hgt[y0 + 1, ox + x0 + 1:ox + x1 - 1] = 0.6
                cv.hgt[y1 - 2, ox + x0 + 1:ox + x1 - 1] = 0.6
                cv.hgt[y0 + 1:y1 - 1, ox + x0 + 1] = 0.6
                cv.hgt[y0 + 1:y1 - 1, ox + x1 - 2] = 0.6
        m = np.zeros((C, cv.w), bool); m[:, ox:ox + C] = cv.hgt[:, ox:ox + C] > 0.5
        cv.speckle(m, ST_FLOOR + [hexc("56607a")], 0.18, 0.25)
        if f == 3:   # retak
            for i in range(9): cv.px(ox + 30 + i, 30 + (i * i) % 5, ST_FLOOR_GAP, 0.2)
    save(cv, tiles, "dng_floor", wrap=True)

    # tembok: 0 atas (tutup), 1 muka bata, 2 muka berlumut, 3 muka rantai, 4 muka jendela berjeruji
    cv = sheet(5, 12)
    # atas: blok besar terang + celah gelap
    cv.rect(0, 0, C, C, ST_TOP_GAP, 0.4)
    for (x, y, w, h) in [(1, 1, 22, 14), (25, 1, 22, 14), (1, 17, 14, 14), (17, 17, 30, 14), (1, 33, 26, 14), (29, 33, 18, 14)]:
        cv.rect(x, y, w, h, ST_TOP[(x + y) % 3], 1.0)
        cv.hgt[y, x:x + w] = 0.75; cv.hgt[y:y + h, x] = 0.75
    m = np.zeros((C, cv.w), bool); m[:, :C] = cv.hgt[:, :C] > 0.9
    cv.speckle(m, ST_TOP + [hexc("2a3140")], 0.12, 0.2)
    for f in range(1, 5):
        ox = f * C
        cv.rect(ox, 0, C, C, ST_MORTAR, 0.0)
        # tutup tembok di atas muka (supaya tembok satu baris tetap terbaca bertutup)
        cv.rect(ox, 0, C, 8, ST_TOP_GAP, 1.3)
        for k in range(3): cv.rect(ox + 1 + k * 16, 1, 14, 5, ST_TOP[k], 1.4)
        cv.rect(ox, 7, C, 1, ST_TRIM[2], 1.35)
        for row in range(5):
            y = 8 + row * 8
            off = 0 if row % 2 == 0 else 8
            for bx in range(-off, C, 16):
                x0, x1 = max(0, bx) + 1, min(C, bx + 16) - 1
                if x1 - x0 < 2: continue
                cv.rect(ox + x0, y + 1, x1 - x0, 6, ST_FACE[(row + bx // 16) % 3], 1.0)
                cv.rect(ox + x0, y + 1, x1 - x0, 1, ST_FACE_HI, 1.05)   # tepi bata (bukan cahaya: warna batu aus)
        m = np.zeros((C, cv.w), bool); m[:, ox:ox + C] = cv.hgt[:, ox:ox + C] > 0.9
        cv.speckle(m, ST_FACE, 0.15, 0.2)
        cv.rect(ox, C - 3, C, 3, hexc("1a202c"), 0.0)   # kaki tembok gelap (lantai bertemu tembok)
        if f == 2:
            for x in range(C):
                hgt = int(6 + 5 * abs(np.sin(x * 0.35)) + (x % 7 == 0) * 4)
                for y in range(C - 3 - hgt, C - 3):
                    if cv.rng.random() < 0.85: cv.px(ox + x, y, MOSS[cv.rng.integers(3)], 1.1)
            for x in range(4, C, 9):
                for y in range(8, 11 + (x % 4)):
                    cv.px(ox + x, y, MOSS[1], 1.1)
        if f == 3:
            for ch in (14, 34):
                for y in range(10, 34, 4):
                    cv.rect(ox + ch - 1, y, 3, 3, IRON, 1.4); cv.px(ox + ch, y + 1, ST_MORTAR, 1.0)
                cv.rect(ox + ch - 3, 34, 7, 4, IRON, 1.5); cv.rect(ox + ch - 2, 35, 5, 2, ST_MORTAR, 1.2)
        if f == 4:
            cv.rect(ox + 12, 8, 24, 20, OUT, 0.0); cv.rect(ox + 13, 9, 22, 18, hexc("0b0e15"), 0.0)
            for x in (17, 23, 29):
                cv.rect(ox + x, 9, 2, 18, IRON, 1.5); cv.px(ox + x, 9, IRON_L, 1.6)
            cv.rect(ox + 12, 28, 24, 2, ST_TRIM[0], 1.2)
    save(cv, tiles, "dng_wall", wrap=True)

    # dekor lantai (transparan): 0 retak, 1 lumut, 2 kerikil, 3 jerami, 4 noda genangan
    cv = sheet(5, 13)
    for i in range(14): cv.px(10 + i, 20 + int(3 * np.sin(i * 0.9)), ST_FLOOR_GAP, -0.3)
    for i in range(6): cv.px(18 + i, 26 + i // 2, ST_FLOOR_GAP, -0.3)
    for k in range(5):
        cx, cy = 48 + 12 + cv.rng.integers(24), 12 + cv.rng.integers(24)
        cv.ellipse(cx, cy, 4 + cv.rng.integers(4), 2 + cv.rng.integers(3), MOSS[cv.rng.integers(3)], 0.2, 0.3)
    for k in range(7):
        cx, cy = 96 + 8 + cv.rng.integers(32), 10 + cv.rng.integers(30)
        cv.ellipse(cx, cy, 2, 1.5, ST_TRIM[cv.rng.integers(3)], 0.5, 0.6); cv.px(int(cx), int(cy + 2), OUT, 0.2)
    for k in range(26):
        x, y = 144 + 8 + cv.rng.integers(32), 14 + cv.rng.integers(22)
        ln = 4 + cv.rng.integers(5); dx = 1 if cv.rng.random() < 0.5 else -1
        for i in range(ln): cv.px(x + i * dx, y + (i // 3), STRAW[cv.rng.integers(3)], 0.3)
    cv.ellipse(192 + 24, 26, 14, 6, (24, 32, 46, 200), 0.0)
    cv.ellipse(192 + 26, 25, 8, 3, (52, 78, 104, 190), 0.0)
    save(cv, tiles, "dng_decor", wrap=False, strength=1.5)

    # ---------- props penjara ----------
    # tong (tempat sembunyi muat 1)
    cv = Canvas(C, C, 21)
    cv.ellipse(24, 36, 13, 9, WOOD_OUT, 0.0)
    cv.rect(11, 14, 26, 24, WOOD[0], 1.0)
    cv.ellipse(24, 38, 13, 6, WOOD[0], 1.0)
    for x in range(11, 37):
        bulge = 1.0 + 0.35 * np.sin((x - 11) / 26 * np.pi)
        cv.hgt[14:42, x] = bulge
        if (x - 11) % 6 == 0: cv.col[16:40, x] = WOOD_D
    cv.ellipse(24, 14, 13, 5, WOOD_L, 1.4); cv.ellipse(24, 14, 10, 3, WOOD_D, 1.3)
    for y in (19, 33):
        cv.rect(11, y, 26, 3, IRON, 1.5); cv.rect(11, y, 26, 1, IRON_L, 1.55)
    m = cv.alpha_mask(); cv.speckle(m & (cv.hgt > 0.9) & (cv.hgt < 1.39), WOOD, 0.1)
    cv.outline(WOOD_OUT)
    save(cv, props, "prop_barrel")

    # peti (tempat sembunyi muat 2): 2 sel lebar? tetap 48, peti kayu besar
    cv = Canvas(C, C, 22)
    cv.rect(6, 16, 36, 28, WOOD[0], 1.0)
    cv.rect(6, 10, 36, 8, WOOD_L, 1.3)                       # tutup (sisi atas, tampak 3/4)
    for x in (6, 39):
        cv.rect(x, 10, 3, 34, WOOD_D, 1.4)
    cv.rect(6, 16, 36, 3, WOOD_D, 1.4); cv.rect(6, 41, 36, 3, WOOD_D, 1.4)
    for i in range(30):   # silang
        cv.px(9 + i, 19 + int(i * 22 / 30), WOOD_D, 1.3); cv.px(10 + i, 19 + int(i * 22 / 30), WOOD_D, 1.3)
        cv.px(38 - i, 19 + int(i * 22 / 30), WOOD_D, 1.3)
    for y in range(20, 41, 5): cv.rect(9, y, 30, 1, WOOD[2], 1.0)
    m = cv.alpha_mask(); cv.speckle(m, WOOD, 0.06)
    cv.outline(WOOD_OUT)
    save(cv, props, "prop_crate")

    # obor dinding (nyala, padam): api = emisif (material unlit di Unity)
    cv = sheet(2, 23)
    for f in range(2):
        ox = f * C
        cv.rect(ox + 21, 22, 6, 18, BARK[0], 1.2); cv.rect(ox + 21, 22, 2, 18, BARK[1], 1.3)
        cv.rect(ox + 18, 20, 12, 4, IRON, 1.5); cv.rect(ox + 18, 20, 12, 1, IRON_L, 1.6)
        cv.rect(ox + 20, 38, 8, 3, IRON, 1.4)
        if f == 0:
            for (cx, cy, rx, ry, c) in [(24, 13, 6, 8, hexc("e85d22")), (24, 14, 4, 6, hexc("f08a2e")), (24, 16, 2.5, 4, hexc("ffc861")), (24, 17, 1.2, 2, hexc("fff0b0"))]:
                cv.ellipse(ox + cx, cy, rx, ry, c, 1.8)
        else:
            cv.rect(ox + 20, 16, 8, 5, hexc("2a2622"), 1.6); cv.px(ox + 23, 12, (120, 120, 130, 120), 1.0); cv.px(ox + 25, 9, (120, 120, 130, 80), 1.0)
    cv.outline(OUT)
    save(cv, props, "prop_torch")

    # ventilasi (2 frame: lubang biasa, lubang exit dengan cahaya hijau aura)
    cv = sheet(2, 24)
    for f in range(2):
        ox = f * C
        cv.rect(ox + 6, 16, 36, 26, IRON, 1.3)
        cv.rect(ox + 6, 16, 36, 2, IRON_L, 1.4)
        cv.rect(ox + 9, 20, 30, 19, hexc("0b0e15") if f == 0 else hexc("123a2c"), 0.2)
        for x in range(11, 38, 5):
            cv.rect(ox + x, 20, 2, 19, IRON, 1.4); cv.px(ox + x, 20, IRON_L, 1.5)
        for (x, y) in [(8, 18), (39, 18), (8, 39), (39, 39)]:
            cv.px(ox + x, y, IRON_L, 1.6)
        if f == 1:
            for y in range(21, 38, 3): cv.rect(ox + 13, y, 22, 1, (63, 209, 160, 140), 0.3)
    cv.outline(OUT)
    save(cv, props, "prop_vent")

    # toples kaca kosong (untuk toples teman: isi teman dirender di atasnya oleh prefab)
    cv = Canvas(C, C, 25)
    glass_edge = (170, 205, 220, 255); glass = (120, 160, 185, 70); glass_hi = (220, 240, 250, 200)
    cv.rect(13, 6, 22, 5, WOOD[0], 1.4); cv.rect(13, 6, 22, 1, WOOD_L, 1.5)       # tutup gabus
    cv.rect(15, 11, 18, 3, glass_edge, 1.2)                                       # leher
    cv.ellipse(24, 28, 14, 15, glass, 0.6, 0.6)
    for y in range(13, 44):
        for x in range(9, 40):
            d = ((x + 0.5 - 24) / 14) ** 2 + ((y + 0.5 - 28) / 15) ** 2
            if 0.82 < d <= 1.0: cv.px(x, y, glass_edge, 1.0)
    for y in range(18, 34): cv.px(14, y, glass_hi, 1.1)
    cv.px(15, 16, glass_hi, 1.1); cv.px(15, 17, glass_hi, 1.1)
    cv.rect(10, 42, 28, 4, IRON, 1.2); cv.rect(10, 42, 28, 1, IRON_L, 1.3)        # alas besi
    cv.outline(OUT, region=cv.col[:, :, 3] == 255)
    save(cv, props, "prop_jar")

    # pintu sel (2 frame: tertutup, terbuka) 48x48, jeruji besi + kayu
    cv = sheet(2, 26)
    for f in range(2):
        ox = f * C
        cv.rect(ox + 2, 2, 44, 44, ST_MORTAR, 0.2)
        cv.rect(ox + 2, 2, 44, 4, ST_TRIM[0], 1.2); cv.rect(ox + 2, 2, 4, 44, ST_TRIM[1], 1.2); cv.rect(ox + 42, 2, 4, 44, ST_TRIM[1], 1.2)
        if f == 0:
            for x in range(9, 41, 6):
                cv.rect(ox + x, 6, 3, 40, IRON, 1.6); cv.px(ox + x, 6, IRON_L, 1.7); cv.px(ox + x + 1, 7, IRON_L, 1.7)
            cv.rect(ox + 6, 20, 36, 3, IRON, 1.65); cv.rect(ox + 6, 20, 36, 1, IRON_L, 1.7)
            cv.rect(ox + 34, 26, 4, 5, hexc("8a7440"), 1.8)   # gembok kuningan
        else:
            cv.rect(ox + 6, 6, 5, 40, IRON, 1.6)              # jeruji terbuka ke samping
            for y in range(8, 44, 6): cv.rect(ox + 6, y, 5, 1, IRON_L, 1.7)
    cv.outline(OUT)
    save(cv, props, "prop_celldoor")

    # rak ramuan (dekor, menempel tembok) - mengacu gambar konsep
    cv = Canvas(C, C, 27)
    cv.rect(4, 6, 40, 40, WOOD[0], 1.0); cv.rect(7, 9, 34, 34, WOOD_D, 0.4)
    for y in (24, 41): cv.rect(4, y, 40, 3, WOOD_L, 1.2)
    bottle_cols = [hexc("3fd1c7"), hexc("7a52c7"), hexc("c2253a"), hexc("ffc861"), hexc("3a6e52"), hexc("4d6a8c")]
    for shelf_y in (24, 41):
        x = 9
        while x < 38:
            w = 3 + cv.rng.integers(3); hgt = 6 + cv.rng.integers(6)
            c = bottle_cols[cv.rng.integers(len(bottle_cols))]
            cv.rect(x, shelf_y - hgt, w, hgt, c, 1.3); cv.rect(x + w // 2, shelf_y - hgt - 2, 1, 2, c, 1.3)
            cv.px(x, shelf_y - hgt + 1, (230, 240, 250, 255), 1.4)
            x += w + 1 + cv.rng.integers(2)
    cv.outline(WOOD_OUT)
    save(cv, props, "prop_shelf")


# ================================================================ TAMAN KASTIL
def garden():
    tiles = os.path.join(SPR, "Tiles", "Garden")
    props = os.path.join(SPR, "Props", "Garden")

    # rumput: 4 variasi tileable (helai, sesekali bunga pucat)
    cv = sheet(4, 31)
    for f in range(4):
        ox = f * C
        cv.rect(ox, 0, C, C, GR[0], 1.0)
        m = np.zeros((C, cv.w), bool); m[:, ox:ox + C] = True
        cv.speckle(m, GR, 0.35, 0.15)
        r = np.random.default_rng(200 + f)
        for k in range(22):
            x, y = r.integers(C), r.integers(C)
            for i in range(2 + r.integers(3)):
                cv.px(ox + (x + (i % 2)) % C, (y - i) % C, GR_BLADE[r.integers(3)], 1.3)
        if f in (1, 3):
            for k in range(3):
                x, y = r.integers(4, C - 4), r.integers(4, C - 4)
                c = FLOWER[r.integers(3)]
                for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)): cv.px(ox + x + dx, y + dy, c if (dx or dy) else hexc("ffc861"), 1.4)
    save(cv, tiles, "gdn_grass", wrap=True, strength=1.6)

    # tanah / jalan setapak: 2 variasi tileable
    cv = sheet(2, 32)
    for f in range(2):
        ox = f * C
        cv.rect(ox, 0, C, C, DIRT[f], 1.0)
        m = np.zeros((C, cv.w), bool); m[:, ox:ox + C] = True
        cv.speckle(m, DIRT + [hexc("615243")], 0.3, 0.3)
        r = np.random.default_rng(300 + f)
        for k in range(6):
            cx, cy = r.integers(C), r.integers(C)
            cv.ellipse(ox + cx, cy, 1.5, 1, hexc("6b5b4a"), 1.4, 0.4)
    save(cv, tiles, "gdn_dirt", wrap=True, strength=1.8)

    # lumpur & genangan (dipakai zona lumpur/air di semua area)
    cv = sheet(2, 33)
    cv.rect(0, 0, C, C, MUD[0], 0.8)
    m = np.zeros((C, cv.w), bool); m[:, :C] = True
    cv.speckle(m, MUD, 0.4, 0.3)
    for k in range(7):
        cx, cy = cv.rng.integers(C), cv.rng.integers(C)
        cv.ellipse(cx, cy, 3 + cv.rng.integers(3), 1.5, MUD_WET, 0.6)
    cv.rect(C, 0, C, C, WATER[0], 0.5)
    m = np.zeros((C, cv.w), bool); m[:, C:] = True
    cv.speckle(m, WATER, 0.3, 0.05)
    for k in range(5):
        x, y = C + cv.rng.integers(4, C - 10), cv.rng.integers(4, C - 4)
        cv.rect(x, y, 4 + cv.rng.integers(5), 1, WATER_L, 0.55)
    save(cv, tiles, "env_mud_water", wrap=True, strength=1.2)

    # pagar tanaman (tembok taman): 0 atas (kanopi daun), 1 muka, 2 muka berbunga, 3 muka dengan akar
    cv = sheet(4, 34)
    # atas: gumpalan daun bertumpuk (tileable)
    cv.rect(0, 0, C, C, HEDGE[2], 1.0)
    r = np.random.default_rng(400)
    for k in range(40):
        cx, cy = r.integers(C), r.integers(C)
        rad = 4 + r.integers(4)
        for dx in (-C, 0, C):
            for dy in (-C, 0, C):
                cv.ellipse(cx + dx, cy + dy, rad, rad * 0.8, HEDGE[r.integers(3)], 1.0, 0.6)
    for k in range(30):
        x, y = r.integers(C), r.integers(C)
        cv.px(x, y, HEDGE_L[r.integers(2)], 1.6); cv.px((x + 1) % C, y, HEDGE_L[0], 1.6)
    cv.hgt[:, :C] = np.clip(cv.hgt[:, :C], 0.8, 2.0)
    for f in range(1, 4):
        ox = f * C
        cv.rect(ox, 0, C, C, HEDGE[2], 1.0)
        rr = np.random.default_rng(410 + f)
        for k in range(55):
            cx, cy = rr.integers(C), rr.integers(-4, C - 8)
            cv.ellipse(ox + cx, cy, 3 + rr.integers(3), 2 + rr.integers(2), HEDGE[rr.integers(3)], 1.0, 0.4)
            if cx < 6: cv.ellipse(ox + cx + C - 0.01, cy, 3, 2, HEDGE[1], 1.0, 0.4)
        # makin bawah makin gelap (bagian dalam semak, bukan cahaya)
        for y in range(C - 12, C):
            t = (y - (C - 12)) / 12
            dark = (np.array(cv.col[y, ox:ox + C, :3], np.float32) * (1 - 0.45 * t)).astype(np.uint8)
            cv.col[y, ox:ox + C, :3] = dark
        cv.rect(ox, C - 2, C, 2, HEDGE_OUT, 0.0)
        if f == 2:
            for k in range(7):
                x, y = rr.integers(3, C - 3), rr.integers(4, C - 16)
                cv.px(ox + x, y, FLOWER[1], 1.5); cv.px(ox + x + 1, y, FLOWER[0], 1.5); cv.px(ox + x, y + 1, FLOWER[2], 1.5)
        if f == 3:
            for x in (9, 30, 41):
                for y in range(C - 9, C - 2): cv.px(ox + x + (y % 2), y, BARK[0], 1.2)
    save(cv, tiles, "gdn_hedge", wrap=True, strength=1.8)

    # dekor taman (transparan): 0 rumput tinggi, 1 bunga, 2 kerikil, 3 jamur, 4 daun gugur
    cv = sheet(5, 35)
    for k in range(9):
        x = 14 + cv.rng.integers(20); h = 6 + cv.rng.integers(7)
        for i in range(h): cv.px(x + int(np.sin(i * 0.4 + k) * 1.5), 34 - i, GR_BLADE[cv.rng.integers(2)], 1.2)
    for k in range(5):
        x, y = 48 + 10 + cv.rng.integers(28), 12 + cv.rng.integers(26)
        for i in range(4): cv.px(x, y + i, GR_BLADE[0], 1.1)
        c = FLOWER[cv.rng.integers(3)]
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, -1), (0, 1)): cv.px(x + dx, y - 1 + dy, c if (dx or dy) else hexc("ffc861"), 1.4)
    for k in range(6):
        cx, cy = 96 + 10 + cv.rng.integers(28), 12 + cv.rng.integers(26)
        cv.ellipse(cx, cy, 2.5, 1.8, hexc("6b7180"), 0.5, 0.7); cv.px(int(cx), int(cy + 2), OUT, 0.2)
    for (cx, cy, s) in [(144 + 18, 28, 1.0), (144 + 28, 32, 0.7), (144 + 24, 22, 0.55)]:
        cv.rect(int(cx - 1), int(cy), 3, int(6 * s), MUSH, 1.2)
        cv.ellipse(cx + 0.5, cy, 5 * s + 1, 3 * s + 1, hexc("7a52c7"), 1.5, 0.6)
        cv.px(int(cx - 1), int(cy - 1), hexc("c9d4ea"), 1.7)
    for k in range(10):
        x, y = 192 + 8 + cv.rng.integers(32), 8 + cv.rng.integers(32)
        c = [hexc("6b4630"), hexc("8a5d3e"), hexc("4d3324")][cv.rng.integers(3)]
        cv.rect(x, y, 3, 2, c, 0.4); cv.px(x + 1, y + 2, c, 0.4)
    save(cv, tiles, "gdn_decor", wrap=False, strength=1.5)

    # pohon (prop dekor 96x96, pivot bawah-tengah): 3 variasi dalam satu sheet 288x96
    tw = 96
    cv = Canvas(tw * 3, tw, 36)
    for f in range(3):
        ox = f * tw
        r = np.random.default_rng(500 + f)
        trunk_w = 10 + f * 2
        cv.rect(ox + 48 - trunk_w // 2, 58, trunk_w, 34, BARK[0], 1.2)
        for y in range(58, 92, 3): cv.px(ox + 48 - trunk_w // 2 + 2 + (y % 5), y, BARK[2], 1.0)
        cv.rect(ox + 48 - trunk_w // 2 - 4, 88, trunk_w + 8, 4, BARK[2], 1.0)   # akar
        canopy_cy = 36 - f * 2
        blobs = [(48, canopy_cy, 34, 26)] + [(48 + r.integers(-24, 25), canopy_cy + r.integers(-16, 14), 12 + r.integers(8), 10 + r.integers(6)) for _ in range(14)]
        for (cx, cy, rx, ry) in blobs:
            cv.ellipse(ox + cx, cy, rx, ry, HEDGE[r.integers(3)], 1.2, 0.8)
        for k in range(60):
            x, y = ox + 48 + r.integers(-30, 31), canopy_cy + r.integers(-22, 22)
            if cv.col[y, x, 3] and cv.hgt[y, x] > 1.4: cv.px(x, y, HEDGE_L[r.integers(2)], cv.hgt[y, x] + 0.1)
        for y in range(canopy_cy + 14, canopy_cy + 30):   # bawah kanopi lebih gelap
            for x in range(ox, ox + tw):
                if cv.col[y, x, 3] and cv.hgt[y, x] >= 1.2 and y < 60:
                    cv.col[y, x, :3] = (np.array(cv.col[y, x, :3], np.float32) * 0.75).astype(np.uint8)
    cv.outline(HEDGE_OUT)
    os.makedirs(props, exist_ok=True)
    Image.fromarray(cv.col, "RGBA").save(os.path.join(props, "prop_tree_c96.png"))
    Image.fromarray(normal_map(cv.hgt, cv.alpha_mask(), 1.6), "RGBA").save(os.path.join(props, "prop_tree_c96_normal.png"))
    PREVIEW.append(("prop_tree", cv.col.copy()))

    # semak (tempat sembunyi taman, 48) + semak bergoyang (frame 1)
    cv = sheet(2, 37)
    for f in range(2):
        ox = f * C
        sh = 1 if f == 1 else 0
        for (cx, cy, rx, ry) in [(24, 30, 18, 13), (14 + sh, 24, 10, 9), (34 - sh, 23, 10, 9), (24, 18, 11, 9)]:
            cv.ellipse(ox + cx, cy, rx, ry, HEDGE[(cx + cy) % 3], 1.1, 0.8)
        for k in range(28):
            x, y = ox + 8 + cv.rng.integers(32), 10 + cv.rng.integers(30)
            if cv.col[y, x, 3]: cv.px(x, y, HEDGE_L[cv.rng.integers(2)], cv.hgt[y, x] + 0.1)
        for k in range(4):
            x, y = ox + 12 + cv.rng.integers(24), 14 + cv.rng.integers(18)
            cv.px(x, y, FLOWER[0], 1.9)
    cv.outline(HEDGE_OUT)
    save(cv, props, "prop_bush", strength=1.8)


def preview(name, items, scale=3):
    pad = 6
    widths = [im.shape[1] for _, im in items]
    W = min(1200, sum(w * scale + pad for w in widths) + pad)
    rows, row, x = [], [], pad
    for n, im in items:
        w = im.shape[1] * scale
        if x + w + pad > 1200 and row:
            rows.append(row); row, x = [], pad
        row.append((n, im)); x += w + pad
    if row: rows.append(row)
    H = sum(max(im.shape[0] for _, im in r) * scale + pad for r in rows) + pad
    out = Image.new("RGBA", (W, H), (11, 15, 26, 255))
    y = pad
    for r in rows:
        x = pad
        for n, im in r:
            img = Image.fromarray(im, "RGBA").resize((im.shape[1] * scale, im.shape[0] * scale), Image.NEAREST)
            out.alpha_composite(img, (x, y)); x += img.width + pad
        y += max(im.shape[0] for _, im in r) * scale + pad
    os.makedirs(os.path.join(ROOT, "art", "sprites"), exist_ok=True)
    out.save(os.path.join(ROOT, "art", "sprites", "_preview_area_" + name + ".png"))


if __name__ == "__main__":
    dungeon(); preview("dungeon", PREVIEW); PREVIEW.clear()
    garden(); preview("garden", PREVIEW)
    print("ok")
