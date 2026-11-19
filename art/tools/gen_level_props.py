# Props level tambahan (AI, 16 Nov 2026) - SEMENTARA, bukan desain final tim desainer.
# Melengkapi art fondasi gen_area_art.py (palet + helper sama). Tidak menulis ulang sprite lama.
#
# Jalankan:  python art/tools/gen_level_props.py      (butuh pillow + numpy)
# Output:    Assets/_Game/Art/Sprites/Props/<Area>/*.png (+ _normal), pivot bawah-tengah
#            art/sprites/_preview_props_level.png (pratinjau 3x, tidak masuk game)
#
# Isi tiap area (dipakai Editor/Level/AreaArt.cs + LevelBlockout.cs):
#   prop_<area>_block.png   perabot PENGHALANG (peta 't'), 5 frame:
#                           0 sendiri di tengah ruang, 1 sendiri menempel tembok atas, 2-4 deretan kiri/tengah/kanan
#   prop_<pilar>_c96.png    pilar / patung / tabung (peta 'I'), sel 96 px
#   prop_<area>_clutter.png serakan lantai tanpa collider (otomatis, jarang, di pinggir ruangan), 4 frame
#   prop_chains / prop_pipes  dekor menempel tembok (ditambahkan ke wallProps)
# Aturan: serakan dan dekor TIDAK boleh mirip tempat sembunyi (tong, peti, panci, semak, lemari, tirai, tudung saji)
# dan tidak mirip musuh (zirah, lukisan), supaya pemain tidak tertipu.
import os
import numpy as np
from PIL import Image

import gen_area_art as A
from gen_area_art import C, Canvas, hexc, sheet, save, save_big, OUT, WOOD, WOOD_D, WOOD_L, WOOD_OUT, IRON, IRON_L

PROPS = os.path.join(A.SPR, "Props")
STONE = [hexc("6a7080"), hexc("5d6372"), hexc("767c8c")]
STONE_D = hexc("444958")
STONE_L = hexc("8a90a0")
LAB_GLOW = hexc("5fd38a"); LAB_GLOW_D = hexc("2f8a5a"); LAB_LIQ = (70, 190, 120, 200)
GLASS_EDGE = (170, 195, 215, 255); GLASS = (120, 160, 190, 70); GLASS_HI = (225, 240, 250, 210)
MARBLE = [hexc("b9b4ab"), hexc("a8a398"), hexc("c8c3ba")]
MARBLE_D = hexc("7c776e")
PAPER = hexc("d8d0b8")


def legs(cv, ox, xs, y0, y1, c):
    for x in xs: cv.rect(ox + x, y0, 3, y1 - y0, c, 1.0)


def slab(cv, ox, x0, x1, y0, top_h, face_h, top, face, hi=None, ht=1.4):
    """Permukaan meja/batu tampak 3/4: atas (lebih terang) + muka depan."""
    cv.rect(ox + x0, y0, x1 - x0, top_h, top, ht)
    if hi is not None: cv.rect(ox + x0, y0, x1 - x0, 1, hi, ht + 0.1)
    cv.rect(ox + x0, y0 + top_h, x1 - x0, face_h, face, ht - 0.3)


