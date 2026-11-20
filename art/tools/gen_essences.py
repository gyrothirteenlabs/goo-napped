# Sprite esens (AI) - SEMENTARA, desain final tim desainer.
# v1 (19 Nov 2026): benda unik lepas (bongkah batu, jamur, daun, arang, kristal, es, asap). Hasil review user:
#   bentuknya sudah oke, tapi terbaca seperti dekor/clutter dan tidak jelas bisa dimakan.
# v2 (20 Nov 2026): SATU bahasa visual untuk semua esens = gelembung aura yang melayang (bentuk & ukuran sama),
#   ikon unik tetap ada DI DALAM gelembung supaya jenisnya terbaca dari bentuk, bukan warna saja.
#   - Warna gelembung = warna aura esens itu, SAMA dengan TraitNames.AuraColor (warna pendar toples teman dan titik aura
#     di HUD). Jadi pemain bisa mencocokkan "toples berpendar biru -> makan gelembung biru".
#   - Kilau bintang kecil + glow unlit tipis di tepi gelembung untuk SEMUA esens (tetap kelihatan di ruangan gelap,
#     tanda "bisa diambil"). Esens yang benar-benar menyala (Pendar, Bara) mendapat glow penuh di isi gelembung.
#   - Bayangan lantai sudah ada di prefab IngredientPickup (anak Shadow), gelembung melayang naik-turun (script).
# Tiap file: 2 frame 48 px -> frame 0 = albedo (outline gelap), frame 1 = lapisan glow (renderer unlit di prefab).
# Normal map dari height map (gelembung = kubah).
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
CX, CY, R = 24, 25, 10.5      # gelembung: pusat dan jari-jari (px). Bawah gelembung ~ baris 35, lantai = baris 47.


def rgb(r, g, b):
    return (int(r * 255 + 0.5), int(g * 255 + 0.5), int(b * 255 + 0.5))


# SAMA dengan TraitNames.colors (Assets/_Game/Scripts/Runtime/Ingredients/TraitNames.cs). Ubah keduanya bersamaan.
AURA = {
    "batu": rgb(0.6, 0.62, 0.68), "pendar": rgb(0.4, 0.85, 0.45), "embun": rgb(0.4, 0.6, 1.0),
    "bara": rgb(0.9, 0.25, 0.25), "belerang": rgb(0.95, 0.85, 0.3), "beku": rgb(0.95, 0.95, 0.95),
    "bayang": rgb(0.6, 0.4, 0.85),
}

# Ikon 11x11 di dalam gelembung. '.' = kosong. Huruf lain -> warna di legenda tiap esens.
ICONS = {
    "batu": (["...........",
              "...........",
              "....oooo...",
              "..oollllo..",
              ".ollllmmmo.",
              ".olmmmkmdo.",
              "olmmmkmmddo",
              "ommmmmmdddo",
              ".odddddddo.",
              "..ooooooo..",
              "..........."],
             {"o": "24262c", "d": "50545e", "m": "858b97", "l": "c8ced8", "k": "3a3d45"}, ""),
    "pendar": (["...........",
                "...ooooo...",
                "..ollslco..",
                ".olllllcco.",
                "olsllcccsco",
                ".ooootuooo.",
                "....otuo...",
                "....otuo...",
                "...ottuuo..",
                "...oooooo..",
                "..........."],
               {"o": "173a22", "c": "3fbf6a", "l": "5ad882", "s": "e8ffe6", "t": "d9dccb", "u": "aeb2a0"}, "cls"),
    "embun": ([".....o.....",
               "....odo....",
               "....omo....",
               "...omddo...",
               "..omlmddo..",
               "..olwmmdo..",
               ".omlwmmmdo.",
               ".omllmmmdo.",
               ".ommmmmddo.",
               "..ommmddo..",
               "...ooooo..."],
              {"o": "16304e", "d": "3f7fc4", "m": "5aa2e6", "l": "8cc8f5", "w": "eaf6ff"}, ""),
    "bara": (["......s....",
              "...s.......",
              ".....s.....",
              "...oooooo..",
              "..okmmkkko.",
              ".okmhkkhkco",
              "okhyhkkmhco",
              "okkhkkhyhco",
              ".ockkhhkcco",
              "..occcccoo.",
              "...ooooo..."],
             {"o": "1a1012", "c": "2a1f20", "k": "3a2c2c", "m": "4a3836", "h": "ff8a3a", "y": "ffd27a", "s": "ffb45a"}, "hys"),
    "belerang": (["..g...g....",
                  "...g...g...",
                  "..g...g....",
                  "....o......",
                  "...olo.o...",
                  "...olmolo..",
                  ".o.olmolmo.",
                  "olmolmdlmdo",
                  "olmdlmdlmdo",
                  "odddddddddo",
                  ".ooooooooo."],
                 {"o": "3a3010", "d": "b39a2e", "m": "e3c84a", "l": "f6e68c", "g": "a8b44c"}, ""),
    "beku": ([".....o.....",
              "....owo....",
              "....owmo...",
              "...owlmo...",
              "...owlmdo..",
              "..owllmdo.o",
              "..owlmmdowo",
              ".owlmmddowo",
              ".olmmdddolo",
              "..oddddodo.",
              "...ooooooo."],
             {"o": "2c4a66", "d": "7aa6c6", "m": "9cc4df", "l": "cfe6f5", "w": "ffffff"}, ""),
    "bayang": (None, {"o": "120c22", "m": "4a3874", "l": "8a6cc8", "h": "c8b4ff"}, ""),   # bulan sabit (gelap/bayang)
}


