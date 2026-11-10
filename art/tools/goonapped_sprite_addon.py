"""GooNapped Sprite Render: render model 3D jadi sprite pixel 48px yang siap dipakai Unity.

Panel: 3D Viewport > Sidebar (N) > tab "GooNapped".

Sudah terpasang di art/blender/sprite_template.blend (text block). Kalau panel tidak muncul:
  - Text Editor > pilih goonapped_sprite_addon.py > Run Script, ATAU
  - Edit > Preferences > Add-ons > (v) > Install from Disk > pilih file ini (sekali saja).

Yang dilakukan tool ini (lihat docs/panduan-blender-3d.md):
  1. Kamera, resolusi, color management dikunci sama dengan sprite slime yang sudah ada.
  2. Material model diganti sementara jadi warna flat (Emission) saat render, lalu dikembalikan.
     Jadi model dari mana pun (OBJ, FBX, .blend lain) tidak perlu material khusus.
  3. Render 3 layer x 3 arah x semua frame animasi:
       body  : putih flat (mode Slime) atau warna flat (mode Warna flat, layer "albedo")
       face  : objek di koleksi MODEL_Face, body jadi holdout (yang ketutup body tidak ikut)
       normal: normal ruang kamera (R kanan, G atas, B ke penonton)
  4. Pixelize: alpha tegas, outline 1px, normal di-pad 1px, susun jadi sheet horizontal,
     plus gambar preview (disinari dari kiri & kanan) untuk dicek sebelum masuk Unity.
Tanpa Python/Pillow eksternal: semua jalan di Python bawaan Blender (numpy).
"""

bl_info = {
    "name": "GooNapped Sprite Render",
    "author": "Tim GooNapped",
    "version": (1, 1, 0),
    "blender": (4, 2, 0),
    "location": "3D Viewport > Sidebar > GooNapped",
    "description": "Render model jadi sprite pixel 48px (body/albedo + face + normal map) untuk Unity",
    "category": "Render",
}

import math
import os
import re

import bpy
import numpy as np

# ---------------------------------------------------------------- spesifikasi (DIKUNCI, lihat art/CLAUDE.md)
CELL = 48                  # px per cell sprite (Unity: PPU 48, 1 tile = 1 unit)
PX_PER_M = 24              # kepadatan pixel: 1 m di Blender = 24 px  -> ortho scale 2.0
CAM_TILT = 55.0            # rotasi X kamera (derajat dari lurus ke bawah) = top-down 3/4
CAM_DIST = 10.0
CAM_AIM_Z = 0.5            # titik bidik kamera (tengah badan setinggi 1 m)
DIRS = {"front": 0, "side": 90, "back": 180}   # rotasi Z Model_Root; side = hadap kanan
OUTLINE_GRAY = (40, 40, 40)                    # outline body slime (shader Unity: abu < 0.2 = outline)

COL_RIG, COL_REF, COL_MODEL, COL_BODY, COL_FACE = "GN_Rig", "GN_Ref", "MODEL", "MODEL_Body", "MODEL_Face"
OBJ_CAM, OBJ_ROOT = "Sprite_Cam", "Model_Root"
MAT_NORMAL, MAT_TMP = "GN_NormalPass", "GN_TmpFlat"
RENDERABLE = {"MESH", "CURVE", "SURFACE", "META", "FONT", "CURVES", "POINTCLOUD", "VOLUME"}

# preview: palet "Slime_Normal" dari Unity (Assets/_Game/Art/Materials/Slime_Normal.mat)
PREVIEW_BANDS = np.array([(0x2f, 0x7a, 0x6a), (0x3f, 0xa8, 0x6a), (0x6f, 0xd2, 0x7e), (0xb4, 0xf2, 0x9a)], np.float32)
PREVIEW_OUTLINE = np.array((0x14, 0x40, 0x2e), np.float32)
PREVIEW_EDGES = (0.35, 0.6, 0.9)


# ================================================================= setup scene
def _collection(name, parent):
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
    if c.name not in parent.children:
        parent.children.link(c)
    return c


def _set_enum(owner, attr, *values):
    for v in values:
        try:
            setattr(owner, attr, v)
            return v
        except (TypeError, ValueError):
            pass
    return None


