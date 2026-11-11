using UnityEngine;

namespace GooNapped
{
    // Satu potongan slime: badan yang bisa digerakkan. TIDAK membaca input.
    // Input ada di PlayerBrain, yang pindah antar potongan (tombol Shift).
    // v2: potongan yang ditinggal diam di tempat dan TETAP bisa terlihat penjaga (nyamar bebas dihapus, desain-v2 bagian 4).
    // Sembunyi hanya di objek (HidingSpot): masuk/keluar butuh waktu, selama itu masih bisa terlihat.
    [RequireComponent(typeof(SlimeMotor), typeof(SlimeBody), typeof(SlimeSpriteAnimator))]
    [DefaultExecutionOrder(10)]   // setelah SlimeMotor: animasi khusus menahan pegas jeli setelah gerak menulisnya
    public class SlimePiece : MonoBehaviour
    {
        [Header("Sprite")]
        [Tooltip("Pakai frame sprite 'move' saat melompat. Matikan selama frame sprite masih punya squash sendiri " +
                 "(dobel dengan squash engine = janky). Nyalakan setelah sprite final dirender sesuai panduan-blender-3d C.1.")]
        [SerializeField] private bool spriteMoveFrames;

        [Header("Makan")]
        [Tooltip("Jarak maksimum ke esens (dari pusat collider).")]
        [SerializeField] private float eatRadius = 0.7f;
        [Tooltip("Lama animasi makan. Selama itu potongan diam (risiko ketahuan).")]
        [SerializeField] private float eatDuration = 0.85f;
        [SerializeField] private LayerMask pickupMask = ~0;

        [Header("Muntah")]
        [Tooltip("Prefab esens yang muncul saat esens terakhir dimuntahkan.")]
        [SerializeField] private IngredientPickup pickupPrefab;
        [SerializeField] private float vomitDuration = 0.6f;

        [Header("Sembunyi (desain-v2 bagian 7)")]
        [Tooltip("Lama masuk ke tempat sembunyi (detik). Selama itu masih terlihat penjaga.")]
        [SerializeField] private float enterTime = 0.35f;
        [Tooltip("Lama keluar dari tempat sembunyi (detik).")]
        [SerializeField] private float exitTime = 0.3f;

        private SlimeSpriteAnimator anim;
        private Collider2D col;
        private Rigidbody2D rb;
        private float eatTimer;
        private bool hideTransition;
        private readonly Collider2D[] hits = new Collider2D[16];

        public SlimeMotor Motor { get; private set; }
        public SlimeBody Body { get; private set; }
        // Arah hadap (4 arah, untuk sprite dan arah belah).
        public Vector2Int Facing { get; private set; } = Vector2Int.down;
        public bool IsControlled { get; private set; }
        // Tempat sembunyi yang sedang dipakai (juga selama masuk/keluar), atau null.
        public HidingSpot HidingIn { get; private set; }
        // Benar-benar tersembunyi (sudah di dalam, tidak sedang masuk/keluar): penjaga tidak melihatnya.
        public bool IsHidden => HidingIn != null && !hideTransition;
        // Sedang masuk/keluar tempat sembunyi: tidak bisa bergerak, masih terlihat.
        public bool IsTransforming => hideTransition;
        public bool IsEating => eatTimer > 0f;
        // Massa tubuh (desain-v2 bagian 5): 1 = kecil, maks 4.
        public int Mass => Body.State.Mass;
        public Vector2 Center => col != null && col.enabled ? (Vector2)col.bounds.center
                                : (Vector2)transform.position + Vector2.up * 0.22f * transform.localScale.y;
        public float Radius => col != null && col.enabled ? col.bounds.extents.x : 0.28f * transform.localScale.x;
        public Collider2D Collider => col;

        private void Awake()
        {
            Motor = GetComponent<SlimeMotor>();
            Body = GetComponent<SlimeBody>();
            anim = GetComponent<SlimeSpriteAnimator>();
            col = GetComponent<Collider2D>();
            rb = GetComponent<Rigidbody2D>();
            Body.Changed += OnBodyChanged;
            Motor.PhaseEntered += OnHopPhase;
            // sisa v1: anak "Disguise" (barang samaran) tidak dipakai lagi
            var disguise = transform.Find("Disguise");
            if (disguise != null && disguise.TryGetComponent<SpriteRenderer>(out var dr)) dr.enabled = false;
        }

