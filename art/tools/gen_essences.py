# Sprite esens (AI, 19 Nov 2026) - SEMENTARA, desain final tim desainer.
# Arah dipilih user: BENTUK UNIK per esens (bukan bulatan berwarna), supaya terbaca dari bentuk, bukan warna saja.
#   Batu = bongkah batu berat   Pendar = jamur berpendar   Embun = tetes embun di daun   Bara = arang membara
#   Belerang = kristal kuning berasap bau   Beku = serpih es   Bayang = gumpalan asap ungu
# Tiap file: 2 frame 48 px -> frame 0 = albedo (dengan outline gelap), frame 1 = bagian yang MENYALA (dipakai
# renderer glow unlit di prefab IngredientPickup; kosong kalau esens tidak menyala). Normal map dari height map.
#
# Jalankan: python art/tools/gen_essences.py   -> Assets/_Game/Art/Sprites/Props/Essence/essence_<id>.png
#           pratinjau art/sprites/_preview_essences.png
import os
import numpy as np
from PIL import Image

import gen_area_art as A
from gen_area_art import C, hexc

OUT = os.path.join(A.SPR, "Props", "Essence")
OUTLINE = hexc("15121c")
BASE_Y = 41          # garis bawah benda (pivot sprite = tepi bawah sel)


class Layer:
    def __init__(self, seed):
        self.cv = A.Canvas(C, C, seed)      # albedo + height
        self.glow = np.zeros((C, C, 4), np.uint8)

    def g(self, x, y, c):
        if 0 <= x < C and 0 <= y < C: self.glow[y, x] = c


def ell(cv, cx, cy, rx, ry, c, ht, dome=0.4):
    cv.ellipse(cx, cy, rx, ry, c, ht, dome)


def finish(L, name):
    cv = L.cv
    cv.outline(OUTLINE, region=cv.col[:, :, 3] == 255)
    # glow hanya di pixel yang terlihat (tidak keluar dari bentuk), kecuali percikan (alpha dijaga)
    sheet = A.Canvas(C * 2, C, 0)
    sheet.col[:, :C] = cv.col; sheet.hgt[:, :C] = cv.hgt
    sheet.col[:, C:] = L.glow
    A.save(sheet, OUT, "essence_" + name, strength=2.4)


def batu():
    L = Layer(301); cv = L.cv
    ST = [hexc("7d838f"), hexc("6b717c"), hexc("8f95a1"), hexc("5a5f69")]
    # bongkah besar bersegi + batu kecil di samping
    poly = [(13, 41), (11, 34), (15, 27), (22, 24), (30, 25), (35, 30), (36, 37), (33, 41)]
    m = Image.new("L", (C, C), 0)
    from PIL import ImageDraw
    ImageDraw.Draw(m).polygon(poly, fill=255)
    mk = np.array(m) > 0
    cv.col[mk] = ST[1]; cv.hgt[mk] = 1.4
    # sisi atas lebih terang, sisi kanan lebih gelap (bentuk bersegi, bukan cahaya terarah keras)
    top = mk & (np.arange(C)[:, None] < 31)
    cv.col[top & (np.arange(C)[None, :] < 30)] = ST[2]; cv.hgt[top] = 1.7
    right = mk & (np.arange(C)[None, :] > 30)
    cv.col[right] = ST[3]; cv.hgt[right] = 1.2
    for (x, y) in [(18, 33), (19, 34), (20, 34), (21, 35), (26, 29), (27, 30), (27, 31)]:
        cv.px(x, y, hexc("43474f"), 1.0)                 # retakan
    cv.speckle(mk, ST, 0.12, 0.2)
    ell(cv, 9, 38, 4.5, 3.5, ST[1], 1.1); ell(cv, 8, 37, 2.5, 1.8, ST[2], 1.3)
    ell(cv, 39, 39, 3.5, 2.8, ST[3], 1.0); ell(cv, 38, 38, 1.8, 1.2, ST[1], 1.1)
    finish(L, "batu")


