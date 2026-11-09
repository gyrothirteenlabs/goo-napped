"""Bangun ulang art/blender/sprite_template.blend dari nol (kamera, render setting, koleksi, add-on).

Jalankan dari root repo (Blender tidak perlu dibuka):
  "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup ^
      --python art/tools/make_sprite_template.py

Template TIDAK diedit tangan: ubah add-on (goonapped_sprite_addon.py) lalu jalankan skrip ini lagi.
"""
import importlib.util
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, "..", ".."))
ADDON = os.path.join(HERE, "goonapped_sprite_addon.py")
OUT = os.path.join(REPO, "art", "blender", "sprite_template.blend")

README = """GOONAPPED SPRITE TEMPLATE  (panduan lengkap: docs/panduan-blender-3d.md)

0. Panel ada di 3D Viewport > tekan N > tab "GooNapped".
   Tidak ada? Buka Text Editor > goonapped_sprite_addon.py > Run Script.
1. Masukkan model:
   - .blend : File > Append (atau drag file .blend ke viewport > Append) > Object > pilih objek.
   - .obj/.fbx/.glb : drag file ke viewport (atau File > Import).
2. Pilih objek badan > "Jadikan Body". Pilih mata/mulut/kilau > "Jadikan Face".
3. "Pas-kan" -> model berdiri di lantai, tinggi 1 m (slime pemain). Cek wireframe Ref_Slime_1m.
4. Isi Nama aset + Animasi (contoh: idle:1-4, move:5-10). Animasi = rentang frame di timeline.
5. "Tes frame ini" -> lihat preview di Image Editor (GN_Preview). Kalau oke, "Render semua".
6. Hasil: art/sprites/<nama>/  -> copy ke Assets/_Game/Art/Sprites/<kategori>/.

JANGAN ubah: kamera Sprite_Cam, resolusi 48x48, Color Management (Standard), Film Filter 0.
Simpan sebagai file baru di art/blender/<nama>_v01.blend (path output relatif ke file .blend).
"""


def main():
    spec = importlib.util.spec_from_file_location("goonapped_sprite_addon", ADDON)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    mod.register()

    # kosongkan scene default (cube, light, camera, Collection)
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    for c in list(bpy.data.collections):
        bpy.data.collections.remove(c)
    for blocks in (bpy.data.meshes, bpy.data.lights, bpy.data.cameras, bpy.data.materials):
        for b in list(blocks):
            if b.users == 0:
                blocks.remove(b)

    scene = bpy.context.scene
    scene.name = "Sprite"
    mod.setup_scene(scene)
    scene.frame_start, scene.frame_end, scene.frame_current = 1, 4, 1
    scene.render.fps = 8   # sama dengan SlimeSpriteAnimator.fps di Unity, supaya preview timeline terasa sama

    # add-on ikut di dalam file (Run Script / auto register kalau Auto Run Python Scripts aktif)
    txt = bpy.data.texts.load(ADDON, internal=True)
    txt.name = "goonapped_sprite_addon.py"
    txt.use_module = True
    rd = bpy.data.texts.new("BACA_DULU.txt")
    rd.write(README)

    # viewport: lihat lewat kamera, solid + cavity supaya bentuk kebaca
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                sp = area.spaces.active
                sp.region_3d.view_perspective = "CAMERA"
                sp.shading.type = "SOLID"
                sp.shading.show_cavity = True
                sp.overlay.show_floor = True
            elif area.type == "PROPERTIES":
                pass

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=OUT, compress=True, relative_remap=True)
    print("TEMPLATE SAVED", OUT)


try:
    main()
except Exception:
    import traceback
    traceback.print_exc()
    sys.exit(1)
