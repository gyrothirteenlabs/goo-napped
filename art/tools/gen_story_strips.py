# Strip cerita SEMENTARA (AI, 15 Nov 2026) untuk Intro dan Ending. Diganti tim (gambar + teks).
# Panel dirakit dari sprite game yang sudah ada (slime, toples, tile, toples raja, mesin penyadap) supaya gayanya nyambung.
# Ukuran sama dengan placeholder lama: lebar 256x120 (layout ImageTop / ImageOnly), tinggi 120x168 (ImageLeft).
# Output: Assets/_Game/Art/Story/strip_*.png + pratinjau art/sprites/_preview_story.png
# Jalankan: python art/tools/gen_story_strips.py
import os, math, random
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SPR = os.path.join(ROOT, "Assets", "_Game", "Art", "Sprites")
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "Story")
WIDE, TALL = (256, 120), (120, 168)

AURA = {  # warna aura teman (selaras palet Slime_* di Unity, kira-kira)
    "pip": (90, 150, 230), "lumi": (150, 220, 90), "tok": (170, 110, 220), "bara": (235, 120, 60),
    "embun": (110, 200, 220), "batu": (150, 140, 130), "king": (240, 200, 80), "clear": (205, 235, 240),
}


def load(rel):
    return Image.open(os.path.join(SPR, rel)).convert("RGBA")


def frame(img, i=0, cell=48):
    return img.crop((i * cell, 0, (i + 1) * cell, cell))


def tint(body, rgb, alpha=1.0):
    """Body slime = abu (putih + outline gelap). Petakan ke warna aura: terang = warna, gelap = warna gelap."""
    a = np.array(body).astype(np.float32)
    v = a[:, :, 0:1] / 255.0
    col = np.array(rgb, np.float32)[None, None, :]
    dark = col * 0.25
    rgbv = dark + (col - dark) * np.clip((v - 0.15) / 0.85, 0, 1)
    out = np.concatenate([rgbv, a[:, :, 3:4] * alpha], axis=2)
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).copy()


def slime(color, face=True, anim="idle", d="front", i=0, alpha=1.0, flip=False):
    b = tint(frame(load(f"Characters/Slime/slime_body_{anim}_{d}.png"), i), AURA.get(color, color), alpha)
    if face:
        b.alpha_composite(frame(load(f"Characters/Slime/slime_face_{anim}_{d}.png"), i))
    return b.transpose(Image.FLIP_LEFT_RIGHT) if flip else b