def pendar():
    L = Layer(302); cv = L.cv
    STEM = hexc("d9dccb"); STEM_D = hexc("aeb2a0")
    CAP = [hexc("3fbf6a"), hexc("5ad882"), hexc("2f9a55")]
    GLOW = (150, 255, 170, 255)
    caps = [(24, 26, 8, 5, 31), (15, 33, 5, 3.5, 36), (32, 33, 4.5, 3, 37)]   # cx, cy, rx, ry, dasar batang
    for cx, cy, rx, ry, base in caps:
        cv.rect(cx - 1, cy, 3, base - cy + 4, STEM, 1.2); cv.rect(cx + 1, cy, 1, base - cy + 4, STEM_D, 1.15)
    ell(cv, 24, 41, 12, 2.5, hexc("2c3a2f"), 0.6, 0)     # tanah lembap di bawah
    for cx, cy, rx, ry, base in caps:
        ell(cv, cx, cy, rx, ry, CAP[2], 1.6, 0.5)
        ell(cv, cx, cy - 1, rx - 1, ry - 1.2, CAP[0], 1.75, 0.5)
        ell(cv, cx - 1, cy - 2, rx * 0.5, ry * 0.4, CAP[1], 1.85, 0.3)
        for (dx, dy) in [(-rx * 0.5, -0.5), (rx * 0.3, -1.5), (rx * 0.1, 0.8)]:
            x, y = int(cx + dx), int(cy + dy)
            cv.px(x, y, hexc("e8ffe6"), 1.9); L.g(x, y, GLOW)
        # tutup jamur menyala lembut
        for y in range(int(cy - ry), int(cy + ry) + 1):
            for x in range(int(cx - rx), int(cx + rx) + 1):
                if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 0.8:
                    if L.glow[y, x, 3] == 0: L.g(x, y, (80, 230, 120, 150))
    finish(L, "pendar")


