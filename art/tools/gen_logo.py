# Logo + ikon SEMENTARA "Goo-napped" (AI, 13 Nov 2026). Nama dan logo final diputuskan tim setelah game jadi.
# Huruf pixel tebal warna slime (aqua -> teal), lendir menetes, outline gelap, mata slime di dalam huruf O kedua.
# Jalankan: python art/tools/gen_logo.py   (pillow + numpy)
# Output:  Assets/_Game/Art/UI/logo_goonapped.png  (ukuran asli, dipakai main menu; diperbesar di UI tanpa blur)
#          Assets/_Game/Art/UI/icon_goonapped.png  (256 px, ikon .exe)
#          art/sprites/_preview_logo.png            (pratinjau 4x)
import os
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "UI")


def hexc(h, a=255):
    h = h.lstrip("#"); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


GLYPHS = {
    "G": [".#####.", "##...##", "##.....", "##.####", "##...##", "##...##", "##...##", ".######"],
    "O": [".#####.", "##...##", "##...##", "##...##", "##...##", "##...##", "##...##", ".#####."],
    "N": ["##...##", "###..##", "####.##", "##.####", "##..###", "##...##", "##...##", "##...##"],
    "A": [".#####.", "##...##", "##...##", "#######", "##...##", "##...##", "##...##", "##...##"],
    "P": ["######.", "##...##", "##...##", "######.", "##.....", "##.....", "##.....", "##....."],
    "E": ["#######", "##.....", "##.....", "######.", "##.....", "##.....", "##.....", "#######"],
    "D": ["######.", "##...##", "##...##", "##...##", "##...##", "##...##", "##...##", "######."],
    "-": [".....", ".....", ".....", "#####", "#####", ".....", ".....", "....."],
}
RAMP = [hexc("dff8f0"), hexc("bff0e2"), hexc("7fe3d2"), hexc("3fd1c7"), hexc("2aa8a6"), hexc("1f8a8f"), hexc("1f6f7a")]
OUTLINE = hexc("0b0f1a")
SHADOW = hexc("123a44")
WHITE = hexc("ffffff"); PUPIL = hexc("0b0f1a")


def logo():
    text = "GOO-NAPPED"
    s = 3                      # skala piksel huruf
    gap = 4
    widths = [len(GLYPHS[ch][0]) * s for ch in text]
    W = sum(widths) + gap * (len(text) - 1) + 12
    H = 8 * s + 24
    mask = np.zeros((H, W), bool)
    x0, y0 = 6, 5
    boxes = []
    x = x0
    for ch, w in zip(text, widths):
        g = GLYPHS[ch]
        for gy, row in enumerate(g):
            for gx, c in enumerate(row):
                if c == "#": mask[y0 + gy * s:y0 + gy * s + s, x + gx * s:x + gx * s + s] = True
        boxes.append((ch, x, w))
        x += w + gap
    # lendir menetes dari bawah beberapa huruf (posisi tetap, panjang beda)
    base = y0 + 8 * s
    drips = [(0, "right", 6), (1, "left", 4), (4, "left", 9), (5, "right", 5), (8, "left", 7), (9, "right", 4)]
    for (i, side, ln) in drips:
        ch, bx, w = boxes[i]
        # tetes hanya di bawah piksel huruf yang ada (3 px berturut di baris paling bawah)
        xs = [x for x in range(bx, bx + w - 2) if mask[base - 1, x:x + 3].all()]
        if not xs: continue
        cx = xs[0] if side == "left" else xs[-1]
        mask[base - 1:base + ln, cx:cx + 3] = True          # badan tetes 3 px menempel ke huruf
        mask[base:base + 2, cx - 1:cx + 4] = True           # pangkal melebar
        mask[base + ln:base + ln + 3, cx - 1:cx + 4] = True # ujung tetes bulat 5x3
        mask[base + ln + 3, cx:cx + 3] = True
    img = np.zeros((H, W, 4), np.uint8)
    # bayangan 2 px ke bawah-kanan
    sh = np.zeros_like(mask); sh[2:, 2:] = mask[:-2, :-2]
    img[sh & ~mask] = SHADOW
    # isi gradasi vertikal (warna, bukan cahaya terarah: logo UI, tidak kena Light 2D)
    for yy in range(H):
        t = (yy - y0) / max(1, (base - y0))
        c = RAMP[int(np.clip(t, 0, 0.999) * len(RAMP))] if yy < base else RAMP[3]
        img[yy][mask[yy]] = c
    # kilap: 1 px di tepi atas tiap goresan
    top = mask & ~np.vstack([np.zeros((1, W), bool), mask[:-1]])
    img[top & (np.arange(H)[:, None] < base)] = RAMP[0]
    # mata slime di O kedua (lubang huruf)
    ch, bx, w = boxes[2]
    # lubang huruf O diisi badan slime (jadi wajah)
    img[y0 + s:y0 + 7 * s, bx + 2 * s:bx + 5 * s] = RAMP[3]
    img[y0 + s, bx + 2 * s:bx + 5 * s] = RAMP[1]
    ey = y0 + 2 * s + 1
    for ex in (bx + 6, bx + 11):
        img[ey:ey + 5, ex:ex + 3] = WHITE
        img[ey + 2:ey + 5, ex + 1:ex + 3] = PUPIL
    # outline gelap 1 px di sekeliling semua (huruf + bayangan)
    filled = img[:, :, 3] > 0
    pad = np.pad(filled, 1)
    near = pad[:-2, 1:-1] | pad[2:, 1:-1] | pad[1:-1, :-2] | pad[1:-1, 2:]
    img[near & ~filled] = OUTLINE
    os.makedirs(OUT, exist_ok=True)
    Image.fromarray(img, "RGBA").save(os.path.join(OUT, "logo_goonapped.png"))
    return img