        private void OnDestroy()
        {
            if (Body != null) Body.Changed -= OnBodyChanged;
            if (Motor != null) Motor.PhaseEntered -= OnHopPhase;
            if (HidingIn != null) HidingIn.Leave(this);
        }

        // suara "plek" lembut tiap mendarat. Tubuh berat/besar mendarat "dug": terdengar penjaga tembus tembok.
        private void OnHopPhase(SlimeMotor.HopPhase p)
        {
            if (p != SlimeMotor.HopPhase.Land) return;
            float noise = Body.State.StepNoiseRadius;
            if (noise > 0f)
            {
                AudioDirector.Play(AudioDirector.Sfx.GolemStep, Center, 0.35f, 0.15f);
                if (IsControlled) NoiseBus.Emit(Center, noise);
            }
            else AudioDirector.Play(AudioDirector.Sfx.SlimeStep, Center, 0.45f, 0.12f);
        }

        // Massa dan esens menentukan kecepatan dan ukuran badan (desain-v2 bagian 5). Ukuran berubah halus.
        private float targetScale = -1f;

        private void OnBodyChanged(SlimeBody b)
        {
            Motor.SpeedMultiplier = b.State.SpeedMultiplier;
            targetScale = SlimeBodyState.MassScale(b.State.Mass);
        }

        private void Start()
        {
            if (targetScale < 0f) OnBodyChanged(Body);
            transform.localScale = new Vector3(targetScale, targetScale, 1f);
        }

        private void UpdateScale()
        {
            if (targetScale <= 0f) return;
            float s = Mathf.MoveTowards(transform.localScale.x, targetScale, Time.deltaTime * 1.6f);
            if (!Mathf.Approximately(s, transform.localScale.x)) transform.localScale = new Vector3(s, s, 1f);
        }

        public void SetControlled(bool on)
        {
            IsControlled = on;
            if (HidingIn != null) { Motor.Stop(); return; }   // di dalam tempat sembunyi: tetap diam, fisika mati
            Motor.SetFrozen(!on);
        }

        public void Face(Vector2Int dir)
        {
            if (dir != Vector2Int.zero) Facing = dir;
        }

        // Dipanggil PlayerBrain tiap frame untuk potongan yang dikendalikan.
        public void Move(Vector2 input)
        {
            if (!IsControlled || IsEating || IsTransforming || HidingIn != null) { Motor.Stop(); return; }
            Motor.MoveInput = input;
            if (input.sqrMagnitude < 0.04f) return;
            Face(Mathf.Abs(input.x) > Mathf.Abs(input.y)
                ? new Vector2Int(input.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, input.y > 0 ? 1 : -1));
        }

        // Esens terdekat dalam jangkauan, atau null.
        public IngredientPickup FindPickup()
        {
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = pickupMask };
            int n = Physics2D.OverlapCircle(Center, eatRadius, filter, hits);
            IngredientPickup best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var p = hits[i].GetComponentInParent<IngredientPickup>();
                if (p == null || p.Ingredient == null) continue;
                float d = ((Vector2)hits[i].bounds.center - Center).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        // Makan esens terdekat. false kalau tidak ada esens dalam jangkauan, perut penuh, atau sedang sembunyi.
        public bool TryEat()
        {
            if (IsEating || HidingIn != null || !Body.State.CanEat) return false;
            var p = FindPickup();
            if (p == null) return false;
            Body.Eat(p.Consume());
            eatTimer = eatDuration;
            Motor.Stop();
            if (Jelly != null) Jelly.Kick(6f);                // mulut membuka
            AudioDirector.Play(AudioDirector.Sfx.Eat, Center);
            return true;
        }

        // Tubuh berbau meninggalkan jejak (diikuti Anjing Lendir): tiap berpindah ±0.4 unit, atau tiap 1.2 detik kalau diam.
        // Tetap berbau walau sembunyi: anjing bisa menemukan tempat sembunyi slime berbau.
        private Vector2 lastScent;
        private float scentTimer;

        private void DropScent()
        {
            float r = Body.State.SmellRadius;
            if (r <= 0f || animating) return;
            scentTimer -= Time.deltaTime;
            if (Vector2.Distance(Center, lastScent) < 0.4f && scentTimer > 0f) return;
            ScentTrail.Drop(Center, r);
            lastScent = Center;
            scentTimer = 1.2f;
        }