def put(cv, img, x, y):
    """Tempel dengan titik (x, y) = tengah bawah gambar."""
    cv.alpha_composite(img, (int(x - img.width // 2), int(y - img.height)))


def tile_fill(cv, rel, rows=None, cell=48, frames=1, y0=0, y1=None, seed=1):
    t = load(rel)
    rnd = random.Random(seed)
    y1 = cv.height if y1 is None else y1
    for y in range(y0, y1, cell):
        for x in range(0, cv.width, cell):
            cv.alpha_composite(frame(t, rnd.randrange(frames), cell), (x, y))


def gradient(size, top, bottom):
    w, h = size
    t = np.linspace(0, 1, h)[:, None, None]
    arr = np.array(top)[None, None, :] * (1 - t) + np.array(bottom)[None, None, :] * t
    arr = np.repeat(arr, w, axis=1)
    return Image.fromarray(np.concatenate([arr, np.full((h, w, 1), 255)], axis=2).astype(np.uint8)).copy()


def stars(cv, n, seed=3, ymax=None):
    rnd = random.Random(seed)
    px = cv.load()
    for _ in range(n):
        x, y = rnd.randrange(cv.width), rnd.randrange(ymax or cv.height // 2)
        c = rnd.choice([(200, 210, 255, 255), (150, 160, 210, 255)])
        px[x, y] = c


def rect(cv, x0, y0, x1, y1, rgb, a=255):
    layer = Image.new("RGBA", cv.size, (0, 0, 0, 0))
    arr = np.array(layer)
    arr[max(0, y0):max(0, y1), max(0, x0):max(0, x1)] = (*rgb, a)
    cv.alpha_composite(Image.fromarray(arr))


def castle(cv, base_y, x0, color=(22, 20, 40), windows=True, seed=5):
    """Siluet kastil sederhana: tembok, menara, menara utama, jendela menyala."""
    w = cv.width
    rect(cv, x0, base_y - 34, x0 + 150, base_y, color)
    for tx, th, tw in ((x0 - 6, 58, 22), (x0 + 60, 82, 30), (x0 + 136, 58, 22)):
        rect(cv, tx, base_y - th, tx + tw, base_y, color)
        for k in range(0, tw, 6):  # gigi benteng
            rect(cv, tx + k, base_y - th - 4, tx + k + 3, base_y - th, color)
        # atap runcing
        for r in range(10):
            rect(cv, tx + r, base_y - th - 4 - (10 - r) * 0 - r, tx + tw - r, base_y - th - 4 - r + 1, color)
    for k in range(0, 150, 8):
        rect(cv, x0 + k, base_y - 38, x0 + k + 4, base_y - 34, color)
    if windows:
        # jendela menyala di posisi tetap: menara samping, menara utama (ungu = mesin penyadap di puncak), tembok
        spots = [(x0 + 3, 40), (x0 + 3, 26), (x0 + 143, 40), (x0 + 143, 26), (x0 + 73, 70), (x0 + 73, 56), (x0 + 73, 42),
                 (x0 + 30, 20), (x0 + 46, 20), (x0 + 104, 20), (x0 + 120, 20)]
        for i, (wx, h) in enumerate(spots):
            c = (210, 120, 240) if 4 <= i <= 6 else (240, 180, 90)
            rect(cv, wx, base_y - h, wx + 3, base_y - h + 5, c)


def glow(cv, x, y, r, rgb, strength=0.6):
    w, h = cv.size
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.sqrt((xx - x) ** 2 + (yy - y) ** 2) / r
    a = np.clip(1 - d, 0, 1) ** 2 * strength
    arr = np.array(cv).astype(np.float32)
    col = np.array(rgb, np.float32)
    arr[:, :, :3] = arr[:, :, :3] * (1 - a[:, :, None] * 0.6) + col * a[:, :, None] * 0.6 + arr[:, :, :3] * a[:, :, None] * 0.4
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)).copy()


def shade(cv, amount):
    """Gelapkan seluruh panel (mood gelap)."""
    arr = np.array(cv).astype(np.float32)
    arr[:, :, :3] *= amount
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)).copy()


def vignette(cv, k=0.55):
    w, h = cv.size
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    m = 1 - np.clip(d - 0.55, 0, 1) * k
    arr = np.array(cv).astype(np.float32)
    arr[:, :, :3] *= m[:, :, None]
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)).copy()


def frame_border(cv):
    """Bingkai panel komik: garis gelap 2 px + garis terang 1 px."""
    arr = np.array(cv)
    h, w = arr.shape[:2]
    for t, c in ((0, (8, 8, 14, 255)), (1, (8, 8, 14, 255)), (2, (70, 64, 90, 255))):
        arr[t, t:w - t] = c; arr[h - 1 - t, t:w - t] = c
        arr[t:h - t, t] = c; arr[t:h - t, w - 1 - t] = c
    return Image.fromarray(arr).copy()


def jar_with(color, alpha=1.0, glow_on=True):
    j = frame(load("Props/Dungeon/prop_jar.png"))
    s = slime(color, anim="idle", i=0).resize((30, 30), Image.NEAREST)
    base = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    base.alpha_composite(s, (9, 14))
    base.alpha_composite(j)
    return base