def icon():
    n = 32
    img = np.zeros((n, n, 4), np.uint8)
    # slime kubah
    for y in range(n):
        for x in range(n):
            dx = (x + 0.5 - 16) / 13
            dy = (y + 0.5 - 19) / 11 if y < 19 else (y + 0.5 - 19) / 9
            if dx * dx + dy * dy <= 1.0 and y < 29:
                t = (y - 7) / 22
                img[y, x] = RAMP[int(np.clip(t, 0, 0.999) * (len(RAMP) - 1)) + 1]
    img[10:13, 9:12] = RAMP[0]; img[9, 10] = RAMP[0]                   # kilap
    for ex in (11, 18):                                                  # mata
        img[16:20, ex:ex + 3] = WHITE
        img[17:20, ex + 1:ex + 3] = PUPIL
    img[22, 14:18] = PUPIL; img[21, 13] = PUPIL; img[21, 18] = PUPIL     # senyum
    img[27:31, 22:24] = RAMP[4]; img[30:31, 21:25] = RAMP[4]           # tetes
    filled = img[:, :, 3] > 0
    pad = np.pad(filled, 1)
    near = pad[:-2, 1:-1] | pad[2:, 1:-1] | pad[1:-1, :-2] | pad[1:-1, 2:]
    img[near & ~filled] = OUTLINE
    big = Image.fromarray(img, "RGBA").resize((256, 256), Image.NEAREST)
    big.save(os.path.join(OUT, "icon_goonapped.png"))
    return img


if __name__ == "__main__":
    lg = logo()
    ic = icon()
    pv = Image.new("RGBA", (lg.shape[1] * 4 + 32 + 128 + 24, max(lg.shape[0] * 4, 128) + 32), (11, 15, 26, 255))
    pv.alpha_composite(Image.fromarray(lg, "RGBA").resize((lg.shape[1] * 4, lg.shape[0] * 4), Image.NEAREST), (16, 16))
    pv.alpha_composite(Image.fromarray(ic, "RGBA").resize((128, 128), Image.NEAREST), (lg.shape[1] * 4 + 40, 16))
    pv.save(os.path.join(ROOT, "art", "sprites", "_preview_logo.png"))
    print("ok", lg.shape)
