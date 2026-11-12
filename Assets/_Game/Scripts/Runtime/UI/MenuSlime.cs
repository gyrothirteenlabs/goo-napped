using UnityEngine;

namespace GooNapped
{
    // Slime di latar main menu (hanya tampilan, tanpa PlayerBrain): diam bernapas, sesekali melompat kecil
    // ke kiri/kanan di sekitar titik awal dan menoleh ke toples. Saat permainan dimulai, melompat ke arah toples.
    [RequireComponent(typeof(SlimePiece))]
    public class MenuSlime : MonoBehaviour
    {
        [SerializeField] private float wanderRadius = 0.7f;
        [SerializeField] private Vector2 restTime = new Vector2(3f, 6f);

        public static MenuSlime Instance { get; private set; }

        private SlimePiece piece;
        private Vector2 home, target;
        private float timer;
        private bool moving, leaving;

        private void Awake() { Instance = this; piece = GetComponent<SlimePiece>(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            home = transform.position;
            timer = Random.Range(1.5f, 3f);
            piece.SetControlled(true);   // tanpa PlayerBrain: potongan harus "dikendalikan" supaya motor jalan
            piece.Face(Vector2Int.down);
        }

        // Permainan dimulai: melompat ke toples di kanan.
        public void Leave() { leaving = true; moving = true; target = home + new Vector2(4f, 0.3f); }

        private void Update()
        {
            if (moving)
            {
                Vector2 d = target - piece.Center;
                if (d.magnitude < 0.12f && !leaving)
                {
                    moving = false;
                    piece.Move(Vector2.zero);
                    timer = Random.Range(restTime.x, restTime.y);
                    piece.Face(Random.value < 0.6f ? Vector2Int.right : Vector2Int.down);
                    return;
                }
                piece.Move(d.normalized * (leaving ? 1f : 0.45f));
                return;
            }
            piece.Move(Vector2.zero);
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            float x = Random.Range(-wanderRadius, wanderRadius);
            target = home + new Vector2(x, Random.Range(-0.15f, 0.15f));
            moving = true;
        }
    }
}
