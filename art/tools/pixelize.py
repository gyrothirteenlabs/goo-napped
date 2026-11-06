#!/usr/bin/env python3
"""Rapikan render Blender jadi sprite pixel: quantize, outline 1px, susun sheet.

Nama input: <char>_<layer>_<anim>_<dir>_<frame>.png
Contoh:
  python3 pixelize.py raw/slime sprites/slime --mode gray --levels 4 --sheet
  python3 pixelize.py raw/golem sprites/golem --mode palette --palette palette.hex --sheet
  python3 pixelize.py raw/x out/x --scale 4     # render 192px → 48px (majority downscale)
"""
import argparse
import re
from collections import Counter, defaultdict
from pathlib import Path

from PIL import Image

ALPHA_CUT = 128


def load_palette(path):
    cols = []
    for line in Path(path).read_text().split():
        h = line.strip().lstrip("#")
        if len(h) == 6:
            cols.append(tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)))
    if not cols:
        raise SystemExit(f"Palet kosong: {path}")
    return cols


def nearest(c, pal):
    return min(pal, key=lambda p: (p[0]-c[0])**2 + (p[1]-c[1])**2 + (p[2]-c[2])**2)


def quantize(img, mode, levels, pal):
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < ALPHA_CUT:
                px[x, y] = (0, 0, 0, 0)
                continue
            if mode == "gray":
                lum = 0.299 * r + 0.587 * g + 0.114 * b
                # tingkat 1..levels (0 dipakai outline)
                lv = 1 + min(levels - 1, int(lum / 256 * levels))
                v = round(lv / levels * 255)
                px[x, y] = (v, v, v, 255)
            elif mode == "palette":
                px[x, y] = (*nearest((r, g, b), pal), 255)
            else:
                px[x, y] = (r, g, b, 255)
    return img


def downscale(img, k):
    """Ambil warna mayoritas tiap blok k×k (lebih stabil daripada nearest)."""
    w, h = img.size
    out = Image.new("RGBA", (w // k, h // k), (0, 0, 0, 0))
    src, dst = img.load(), out.load()
    for by in range(h // k):
        for bx in range(w // k):
            cnt = Counter(src[bx*k + i, by*k + j] for j in range(k) for i in range(k))
            opaque = sum(v for c, v in cnt.items() if c[3])
            if opaque * 2 < k * k:
                continue
            dst[bx, by] = max((c for c in cnt if c[3]), key=cnt.__getitem__)
    return out


def outline(img, color):
    w, h = img.size
    src = img.copy().load()
    px = img.load()
    for y in range(h):
        for x in range(w):
            if src[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src[nx, ny][3]:
                    px[x, y] = (*color, 255)
                    break
    return img


NAME = re.compile(r"^(?P<key>.+)_(?P<frame>\d+)$")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src")
    ap.add_argument("dst")
    ap.add_argument("--mode", choices=["gray", "palette", "none"], default="gray")
    ap.add_argument("--levels", type=int, default=4)
    ap.add_argument("--palette")
    ap.add_argument("--scale", type=int, default=1, help="faktor downscale (render > 48px)")
    ap.add_argument("--outline", default="000000", help="hex, 'none' untuk mematikan")
    ap.add_argument("--sheet", action="store_true", help="gabung frame jadi sheet horizontal")
    a = ap.parse_args()

    pal = load_palette(a.palette) if a.mode == "palette" else None
    if a.mode == "palette" and not a.palette:
        raise SystemExit("--mode palette butuh --palette")
    oc = None if a.outline == "none" else tuple(int(a.outline[i:i+2], 16) for i in (0, 2, 4))

    dst = Path(a.dst)
    dst.mkdir(parents=True, exist_ok=True)
    groups = defaultdict(list)
    files = sorted(Path(a.src).glob("*.png"))
    if not files:
        raise SystemExit(f"Tidak ada PNG di {a.src}")

    for f in files:
        img = Image.open(f).convert("RGBA")
        img = quantize(img, a.mode, a.levels, pal)
        if a.scale > 1:
            img = downscale(img, a.scale)
        if oc:
            img = outline(img, oc)
        m = NAME.match(f.stem)
        if a.sheet and m:
            groups[m["key"]].append((int(m["frame"]), img))
        else:
            img.save(dst / f.name)

    for key, frames in groups.items():
        frames.sort(key=lambda t: t[0])
        w, h = frames[0][1].size
        sheet = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
        for i, (_, im) in enumerate(frames):
            sheet.paste(im, (i * w, 0))
        sheet.save(dst / f"{key}.png")
        print(f"{key}.png  {len(frames)} frame  {sheet.size[0]}x{sheet.size[1]}")


if __name__ == "__main__":
    main()
