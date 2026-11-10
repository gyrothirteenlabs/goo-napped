using UnityEngine;

namespace GooNapped
{
    // Gambar kerucut pandang penjaga (mesh, terpotong dinding). Objek terpisah dari penjaga
    // supaya tidak ikut SortingGroup penjaga: kerucut selalu di atas lantai, di bawah karakter.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VisionCone : MonoBehaviour
    {
        [SerializeField, Range(4, 64)] private int segments = 24;
        [SerializeField] private Color calmColor = new Color(1f, 0.25f, 0.25f, 0.22f);
        [SerializeField] private Color alertColor = new Color(1f, 0.15f, 0.1f, 0.55f);

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
            verts = new Vector3[segments + 2];
            colors = new Color[segments + 2];
            tris = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 2;
                tris[i * 3 + 2] = i + 1;
            }
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        // blocked(from, to) -> jarak tembus maksimum (dinding). alert 0..1 = tingkat curiga.
        public void Draw(Vector2 origin, Vector2 forward, float range, float fullAngle, float alert,
            System.Func<Vector2, Vector2, float> clip)
        {
            if (mesh == null || verts.Length != segments + 2) Build();
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
                colors[i + 1] = new Color(col.r, col.g, col.b, col.a * 0.35f);   // pudar ke ujung
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