def embun():
    L = Layer(303); cv = L.cv
    LEAF = [hexc("3d6e45"), hexc("4f8a56"), hexc("2c5234")]
    W = [hexc("5aa2e6"), hexc("7cbcf2"), hexc("3f7fc4")]
    # daun lebar miring dengan tulang daun
    for y in range(30, 43):
        for x in range(6, 43):
            u = (x - 24) / 18.0; v = (y - 37 + (x - 24) * 0.12) / 5.0
            if u * u + v * v <= 1.0: cv.px(x, y, LEAF[0] if v < 0 else LEAF[2], 1.0 + 0.3 * (1 - u * u))
    for x in range(8, 41): cv.px(x, int(37 - (x - 24) * 0.12), LEAF[1], 1.25)
    cv.rect(40, 34, 3, 2, LEAF[2], 1.0)                     # tangkai
    # tetes besar: bulat bawah + ujung runcing atas
    ell(cv, 22, 31, 6, 6, W[2], 1.8, 0.8)
    for y in range(19, 27):
        w = int((y - 19) * 0.75) + 1
        cv.rect(22 - w // 2, y, w, 1, W[2], 1.7)
    ell(cv, 22, 31, 4.5, 4.5, W[0], 2.0, 0.8)
    for y in range(22, 27): cv.px(22, y, W[0], 1.8)
    ell(cv, 20, 29, 1.5, 2, W[1], 2.3, 0.2); cv.px(19, 28, hexc("eaf6ff"), 2.4); cv.px(19, 27, hexc("eaf6ff"), 2.4)
    # tetes kecil
    ell(cv, 33, 35, 3.2, 3.2, W[2], 1.5, 0.6); ell(cv, 33, 35, 2.2, 2.2, W[0], 1.6); cv.px(32, 34, hexc("eaf6ff"), 1.8)
    finish(L, "embun")


def bara():
    L = Layer(304); cv = L.cv
    COAL = [hexc("3a2c2c"), hexc("4a3836"), hexc("2a1f20")]
    HOT = hexc("ff8a3a"); HOTTER = hexc("ffd27a")
    ell(cv, 24, 36, 12, 6.5, COAL[2], 1.3, 0.5)
    ell(cv, 23, 34, 10, 5, COAL[0], 1.5, 0.5)
    ell(cv, 21, 32, 5, 2.5, COAL[1], 1.7, 0.3)
    ell(cv, 36, 39, 4, 2.5, COAL[0], 1.1)                   # pecahan kecil
    mk = cv.col[:, :, 3] > 0
    cv.speckle(mk, COAL, 0.15, 0.2)
    cracks = [[(14, 35), (16, 34), (18, 35), (20, 34), (22, 36)], [(25, 31), (27, 33), (29, 33), (31, 35), (33, 34)],
              [(19, 38), (22, 39), (25, 38), (28, 39)], [(35, 39), (37, 38)]]
    for line in cracks:
        for (x0, y0), (x1, y1) in zip(line, line[1:]):
            for t in np.linspace(0, 1, 6):
                x, y = int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t))
                cv.px(x, y, HOT, 1.2); L.g(x, y, (255, 150, 60, 255))
        x, y = line[len(line) // 2]; cv.px(x, y, HOTTER, 1.25); L.g(x, y, (255, 230, 150, 255))
    for (x, y, a) in [(22, 24, 255), (26, 20, 210), (19, 18, 170), (28, 15, 130)]:   # percikan naik
        cv.px(x, y, (255, 170, 80, a), 2.0); L.g(x, y, (255, 180, 90, a))
    finish(L, "bara")


def belerang():
    L = Layer(305); cv = L.cv
    ROCK = [hexc("5c564a"), hexc("6e6758")]
    Y = [hexc("e3c84a"), hexc("f3e07a"), hexc("b39a2e")]
    ell(cv, 24, 39, 12, 3.5, ROCK[0], 1.0, 0.3); ell(cv, 22, 38, 7, 2, ROCK[1], 1.15)
    shards = [(24, 38, 5, 16), (17, 39, 3, 9), (31, 39, 3.5, 11), (28, 39, 2.5, 7)]   # x dasar, y dasar, lebar, tinggi
    for (bx, by, hw, h) in shards:
        for i in range(h):
            y = by - i; w = max(1, int(hw * (1 - i / h) + 0.5))
            for x in range(int(bx - w), int(bx + w) + 1):
                cv.px(x, y, Y[1] if x < bx else (Y[0] if x == int(bx) else Y[2]), 1.4 + i * 0.06)
    # asap bau (garis bergelombang kuning-hijau tipis di atas)
    for k, x0 in enumerate((19, 27)):
        for y in range(6, 20):
            x = x0 + int(round(1.6 * np.sin(y * 0.7 + k)))
            cv.px(x, y, (190, 200, 90, 120 + y * 5), 0.3)
    finish(L, "belerang")


def beku():
    L = Layer(306); cv = L.cv
    ICE = [hexc("cfe6f5"), hexc("f2fbff"), hexc("9cc4df"), hexc("7aa6c6")]
    ell(cv, 24, 40, 12, 2.5, ICE[3], 0.8, 0.2)              # genangan beku
    shards = [(24, 39, 4, 17, 0), (18, 40, 3, 10, -2), (30, 40, 3, 12, 2), (34, 40, 2, 6, 1)]
    for (bx, by, hw, h, lean) in shards:
        for i in range(h):
            y = by - i; cx = bx + lean * i / h; w = max(0.6, hw * (1 - i / h))
            for x in range(int(cx - w), int(cx + w) + 1):
                cv.px(x, y, ICE[1] if x < cx - 0.5 else (ICE[0] if x <= cx + 0.5 else ICE[2]), 1.5 + i * 0.07)
    for (x, y) in [(22, 27), (29, 31), (16, 34)]:
        cv.px(x, y, hexc("ffffff"), 2.2)
    for (x, y) in [(12, 26), (37, 24)]:                     # kilau dingin melayang
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            cv.px(x + dx, y + dy, (230, 245, 255, 255 if (dx, dy) == (0, 0) else 150), 0.5)
    finish(L, "beku")


def bayang():
    L = Layer(307); cv = L.cv
    P = [hexc("3a2a5c"), hexc("553f86"), hexc("2a1e44"), hexc("7a5cb8")]
    # gumpalan asap berlapis + sulur di bawah, inti gelap
    blobs = [(24, 30, 9, 7.5, P[2]), (19, 27, 5, 4.5, P[0]), (29, 26, 5.5, 5, P[0]), (24, 24, 5, 4, P[1]), (25, 31, 5.5, 4, P[2])]
    for cx, cy, rx, ry, c in blobs: ell(cv, cx, cy, rx, ry, c, 1.5, 0.5)
    ell(cv, 22, 23, 2.5, 1.8, P[3], 1.9, 0.2)
    for k, (x0, y0) in enumerate([(19, 36), (24, 37), (29, 36)]):   # sulur menjuntai
        for i in range(6):
            x = x0 + int(round(np.sin(i * 0.9 + k) * 1.2)); cv.px(x, y0 + i, P[0] if i < 4 else (58, 42, 92, 150), 1.2 - i * 0.1)
    for (x, y) in [(13, 22), (35, 20), (33, 33)]:
        cv.px(x, y, (122, 92, 184, 160), 0.4)
    finish(L, "bayang")


def preview():
    names = ["batu", "pendar", "embun", "bara", "belerang", "beku", "bayang"]
    S = 4
    img = Image.new("RGBA", (len(names) * (C * S + 12) + 12, C * S * 2 + 36), (14, 16, 24, 255))
    for i, n in enumerate(names):
        sh = Image.open(os.path.join(OUT, "essence_" + n + ".png")).convert("RGBA")
        alb = sh.crop((0, 0, C, C)).resize((C * S, C * S), Image.NEAREST)
        glow = sh.crop((C, 0, C * 2, C)).resize((C * S, C * S), Image.NEAREST)
        x = 12 + i * (C * S + 12)
        img.alpha_composite(alb, (x, 12))
        dark = Image.new("RGBA", alb.size, (6, 7, 12, 255)); dark.alpha_composite(Image.blend(Image.new("RGBA", alb.size, (0, 0, 0, 0)), alb, 0.25)); dark.alpha_composite(glow)
        img.alpha_composite(dark, (x, 24 + C * S))
    img.save(os.path.join(A.ROOT, "art", "sprites", "_preview_essences.png"))


if __name__ == "__main__":
    batu(); pendar(); embun(); bara(); belerang(); beku(); bayang()
    preview()
    print("ok")
