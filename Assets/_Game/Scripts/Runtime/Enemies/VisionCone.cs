using UnityEngine;

namespace GooNapped
{
    // Gambar kerucut pandang + lingkar radius sadar penjaga (mesh, terpotong dinding).
    // Objek terpisah dari penjaga supaya tidak ikut SortingGroup: selalu di atas lantai, di bawah karakter.
    // Sengaja samar: cukup terbaca di lantai gelap, tidak mencolok.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VisionCone : MonoBehaviour
    {
        [SerializeField, Range(4, 64)] private int segments = 24;
        [SerializeField, Range(8, 64)] private int ringSegments = 32;
        [SerializeField] private Color calmColor = new Color(1f, 0.3f, 0.25f, 0.09f);
        [SerializeField] private Color alertColor = new Color(1f, 0.15f, 0.1f, 0.32f);
        [Tooltip("Alpha lingkar radius sadar, relatif ke warna kerucut.")]
        [SerializeField, Range(0f, 1f)] private float ringAlpha = 0.45f;

        private Mesh mesh;
        private Vector3[] verts;
        private Color[] colors;
        private int[] tris;

        public static VisionCone Create(string name, Material material, int sortingOrder)
        {
            var go = new GameObject(name);
            var cone = go.AddComponent<VisionCone>();
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.sortingOrder = sortingOrder;
            return cone;
        }

        private void Awake() => Build();

        private void Build()
        {
            mesh = new Mesh { name = "VisionCone" };
            mesh.MarkDynamic();
            int nv = (segments + 2) + (ringSegments * 2);
            verts = new Vector3[nv];
            colors = new Color[nv];
            tris = new int[segments * 3 + ringSegments * 6];
            for (int i = 0; i < segments; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 2;
                tris[i * 3 + 2] = i + 1;
            }
            // lingkar tipis (cincin), bukan piringan penuh, supaya tidak menutupi lantai
            int rv = segments + 2, rt = segments * 3;
            for (int i = 0; i < ringSegments; i++)
            {
                int a = rv + i * 2, b = rv + ((i + 1) % ringSegments) * 2;
                tris[rt + i * 6] = a; tris[rt + i * 6 + 1] = b; tris[rt + i * 6 + 2] = a + 1;
                tris[rt + i * 6 + 3] = b; tris[rt + i * 6 + 4] = b + 1; tris[rt + i * 6 + 5] = a + 1;
            }
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        // clip(from, delta) -> jarak tembus maksimum (dinding). alert 0..1 = tingkat curiga.
        public void Draw(Vector2 origin, Vector2 forward, float range, float fullAngle, float alert,
            System.Func<Vector2, Vector2, float> clip, Vector2 ringCenter = default, float ringRadius = 0f)
        {
            if (mesh == null || verts.Length != (segments + 2) + ringSegments * 2) Build();
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            var col = Color.Lerp(calmColor, alertColor, alert);
            float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            verts[0] = origin;
            colors[0] = col;
            for (int i = 0; i <= segments; i++)
            {
                float a = (baseAngle - fullAngle * 0.5f + fullAngle * i / segments) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float len = clip != null ? clip(origin, dir * range) : range;
                verts[i + 1] = origin + dir * len;
                colors[i + 1] = new Color(col.r, col.g, col.b, 0f);   // pudar habis ke ujung
            }
            int rv = segments + 2;
            var rc = new Color(col.r, col.g, col.b, ringRadius > 0f ? col.a * ringAlpha : 0f);
            for (int i = 0; i < ringSegments; i++)
            {
                float a = i * Mathf.PI * 2f / ringSegments;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                verts[rv + i * 2] = ringCenter + dir * ringRadius;
                verts[rv + i * 2 + 1] = ringCenter + dir * Mathf.Max(0f, ringRadius - 0.06f);
                colors[rv + i * 2] = rc;
                colors[rv + i * 2 + 1] = rc;
            }
            mesh.Clear();
            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
        }

        public void Hide()
        {
            if (mesh != null) mesh.Clear();
        }
    }
}
