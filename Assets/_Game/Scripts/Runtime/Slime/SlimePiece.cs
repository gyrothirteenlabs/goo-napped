using UnityEngine;

namespace GooNapped
{
    // Satu potongan slime: badan yang bisa digerakkan. TIDAK membaca input.
    // Input ada di PlayerBrain, yang pindah antar potongan (tombol Shift).
    // Potongan yang tidak dikendalikan otomatis nyamar dan diam di tempat.
    [RequireComponent(typeof(SlimeMotor), typeof(SlimeBody), typeof(SlimeSpriteAnimator))]
    [DefaultExecutionOrder(10)]   // setelah SlimeMotor: animasi khusus menahan pegas jeli setelah gerak menulisnya
    public class SlimePiece : MonoBehaviour
    {
        [Header("Sprite")]
        [Tooltip("Pakai frame sprite 'move' saat melompat. Matikan selama frame sprite masih punya squash sendiri " +
                 "(dobel dengan squash engine = janky). Nyalakan setelah sprite final dirender sesuai panduan-blender-3d C.1.")]
        [SerializeField] private bool spriteMoveFrames;

        [Header("Makan")]
        [Tooltip("Jarak maksimum ke bahan (dari pusat collider).")]
        [SerializeField] private float eatRadius = 0.7f;
        [Tooltip("Lama animasi makan. Selama itu potongan diam (risiko ketahuan).")]
        [SerializeField] private float eatDuration = 0.85f;
        [SerializeField] private LayerMask pickupMask = ~0;

        [Header("Muntah")]
        [Tooltip("Prefab bahan yang muncul saat bahan terakhir dimuntahkan.")]
        [SerializeField] private IngredientPickup pickupPrefab;
        [SerializeField] private float vomitDuration = 0.6f;

        [Header("Nyamar")]
        [Tooltip("Renderer barang samaran (anak objek). Aktif saat potongan nyamar, body/face disembunyikan.")]
        [SerializeField] private SpriteRenderer disguiseRenderer;
        [Tooltip("Sprite barang samaran untuk lokasi ini (chapter 1: batu). Placeholder sampai ada art tim.")]
        [SerializeField] private Sprite disguiseSprite;
        [Tooltip("Lama berubah jadi barang (detik). Selama itu belum tersamar = masih bisa ketahuan.")]
        [SerializeField] private float disguiseTime = 0.45f;
        [Tooltip("Lama kembali jadi slime (detik). Selama itu belum bisa bergerak.")]
        [SerializeField] private float revealTime = 0.3f;

        // 0 = slime, 1 = barang. Berubah bertahap (tidak bisa nyamar/lepas instan).
        private float disguiseT;

        private SlimeSpriteAnimator anim;
        private Collider2D col;
        private float eatTimer;
        private readonly Collider2D[] hits = new Collider2D[16];

        public SlimeMotor Motor { get; private set; }
        public SlimeBody Body { get; private set; }
        // Arah hadap (4 arah, untuk sprite dan arah belah).
        public Vector2Int Facing { get; private set; } = Vector2Int.down;
        public bool IsControlled { get; private set; }
        // Diminta pemain (tahan tombol nyamar). Potongan yang tidak dikendalikan selalu nyamar.
        public bool HideRequested { get; set; }
        // Potongan yang dikendalikan baru tersamar setelah transformasi selesai.
        public bool IsDisguised => !IsControlled || disguiseT >= 1f;
        // Sedang berubah jadi barang / kembali jadi slime (tidak bisa bergerak).
        public bool IsTransforming => IsControlled && disguiseT > 0f;
        // Umpan: barang samaran yang menyala/berbau menarik perhatian penjaga (tanpa menaikkan curiga).
        public bool IsBait => IsDisguised && (Body.State.EmitsLight || Body.State.SmellRadius > 0f);
        public bool IsEating => eatTimer > 0f;
        // Massa tubuh (desain-v2 bagian 5): 1 = kecil, maks 4.
        public int Mass => Body.State.Mass;
        public Vector2 Center => col != null ? (Vector2)col.bounds.center : (Vector2)transform.position;
        public float Radius => col != null ? col.bounds.extents.x : 0.28f;
        public Collider2D Collider => col;

        private void Awake()
        {
            Motor = GetComponent<SlimeMotor>();
            Body = GetComponent<SlimeBody>();
            anim = GetComponent<SlimeSpriteAnimator>();
            col = GetComponent<Collider2D>();
            Body.Changed += OnBodyChanged;
            Motor.PhaseEntered += OnHopPhase;
        }

        private void OnDestroy()
        {
            if (Body != null) Body.Changed -= OnBodyChanged;
            if (Motor != null) Motor.PhaseEntered -= OnHopPhase;
        }