def apply_render_settings(scene):
    r = scene.render
    _set_enum(r, "engine", "BLENDER_EEVEE", "BLENDER_EEVEE_NEXT")
    r.resolution_x = r.resolution_y = CELL
    r.resolution_percentage = 100
    r.pixel_aspect_x = r.pixel_aspect_y = 1
    r.film_transparent = True          # background transparan
    r.filter_size = 0.0                # tanpa anti-aliasing: pixel tegas
    r.dither_intensity = 0.0           # tanpa noise dither (merusak warna flat & normal)
    r.use_motion_blur = False
    r.use_compositing = False
    r.use_sequencer = False
    im = r.image_settings
    im.file_format = "PNG"
    im.color_mode = "RGBA"
    im.color_depth = "8"
    try:
        scene.eevee.taa_render_samples = 1
    except AttributeError:
        pass
    _set_enum(scene.display_settings, "display_device", "sRGB")
    vs = scene.view_settings
    _set_enum(vs, "view_transform", "Standard")   # BUKAN AgX/Filmic (mengubah warna)
    _set_enum(vs, "look", "None")
    vs.exposure = 0.0
    vs.gamma = 1.0
    vs.use_curve_mapping = False


def _normal_material():
    m = bpy.data.materials.get(MAT_NORMAL)
    if m is not None:
        return m
    m = bpy.data.materials.new(MAT_NORMAL)
    try:
        m.use_nodes = True
    except AttributeError:
        pass
    nt = m.node_tree
    nt.nodes.clear()
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    vt = nt.nodes.new("ShaderNodeVectorTransform")
    vt.vector_type, vt.convert_from, vt.convert_to = "NORMAL", "WORLD", "CAMERA"
    vm = nt.nodes.new("ShaderNodeVectorMath")
    vm.operation = "MULTIPLY_ADD"
    vm.inputs[1].default_value = (0.5, 0.5, -0.5)   # ruang kamera Blender menghadap +Z -> Z dibalik
    vm.inputs[2].default_value = (0.5, 0.5, 0.5)
    em = nt.nodes.new("ShaderNodeEmission")
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(geo.outputs["Normal"], vt.inputs[0])
    nt.links.new(vt.outputs[0], vm.inputs[0])
    nt.links.new(vm.outputs[0], em.inputs["Color"])
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    for i, n in enumerate((geo, vt, vm, em, out)):
        n.location = (i * 220, 0)
    m.use_fake_user = True
    return m


def setup_scene(scene):
    apply_render_settings(scene)
    root_col = scene.collection
    rig = _collection(COL_RIG, root_col)
    ref = _collection(COL_REF, root_col)
    model = _collection(COL_MODEL, root_col)
    _collection(COL_BODY, model)
    _collection(COL_FACE, model)

    cam = bpy.data.objects.get(OBJ_CAM)
    if cam is None:
        cam = bpy.data.objects.new(OBJ_CAM, bpy.data.cameras.new(OBJ_CAM))
    if cam.name not in rig.objects:
        rig.objects.link(cam)
    t = math.radians(CAM_TILT)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = CELL / PX_PER_M
    cam.data.clip_start, cam.data.clip_end = 0.1, 50.0
    cam.data.shift_x = cam.data.shift_y = 0.0
    cam.location = (0.0, -CAM_DIST * math.sin(t), CAM_AIM_Z + CAM_DIST * math.cos(t))
    cam.rotation_mode = "XYZ"
    cam.rotation_euler = (t, 0.0, 0.0)
    cam.lock_location = cam.lock_rotation = (True, True, True)
    scene.camera = cam

    root = bpy.data.objects.get(OBJ_ROOT)
    if root is None:
        root = bpy.data.objects.new(OBJ_ROOT, None)
        root.empty_display_type = "ARROWS"
        root.empty_display_size = 0.4
    if root.name not in rig.objects:
        rig.objects.link(root)

    if "Ref_Slime_1m" not in bpy.data.objects:
        me = bpy.data.meshes.new("Ref_Slime_1m")
        import bmesh
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.6, radius2=0.6, depth=1.0)
        bmesh.ops.translate(bm, vec=(0, 0, 0.5), verts=bm.verts)
        bm.to_mesh(me)
        bm.free()
        o = bpy.data.objects.new("Ref_Slime_1m", me)
        o.display_type = "WIRE"
        o.hide_render = True
        o.hide_select = True
        ref.objects.link(o)
    if "Ref_Tile_48px" not in bpy.data.objects:
        me = bpy.data.meshes.new("Ref_Tile_48px")
        h = CELL / PX_PER_M / 2
        me.from_pydata([(-h, -h, 0), (h, -h, 0), (h, h, 0), (-h, h, 0)], [], [(0, 1, 2, 3)])
        o = bpy.data.objects.new("Ref_Tile_48px", me)
        o.display_type = "WIRE"
        o.hide_render = True
        o.hide_select = True
        ref.objects.link(o)
    ref.hide_render = True
    _normal_material()
    return cam, root