# ================================================================ PENJARA
def dungeon():
    out = os.path.join(PROPS, "Dungeon")

    cv = sheet(5, 201)
    # 0 meja kayu sendiri + lilin padam + mangkuk
    slab(cv, 0, 6, 42, 16, 15, 4, WOOD[1], WOOD_D, WOOD_L)
    for y in range(18, 31, 4): cv.rect(7, y, 34, 1, WOOD[2], 1.35)
    legs(cv, 0, (8, 37), 35, 46, WOOD_D)
    cv.ellipse(30, 22, 5, 3, hexc("8a8478"), 1.6); cv.ellipse(30, 21, 3, 1.5, hexc("3a352e"), 1.5)
    cv.rect(14, 16, 3, 6, A.CLOTH[0], 1.7); cv.px(15, 15, hexc("33211a"), 1.7); cv.rect(12, 21, 7, 2, IRON_L, 1.6)
    # 1 kandang besi kosong menempel tembok
    ox = C
    cv.rect(ox + 7, 8, 34, 3, IRON_L, 1.6); cv.rect(ox + 7, 42, 34, 4, IRON, 1.3)
    for x in range(8, 41, 5): cv.rect(ox + x, 11, 2, 31, IRON, 1.5); cv.px(ox + x, 11, IRON_L, 1.6)
    cv.rect(ox + 7, 26, 34, 2, IRON, 1.5)
    cv.rect(ox + 22, 4, 4, 4, IRON_L, 1.7)
    # 2-4 meja panjang + bangku (deretan)
    for f, (x0, x1) in zip((2, 3, 4), ((5, C), (0, C), (0, 43))):
        ox = f * C
        slab(cv, ox, x0, x1, 8, 22, 5, WOOD[0], WOOD_D, WOOD_L)
        for y in range(11, 30, 5): cv.rect(ox + x0, y, x1 - x0, 1, WOOD[2], 1.35)
        cv.rect(ox + x0, 40, x1 - x0, 3, WOOD[2], 1.0)   # bangku depan
        if f != 3: legs(cv, ox, (x0 + 2,) if f == 2 else (x1 - 5,), 35, 46, WOOD_D)
        else: legs(cv, ox, (22,), 43, 46, WOOD_D)
    cv.ellipse(3 * C + 14, 18, 5, 3, hexc("8a8478"), 1.6); cv.ellipse(3 * C + 14, 17, 3, 1.5, hexc("3a352e"), 1.5)
    cv.rect(3 * C + 30, 13, 4, 7, hexc("5a6070"), 1.7); cv.rect(3 * C + 30, 13, 4, 1, IRON_L, 1.8)
    cv.rect(4 * C + 18, 16, 8, 6, hexc("c9c2b0"), 1.6); cv.ellipse(2 * C + 26, 20, 4, 2.5, hexc("8a8478"), 1.6)
    cv.outline(WOOD_OUT)
    save(cv, out, "prop_dng_block")

    # pilar batu + rantai (96)
    cv = Canvas(96, 96, 202)
    cv.rect(30, 84, 36, 10, A.ST_FACE[0], 1.2); cv.rect(30, 84, 36, 2, A.ST_FACE_HI, 1.3)
    for y in range(22, 84, 8):
        off = 0 if (y // 8) % 2 == 0 else 6
        cv.rect(34, y, 28, 8, A.ST_MORTAR, 1.0)
        for bx in range(34 - off, 62, 12):
            x0, x1 = max(34, bx) + 1, min(62, bx + 12) - 1
            if x1 > x0: cv.rect(x0, y + 1, x1 - x0, 6, A.ST_FACE[(y // 8 + bx) % 3], 1.3); cv.rect(x0, y + 1, x1 - x0, 1, A.ST_FACE_HI, 1.35)
    cv.rect(30, 12, 36, 10, A.ST_TRIM[0], 1.5); cv.rect(30, 12, 36, 2, A.ST_TRIM[1], 1.6)
    for x in range(40, 56, 3): cv.px(x, 30 + abs(x - 48) // 2, IRON_L, 1.6); cv.px(x + 1, 31 + abs(x - 48) // 2, IRON, 1.6)
    cv.ellipse(48, 40, 3, 3, IRON_L, 1.6); cv.ellipse(48, 40, 1.5, 1.5, A.ST_MORTAR, 1.5)
    m = np.zeros((96, 96), bool); m[22:84, 34:62] = True
    cv.speckle(m & (cv.hgt > 1.2), A.MOSS, 0.06)
    cv.outline(OUT)
    save_big(cv, out, "prop_pillar_c96")

    # serakan: 0 tulang + tengkorak kecil, 1 ember, 2 pecahan gerabah, 3 jerami tercecer
    cv = sheet(4, 203)
    BONE = hexc("cfc6b0"); BONE_D = hexc("8f8775")
    cv.ellipse(20, 30, 5, 4, BONE, 0.8); cv.px(18, 30, BONE_D, 0.7); cv.px(22, 30, BONE_D, 0.7); cv.rect(19, 33, 3, 1, BONE_D, 0.7)
    for i in range(10): cv.px(26 + i, 34 - i // 3, BONE, 0.6)
    cv.rect(25, 33, 2, 2, BONE, 0.6); cv.rect(35, 30, 2, 2, BONE, 0.6)
    ox = C
    cv.rect(ox + 17, 24, 14, 14, WOOD[0], 1.0); cv.ellipse(ox + 24, 24, 7, 2.5, WOOD_L, 1.2); cv.ellipse(ox + 24, 24, 5, 1.5, hexc("2a3a50"), 0.8)
    cv.rect(ox + 17, 28, 14, 1, IRON, 1.1); cv.rect(ox + 17, 35, 14, 1, IRON, 1.1)
    for i in range(14): cv.px(ox + 17 + i, 20 - int(3 * np.sin(i / 13 * np.pi)), IRON_L, 1.3)
    ox = 2 * C
    for (x, y, w, h) in [(12, 30, 7, 4), (22, 33, 6, 3), (30, 28, 5, 4), (17, 36, 4, 3)]:
        cv.rect(ox + x, y, w, h, hexc("8a5a3e"), 0.7); cv.rect(ox + x, y, w, 1, hexc("a8724e"), 0.8)
    ox = 3 * C
    for k in range(40):
        x, y = 8 + cv.rng.integers(32), 22 + cv.rng.integers(16)
        L = 2 + cv.rng.integers(4)
        for i in range(L): cv.px(ox + x + i, y + (i if k % 2 else 0) // 2, A.STRAW[k % 3], 0.4)
    cv.outline(OUT, region=np.ones_like(cv.alpha_mask()) & (np.arange(cv.w)[None, :] < 3 * C))
    save(cv, out, "prop_dng_clutter", strength=1.4)

    # rantai di tembok (dekor menempel tembok, tanpa collider)
    cv = Canvas(C, C, 204)
    for cx in (16, 32):
        cv.rect(cx - 3, 4, 7, 3, IRON_L, 1.6)
        for y in range(7, 30, 3):
            cv.rect(cx - 2 + (y // 3) % 2, y, 3, 3, IRON_L if (y // 3) % 2 else hexc("7a8292"), 1.5)
        cv.ellipse(cx, 33, 4.5, 4, hexc("7a8292"), 1.5); cv.ellipse(cx, 33, 2.2, 1.8, (0, 0, 0, 0), 0)
    save(cv, out, "prop_chains")


# ================================================================ LAB (penjara bagian lab: L06)
def lab():
    out = os.path.join(PROPS, "Dungeon")

    cv = sheet(5, 211)
    # 0 kuali gelembung hijau
    cv.ellipse(24, 34, 15, 10, IRON, 1.2, 0.5); cv.rect(9, 24, 30, 11, IRON, 1.3)
    cv.ellipse(24, 24, 15, 5, IRON_L, 1.5); cv.ellipse(24, 24, 12, 3.4, LAB_GLOW_D, 1.2)
    for (x, y, r) in [(20, 23, 2), (28, 24, 1.5), (24, 22, 1)]: cv.ellipse(x, y, r, r, LAB_GLOW, 1.3)
    for (x, y) in [(18, 16), (27, 12), (23, 8)]: cv.ellipse(x, y, 1.5, 1.5, (95, 211, 138, 170), 1.5)
    cv.rect(12, 44, 4, 2, IRON, 1.0); cv.rect(32, 44, 4, 2, IRON, 1.0)
    # 1 rak tabung spesimen menempel tembok
    ox = C
    cv.rect(ox + 5, 30, 38, 4, WOOD[0], 1.3); cv.rect(ox + 5, 30, 38, 1, WOOD_L, 1.4); legs(cv, ox, (6, 39), 34, 46, WOOD_D)
    for i, cx in enumerate((12, 24, 36)):
        h = 18 - (i % 2) * 4
        cv.rect(ox + cx - 4, 30 - h, 8, h, GLASS, 1.1)
        cv.rect(ox + cx - 4, 30 - h // 2, 8, h // 2, LAB_LIQ if i != 1 else (160, 110, 220, 200), 1.15)
        cv.rect(ox + cx - 4, 30 - h, 1, h, GLASS_EDGE, 1.3); cv.rect(ox + cx + 3, 30 - h, 1, h, GLASS_EDGE, 1.3)
        cv.rect(ox + cx - 5, 29 - h, 10, 2, IRON_L, 1.5)
    # 2-4 meja kerja panjang: labu, buku, lumpang
    for f, (x0, x1) in zip((2, 3, 4), ((5, C), (0, C), (0, 43))):
        ox = f * C
        slab(cv, ox, x0, x1, 10, 22, 5, WOOD[1], WOOD_D, WOOD_L)
        cv.rect(ox + x0, 31, x1 - x0, 1, IRON_L, 1.2)
        cv.rect(ox + x0, 37, x1 - x0, 2, IRON, 1.0)
        if f != 3: legs(cv, ox, (x0 + 2,) if f == 2 else (x1 - 5,), 37, 46, IRON)
        else: legs(cv, ox, (22,), 39, 46, IRON)
    ox = 2 * C   # labu bundar
    cv.ellipse(ox + 22, 20, 5, 5, GLASS, 1.6); cv.ellipse(ox + 22, 22, 4, 3, LAB_LIQ, 1.65); cv.rect(ox + 21, 12, 3, 5, GLASS_EDGE, 1.7)
    cv.rect(ox + 33, 16, 3, 7, GLASS_EDGE, 1.6); cv.rect(ox + 33, 19, 3, 4, (200, 90, 90, 220), 1.65)
    ox = 3 * C   # buku terbuka + kertas
    cv.rect(ox + 12, 19, 10, 8, PAPER, 1.6); cv.rect(ox + 22, 19, 10, 8, PAPER, 1.6); cv.rect(ox + 21, 19, 2, 8, hexc("6b4630"), 1.6)
    for y in (21, 23, 25): cv.rect(ox + 14, y, 6, 1, hexc("6e6a60"), 1.6); cv.rect(ox + 24, y, 6, 1, hexc("6e6a60"), 1.6)
    cv.rect(ox + 36, 21, 7, 5, PAPER, 1.5)
    ox = 4 * C   # lumpang + tabung
    cv.ellipse(ox + 16, 22, 5, 3, STONE[0], 1.7); cv.ellipse(ox + 16, 21, 3, 1.5, STONE_D, 1.6); cv.rect(ox + 18, 14, 2, 7, WOOD_L, 1.8)
    cv.rect(ox + 28, 13, 4, 10, GLASS_EDGE, 1.6); cv.rect(ox + 28, 17, 4, 6, LAB_LIQ, 1.65)
    cv.outline(OUT)
    save(cv, out, "prop_lab_block")

    # tabung spesimen besar (96): cairan hijau + bayangan slime di dalam
    cv = Canvas(96, 96, 212)
    cv.rect(28, 82, 40, 12, IRON, 1.3); cv.rect(28, 82, 40, 2, IRON_L, 1.4)
    cv.rect(30, 8, 36, 8, IRON, 1.5); cv.rect(30, 8, 36, 2, IRON_L, 1.6); cv.rect(44, 2, 8, 6, IRON_L, 1.6)
    cv.rect(32, 16, 32, 66, GLASS, 1.0)
    cv.rect(32, 34, 32, 48, LAB_LIQ, 1.05)
    cv.rect(32, 34, 32, 1, LAB_GLOW, 1.1)
    cv.ellipse(48, 66, 9, 7, (30, 90, 60, 230), 1.1)        # sosok slime samar (sedang disadap)
    cv.px(45, 64, (200, 240, 210, 255), 1.15); cv.px(51, 64, (200, 240, 210, 255), 1.15)
    for (x, y) in [(38, 44), (56, 52), (42, 72), (54, 40)]: cv.ellipse(x, y, 1.5, 1.5, (160, 240, 190, 200), 1.12)
    cv.rect(32, 16, 1, 66, GLASS_EDGE, 1.3); cv.rect(63, 16, 1, 66, GLASS_EDGE, 1.3)
    cv.rect(36, 20, 2, 40, GLASS_HI, 1.25)
    cv.outline(OUT, region=cv.col[:, :, 3] == 255)
    save_big(cv, out, "prop_tank_c96")

    # serakan lab: 0 kabel, 1 labu tumpah, 2 kertas berserakan, 3 pecahan kaca
    cv = sheet(4, 213)
    for x in range(6, 42):
        y = 30 + int(4 * np.sin(x * 0.22)); cv.px(x, y, hexc("2a2d36"), 0.5); cv.px(x, y + 1, hexc("1a1c22"), 0.4)
    for x in range(10, 40):
        y = 34 + int(3 * np.sin(x * 0.3 + 1)); cv.px(x, y, hexc("6e2a2a"), 0.5)
    ox = C
    cv.ellipse(ox + 26, 32, 10, 4, (70, 190, 120, 150), 0.2); cv.ellipse(ox + 24, 31, 5, 2, (120, 220, 160, 170), 0.2)
    cv.ellipse(ox + 16, 30, 4, 3, GLASS_EDGE, 0.8); cv.rect(ox + 10, 29, 4, 2, GLASS_EDGE, 0.8)
    ox = 2 * C
    for (x, y) in [(10, 22), (22, 28), (30, 20), (16, 34)]:
        cv.rect(ox + x, y, 9, 7, PAPER, 0.4); cv.rect(ox + x + 1, y + 2, 6, 1, hexc("6e6a60"), 0.4); cv.rect(ox + x + 1, y + 4, 5, 1, hexc("6e6a60"), 0.4)
    ox = 3 * C
    for k in range(14):
        x, y = 10 + cv.rng.integers(28), 22 + cv.rng.integers(16)
        cv.px(ox + x, y, GLASS_HI, 0.5); cv.px(ox + x + 1, y, GLASS_EDGE, 0.5)
    save(cv, out, "prop_lab_clutter", strength=1.2)

    # pipa di tembok
    cv = Canvas(C, C, 214)
    for x in (14, 30):
        cv.rect(x, 0, 5, 40, IRON, 1.5); cv.rect(x + 1, 0, 1, 40, IRON_L, 1.6)
        for y in (8, 26): cv.rect(x - 1, y, 7, 3, IRON_L, 1.7)
    cv.rect(14, 36, 21, 5, IRON, 1.5); cv.rect(14, 36, 21, 1, IRON_L, 1.6)
    cv.ellipse(24, 30, 3, 3, IRON_L, 1.7); cv.px(24, 30, LAB_GLOW, 1.8)
    cv.outline(OUT)
    save(cv, out, "prop_pipes")


# ================================================================ TAMAN
def garden():
    out = os.path.join(PROPS, "Garden")

    cv = sheet(5, 221)
    # 0 sumur batu
    cv.ellipse(24, 30, 15, 9, STONE[0], 1.2, 0.3); cv.rect(9, 30, 30, 10, STONE[1], 1.1)
    for x in range(10, 38, 7): cv.rect(x, 31, 1, 9, STONE_D, 1.0)
    cv.ellipse(24, 30, 11, 6, hexc("101820"), 0.4); cv.ellipse(24, 31, 7, 3, A.WATER[2], 0.3)
    cv.rect(8, 8, 3, 24, A.BARK[0], 1.6); cv.rect(37, 8, 3, 24, A.BARK[0], 1.6); cv.rect(7, 7, 34, 3, A.BARK[1], 1.7)
    cv.rect(23, 10, 2, 12, hexc("8a7a5a"), 1.5); cv.rect(21, 22, 6, 4, WOOD[0], 1.4)
    # 1 bangku batu menempel pagar
    ox = C
    slab(cv, ox, 6, 42, 24, 8, 4, STONE[2], STONE_D, STONE_L)
    cv.rect(ox + 9, 36, 6, 10, STONE[1], 1.0); cv.rect(ox + 33, 36, 6, 10, STONE[1], 1.0)
    cv.speckle(np.pad(np.ones((8, 36), bool), ((24, 16), (ox + 6, cv.w - ox - 42))), A.MOSS, 0.15)
    # 2-4 bak bunga batu panjang
    for f, (x0, x1) in zip((2, 3, 4), ((5, C), (0, C), (0, 43))):
        ox = f * C
        cv.rect(ox + x0, 26, x1 - x0, 18, STONE[1], 1.0); cv.rect(ox + x0, 26, x1 - x0, 2, STONE_L, 1.2)
        cv.rect(ox + x0, 42, x1 - x0, 3, STONE_D, 0.9)
        for bx in range(x0 + (6 if f == 2 else 0), x1, 12): cv.rect(ox + bx, 30, 1, 12, STONE_D, 0.95)
        cv.rect(ox + x0 + 2, 22, x1 - x0 - 4, 7, A.HEDGE[1], 1.3)
        for k in range(26):
            x = x0 + 3 + cv.rng.integers(max(1, x1 - x0 - 6)); y = 14 + cv.rng.integers(12)
            cv.rect(ox + x, y, 1, 28 - y, A.HEDGE_L[k % 2], 1.4)
            if k % 3 == 0: cv.px(ox + x, y - 1, A.FLOWER[k % 3], 1.6); cv.px(ox + x + 1, y, A.FLOWER[(k + 1) % 3], 1.6)
    cv.outline(OUT)
    save(cv, out, "prop_gdn_block")

    # patung slime raja di alas (96), berlumut
    cv = Canvas(96, 96, 222)
    cv.rect(26, 74, 44, 20, STONE[1], 1.2); cv.rect(26, 74, 44, 3, STONE_L, 1.35); cv.rect(30, 80, 36, 1, STONE_D, 1.2)
    cv.rect(31, 84, 34, 6, STONE[0], 1.25)
    for y in range(36, 74):
        for x in range(26, 70):
            d = ((x + 0.5 - 48) / 21) ** 2 + ((y + 0.5 - 62) / 26) ** 2
            if d <= 1.0 and y >= 36 + max(0, (abs(x - 48) - 6)) * 0.4:
                cv.px(x, y, STONE[0] if (x + y) % 9 else STONE[2], 1.3 + 0.8 * (1 - d) ** 0.5)
    for (x, y) in [(41, 54), (55, 54)]: cv.rect(x, y, 3, 5, STONE_D, 1.5)
    cv.rect(44, 63, 8, 2, STONE_D, 1.5)
    for (x0, x1) in [(36, 60)]:                     # mahkota
        cv.rect(x0, 30, x1 - x0, 6, A.CS_GOLD_D, 1.9)
        for x in range(x0, x1, 6): cv.rect(x + 1, 25, 3, 5, A.CS_GOLD_D, 1.95)
    m = cv.alpha_mask() & (cv.hgt > 1.25)
    cv.speckle(m, A.MOSS, 0.12)
    cv.outline(OUT)
    save_big(cv, out, "prop_statue_c96")

    # serakan: 0 tumpukan batu, 1 lingkar jamur, 2 nisan kecil, 3 daun gugur
    cv = sheet(4, 223)
    for (x, y, r) in [(18, 32, 5), (26, 34, 4), (22, 28, 3.5), (31, 31, 3)]:
        cv.ellipse(x, y, r, r * 0.75, STONE[int(x) % 3], 0.8, 0.4)
    ox = C
    for i in range(7):
        a = i / 7 * 2 * np.pi; x, y = 24 + 11 * np.cos(a), 30 + 6 * np.sin(a)
        cv.rect(ox + int(x), int(y), 1, 3, hexc("cfc8b8"), 0.6); cv.ellipse(ox + x + 0.5, y, 2, 1.3, A.MUSH, 0.8)
    ox = 2 * C
    cv.rect(ox + 17, 20, 14, 18, STONE[1], 1.2); cv.ellipse(ox + 24, 20, 7, 5, STONE[1], 1.25); cv.rect(ox + 17, 20, 1, 18, STONE_L, 1.3)
    for y in (24, 27, 30): cv.rect(ox + 20, y, 8, 1, STONE_D, 1.15)
    cv.rect(ox + 14, 38, 20, 3, A.DIRT[0], 0.7)
    ox = 3 * C
    for k in range(18):
        x, y = 8 + cv.rng.integers(32), 22 + cv.rng.integers(18)
        c = [hexc("6e5a3a"), hexc("8a6a3a"), hexc("5a4a32")][k % 3]
        cv.rect(ox + x, y, 2, 1, c, 0.4); cv.px(ox + x + 1, y + 1, c, 0.4)
    reg = np.zeros_like(cv.alpha_mask()); reg[:, C:3 * C] = True
    cv.outline(OUT, region=reg)
    save(cv, out, "prop_gdn_clutter", strength=1.4)


# ================================================================ KASTIL
def castle():
    out = os.path.join(PROPS, "Castle")

    cv = sheet(5, 231)
    # 0 meja bundar kecil bertaplak + lilin
    cv.ellipse(24, 26, 16, 9, A.CLOTH[0], 1.4, 0.2); cv.rect(8, 26, 32, 10, A.CLOTH[1], 1.1)
    for x in range(10, 39, 5): cv.rect(x, 33, 2, 3, A.CLOTH[0], 1.15)
    cv.rect(22, 36, 4, 9, WOOD_D, 1.0); cv.rect(17, 44, 14, 2, WOOD_D, 1.0)
    cv.rect(19, 23, 10, 2, A.CS_GOLD, 1.6); cv.rect(23, 14, 3, 9, A.CLOTH[0], 1.7)
    cv.px(24, 12, hexc("ffc861"), 1.8); cv.px(24, 13, hexc("f08a2e"), 1.8)
    # 1 rak buku menempel tembok
    ox = C
    cv.rect(ox + 5, 2, 38, 44, WOOD[0], 1.0); cv.rect(ox + 5, 2, 38, 3, WOOD_L, 1.3)
    BOOKS = [A.CS_WINE[1], hexc("2c4a6e"), A.CS_GOLD_D, hexc("3f5a3a"), hexc("5a3a6e"), A.CLOTH[1]]
    for sy in (6, 19, 32):
        cv.rect(ox + 8, sy, 32, 11, WOOD_D, 0.8)
        x = 9
        while x < 39:
            w = 2 + cv.rng.integers(3); hh = 8 + cv.rng.integers(3)
            cv.rect(ox + x, sy + 11 - hh, w, hh, BOOKS[cv.rng.integers(len(BOOKS))], 1.2)
            x += w + (cv.rng.random() < 0.15)
        cv.rect(ox + 7, sy + 11, 34, 2, WOOD_L, 1.3)
    # 2-4 meja makan panjang bertaplak putih, kursi di belakang, piring
    for f, (x0, x1) in zip((2, 3, 4), ((5, C), (0, C), (0, 43))):
        ox = f * C
        for cx in ((x0 + 14,) if f == 2 else (12, 36) if f == 3 else (x1 - 14,)):   # sandaran kursi
            cv.rect(ox + cx - 6, 1, 12, 10, A.CS_WINE[0], 1.2); cv.rect(ox + cx - 6, 1, 12, 2, A.CS_GOLD_D, 1.3)
            cv.rect(ox + cx - 6, 1, 2, 10, WOOD_D, 1.25); cv.rect(ox + cx + 4, 1, 2, 10, WOOD_D, 1.25)
        slab(cv, ox, x0, x1, 9, 22, 7, A.CLOTH[0], A.CLOTH[1], hexc("e2dccb"), 1.5)
        for x in range(x0 + 2, x1 - 1, 6): cv.rect(ox + x, 36, 2, 3, A.CLOTH[0], 1.25)
        cv.rect(ox + x0, 19, x1 - x0, 3, A.CS_WINE[1], 1.55)    # pelari meja merah
        if f != 3: legs(cv, ox, (x0 + 3,) if f == 2 else (x1 - 6,), 38, 46, WOOD_D)
        else: legs(cv, ox, (22,), 39, 46, WOOD_D)
    for (ox, cx, cy) in [(2 * C, 20, 14), (2 * C, 30, 26), (3 * C, 12, 14), (3 * C, 36, 14), (3 * C, 24, 27), (4 * C, 26, 14), (4 * C, 16, 26)]:
        cv.ellipse(ox + cx, cy, 4, 2, hexc("e8e4dc"), 1.65); cv.ellipse(ox + cx, cy, 2, 1, hexc("a8a398"), 1.65)
    cv.rect(3 * C + 22, 12, 4, 9, A.CS_GOLD, 1.7); cv.rect(3 * C + 18, 20, 12, 2, A.CS_GOLD_D, 1.7)
    cv.px(3 * C + 23, 10, hexc("ffc861"), 1.8); cv.px(3 * C + 23, 11, hexc("f08a2e"), 1.8)
    cv.outline(OUT)
    save(cv, out, "prop_cst_block")

    # tiang marmer + kepala emas (96)
    cv = Canvas(96, 96, 232)
    cv.rect(30, 84, 36, 10, MARBLE[1], 1.2); cv.rect(30, 84, 36, 2, MARBLE[2], 1.3); cv.rect(28, 90, 40, 4, MARBLE_D, 1.1)
    for x in range(35, 61):
        k = (x - 35) / 25
        shade = MARBLE[2] if 0.15 < k < 0.4 else MARBLE[0] if k < 0.8 else MARBLE[1]
        cv.rect(x, 20, 1, 64, shade, 1.2 + 0.6 * np.sin(k * np.pi))
    for x in (39, 45, 51, 57): cv.rect(x, 22, 1, 60, MARBLE_D, 1.25)
    cv.rect(30, 12, 36, 8, A.CS_GOLD, 1.6); cv.rect(30, 12, 36, 2, A.CS_GOLD_L, 1.7); cv.rect(33, 20, 30, 2, A.CS_GOLD_D, 1.5)
    cv.rect(31, 82, 34, 2, A.CS_GOLD_D, 1.4)
    cv.outline(OUT)
    save_big(cv, out, "prop_column_c96")


# ================================================================ PUNCAK
def summit():
    # Remake 19 Nov: menara alkemis medieval (dulu konsol/generator/pipa sci-fi).
    out = os.path.join(PROPS, "Summit")
    SG, SGD = A.SM_GLOW, A.SM_GLOW_D
    ST, STL = A.SM_STONE, A.SM_STONE_L
    BR, BRL, BRD = A.SM_BRASS, A.SM_BRASS_L, A.SM_BRASS_D
    LIQ = (150, 100, 230, 220)

    cv = sheet(5, 241)
    # 0 meja alkimia sendiri: alembik kaca (cairan ungu) di atas tungku kecil + lilin
    slab(cv, 0, 6, 42, 14, 16, 5, WOOD[1], WOOD_D, WOOD_L)
    legs(cv, 0, (8, 37), 35, 46, WOOD_D)
    cv.rect(17, 22, 10, 6, A.SM_METAL[1], 1.6); cv.px(21, 26, hexc("ff8a3a"), 1.7); cv.px(22, 26, hexc("ffc861"), 1.7)   # tungku
    cv.ellipse(22, 16, 6, 6, GLASS, 1.8); cv.ellipse(22, 18, 5, 3.5, LIQ, 1.85)
    cv.rect(21, 6, 3, 5, GLASS_EDGE, 1.9); cv.rect(23, 6, 9, 2, GLASS_EDGE, 1.9); cv.rect(31, 7, 2, 8, GLASS_EDGE, 1.85)  # leher + pipa
    cv.ellipse(19, 14, 1.2, 1.5, GLASS_HI, 2.0)
    cv.rect(35, 18, 3, 7, A.SM_WAX, 1.7); cv.px(36, 16, A.SM_FLAME, 1.8); cv.px(36, 17, hexc("f08a2e"), 1.8)
    # 1 rak buku tinggi menempel tembok
    ox = C
    cv.rect(ox + 5, 2, 38, 44, WOOD[2], 1.0); cv.rect(ox + 5, 2, 38, 3, WOOD_L, 1.3)
    BOOKS = [hexc("5a2430"), hexc("2c3a5a"), hexc("4a3a28"), hexc("3f2a52"), hexc("6b5a3a"), hexc("2f4a3a")]
    for sy in (6, 19, 32):
        cv.rect(ox + 8, sy, 32, 11, hexc("17141c"), 0.8)
        x = 9
        while x < 39:
            w = 2 + int(cv.rng.integers(3)); hh = 7 + int(cv.rng.integers(4))
            if cv.rng.random() < 0.12:   # botol ramuan di sela buku
                cv.rect(ox + x, sy + 11 - 6, 3, 6, LIQ, 1.25); cv.rect(ox + x + 1, sy + 11 - 8, 1, 2, hexc("8a8478"), 1.3)
            else:
                cv.rect(ox + x, sy + 11 - hh, w, hh, BOOKS[int(cv.rng.integers(len(BOOKS)))], 1.2)
            x += w + (1 if cv.rng.random() < 0.15 else 0)
        cv.rect(ox + 7, sy + 11, 34, 2, WOOD_L, 1.3)
    # 2-4 meja kerja alkemis panjang: labu, tengkorak berlilin, gulungan, lumpang, kristal
    for f, (x0, x1) in zip((2, 3, 4), ((5, C), (0, C), (0, 43))):
        ox = f * C
        slab(cv, ox, x0, x1, 10, 22, 5, WOOD[1], WOOD_D, WOOD_L)
        cv.rect(ox + x0, 37, x1 - x0, 2, WOOD_D, 1.0)
        if f != 3: legs(cv, ox, (x0 + 2,) if f == 2 else (x1 - 5,), 37, 46, WOOD_D)
        else: legs(cv, ox, (22,), 39, 46, WOOD_D)
    ox = 2 * C   # labu bundar cairan ungu + botol tinggi
    cv.ellipse(ox + 20, 20, 5, 5, GLASS, 1.6); cv.ellipse(ox + 20, 22, 4, 3, LIQ, 1.65); cv.rect(ox + 19, 12, 3, 5, GLASS_EDGE, 1.7)
    cv.rect(ox + 32, 13, 4, 11, GLASS_EDGE, 1.6); cv.rect(ox + 32, 17, 4, 7, (90, 200, 140, 220), 1.65)
    ox = 3 * C   # tengkorak berlilin + gulungan
    cv.ellipse(ox + 16, 21, 5, 4.5, hexc("cfc6b0"), 1.7); cv.px(ox + 14, 21, hexc("2a2230"), 1.75); cv.px(ox + 18, 21, hexc("2a2230"), 1.75)
    cv.rect(ox + 14, 24, 5, 2, hexc("b8af98"), 1.7); cv.rect(ox + 15, 11, 3, 7, A.SM_WAX, 1.8); cv.px(ox + 16, 9, A.SM_FLAME, 1.9); cv.px(ox + 16, 10, hexc("f08a2e"), 1.9)
    cv.rect(ox + 15, 17, 1, 3, A.SM_WAX, 1.75)
    cv.rect(ox + 26, 18, 14, 6, PAPER, 1.6); cv.rect(ox + 25, 17, 2, 8, hexc("b8a888"), 1.65); cv.rect(ox + 39, 17, 2, 8, hexc("b8a888"), 1.65)
    for y in (19, 21): cv.rect(ox + 28, y, 9, 1, hexc("6e6a60"), 1.62)
    ox = 4 * C   # lumpang + serpih kristal aura
    cv.ellipse(ox + 14, 22, 5, 3, STL, 1.7); cv.ellipse(ox + 14, 21, 3, 1.5, ST[2], 1.6); cv.rect(ox + 16, 14, 2, 7, WOOD_L, 1.8)
    for (cx, h) in [(27, 9), (31, 12), (34, 7)]:
        for i in range(h):
            w = max(1, int((h - i) / h * 2.5))
            cv.rect(ox + cx - w // 2, 26 - i, w, 1, SG if i % 3 else SGD, 1.7 + i * 0.03)
    cv.outline(OUT)
    save(cv, out, "prop_smt_block")

    # tiang batu menara (96) dengan sangkar besi berisi kristal aura ungu di puncaknya
    cv = Canvas(96, 96, 242)
    cv.rect(30, 84, 36, 10, ST[1], 1.2); cv.rect(30, 84, 36, 2, STL, 1.3)
    for y in range(32, 84, 8):
        off = 0 if (y // 8) % 2 == 0 else 6
        cv.rect(34, y, 28, 8, A.SM_MORTAR, 1.0)
        for bx in range(34 - off, 62, 12):
            x0, x1 = max(34, bx) + 1, min(62, bx + 12) - 1
            if x1 > x0:
                cv.rect(x0, y + 1, x1 - x0, 6, ST[(y // 8 + bx) % 4], 1.3); cv.rect(x0, y + 1, x1 - x0, 1, STL, 1.35)
    cv.rect(30, 26, 36, 6, ST[3], 1.5); cv.rect(30, 26, 36, 1, STL, 1.6)          # kepala tiang
    for i in range(16):                                                       # kristal
        w = max(1, int((16 - abs(i - 8) * 1.6) / 2))
        cv.rect(48 - w, 24 - i, 2 * w, 1, SG if i % 4 else SGD, 1.8 + 0.4 * (1 - abs(i - 8) / 8))
    cv.rect(46, 12, 2, 8, hexc("e2d4ff"), 2.1)
    for x in (39, 47, 56):                                                    # sangkar besi
        cv.rect(x, 6, 2, 20, A.SM_METAL[1], 1.9)
    cv.rect(38, 5, 21, 2, A.SM_METAL_L, 2.0); cv.rect(38, 25, 21, 2, A.SM_METAL_L, 1.9)
    cv.rect(47, 1, 2, 4, A.SM_METAL_L, 2.0)
    cv.outline(OUT)
    save_big(cv, out, "prop_auracolumn_c96")


def preview(items, scale=3):
    A.preview("props_level", items, scale)
    src = os.path.join(A.ROOT, "art", "sprites", "_preview_area_props_level.png")
    dst = os.path.join(A.ROOT, "art", "sprites", "_preview_props_level.png")
    os.replace(src, dst)


if __name__ == "__main__":
    dungeon(); lab(); garden(); castle(); summit()
    preview(A.PREVIEW)
    print("ok", len(A.PREVIEW))