def alchemist(h=64):
    """Siluet Alkemis SEMENTARA (bentuk final = desainer): jubah bertudung, mata menyala ungu."""
    w = h // 2 + 6
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    arr = np.array(im)
    for y in range(h):
        t = y / h
        half = int((0.18 + 0.32 * t) * w) if y > h * 0.22 else int(0.22 * w * math.sin(math.pi * y / (h * 0.44)) + 2)
        c = (14, 10, 24, 255)
        arr[y, max(0, w // 2 - half):min(w, w // 2 + half)] = c
    im = Image.fromarray(arr).copy()
    px = im.load()
    for ex in (w // 2 - 4, w // 2 + 3):
        for dy in range(2):
            px[ex, int(h * 0.17) + dy] = (210, 120, 255, 255)
            px[ex + 1, int(h * 0.17) + dy] = (210, 120, 255, 255)
    return im


def smoke(cv, x, y, rgb=(140, 80, 200), seed=7):
    rnd = random.Random(seed)
    for k in range(60):
        t = k / 60
        px = x + int(math.sin(t * 9) * 10 * t) + rnd.randint(-2, 2)
        py = y - int(t * 70)
        r = 2 + int(t * 6)
        rect(cv, px - r, py - r, px + r, py + r, rgb, int(110 * (1 - t)))


# ------------------------------------------------------------------ panel
def p_kingdom():
    cv = gradient(WIDE, (40, 50, 90), (80, 70, 110))
    stars(cv, 25)
    castle(cv, 86, 70, color=(40, 36, 70), windows=False)
    tile_fill(cv, "Tiles/Garden/gdn_grass.png", frames=4, y0=84)
    for x, c, f in ((44, "pip", False), (92, "lumi", True), (150, "tok", False), (204, "bara", True)):
        put(cv, slime(c, flip=f), x, 112)
    cv = glow(cv, 128, 100, 120, (255, 220, 150), 0.35)
    return frame_border(vignette(cv, 0.4))


def p_spirit():
    cv = gradient(TALL, (12, 10, 22), (30, 22, 40))
    tile_fill(cv, "Tiles/Dungeon/dng_floor.png", frames=3, y0=120)
    j = frame(load("Props/Dungeon/prop_jar.png")).rotate(70, expand=False)
    put(cv, j, 74, 150)
    smoke(cv, 70, 128)
    put(cv, alchemist(70), 38, 150)
    cv = glow(cv, 70, 90, 60, (170, 90, 230), 0.5)
    return frame_border(vignette(cv))


def p_drain():
    cv = Image.new("RGBA", WIDE, (10, 10, 18, 255))
    tile_fill(cv, "Tiles/Dungeon/dng_wall.png", frames=1, y0=0, y1=72)
    tile_fill(cv, "Tiles/Dungeon/dng_floor.png", frames=3, y0=72)
    cv = shade(cv, 0.55)
    m = load("Props/Summit/prop_drainmachine_c96.png").crop((0, 0, 96, 96))
    put(cv, m, 128, 104)
    for x, c in ((34, "pip"), (72, "lumi"), (184, "tok"), (222, "embun")):
        cv = glow(cv, x, 86, 26, AURA[c], 0.7)
        put(cv, jar_with(c), x, 108)
        for yy in range(30, 70, 3):  # pipa aura ke mesin
            rect(cv, x - 1, yy, x + 1, yy + 2, AURA[c], 160)
    return frame_border(vignette(cv))


def p_thrown():
    cv = gradient(TALL, (16, 18, 34), (34, 30, 44))
    stars(cv, 14, seed=9, ymax=50)
    tile_fill(cv, "Tiles/Dungeon/dng_wall.png", frames=1, y0=48, y1=96)
    cv = shade(cv, 0.7)
    tile_fill(cv, "Tiles/Garden/gdn_dirt.png", frames=2, y0=96)
    put(cv, slime("clear", anim="jelly", i=3, alpha=0.8), 60, 150)
    rect(cv, 54, 154, 66, 156, (10, 8, 16), 120)
    return frame_border(vignette(cv))


def p_resolve():
    cv = gradient(WIDE, (14, 16, 34), (30, 30, 54))
    stars(cv, 30, seed=11)
    castle(cv, 82, 52, color=(34, 30, 58))
    tile_fill(cv, "Tiles/Garden/gdn_grass.png", frames=4, y0=80)
    cv = shade(cv, 0.8)
    hedge = load("Props/Garden/prop_bush.png")
    for x in (24, 60, 200, 236):
        put(cv, frame(hedge), x, 118)
    put(cv, slime("clear", d="back", alpha=0.85), 128, 116)
    return frame_border(vignette(cv))


def p_sealed():
    cv = Image.new("RGBA", WIDE, (14, 10, 24, 255))
    tile_fill(cv, "Tiles/Summit/smt_wall.png", frames=1, y0=0, y1=64)
    tile_fill(cv, "Tiles/Summit/smt_floor.png", frames=3, y0=64)
    cv = shade(cv, 0.6)
    kj = load("Props/Summit/prop_kingjar_c96.png").crop((0, 0, 96, 96))
    inside = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    inside.alpha_composite(alchemist(54), (34, 30))
    inside.alpha_composite(kj)
    cv = glow(cv, 128, 70, 70, (240, 200, 80), 0.6)
    put(cv, inside, 128, 112)
    put(cv, slime("king"), 60, 112)
    # mahkota: emas terang + garis gelap supaya tidak tenggelam di slime raja yang kuning
    rows = ["k.k..k..k.k", "kgkk.k.kkgk", "kgggkgkgggk", "kgrgggggrgk", "kkkkkkkkkkk"]
    pal = {"k": (40, 20, 10, 255), "g": (255, 230, 120, 255), "r": (220, 50, 70, 255)}
    crown = Image.new("RGBA", (11, 5), (0, 0, 0, 0))
    cp = crown.load()
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch in pal: cp[x, y] = pal[ch]
    put(cv, crown, 60, 80)
    put(cv, slime("clear", flip=True, alpha=0.85), 200, 112)
    return frame_border(vignette(cv))


def p_return():
    cv = gradient(TALL, (20, 14, 36), (40, 28, 60))
    m = load("Props/Summit/prop_drainmachine_c96.png").crop((0, 0, 96, 96))
    put(cv, m, 60, 160)
    rnd = random.Random(4)
    cols = [AURA[k] for k in ("pip", "lumi", "tok", "bara", "embun", "king")]
    for k in range(90):  # aura kembali naik ke semua warga
        x = 60 + rnd.randint(-50, 50)
        y = rnd.randint(4, 110)
        c = rnd.choice(cols)
        rect(cv, x, y, x + 2, y + 2, c, rnd.randint(140, 255))
    cv = glow(cv, 60, 60, 70, (220, 200, 255), 0.5)
    return frame_border(vignette(cv, 0.35))


def p_reunion():
    cv = gradient(WIDE, (110, 80, 120), (240, 170, 120))
    castle(cv, 82, 70, color=(70, 50, 80), windows=False)
    tile_fill(cv, "Tiles/Garden/gdn_grass.png", frames=4, y0=80)
    cv = glow(cv, 128, 80, 160, (255, 220, 160), 0.45)
    xs = [(26, "pip"), (58, "lumi"), (90, "king"), (128, "clear"), (164, "tok"), (198, "bara"), (232, "embun")]
    for x, c in xs:
        put(cv, slime(c, flip=x > 128, alpha=0.9 if c == "clear" else 1.0), x, 114)
    return frame_border(vignette(cv, 0.3))


def p_rebuild():
    cv = gradient(WIDE, (120, 150, 200), (200, 210, 230))
    tile_fill(cv, "Tiles/Castle/cst_floor.png", frames=2, y0=72)
    crate = frame(load("Props/Dungeon/prop_crate.png"))
    barrel = frame(load("Props/Dungeon/prop_barrel.png"))
    put(cv, crate, 44, 104); put(cv, barrel, 210, 104)
    for x, c in ((90, "lumi"), (170, "pip")):  # toples jadi lentera
        cv = glow(cv, x, 84, 30, AURA[c], 0.6)
        put(cv, jar_with(c), x, 104)
    put(cv, slime("clear"), 130, 114)
    put(cv, slime("tok", anim="move", d="side", i=2), 66, 116)
    return frame_border(vignette(cv, 0.25))


PANELS = {
    "strip_intro_1_kingdom": p_kingdom, "strip_intro_2_spirit": p_spirit, "strip_intro_3_drain": p_drain,
    "strip_intro_4_thrown": p_thrown, "strip_intro_5_resolve": p_resolve,
    "strip_ending_1_sealed": p_sealed, "strip_ending_2_return": p_return, "strip_ending_3_reunion": p_reunion,
    "strip_ending_4_rebuild": p_rebuild,
}


def main():
    os.makedirs(OUT, exist_ok=True)
    imgs = []
    for name, fn in PANELS.items():
        im = fn().convert("RGBA")
        im.save(os.path.join(OUT, name + ".png"))
        imgs.append((name, im))
        print(name, im.size)
    # pratinjau 3x
    sc = 3
    W = 2 * (256 * sc + 12) + 12
    rows = (len(imgs) + 1) // 2
    pv = Image.new("RGBA", (W, rows * (168 * sc + 12) + 12), (24, 24, 32, 255))
    for k, (_, im) in enumerate(imgs):
        big = im.resize((im.width * sc, im.height * sc), Image.NEAREST)
        pv.alpha_composite(big, (12 + (k % 2) * (256 * sc + 12), 12 + (k // 2) * (168 * sc + 12)))
    pv.save(os.path.join(ROOT, "art", "sprites", "_preview_story.png"))


if __name__ == "__main__":
    main()