# ================================================================= model helpers
def _objects(col_name):
    c = bpy.data.collections.get(col_name)
    return [o for o in c.all_objects if o.type in RENDERABLE] if c else []


def _top_level(objs):
    s = set(objs)
    return [o for o in objs if o.parent not in s]


def assign_objects(context, target):
    scene = context.scene
    setup_scene(scene)
    col = bpy.data.collections[target]
    root = bpy.data.objects[OBJ_ROOT]
    sel = [o for o in context.selected_objects if o.name not in (OBJ_CAM, OBJ_ROOT)]
    # ikutkan semua anak (mata yang di-parent ke badan, lattice, armature, dst.)
    allo = list(sel)
    for o in sel:
        allo += [c for c in o.children_recursive if c not in allo]
    for o in allo:
        for c in list(o.users_collection):
            if c != col:
                c.objects.unlink(o)
        if o.name not in col.objects:
            col.objects.link(o)
    for o in _top_level(allo):
        if o.parent is None:
            mw = o.matrix_world.copy()
            o.parent = root
            o.matrix_world = mw
    return len(allo)


def fit_model(context, height):
    """Taruh model berdiri di Z=0, tengah di origin, tinggi = height (m)."""
    from mathutils import Matrix, Vector
    objs = _objects(COL_BODY) + _objects(COL_FACE)
    if not objs:
        return False
    dg = context.evaluated_depsgraph_get()
    pts = []
    for o in objs:
        ev = o.evaluated_get(dg)
        pts += [ev.matrix_world @ Vector(c) for c in ev.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    size_z = hi.z - lo.z
    if size_z <= 1e-6:
        return False
    f = height / size_z
    pivot = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
    m = Matrix.Scale(f, 4) @ Matrix.Translation(-pivot)
    root = bpy.data.objects[OBJ_ROOT]
    root_inv = root.matrix_world.inverted()
    # transform objek level atas saja (termasuk armature/lattice/empty), anak-anaknya ikut
    allm = set()
    for cn in (COL_BODY, COL_FACE):
        c = bpy.data.collections.get(cn)
        if c:
            allm.update(c.all_objects)
    for o in [o for o in allm if o.parent not in allm]:
        o.matrix_world = root.matrix_world @ (m @ (root_inv @ o.matrix_world))
    return True


# ================================================================= material flat sementara
def _upstream_types(node, seen=None):
    seen = seen if seen is not None else set()
    for inp in node.inputs:
        for l in inp.links:
            n = l.from_node
            if n.name not in seen:
                seen.add(n.name)
                yield n.type
                yield from _upstream_types(n, seen)


def _color_socket(nt, out):
    surf = out.inputs["Surface"]
    if surf.is_linked:
        n = surf.links[0].from_node
        if n.type == "EMISSION" and "SHADERTORGB" not in set(_upstream_types(n)):
            return n.inputs["Color"]
    for t, s in (("BSDF_PRINCIPLED", "Base Color"), ("BSDF_DIFFUSE", "Color"), ("BSDF_TOON", "Color"),
                 ("PRINCIPLED_VOLUME", "Color"), ("EMISSION", "Color")):
        n = next((n for n in nt.nodes if n.type == t), None)
        if n:
            return n.inputs[s]
    return None


def _output_node(nt):
    outs = [n for n in nt.nodes if n.type == "OUTPUT_MATERIAL"]
    return next((n for n in outs if n.is_active_output), outs[0] if outs else None)


class FlatSwap:
    """Selama render: semua material objek -> Emission warna flat (putih atau warna dasar). Dikembalikan di restore()."""

    def __init__(self):
        self.mats, self.slots = [], []

    def apply(self, objs, white):
        tmp = bpy.data.materials.get(MAT_TMP) or bpy.data.materials.new(MAT_TMP)
        tmp.diffuse_color = (0.8, 0.8, 0.8, 1)
        done = set()
        for o in objs:
            if o.type == "MESH" and len(o.material_slots) == 0:
                o.data.materials.append(tmp)
                self.slots.append(o)
            for slot in o.material_slots:
                m = slot.material
                if m is None or m.name in done:
                    continue
                done.add(m.name)
                self._swap(m, white)

    def _swap(self, m, white):
        try:
            if not m.use_nodes:
                m.use_nodes = True
        except AttributeError:
            pass
        nt = m.node_tree
        if nt is None:
            return
        out = _output_node(nt)
        if out is None:
            out = nt.nodes.new("ShaderNodeOutputMaterial")
        surf = out.inputs["Surface"]
        old = surf.links[0].from_socket if surf.is_linked else None
        em = nt.nodes.new("ShaderNodeEmission")
        em.name = "GN_TMP"
        em.inputs["Strength"].default_value = 1.0
        col = em.inputs["Color"]
        if white:
            col.default_value = (1, 1, 1, 1)
        else:
            src = _color_socket(nt, out)
            if src is None:
                col.default_value = tuple(m.diffuse_color)
            elif src.is_linked:
                nt.links.new(src.links[0].from_socket, col)
            else:
                v = tuple(src.default_value)
                col.default_value = v if len(v) == 4 else (*v[:3], 1)
        nt.links.new(em.outputs[0], surf)
        self.mats.append((m, out, em, old))

    def restore(self):
        for m, out, em, old in reversed(self.mats):
            nt = m.node_tree
            nt.nodes.remove(em)
            if old is not None:
                nt.links.new(old, out.inputs["Surface"])
        self.mats.clear()
        for o in self.slots:
            o.data.materials.pop(index=len(o.data.materials) - 1)
        self.slots.clear()


# ================================================================= render
ANIM_RE = re.compile(r"^\s*([A-Za-z][A-Za-z0-9]*)\s*:\s*(-?\d+)\s*(?:-\s*(-?\d+))?\s*(?:/\s*(\d+))?\s*$")


def parse_anims(text):
    """'idle:1-4, move:10-15, jelly:20-52/2, pose:1' -> [(nama, [frame...])]"""
    out = []
    for part in re.split(r"[,;\n]+", text):
        if not part.strip():
            continue
        m = ANIM_RE.match(part)
        if not m:
            raise ValueError(f"Format animasi salah: '{part.strip()}' (contoh: idle:1-4, jelly:20-52/2)")
        a, b = int(m[2]), int(m[3]) if m[3] else int(m[2])
        step = int(m[4]) if m[4] else 1
        if b < a:
            raise ValueError(f"Frame akhir lebih kecil dari awal: '{part.strip()}'")
        out.append((m[1], list(range(a, b + 1, step))))
    if not out:
        raise ValueError("Daftar animasi kosong")
    return out


def _paths(st):
    raw = bpy.path.abspath(st.out_raw)
    spr = bpy.path.abspath(st.out_sprites)
    if not bpy.data.filepath and (st.out_raw.startswith("//") or st.out_sprites.startswith("//")):
        raise ValueError("Simpan file .blend dulu (path output relatif ke file .blend)")
    return os.path.normpath(os.path.join(raw, st.name)), os.path.normpath(os.path.join(spr, st.name))


def body_layer(st):
    return "body" if st.mode == "GRADIENT" else "albedo"


def _physics_caches():
    """[(objek, point_cache)] semua simulasi (soft body, cloth, partikel) pada objek di koleksi MODEL."""
    col = bpy.data.collections.get(COL_MODEL)
    out = []
    for o in (col.all_objects if col else []):
        for m in o.modifiers:
            pc = getattr(m, "point_cache", None)
            if pc is not None and m.type != "PARTICLE_SYSTEM":
                out.append((o, pc))
        for ps in o.particle_systems:
            out.append((o, ps.point_cache))
    return out


def _rebake(scene, caches, bake=True):
    """Hasil bake physics tersimpan di koordinat dunia dan TIDAK ikut rotasi Model_Root.
    Jadi setelah Model_Root diputar, simulasi harus di-bake ulang supaya side/back tidak sama dengan front."""
    scene.frame_set(scene.frame_start)
    for o, pc in caches:
        with bpy.context.temp_override(scene=scene, point_cache=pc, active_object=o, object=o):
            bpy.ops.ptcache.free_bake()
            if bake:
                bpy.ops.ptcache.bake(bake=True)


def render_sprites(context, test=False):
    scene = context.scene
    st = scene.gn_sprite
    if not re.fullmatch(r"[a-z][a-z0-9]*", st.name):
        raise ValueError("Nama aset: huruf kecil/angka saja, tanpa spasi/underscore (contoh: slime, golem, jar)")
    cam, root = setup_scene(scene)
    body = _objects(COL_BODY)
    face = _objects(COL_FACE)
    if not body:
        raise ValueError("Koleksi MODEL_Body kosong. Pilih model lalu klik 'Jadikan Body'.")
    anims = [("test", [scene.frame_current])] if test else parse_anims(st.anims)
    dirs = [d for d, on in zip(DIRS, (st.dir_front, st.dir_side, st.dir_back)) if on]
    if not dirs:
        raise ValueError("Pilih minimal satu arah")
    raw, spr = _paths(st)
    bl = body_layer(st)
    layers = [bl] + (["face"] if face else []) + ["normal"]

    vl = context.view_layer
    s_frame, s_rot, s_mode = scene.frame_current, tuple(root.rotation_euler), root.rotation_mode
    s_view = scene.view_settings.view_transform
    s_override = vl.material_override
    s_root_act = root.animation_data.action if root.animation_data else None
    s_hide = {o.name: o.hide_render for o in body + face}
    s_hold = {o.name: o.is_holdout for o in body + face}
    caches = _physics_caches() if st.rebake_physics else []
    s_baked = [pc.is_baked for _, pc in caches]
    swap = FlatSwap()
    count = 0
    try:
        if root.animation_data:
            root.animation_data.action = None
        root.rotation_mode = "XYZ"
        for layer in layers:
            ldir = os.path.join(raw, layer)
            os.makedirs(ldir, exist_ok=True)
            # buang frame lama (supaya sheet tidak tercampur sisa render lama): HANYA anim + arah yang dirender
            olds = tuple(f"{st.name}_{layer}_{a}_{d}_" for a, _ in anims for d in dirs)
            for f in os.listdir(ldir):
                if f.startswith(olds) and f.endswith(".png"):
                    os.remove(os.path.join(ldir, f))
        for d in dirs:
            root.rotation_euler = (0.0, 0.0, math.radians(DIRS[d]))
            if caches:
                _rebake(scene, caches)
            for layer in layers:
                ldir = os.path.join(raw, layer)
                is_face, is_normal = layer == "face", layer == "normal"
                for o in body:
                    o.hide_render = s_hide[o.name]
                    o.is_holdout = is_face
                swap.restore()
                if is_normal:
                    vl.material_override = _normal_material()
                    scene.view_settings.view_transform = "Raw"
                else:
                    vl.material_override = s_override
                    scene.view_settings.view_transform = "Standard"
                    swap.apply(face if is_face else body, white=(layer == "body"))
                show_face = is_face and d != "back"   # tampak belakang: face kosong
                for o in face:
                    o.hide_render = True if not show_face else s_hide[o.name]
                for a, frames in anims:
                    for i, f in enumerate(frames):
                        scene.frame_set(f)
                        scene.render.filepath = os.path.join(ldir, f"{st.name}_{layer}_{a}_{d}_{i:02d}.png")
                        bpy.ops.render.render(write_still=True)
                        count += 1
    finally:
        swap.restore()
        vl.material_override = s_override
        scene.view_settings.view_transform = s_view
        for o in body + face:
            o.hide_render = s_hide[o.name]
            o.is_holdout = s_hold[o.name]
        root.rotation_mode = s_mode
        root.rotation_euler = s_rot
        if root.animation_data:
            root.animation_data.action = s_root_act
        # kembalikan simulasi ke arah semula (yang tadinya ter-bake di-bake lagi, sisanya dikosongkan)
        for (o, pc), baked in zip(caches, s_baked):
            _rebake(scene, [(o, pc)], bake=baked)
        scene.frame_set(s_frame)
    pre = pixelize(st, raw, spr, layers, anims, dirs, test)
    return count, spr, pre


# ================================================================= pixelize (numpy, tanpa Pillow)
def load_png(path):
    img = bpy.data.images.load(path, check_existing=False)
    try:
        w, h = img.size
        a = np.empty(w * h * 4, np.float32)
        img.pixels.foreach_get(a)
    finally:
        bpy.data.images.remove(img)
    return np.round(a.reshape(h, w, 4) * 255).astype(np.uint8)   # baris bawah-ke-atas (konvensi Blender)


def save_png(arr, path):
    h, w, _ = arr.shape
    img = bpy.data.images.new("gn_tmp_save", w, h, alpha=True)
    try:
        img.colorspace_settings.name = "Non-Color"
    except TypeError:
        pass
    img.alpha_mode = "STRAIGHT"
    img.pixels.foreach_set((arr.astype(np.float32) / 255.0).ravel())
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def _neighbors(mask):
    """4 versi mask yang digeser (kanan, kiri, atas, bawah), padding False."""
    p = np.pad(mask, 1)
    return p[1:-1, :-2], p[1:-1, 2:], p[:-2, 1:-1], p[2:, 1:-1]


def _shift(arr):
    p = np.pad(arr, ((1, 1), (1, 1), (0, 0)))
    return p[1:-1, :-2], p[1:-1, 2:], p[:-2, 1:-1], p[2:, 1:-1]


def alpha_cut(img):
    img = img.copy()
    on = img[..., 3] >= 128
    img[~on] = 0
    img[on, 3] = 255
    return img


def quantize_gray(img, levels=4):
    on = img[..., 3] > 0
    lum = img[..., 0] * 0.299 + img[..., 1] * 0.587 + img[..., 2] * 0.114
    lv = 1 + np.minimum(levels - 1, (lum / 256 * levels).astype(int))
    v = np.round(lv / levels * 255).astype(np.uint8)
    img[on, 0] = img[on, 1] = img[on, 2] = v[on]
    return img


def quantize_palette(img, pal):
    on = img[..., 3] > 0
    c = img[on, :3].astype(np.int32)
    d = ((c[:, None, :] - pal[None, :, :]) ** 2).sum(-1)
    img[on, :3] = pal[d.argmin(1)]
    return img


def add_outline(img, color=None):
    """Outline 1px di luar siluet. color=None -> versi gelap warna tetangga."""
    on = img[..., 3] > 0
    ring = ~on & np.logical_or.reduce(_neighbors(on))
    if color is not None:
        img[ring, :3] = color
    else:
        acc = np.zeros(img.shape[:2] + (3,), np.float32)
        cnt = np.zeros(img.shape[:2], np.float32)
        for nb, sm in zip(_shift(img[..., :3].astype(np.float32)), _neighbors(on)):
            acc += nb * sm[..., None]
            cnt += sm
        img[ring, :3] = np.round(acc[ring] / cnt[ring, None] * 0.4).astype(np.uint8)
    img[ring, 3] = 255
    return img


def pad_normal(img, n=1):
    for _ in range(n):
        on = img[..., 3] > 0
        acc = np.zeros(img.shape[:2] + (3,), np.float32)
        cnt = np.zeros(img.shape[:2], np.float32)
        for nb, sm in zip(_shift(img[..., :3].astype(np.float32)), _neighbors(on)):
            acc += nb * sm[..., None]
            cnt += sm
        grow = ~on & (cnt > 0)
        img[grow, :3] = (acc[grow] / cnt[grow, None]).astype(np.uint8)
        img[grow, 3] = 255
    off = img[..., 3] == 0
    img[off, :3] = (128, 128, 255)   # normal datar
    return img


def _load_palette(path):
    cols = []
    if path and os.path.isfile(bpy.path.abspath(path)):
        for tok in open(bpy.path.abspath(path)).read().split():
            h = tok.strip().lstrip("#")
            if len(h) == 6:
                cols.append([int(h[i:i + 2], 16) for i in (0, 2, 4)])
    return np.array(cols, np.int32) if cols else None


def process(st, layer, img):
    img = alpha_cut(img)
    if layer == "body":
        img = add_outline(quantize_gray(img), OUTLINE_GRAY)
    elif layer == "albedo":
        pal = _load_palette(st.palette)
        if pal is not None:
            img = quantize_palette(img, pal)
        if st.outline:
            img = add_outline(img, None)
    elif layer == "normal":
        img = pad_normal(img, st.normal_pad)
    return img


def pixelize(st, raw, spr, layers, anims, dirs, test):
    os.makedirs(spr, exist_ok=True)
    cells = {}
    for layer in layers:
        for a, frames in anims:
            for d in dirs:
                fr = [process(st, layer, load_png(os.path.join(raw, layer, f"{st.name}_{layer}_{a}_{d}_{i:02d}.png")))
                      for i in range(len(frames))]
                cells[(layer, a, d)] = fr
                if not test:
                    save_png(np.concatenate(fr, axis=1), os.path.join(spr, f"{st.name}_{layer}_{a}_{d}.png"))
    pre = os.path.join(spr, f"_preview_{st.name}{'_test' if test else ''}.png")
    save_png(make_preview(st, cells, layers, anims, dirs), pre)
    return pre


# ================================================================= preview (simulasi lampu Unity kasar)
def _lit(st, base, face, normal, light):
    n = normal[..., :3].astype(np.float32) / 255 * 2 - 1
    L = np.array(light, np.float32)
    L /= np.linalg.norm(L)
    inten = np.clip((n * L).sum(-1), 0, 1) * 0.85 + 0.15
    out = np.zeros_like(base)
    on = base[..., 3] > 0
    if st.mode == "GRADIENT":
        band = np.digitize(inten, PREVIEW_EDGES)
        col = PREVIEW_BANDS[band]
        col[base[..., 0] < 51] = PREVIEW_OUTLINE
    else:
        q = np.maximum(np.floor(inten * 4 + 0.5) / 4, 0.35)
        col = base[..., :3].astype(np.float32) * q[..., None]
    out[on, :3] = np.clip(col[on], 0, 255).astype(np.uint8)
    out[on, 3] = 255
    if face is not None:
        fo = face[..., 3] > 0
        out[fo] = face[fo]
    return out


def make_preview(st, cells, layers, anims, dirs, scale=4):
    bl = layers[0]
    rows = []
    for a, _ in anims:
        row = []
        for d in dirs:
            base = cells[(bl, a, d)][0]
            face = cells[("face", a, d)][0] if ("face", a, d) in cells else None
            nrm = cells[("normal", a, d)][0]
            for light in ((-1, 0.4, 0.6), (1, 0.4, 0.6)):      # lampu dari kiri, lalu kanan
                row.append(_lit(st, base, face, nrm, light))
            row.append(nrm.copy())
            row.append(np.zeros((CELL, 8, 4), np.uint8))       # jarak antar arah
        rows.append(np.concatenate(row, axis=1))
        rows.append(np.zeros((8, rows[-1].shape[1], 4), np.uint8))
    rows.reverse()                                              # array Blender bawah-ke-atas: anim pertama di atas
    img = np.concatenate(rows, axis=0)
    bg = np.zeros_like(img)
    bg[..., :3] = (36, 40, 52)
    bg[..., 3] = 255
    on = img[..., 3] > 0
    bg[on] = img[on]
    return np.repeat(np.repeat(bg, scale, axis=0), scale, axis=1)


def _show_image(context, path):
    img = bpy.data.images.get("GN_Preview")
    if img is None:
        img = bpy.data.images.load(path, check_existing=False)
        img.name = "GN_Preview"
    else:
        img.filepath = path
        img.reload()
    for area in context.screen.areas if context.screen else []:
        if area.type == "IMAGE_EDITOR":
            area.spaces.active.image = img
            return True
    return False


# ================================================================= UI
class GN_Settings(bpy.types.PropertyGroup):
    name: bpy.props.StringProperty(name="Nama aset", default="slime",
                                   description="Huruf kecil tanpa spasi. Jadi awalan nama file: <nama>_body_idle_front.png")
    mode: bpy.props.EnumProperty(name="Mode", items=[
        ("GRADIENT", "Slime (warna di Unity)", "Body dirender putih flat + outline abu. Warna per state bahan diatur material Unity (gradient map)"),
        ("ALBEDO", "Warna flat (musuh/prop)", "Body dirender dengan warna dasar material (Base Color), flat tanpa cahaya"),
    ], default="GRADIENT")
    anims: bpy.props.StringProperty(name="Animasi", default="idle:1-4",
                                    description="nama:frameAwal-frameAkhir[/langkah], pisah koma. Contoh: idle:1-4, move:10-15, jelly:20-52/2")
    dir_front: bpy.props.BoolProperty(name="front", default=True)
    dir_side: bpy.props.BoolProperty(name="side", default=True, description="Hadap kanan. Hadap kiri = flip di Unity")
    dir_back: bpy.props.BoolProperty(name="back", default=True)
    height: bpy.props.FloatProperty(name="Tinggi", default=1.0, min=0.05, max=4.0, unit="LENGTH",
                                    description="Tinggi model setelah 'Pas-kan'. Slime pemain = 1.0 m (24 px)")
    outline: bpy.props.BoolProperty(name="Outline gelap", default=True,
                                    description="Mode warna flat: outline 1px versi gelap warna objek")
    palette: bpy.props.StringProperty(name="Palet", subtype="FILE_PATH", default="",
                                      description="Opsional, mode warna flat: kunci warna ke palet .hex (mis. art/palette.hex)")
    normal_pad: bpy.props.IntProperty(name="Pad normal", default=1, min=0, max=3)
    rebake_physics: bpy.props.BoolProperty(
        name="Bake ulang physics per arah", default=True,
        description="Soft body/cloth/partikel di-bake ulang tiap arah (cache bake tidak ikut rotasi Model_Root). "
                    "Tanpa ini side/back sama dengan front. Tidak berpengaruh kalau model tanpa physics")
    out_raw: bpy.props.StringProperty(name="Raw", subtype="DIR_PATH", default="//../renders/raw/")
    out_sprites: bpy.props.StringProperty(name="Sprite", subtype="DIR_PATH", default="//../sprites/")


class GN_OT_setup(bpy.types.Operator):
    bl_idname = "goonapped.setup"
    bl_label = "Setup / reset template"
    bl_description = "Pasang kamera, resolusi 48px, color management Standard, koleksi MODEL_Body/MODEL_Face"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        setup_scene(context.scene)
        self.report({"INFO"}, "Template siap: kamera ortho 2.0, 48x48, EEVEE, Standard")
        return {"FINISHED"}


class GN_OT_assign(bpy.types.Operator):
    bl_idname = "goonapped.assign"
    bl_label = "Masukkan ke model"
    bl_description = "Pindahkan objek terpilih (beserta anaknya) ke koleksi ini dan parent ke Model_Root"
    bl_options = {"REGISTER", "UNDO"}
    target: bpy.props.EnumProperty(items=[(COL_BODY, "Body", ""), (COL_FACE, "Face", "")])

    def execute(self, context):
        n = assign_objects(context, self.target)
        if not n:
            self.report({"WARNING"}, "Pilih objek model dulu")
            return {"CANCELLED"}
        self.report({"INFO"}, f"{n} objek masuk {self.target}")
        return {"FINISHED"}


class GN_OT_fit(bpy.types.Operator):
    bl_idname = "goonapped.fit"
    bl_label = "Pas-kan ukuran & posisi"
    bl_description = "Model berdiri di lantai (Z=0), di tengah, dengan tinggi sesuai isian"
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        if not fit_model(context, context.scene.gn_sprite.height):
            self.report({"WARNING"}, "MODEL_Body/MODEL_Face kosong")
            return {"CANCELLED"}
        return {"FINISHED"}


class GN_OT_render(bpy.types.Operator):
    bl_idname = "goonapped.render"
    bl_label = "Render"
    bl_description = "Render semua layer x arah x frame, lalu pixelize jadi sheet"
    test: bpy.props.BoolProperty(default=False)

    def execute(self, context):
        try:
            n, spr, pre = render_sprites(context, self.test)
        except ValueError as e:
            self.report({"ERROR"}, str(e))
            return {"CANCELLED"}
        shown = _show_image(context, pre)
        self.report({"INFO"}, f"{n} frame dirender. Sheet di {spr}" + ("" if shown else f". Preview: {pre}"))
        return {"FINISHED"}


class GN_PT_panel(bpy.types.Panel):
    bl_label = "GooNapped Sprite"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "GooNapped"

    def draw(self, context):
        st = context.scene.gn_sprite
        lay = self.layout
        lay.operator("goonapped.setup", icon="CAMERA_DATA")

        box = lay.box()
        box.label(text="1. Model", icon="MESH_MONKEY")
        row = box.row(align=True)
        row.operator("goonapped.assign", text="Jadikan Body").target = COL_BODY
        row.operator("goonapped.assign", text="Jadikan Face").target = COL_FACE
        row = box.row(align=True)
        row.prop(st, "height")
        row.operator("goonapped.fit", text="Pas-kan", icon="FULLSCREEN_EXIT")
        nb, nf = len(_objects(COL_BODY)), len(_objects(COL_FACE))
        box.label(text=f"Body: {nb} objek   Face: {nf} objek")

        box = lay.box()
        box.label(text="2. Aset", icon="IMAGE_DATA")
        box.prop(st, "name")
        box.prop(st, "mode", text="")
        if st.mode == "ALBEDO":
            box.prop(st, "outline")
            box.prop(st, "palette")
        box.prop(st, "anims")
        row = box.row(align=True)
        row.prop(st, "dir_front", toggle=True)
        row.prop(st, "dir_side", toggle=True)
        row.prop(st, "dir_back", toggle=True)

        box = lay.box()
        box.label(text="3. Render", icon="RENDER_STILL")
        col = box.column(align=True)
        col.prop(st, "out_raw")
        col.prop(st, "out_sprites")
        col.prop(st, "normal_pad")
        if _physics_caches():
            box.prop(st, "rebake_physics")
        row = box.row(align=True)
        row.scale_y = 1.4
        row.operator("goonapped.render", text="Tes frame ini", icon="VIEWZOOM").test = True
        row.operator("goonapped.render", text="Render semua", icon="RENDER_ANIMATION").test = False


classes = (GN_Settings, GN_OT_setup, GN_OT_assign, GN_OT_fit, GN_OT_render, GN_PT_panel)


def register():
    for cls in classes:                       # aman dijalankan ulang (Run Script berkali-kali)
        old = getattr(bpy.types, cls.__name__, None)
        if old is not None:
            try:
                bpy.utils.unregister_class(old)
            except RuntimeError:
                pass
        bpy.utils.register_class(cls)
    bpy.types.Scene.gn_sprite = bpy.props.PointerProperty(type=GN_Settings)


def unregister():
    if hasattr(bpy.types.Scene, "gn_sprite"):
        del bpy.types.Scene.gn_sprite
    for cls in reversed(classes):
        try:
            bpy.utils.unregister_class(cls)
        except RuntimeError:
            pass


if __name__ == "__main__" or __name__.endswith(".py"):
    register()
