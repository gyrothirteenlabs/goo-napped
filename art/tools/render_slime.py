"""Render semua animasi slime pemain (layer body + face, 3 arah) ke art/renders/raw/slime.

Jalankan di Blender (file art/blender/slime_v06.blend atau lebih baru), misal lewat Blender MCP:
    exec(open(r'<repo>/art/tools/render_slime.py').read()); render_anims(['idle'])
Tanpa argumen = semua anim. Setelah itu jalankan pixelize (lihat bawah file).

Aturan (art/CLAUDE.md + skill sprite-render):
- body: objek face disembunyikan.
- face: body (dan piece) jadi holdout, supaya mata yang ketutup body tidak ikut.
- tampak belakang: layer face dikosongkan (semua objek face disembunyikan).
- arah diatur lewat rotasi Slime_Root (front 0, side 90 = hadap kanan, back 180).
"""
import bpy, math, os

RAW = os.path.join(os.path.dirname(bpy.data.filepath), '..', 'renders', 'raw', 'slime')
DIRS = {'front': 0, 'side': 90, 'back': 180}
FACE = ('Slime_EyeL', 'Slime_EyeR', 'Slime_GlintL', 'Slime_GlintR', 'Slime_Mouth', 'Slime_Shine')
PIECE_FACE = ('Slime_PieceEyeL', 'Slime_PieceEyeR')

# anim -> (action shape key lattice, frame yang dirender, pakai piece?)
ANIMS = {
    'idle':  ('idle',  range(0, 4), False),
    'move':  ('move',  range(0, 6), False),
    'eat':   ('eat',   range(0, 5), False),
    'split': ('split', range(0, 6), True),
    'merge': ('merge', range(0, 5), True),
    'jelly': ('jelly', range(6, 39, 2), False),   # 17 frame, setengah rate; tanpa translasi root
}


def _ad(idb):
    if idb.animation_data is None:
        idb.animation_data_create()
    return idb.animation_data


def render_anims(names=None):
    s = bpy.context.scene
    ob = bpy.data.objects
    root, body, pb = ob['Slime_Root'], ob['Slime_Body'], ob['Slime_PieceBody']
    key = ob['Slime_Lattice'].data.shape_keys
    face = [ob[n] for n in FACE]
    pface = [ob[n] for n in PIECE_FACE]
    raw = os.path.normpath(RAW)
    os.makedirs(raw, exist_ok=True)

    saved = dict(frame=s.frame_current, fs=s.frame_start, fe=s.frame_end, rot=tuple(root.rotation_euler),
                 key=key.animation_data.action if key.animation_data else None,
                 root=root.animation_data.action if root.animation_data else None)
    cnt = 0
    try:
        for anim in (names or ANIMS):
            act, frames, piece = ANIMS[anim]
            _ad(key).action = bpy.data.actions[act]
            _ad(root).action = None
            root.location = (0, 0, 0)
            if piece:
                _ad(body).action = bpy.data.actions[anim + '_body']
                _ad(pb).action = bpy.data.actions[anim + '_piece']
            pb.hide_render = not piece
            for d, ang in DIRS.items():
                root.rotation_euler = (0, 0, math.radians(ang))
                for layer in ('body', 'face'):
                    is_face = layer == 'face'
                    body.is_holdout = pb.is_holdout = is_face
                    show_face = is_face and d != 'back'
                    for o in face:
                        o.hide_render = not show_face
                    for o in pface:
                        o.hide_render = not (show_face and piece)
                    for i, f in enumerate(frames):
                        s.frame_set(f)
                        s.render.filepath = os.path.join(raw, f'slime_{layer}_{anim}_{d}_{i:02d}.png')
                        bpy.ops.render.render(write_still=True)
                        cnt += 1
    finally:
        # kembalikan ke kondisi rest
        body.is_holdout = pb.is_holdout = False
        if body.animation_data: body.animation_data.action = None
        if pb.animation_data: pb.animation_data.action = None
        body.location = (0, 0, 0); body.scale = (1, 1, 1)
        pb.location = (0, 0, 0); pb.scale = (0, 0, 0)
        pb.hide_render = True
        for o in face: o.hide_render = False
        for o in pface: o.hide_render = True
        root.rotation_euler = saved['rot']
        _ad(key).action = saved['key']
        _ad(root).action = saved['root']
        s.frame_start, s.frame_end = saved['fs'], saved['fe']
        s.frame_set(saved['frame'])
    print('rendered', cnt, 'to', raw)
    return cnt

# Pixelize (dari root repo; body dan face harus di folder input terpisah):
#   pixelize.py <in_body> art/sprites/slime --mode gray --levels 4 --outline 282828 --sheet
#   pixelize.py <in_face> art/sprites/slime --mode none --outline none --sheet
