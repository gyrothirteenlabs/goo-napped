"""PLACEHOLDER musuh (bukan desain final): bentuk kasar dari primitif, supaya penjaga bisa dites di Unity
sebelum desain tim desainer masuk. Dibuat hanya dari arahan yang sudah ada di docs:
  - sifat musuh: CLAUDE.md bagian "Musuh"
  - gaya bulat/soft, maksimal 2 aksesori, maksimal 4 hue: art/CLAUDE.md + skill slime-model
  - warna: art/palette.hex (nada menengah), albedo flat + normal map, bagian menyala = layer face (unlit di Unity)

Hasil per musuh:
  art/blender/<nama>_placeholder_v01.blend      (template + model, bisa dibuka/diubah desainer)
  art/sprites/<nama>/<nama>_{albedo,face,normal}_pose_{front,side,back}.png

Jalankan dari root repo:
  blender -b --factory-startup --gpu-backend vulkan --python art/tools/make_placeholder_enemies.py [-- nama ...]
"""
import importlib.util
import math
import os
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, "..", ".."))
TEMPLATE = os.path.join(REPO, "art", "blender", "sprite_template.blend")

spec = importlib.util.spec_from_file_location("goonapped_sprite_addon", os.path.join(HERE, "goonapped_sprite_addon.py"))
gn = importlib.util.module_from_spec(spec)
sys.dont_write_bytecode = True
spec.loader.exec_module(gn)
gn.register()


# ---------------------------------------------------------------- helper
def mat(name, hexcol):
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except AttributeError:
        pass
    lin = [((int(hexcol[i:i + 2], 16) / 255 + 0.055) / 1.055) ** 2.4 for i in (0, 2, 4)]
    p = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    p.inputs["Base Color"].default_value = (*lin, 1)
    m.diffuse_color = (*lin, 1)
    return m


def _finish(bm, name, material, loc, rot, face):
    m = Matrix.Translation(Vector(loc)) @ Euler([math.radians(a) for a in rot]).to_matrix().to_4x4()
    bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
    for f in bm.faces:
        f.smooth = True
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(material)
    o = bpy.data.objects.new(name, me)
    bpy.data.collections[gn.COL_FACE if face else gn.COL_BODY].objects.link(o)
    o.parent = bpy.data.objects[gn.OBJ_ROOT]
    return o


def box(name, size, loc, material, bevel=0.12, rot=(0, 0, 0), face=False):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    bmesh.ops.bevel(bm, geom=bm.edges[:], offset=min(bevel, min(size) * 0.45), segments=4, profile=0.5, affect="EDGES")
    return _finish(bm, name, material, loc, rot, face)


def blob(name, radii, loc, material, rot=(0, 0, 0), face=False, shape=None):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=14, radius=1.0)
    if shape:
        for v in bm.verts:
            v.co = shape(v.co)
    bmesh.ops.scale(bm, vec=radii, verts=bm.verts)
    return _finish(bm, name, material, loc, rot, face)


def cone(name, radius, height, loc, material, rot=(0, 0, 0), face=False):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=16, radius1=radius, radius2=radius * 0.15, depth=height)
    return _finish(bm, name, material, loc, rot, face)


def flame(co):
    """bola -> tetesan api: atas ditarik jadi ujung lancip yang melengkung ke belakang."""
    x, y, z = co
    if z > 0:
        k = (1 - z) ** 0.45
        return Vector((x * k, y * k + 0.3 * z * z, z * 1.7))
    return Vector((x, y, z))


# ---------------------------------------------------------------- musuh
def golem():
    """Golem Batu: besar, berat, lambat, melihat lurus ke depan -> badan kotak batu + celah mata sempit."""
    stone, dark, eye = mat("Golem_Stone", "8a92a0"), mat("Golem_StoneDark", "5d6472"), mat("Golem_Eye", "ff5a5f")
    box("Golem_Torso", (1.05, 0.8, 0.8), (0, 0, 0.62), stone, 0.2)
    box("Golem_Head", (0.62, 0.56, 0.42), (0, -0.1, 1.2), stone, 0.14)
    for s, n in ((-1, "L"), (1, "R")):
        box(f"Golem_Arm{n}", (0.34, 0.42, 0.78), (s * 0.7, -0.02, 0.58), dark, 0.13)
        box(f"Golem_Foot{n}", (0.4, 0.52, 0.26), (s * 0.29, -0.03, 0.13), dark, 0.1)
    box("Golem_EyeSlit", (0.42, 0.08, 0.15), (0, -0.37, 1.2), eye, 0.03, face=True)


MOVE_START, MOVE_LEN = 11, 6   # siklus jalan golem: frame 11-16 (6 frame @8fps, loop)