def crescent():
    # bulan sabit 11x11: lingkaran besar dikurangi lingkaran bergeser ke kanan atas, tepi dalam lebih terang
    rows = []
    for y in range(11):
        r = ""
        for x in range(11):
            o = (x - 5) ** 2 + (y - 5) ** 2 <= 5.2 ** 2
            i = (x - 8.4) ** 2 + (y - 3.0) ** 2 <= 4.2 ** 2
            r += "m" if o and not i else "."
        rows.append(r)
    g = [list(r) for r in rows]
    for y in range(11):
        for x in range(11):
            if g[y][x] != "m": continue
            nb = [(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))]
            if any(not (0 <= u < 11 and 0 <= v < 11) or rows[v][u] == "." for u, v in nb):
                inner = (x - 8.4) ** 2 + (y - 3.0) ** 2 <= 5.3 ** 2
                g[y][x] = "l" if inner else "o"
    for (hx, hy) in ((2, 4), (2, 5), (3, 3)):
        if g[hy][hx] == "m": g[hy][hx] = "h"
    return ["".join(r) for r in g]


ICONS["bayang"] = (crescent(),) + ICONS["bayang"][1:]
GLOWING = {"pendar", "bara"}     # SAMA dengan emitsLight di Data/Ingredients/Essence_*.asset


def mix(c, t, k):
    return tuple(int(c[i] + (t[i] - c[i]) * k) for i in range(3))


