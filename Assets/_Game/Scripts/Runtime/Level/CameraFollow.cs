using UnityEngine;

namespace GooNapped
{
    // Kamera mengikuti potongan slime yang sedang dikendalikan (pindah halus saat Shift),
    // dijepit supaya tidak keluar dari batas level.
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private PlayerBrain brain;
        [Tooltip("Batas level (BoxCollider2D trigger / collider apa saja). Kosong = tanpa batas.")]
        [SerializeField] private Collider2D bounds;
        [Tooltip("Detik untuk mengejar target. Kecil = kaku, besar = melayang.")]
        [SerializeField] private float smoothTime = 0.25f;
        [Tooltip("Geser bidikan dari pusat potongan (unit).")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 0.3f);

        private Camera cam;
        private Vector3 velocity;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (brain == null) brain = FindFirstObjectByType<PlayerBrain>();
        }

        private void Start() => Snap();

        public void Snap()
        {
            var t = Target();
            if (t.HasValue) transform.position = t.Value;
            velocity = Vector3.zero;
        }

        private Vector3? Target()
        {
            var a = brain != null ? brain.Active : null;
            if (a == null) return null;
            var p = (Vector2)a.transform.position + offset;
            return Clamp(new Vector3(p.x, p.y, transform.position.z));
        }

        private Vector3 Clamp(Vector3 p)
        {
            if (bounds == null || cam == null || !cam.orthographic) return p;
            var b = bounds.bounds;
            float h = cam.orthographicSize, w = h * cam.aspect;
            p.x = b.size.x <= w * 2f ? b.center.x : Mathf.Clamp(p.x, b.min.x + w, b.max.x - w);
            p.y = b.size.y <= h * 2f ? b.center.y : Mathf.Clamp(p.y, b.min.y + h, b.max.y - h);
            return p;
        }

        private void LateUpdate()
        {
            var t = Target();
            if (t.HasValue) transform.position = Vector3.SmoothDamp(transform.position, t.Value, ref velocity, smoothTime);
        }
    }
}