        // suara "plek" lembut tiap mendarat. Tubuh berat (Serbuk Batu) mendarat "dug": terdengar penjaga tembus tembok.
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
            HideRequested = false;
            Motor.SetFrozen(!on);
            ApplyDisguiseVisual();
        }

        private void ApplyDisguiseVisual()
        {
            bool hidden = disguiseT >= 1f && !animating && disguiseRenderer != null && disguiseSprite != null;
            if (disguiseRenderer != null)
            {
                disguiseRenderer.enabled = hidden;
                disguiseRenderer.sprite = disguiseSprite;
                // umpan menyala: barang samaran ikut berpendar hangat supaya pemain tahu ini umpan
                disguiseRenderer.color = hidden && Body.State.EmitsLight ? new Color(1f, 0.8f, 0.55f) : Color.white;
            }
            if (anim != null)
            {
                if (anim.bodyRenderer != null) anim.bodyRenderer.enabled = !hidden;
                // tanpa sprite samaran (fallback lama): body tetap, wajah disembunyikan
                if (anim.faceRenderer != null) anim.faceRenderer.enabled = disguiseT < 1f || animating;
            }
        }

        public void Face(Vector2Int dir)
        {
            if (dir != Vector2Int.zero) Facing = dir;
        }

        // Dipanggil PlayerBrain tiap frame untuk potongan yang dikendalikan.
        public void Move(Vector2 input)
        {
            if (!IsControlled || IsEating || IsTransforming) { Motor.Stop(); return; }
            Motor.MoveInput = input;
            if (input.sqrMagnitude < 0.04f) return;
            Face(Mathf.Abs(input.x) > Mathf.Abs(input.y)
                ? new Vector2Int(input.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, input.y > 0 ? 1 : -1));
        }

        // Bahan terdekat dalam jangkauan, atau null.
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

        // Makan esens terdekat. false kalau tidak ada esens dalam jangkauan atau perut penuh (massa maksimum).
        public bool TryEat()
        {
            if (IsEating || !Body.State.CanEat) return false;
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
            if (IsEating || IsTransforming || !Body.State.CanVomit) return false;
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

        private void UpdateDisguise()
        {
            if (animating) return;
            bool want = !IsControlled || (HideRequested && !IsEating);
            float before = disguiseT;
            disguiseT = Mathf.MoveTowards(disguiseT, want ? 1f : 0f,
                Time.deltaTime / Mathf.Max(0.01f, want ? disguiseTime : revealTime));
            var jelly = Jelly;
            if (before < 1f && disguiseT >= 1f)
            {
                if (jelly != null) { jelly.Release(); jelly.Size = 1f; jelly.Kick(-5f); }
                AudioDirector.Play(AudioDirector.Sfx.Disguise, Center, 0.6f);
            }
            else if (before >= 1f && disguiseT < 1f)
            {
                if (jelly != null) jelly.Kick(5f);
                if (IsControlled) AudioDirector.Play(AudioDirector.Sfx.Reveal, Center, 0.6f);
            }
            if (jelly == null) return;
            if (disguiseT > 0f && disguiseT < 1f)
            {
                // meringkuk mengecil sebelum jadi barang (dan kebalikannya saat kembali)
                jelly.Hold(-0.3f * disguiseT);
                jelly.Size = 1f - 0.18f * disguiseT;
            }
            else if (!IsEating)
            {
                jelly.Release();
                if (disguiseT <= 0f) jelly.Size = 1f;
            }
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
            UpdateDisguise();
            ApplyDisguiseVisual();
            DropScent();
            UpdateScale();
            string dir = Facing.x != 0 ? "side" : (Facing.y > 0 ? "back" : "front");
            bool left = Facing.x != 0 ? Facing.x < 0 : anim.faceLeft;
            bool moving = IsControlled && !IsEating && Motor.IsHopping;
            anim.speedScale = 1f;
            string a = animating ? "move" : disguiseT > 0f ? "idle" : IsEating ? "eat" : moving ? "move" : "idle";
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

        // Potongan baru melompat keluar dari induk ke tempat mendaratnya (busur), lalu baru nyamar.
        public void PlaySplitArc(Vector3 from, Vector3 to, float duration = 0.32f)
        {
            StopAllCoroutines();
            StartCoroutine(SplitArc(from, to, duration));
        }

        private System.Collections.IEnumerator SplitArc(Vector3 from, Vector3 to, float duration)
        {
            animating = true;
            if (col != null) col.enabled = false;            // jangan mendorong induk saat keluar dari badannya
            var rb = GetComponent<Rigidbody2D>();
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
            yield return new WaitForSeconds(0.12f);           // goyang sebentar sebelum berubah jadi barang
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
            var rb = GetComponent<Rigidbody2D>();
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