def golem_walk():
    """Jalan berat: kaki bergantian melangkah, lengan berlawanan, badan turun + oleng saat kaki menapak.
    Frame 1 = pose diam. Badan hanya TURUN dari pose (tidak naik) supaya kepala tidak keluar cell 48px."""
    ob = bpy.data.objects
    upper = [ob[n] for n in ("Golem_Torso", "Golem_Head", "Golem_EyeSlit")]
    parts = upper + [ob[f"Golem_{p}{s}"] for p in ("Arm", "Foot") for s in "LR"]

    def key(frame):
        for o in parts:
            o.keyframe_insert("location", frame=frame)
            o.keyframe_insert("rotation_euler", frame=frame)

    key(1)
    for i in range(MOVE_LEN):
        ph = 2 * math.pi * i / MOVE_LEN
        dip = -0.05 * abs(math.cos(ph))
        roll = math.radians(4) * math.sin(ph)
        for o in upper:
            o.location = (0, 0, dip)
            o.rotation_euler = (0, roll, 0)
        for s, off in (("L", 0.0), ("R", math.pi)):
            p = ph + off
            ob[f"Golem_Foot{s}"].location = (0, 0.13 * math.cos(p), 0.1 * max(0.0, math.sin(p)))
            ob[f"Golem_Arm{s}"].location = (0, -0.1 * math.cos(p), dip)
        key(MOVE_START + i)
    for o in parts:
        o.location, o.rotation_euler = (0, 0, 0), (0, 0, 0)
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = MOVE_START, MOVE_START + MOVE_LEN - 1


def familiarapi():
    """Familiar Api: melayang, menyala (unlit di Unity), melihat benda panas -> tetesan api + 2 lidah api kecil."""
    fire, core, eye = mat("Familiar_Fire", "f08a2e"), mat("Familiar_Core", "ffc861"), mat("Familiar_Eye", "4d3324")
    blob("Familiar_Body", (0.42, 0.42, 0.42), (0, 0, 0.62), fire, shape=flame)
    blob("Familiar_Belly", (0.27, 0.2, 0.24), (0, -0.26, 0.52), core)
    for s, n in ((-1, "L"), (1, "R")):
        blob(f"Familiar_Wisp{n}", (0.14, 0.14, 0.14), (s * 0.55, 0, 0.52), fire, shape=flame)
        blob(f"Familiar_Eye{n}", (0.07, 0.04, 0.1), (s * 0.15, -0.4, 0.78), eye, face=True)


def anjinglendir():
    """Anjing Lendir: mengendus bau -> anjing jeli berkaki pendek, moncong + hidung besar, kuping terkulai."""
    goo, dark, eye = mat("Dog_Goo", "3fd1c7"), mat("Dog_GooDark", "1f6f7a"), mat("Dog_Eye", "121a2b")
    oy = 0.1
    blob("Dog_Body", (0.42, 0.55, 0.36), (0, 0.1 + oy, 0.5), goo)
    blob("Dog_Head", (0.34, 0.34, 0.33), (0, -0.45 + oy, 0.72), goo)
    blob("Dog_Snout", (0.2, 0.24, 0.16), (0, -0.74 + oy, 0.62), goo)
    blob("Dog_Nose", (0.11, 0.1, 0.1), (0, -0.95 + oy, 0.67), dark)
    cone("Dog_Tail", 0.11, 0.42, (0, 0.68 + oy, 0.8), goo, rot=(35, 0, 0))
    for s, n in ((-1, "L"), (1, "R")):
        blob(f"Dog_Ear{n}", (0.09, 0.17, 0.26), (s * 0.36, -0.4 + oy, 0.62), dark, rot=(0, s * 12, 0))
        for y, fb in ((-0.28, "F"), (0.42, "B")):
            blob(f"Dog_Leg{fb}{n}", (0.15, 0.16, 0.22), (s * 0.27, y + oy, 0.2), dark)
        blob(f"Dog_Eye{n}", (0.07, 0.05, 0.08), (s * 0.15, -0.71 + oy, 0.88), eye, face=True)


def matapenyihir():
    """Mata Penyihir: melayang, memeriksa toples -> bola mata besar bertudung + ekor saraf."""
    white, hood = mat("Eye_White", "b9c0cc"), mat("Eye_Hood", "7a52c7")
    iris, pupil = mat("Eye_Iris", "c2253a"), mat("Eye_Pupil", "121a2b")
    blob("Eye_Ball", (0.38, 0.38, 0.38), (0, 0, 0.85), white)
    blob("Eye_Hood", (0.43, 0.42, 0.4), (0, 0.13, 0.93), hood)
    blob("Eye_Iris", (0.21, 0.1, 0.21), (0, -0.31, 0.83), iris)
    blob("Eye_Pupil", (0.1, 0.06, 0.1), (0, -0.38, 0.83), pupil)
    cone("Eye_Tail", 0.13, 0.42, (0, 0.1, 0.36), hood, rot=(200, 0, 0))


ENEMIES = {"golem": golem, "familiarapi": familiarapi, "anjinglendir": anjinglendir, "matapenyihir": matapenyihir}


def build(name):
    bpy.ops.wm.open_mainfile(filepath=TEMPLATE, load_ui=False)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, "art", "blender", f"{name}_placeholder_v01.blend"), compress=True)
    scene = bpy.context.scene
    gn.setup_scene(scene)
    ENEMIES[name]()
    st = scene.gn_sprite
    st.name, st.mode, st.anims = name, "ALBEDO", "pose:1"
    if name == "golem":
        golem_walk()
        st.anims = f"pose:1, move:{MOVE_START}-{MOVE_START + MOVE_LEN - 1}"
    st.outline = True
    st.palette = "//../palette.hex"
    st.dir_front = st.dir_side = st.dir_back = True
    bpy.context.view_layer.update()
    n, spr, pre = gn.render_sprites(bpy.context)
    bpy.ops.wm.save_mainfile(compress=True)
    print("PLACEHOLDER", name, n, "frame ->", spr)


names = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else list(ENEMIES)
for nm in names:
    build(nm)