        // Muntahkan esens terakhir ke lantai di depan (koreksi urutan makan). Massa -1, minimal tersisa 1.
        public bool TryVomit()
        {
            if (IsEating || IsTransforming || HidingIn != null || !Body.State.CanVomit) return false;
            var ing = Body.Vomit();
            if (ing == null) return false;
            eatTimer = vomitDuration;
            Motor.Stop();
            if (Jelly != null) Jelly.Kick(8f);
            if (pickupPrefab != null)
            {
                var pos = transform.position + (Vector3)(Vector2)Facing * 0.6f;
                var p = Instantiate(pickupPrefab, pos, Quaternion.identity);
                p.Init(ing);
            }
            AudioDirector.Play(AudioDirector.Sfx.Vomit, Center);
            return true;
        }

        // ---- tempat sembunyi ----

        // Masuk ke tempat sembunyi: menyusut masuk (enterTime), lalu tidak terlihat. false kalau penuh/sedang sibuk.
        public bool EnterHiding(HidingSpot spot)
        {
            if (spot == null || HidingIn != null || IsEating || animating || !spot.HasRoom()) return false;
            HidingIn = spot;
            spot.Enter(this);
            StartCoroutine(EnterRoutine(spot));
            return true;
        }

        private System.Collections.IEnumerator EnterRoutine(HidingSpot spot)
        {
            hideTransition = true;
            Motor.Stop();
            Vector3 from = transform.position, to = spot.transform.position;
            enteredFrom = from;
            if (col != null) col.enabled = false;
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
            AudioDirector.Play(AudioDirector.Sfx.Disguise, Center, 0.6f);
            var jelly = Jelly;
            for (float t = 0f; t < enterTime; t += Time.deltaTime)
            {
                float k = t / enterTime;
                transform.position = Vector3.Lerp(from, to, k * k);
                if (jelly != null) { jelly.Hold(0.25f * Mathf.Sin(k * Mathf.PI)); jelly.Size = 1f - 0.7f * k; jelly.Lift = 0.15f * Mathf.Sin(k * Mathf.PI); }
                yield return null;
            }
            transform.position = to;
            if (jelly != null) { jelly.Release(); jelly.Lift = 0f; }
            SetVisible(false);
            hideTransition = false;
        }

        private Vector3 enteredFrom;

        // Keluar dari tempat sembunyi ke sisi tempat masuk (atau sisi lain yang kosong).
        public bool ExitHiding()
        {
            if (HidingIn == null || hideTransition) return false;
            StartCoroutine(ExitRoutine(HidingIn));
            return true;
        }

        private System.Collections.IEnumerator ExitRoutine(HidingSpot spot)
        {
            hideTransition = true;
            Vector3 from = transform.position;
            Vector3 to = spot.ExitPoint(enteredFrom, Radius);
            SetVisible(true);
            AudioDirector.Play(AudioDirector.Sfx.Reveal, Center, 0.6f);
            var jelly = Jelly;
            for (float t = 0f; t < exitTime; t += Time.deltaTime)
            {
                float k = t / exitTime;
                transform.position = Vector3.Lerp(from, to, 1f - (1f - k) * (1f - k));
                if (jelly != null) { jelly.Size = 0.3f + 0.7f * k; jelly.Lift = 0.15f * Mathf.Sin(k * Mathf.PI); jelly.Hold(0.2f * (1f - k)); }
                yield return null;
            }
            transform.position = to;
            if (rb != null) { rb.position = to; rb.simulated = true; }
            if (col != null) col.enabled = true;
            if (jelly != null) { jelly.Release(); jelly.Size = 1f; jelly.Lift = 0f; jelly.Kick(-5f); }
            spot.Leave(this);
            HidingIn = null;
            hideTransition = false;
            Motor.SetFrozen(!IsControlled);
        }

        private void SetVisible(bool on)
        {
            if (anim.bodyRenderer != null) anim.bodyRenderer.enabled = on;
            if (anim.faceRenderer != null) anim.faceRenderer.enabled = on;
            var shadow = transform.Find("Shadow");
            if (shadow != null && shadow.TryGetComponent<SpriteRenderer>(out var sr)) sr.enabled = on;
        }