def essence(name, seed):
    aura = AURA[name]
    rows, legend, glow_keys = ICONS[name]
    cv = A.Canvas(C, C, seed)
    glow = np.zeros((C, C, 4), np.uint8)
    lite, dark = mix(aura, (255, 255, 255), 0.45), mix(aura, (10, 8, 20), 0.45)

    # gelembung: cincin outline 1 px, cincin aura 1 px (terang kiri atas, gelap kanan bawah), isi tembus pandang,
    # dan "cairan aura" yang mengendap di bagian bawah (lebih pekat, garis permukaan terang) supaya terbaca sebagai
    # esens cair, bukan koin/tombol.
    yy, xx = np.mgrid[0:C, 0:C]
    dx, dy = (xx + 0.5 - CX) / R, (yy + 0.5 - CY) / R
    d = np.sqrt(dx * dx + dy * dy)
    m0 = d <= 1.0
    pad = np.pad(m0, 1)
    m1 = m0 & pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:]
    pad = np.pad(m1, 1)
    m2 = m1 & pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:]
    surf = CY + 2.5
    for y in range(C):
        for x in range(C):
            if not m0[y, x]: continue
            ht = 1.0 + 1.2 * max(0.0, 1 - d[y, x] ** 2) ** 0.5
            if not m1[y, x]:
                cv.px(x, y, OUTLINE, ht)
            elif not m2[y, x]:
                k = dx[y, x] + dy[y, x]
                cv.px(x, y, (lite if k < -0.3 else dark if k > 0.6 else aura) + (255,), ht)
                glow[y, x] = aura + (150,)
            elif ((x + 0.5 - CX) / (R * 0.8)) ** 2 + ((y + 0.5 - surf) / 2.6) ** 2 <= 1.0:
                # permukaan cairan = elips (tampak 3/4 dari atas), lebih terang dari cairan di bawahnya
                cv.px(x, y, mix(aura, (255, 255, 255), 0.25) + (175,), ht)
                glow[y, x] = aura + (110,)
            elif y > surf:
                cv.px(x, y, aura + (150,), ht)
                if name in GLOWING: glow[y, x] = aura + (130,)
            else:
                cv.px(x, y, aura + (55,), ht)
                if name in GLOWING: glow[y, x] = aura + (60,)

    # ikon di tengah gelembung
    ox, oy = CX - 6, CY - 6
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch == ".": continue
            c = hexc(legend[ch])
            a = 150 if ch == "g" else 255
            cv.px(ox + i, oy + j, c[:3] + (a,), 2.0 if ch != "o" else 1.8)
            if ch in glow_keys: glow[oy + j, ox + i] = c[:3] + (255,)

    # kilau kaca kiri atas (lengkung + titik), di atas ikon
    for (x, y) in [(17, 20), (17, 21), (18, 19), (19, 18), (20, 18)]:
        cv.px(x, y, (240, 246, 255, 230), 2.2); glow[y, x] = (255, 255, 255, 170)
    cv.px(19, 21, (240, 246, 255, 200), 2.2)

    # bintang kilau kecil di luar gelembung kanan atas (tanda "bisa diambil")
    sx, sy = 35, 14
    for (dx, dy, a) in [(0, 0, 255), (1, 0, 170), (-1, 0, 170), (0, 1, 170), (0, -1, 170), (0, -2, 90), (0, 2, 90)]:
        cv.px(sx + dx, sy + dy, (255, 252, 230, a), 0.5)
        glow[sy + dy, sx + dx] = mix(aura, (255, 255, 255), 0.6) + (a,)

    sheet = A.Canvas(C * 2, C, 0)
    sheet.col[:, :C] = cv.col; sheet.hgt[:, :C] = cv.hgt
    sheet.col[:, C:] = glow
    A.save(sheet, OUT, "essence_" + name, strength=2.4)


NAMES = ["batu", "pendar", "embun", "bara", "belerang", "beku", "bayang"]


def preview():
    S = 4
    img = Image.new("RGBA", (len(NAMES) * (C * S + 12) + 12, C * S * 2 + 36), (14, 16, 24, 255))
    for i, n in enumerate(NAMES):
        sh = Image.open(os.path.join(OUT, "essence_" + n + ".png")).convert("RGBA")
        alb = sh.crop((0, 0, C, C)).resize((C * S, C * S), Image.NEAREST)
        glow = sh.crop((C, 0, C * 2, C)).resize((C * S, C * S), Image.NEAREST)
        x = 12 + i * (C * S + 12)
        lit = Image.new("RGBA", alb.size, (38, 40, 52, 255)); lit.alpha_composite(alb)
        img.alpha_composite(lit, (x, 12))
        dark = Image.new("RGBA", alb.size, (6, 7, 12, 255)); dark.alpha_composite(Image.blend(Image.new("RGBA", alb.size, (0, 0, 0, 0)), alb, 0.25)); dark.alpha_composite(glow)
        img.alpha_composite(dark, (x, 24 + C * S))
    img.save(os.path.join(A.ROOT, "art", "sprites", "_preview_essences.png"))


if __name__ == "__main__":
    for k, n in enumerate(NAMES):
        essence(n, 301 + k)
    preview()
    print("ok")
