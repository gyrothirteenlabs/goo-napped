---
name: sprite-render
description: Render model Blender jadi sprite sheet pixel 48px (top-down 3/4, 3 arah) dan siapkan untuk Unity. Pakai saat diminta render, export sprite, bikin sprite sheet, atau cek hasil pixel.
---

# Sprite render workflow

Baca `art/CLAUDE.md` dulu. Jalankan kode lewat Blender MCP (`execute_blender_code`).

## 1. Render rig (sekali per file)
```python
import bpy, math
from mathutils import Vector
s = bpy.context.scene
s.render.engine = 'BLENDER_EEVEE'          # Blender 3.6
s.render.resolution_x = s.render.resolution_y = 48
s.render.resolution_percentage = 100
s.render.filter_size = 0.0                 # tanpa AA → pixel tajam
s.render.film_transparent = True
s.render.image_settings.file_format = 'PNG'
s.render.image_settings.color_mode = 'RGBA'
s.view_settings.view_transform = 'Standard'  # bukan Filmic
s.eevee.taa_render_samples = 1

cam = bpy.data.objects.get('Sprite_Cam') or bpy.data.objects.new('Sprite_Cam', bpy.data.cameras.new('Sprite_Cam'))
if cam.name not in s.collection.objects: s.collection.objects.link(cam)
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 2.0                 # 48px = 2m → 24px/m. JANGAN diubah antar karakter
tilt = math.radians(55)                    # 0 = lurus ke bawah, 90 = horizontal
target = Vector((0, 0, 0.5))
cam.rotation_euler = (tilt, 0, 0)
cam.location = target + Vector((0, -math.sin(tilt), math.cos(tilt))) * 10
s.camera = cam
```
- Satu sun light: rotasi sekitar (50°, 0, -35°), shadow off. Arahnya sama untuk semua file.
- Arah karakter diatur dengan memutar **root karakter** (Z: front 0°, side 90° = hadap kanan, back 180°), bukan kameranya.

## 2. Preview cepat
Render 1 frame ke `art/renders/raw/_preview.png`, jalankan `pixelize.py` (langkah 4), lalu lihat hasilnya diperbesar 8× (nearest). Cek:
- Karakter muat di cell dengan margin minimal 2px (squash/stretch dan aksesori tidak kepotong)?
- Wajah terbaca? Outline utuh?

## 3. Render semua frame
Nama file: `<char>_<layer>_<anim>_<dir>_<frame:02d>.png` di `art/renders/raw/<char>/`.
- `layer` = `body` / `face` untuk slime pemain, `color` untuk lainnya.
- Layer `face`: sembunyikan body dari render tapi tetap jadi holdout (Object > Visibility > Holdout), supaya mata yang ketutup body tidak ikut ter-render.
- Target jumlah frame (sesuaikan kalau scope kepotong):

| Anim | Frame | Catatan |
|---|---|---|
| idle | 4 | loop, napas squash |
| move | 6 | loop, lompat kecil |
| eat | 5 | |
| split | 6 | dari 1 badan jadi 2 |
| merge | 5 | |
| disguise | 4 | jadi pipih/bentuk barang |
| alert | 3 | kaget saat ketahuan |

## 4. Pixelize + outline + sheet
```bash
# slime body (grayscale 4 tingkat + outline hitam)
python3 art/tools/pixelize.py art/renders/raw/slime art/sprites/slime --mode gray --levels 4 --sheet
# musuh/prop (palet tim, file .hex dari lospec)
python3 art/tools/pixelize.py art/renders/raw/golem art/sprites/golem --mode palette --palette art/palette.hex --sheet
```
Hasilnya sheet horizontal per `<char>_<layer>_<anim>_<dir>.png`, tiap cell 48px.

## 5. CHECK akhir
- Buka 1 sheet per anim dan lihat diperbesar. Frame konsisten (tidak geser/jitter, origin tetap)?
- Ukuran file = (48 × jumlah frame) × 48?
- Lapor ke user dengan preview gambar, lalu copy ke `Assets/Sprites/<char>/` kalau diminta.

## Unity import (catat ke user, jangan diasumsikan sudah di-set)
Texture Type Sprite, Mode Multiple, slice Grid 48×48, PPU 48, Filter Point, Compression None, pivot Bottom Center.