        private void Update()
        {
            if (eatTimer > 0f)
            {
                eatTimer -= Time.deltaTime;
                // menelan: badan berdenyut dua kali
                if (Jelly != null) Jelly.Hold(eatTimer > 0f ? 0.12f * Mathf.Sin((1f - eatTimer / eatDuration) * Mathf.PI * 4f) : 0f);
                if (eatTimer <= 0f && Jelly != null) Jelly.Release();
            }
            DropScent();
            UpdateScale();
            if (HidingIn != null && !hideTransition) return;
            string dir = Facing.x != 0 ? "side" : (Facing.y > 0 ? "back" : "front");
            bool left = Facing.x != 0 ? Facing.x < 0 : anim.faceLeft;
            bool moving = IsControlled && !IsEating && Motor.IsHopping;
            anim.speedScale = 1f;
            string a = animating ? "move" : IsEating ? "eat" : moving ? "move" : "idle";
            if (a == "move" && !spriteMoveFrames)
            {
                // frame "move" lama punya squash sendiri (dobel dengan squash engine): pakai frame diam pertama,
                // semua bentuk lompatan dari SlimeJelly
                a = "idle";
                anim.forcedPhase = 0f;
            }
            // frame sprite "move" mengikuti fase lompatan (bukan jam), jadi squash di sprite sinkron dengan lompatan
            else anim.forcedPhase = a == "move" && moving ? Motor.CyclePhase : -1f;
            anim.Play(a, dir, left);
        }

        // ---- animasi belah / gabung ----
        private bool animating;

        // Potongan baru melompat keluar dari induk ke tempat mendaratnya (busur).
        public void PlaySplitArc(Vector3 from, Vector3 to, float duration = 0.32f)
        {
            StopAllCoroutines();
            StartCoroutine(SplitArc(from, to, duration));
        }

        private System.Collections.IEnumerator SplitArc(Vector3 from, Vector3 to, float duration)
        {
            animating = true;
            if (col != null) col.enabled = false;            // jangan mendorong induk saat keluar dari badannya
            var jelly = Jelly;
            if (jelly != null) { jelly.Size = 0.55f; jelly.Kick(6f); }
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float e = 1f - (1f - k) * (1f - k);          // cepat keluar, pelan mendarat
                var p = Vector3.Lerp(from, to, e);
                transform.position = p;
                if (rb != null) rb.position = p;
                if (jelly != null)
                {
                    jelly.Lift = 0.28f * Mathf.Sin(k * Mathf.PI);
                    jelly.Size = Mathf.Lerp(0.55f, 1f, e);
                    jelly.Target = 0.2f * (1f - k);
                }
                yield return null;
            }
            transform.position = to;
            if (rb != null) rb.position = to;
            if (jelly != null) { jelly.Lift = 0f; jelly.Size = 1f; jelly.Target = 0f; jelly.Kick(-8f); }
            if (col != null) col.enabled = true;
            yield return new WaitForSeconds(0.12f);           // goyang sebentar setelah mendarat
            animating = false;
        }

        // Potongan ini tersedot ke potongan lain (gabung), lalu hilang.
        public void PlayAbsorbInto(SlimePiece target, float duration = 0.2f)
        {
            StopAllCoroutines();
            StartCoroutine(AbsorbInto(target, duration));
        }

        private System.Collections.IEnumerator AbsorbInto(SlimePiece target, float duration)
        {
            animating = true;
            if (col != null) col.enabled = false;
            if (rb != null) rb.simulated = false;
            var jelly = Jelly;
            var from = transform.position;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                var to = target != null ? target.transform.position : from;
                transform.position = Vector3.Lerp(from, to, k * k);
                if (jelly != null) { jelly.Size = 1f - k; jelly.Target = 0.25f * k; jelly.Lift = 0.1f * Mathf.Sin(k * Mathf.PI); }
                yield return null;
            }
            if (target != null && target.Jelly != null) target.Jelly.Kick(7f);   // yang menyerap menggembung sesaat
            Destroy(gameObject);
        }

        public SlimeJelly Jelly => jellyCache != null ? jellyCache : (jellyCache = GetComponent<SlimeJelly>());
        private SlimeJelly jellyCache;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var c = GetComponent<Collider2D>();
            Gizmos.DrawWireSphere(c != null ? c.bounds.center : transform.position, eatRadius);
        }
    }
}
