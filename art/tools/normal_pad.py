"""Rapikan sheet normal map setelah pixelize.

- Pixel transparan yang bersebelahan dengan siluet diisi normal tetangganya (dilate N px),
  supaya outline 1px yang ditambah pixelize di layer body tetap punya normal (jadi ikut rim light).
- Sisa pixel transparan diisi normal datar (128,128,255) dengan alpha 0.

Pakai (dari root repo):
  python art/tools/normal_pad.py art/sprites/slime --pattern "*_normal_*.png" --px 1
"""
import argparse
from pathlib import Path
from PIL import Image

FLAT = (128, 128, 255)


def pad(img, n):
    img = img.convert("RGBA")
    w, h = img.size
    px = img.load()
    for _ in range(n):
        src = img.copy()
        sp = src.load()
        for y in range(h):
            for x in range(w):
                if sp[x, y][3] > 0:
                    continue
                acc = [0, 0, 0]
                cnt = 0
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    xx, yy = x + dx, y + dy
                    if 0 <= xx < w and 0 <= yy < h and sp[xx, yy][3] > 0:
                        r, g, b, _ = sp[xx, yy]
                        acc[0] += r; acc[1] += g; acc[2] += b; cnt += 1
                if cnt:
                    px[x, y] = (acc[0] // cnt, acc[1] // cnt, acc[2] // cnt, 255)
    for y in range(h):
        for x in range(w):
            if px[x, y][3] == 0:
                px[x, y] = (*FLAT, 0)
    return img


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("dir")
    ap.add_argument("--pattern", default="*_normal_*.png")
    ap.add_argument("--px", type=int, default=1)
    a = ap.parse_args()
    files = sorted(Path(a.dir).glob(a.pattern))
    for f in files:
        pad(Image.open(f), a.px).save(f)
    print(f"{len(files)} normal map dirapikan")


if __name__ == "__main__":
    main()
