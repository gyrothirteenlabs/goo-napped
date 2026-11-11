using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Peti dorong (desain-v2 bagian 10). Didorong satu tile per dorongan (seperti Sokoban, mudah ditebak untuk puzzle),
    // hanya oleh tubuh massa >= 2 yang menekannya terus ke satu arah. Menutup pandangan, menekan pelat, menghalangi jalan
    // penjaga (sel peti diblok di peta A*). Gesekannya berisik. Pivot di tepi bawah tile, collider di tengah tile.
    [RequireComponent(typeof(BoxCollider2D))]
    public class PushCrate : MonoBehaviour
    {
        [Tooltip("Massa minimum slime untuk mendorong.")]
        [SerializeField, Range(1, 4)] private int minMass = 2;
        [Tooltip("Lama menekan sebelum peti bergeser (detik).")]
        [SerializeField] private float pushDelay = 0.25f;
        [SerializeField] private float slideTime = 0.3f;
        [SerializeField] private float noiseRadius = 2.5f;

        public static readonly List<PushCrate> All = new List<PushCrate>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private BoxCollider2D box;
        private Vector2Int cell;
        private float pressTime;
        private Vector2Int pressDir;
        private bool sliding;
        private float hintCooldown;

        public Vector2 Center => transform.TransformPoint(box.offset);
        public bool IsSliding => sliding;

        private void Awake() => box = GetComponent<BoxCollider2D>();

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            if (box == null) box = GetComponent<BoxCollider2D>();
            cell = NavGrid.CellOf(Center);
            NavGrid.SetBlocked(cell, true);
        }

        private void OnDisable()
        {
            All.Remove(this);
            NavGrid.SetBlocked(cell, false);
        }

        private void Update() { if (hintCooldown > 0f) hintCooldown -= Time.deltaTime; }

        private void OnCollisionStay2D(Collision2D c)
        {
            if (sliding) return;
            var p = c.collider.GetComponentInParent<SlimePiece>();
            if (p == null || !p.IsControlled || p.Motor.MoveInput.sqrMagnitude < 0.25f) { pressTime = 0f; return; }
            // arah dorong = arah input yang menuju peti (sumbu dominan)
            Vector2 toCrate = Center - p.Center;
            Vector2Int dir = Mathf.Abs(toCrate.x) > Mathf.Abs(toCrate.y)
                ? new Vector2Int(toCrate.x > 0 ? 1 : -1, 0) : new Vector2Int(0, toCrate.y > 0 ? 1 : -1);
            if (Vector2.Dot(p.Motor.MoveInput.normalized, dir) < 0.6f) { pressTime = 0f; return; }
            if (p.Body.State.Mass < minMass)
            {
                if (hintCooldown <= 0f)
                {
                    var brain = FindFirstObjectByType<PlayerBrain>();
                    if (brain != null) brain.Notify("Too light to push this crate. You need size " + minMass + "+.");
                    hintCooldown = 3f;
                }
                return;
            }
            if (dir != pressDir) { pressDir = dir; pressTime = 0f; }
            pressTime += Time.fixedDeltaTime;
            if (pressTime >= pushDelay && CanMoveTo(cell + dir)) StartCoroutine(Slide(dir));
        }

        private bool CanMoveTo(Vector2Int target)
        {
            Vector2 c = new Vector2(target.x + 0.5f, target.y + 0.5f);
            if (NavGrid.Available && !NavGrid.Walkable(target)) return false;
            foreach (var h in Physics2D.OverlapBoxAll(c, box.size * 0.9f, 0f))
            {
                if (h == null || h == box || h.isTrigger) continue;
                return false;   // tembok, barang, slime, penjaga
            }
            return true;
        }

        private IEnumerator Slide(Vector2Int dir)
        {
            sliding = true;
            pressTime = 0f;
            NavGrid.SetBlocked(cell, false);
            cell += dir;
            NavGrid.SetBlocked(cell, true);
            AudioDirector.Play(AudioDirector.Sfx.GolemStep, Center, 0.6f, 0.2f);
            NoiseBus.Emit(Center, noiseRadius);
            Vector3 from = transform.position, to = from + new Vector3(dir.x, dir.y, 0f);
            for (float t = 0f; t < slideTime; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(from, to, t / slideTime);
                yield return null;
            }
            transform.position = to;
            Physics2D.SyncTransforms();
            sliding = false;
        }
    }
}
