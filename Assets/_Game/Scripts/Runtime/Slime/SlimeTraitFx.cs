using UnityEngine;

namespace GooNapped
{
    // Ciri tubuh terlihat di badan, supaya pemain (dan penonton) tahu sifat slime tanpa melihat HUD:
    //   panas    = uap hangat naik          dingin   = serpih es jatuh
    //   bau      = garis bau bergoyang       berkilau = kerlip kecil
    //   berat    = debu tiap mendarat
    // Partikel kotak 2 px dibuat dari kode. PLACEHOLDER: warna/bentuk final oleh tim desainer (ubah di Inspector).
    // Ikut terbawa saat belah (Instantiate menyalin anak objek; dipakai ulang, tidak dobel).
    [RequireComponent(typeof(SlimePiece))]
    public class SlimeTraitFx : MonoBehaviour
    {
        [Tooltip("Material partikel tanpa cahaya (Vfx_Particle). Kosong = partikel tidak dibuat.")]
        [SerializeField] private Material particleMaterial;
        [Tooltip("Ukuran satu piksel game (unit). PPU 48.")]
        [SerializeField] private float pixel = 1f / 48f;
        [Tooltip("Tinggi pusat efek dari titik kaki (unit).")]
        [SerializeField] private float centerHeight = 0.22f;

        [Header("Warna (placeholder)")]
        [SerializeField] private Color heatColor = new Color(1f, 0.6f, 0.3f, 0.75f);
        [SerializeField] private Color coldColor = new Color(0.75f, 0.92f, 1f, 0.9f);
        [SerializeField] private Color smellColor = new Color(0.82f, 0.86f, 0.3f, 0.7f);
        [SerializeField] private Color shineColor = new Color(1f, 1f, 0.9f, 1f);
        [SerializeField] private Color dustColor = new Color(0.6f, 0.57f, 0.52f, 0.85f);

        [Header("Jumlah per detik")]
        [SerializeField] private float heatRate = 7f;
        [SerializeField] private float coldRate = 5f;
        [SerializeField] private float smellRate = 5f;
        [SerializeField] private float shineRate = 3f;
        [SerializeField] private int dustPerLanding = 5;

        private SlimePiece piece;
        private ParticleSystem heat, cold, smell, shine, dust;
        private bool heavy;

        private void Awake()
        {
            piece = GetComponent<SlimePiece>();
            if (particleMaterial == null) return;
            heat = Make("Fx_Heat", heatColor, 0.9f, 0.15f, 2, new Vector2(0f, 0.35f));
            cold = Make("Fx_Cold", coldColor, 0.8f, 0.24f, 2, new Vector2(0f, -0.18f));
            smell = Make("Fx_Smell", smellColor, 1.4f, 0.18f, 2, new Vector2(0f, 0.25f));
            var n = smell.noise;
            n.enabled = true; n.strength = 0.25f; n.frequency = 1.2f; n.scrollSpeed = 0.6f;
            shine = Make("Fx_Shine", shineColor, 0.35f, 0.22f, 3, Vector2.zero);
            var sz = shine.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f)));
            dust = Make("Fx_Dust", dustColor, 0.4f, 0.12f, 2, Vector2.zero);
            var dm = dust.main;
            dm.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            var dl = dust.limitVelocityOverLifetime;
            dl.enabled = true; dl.limit = 0f; dl.dampen = 0.15f;
        }

        private void Start()
        {
            var body = piece.Body;
            body.Changed += OnBodyChanged;
            piece.Motor.PhaseEntered += OnHopPhase;
            OnBodyChanged(body);
        }

        private void OnDestroy()
        {
            if (piece == null) return;
            if (piece.Body != null) piece.Body.Changed -= OnBodyChanged;
            if (piece.Motor != null) piece.Motor.PhaseEntered -= OnHopPhase;
        }

        private ParticleSystem Make(string childName, Color color, float lifetime, float radius, int sizePx, Vector2 drift)
        {
            var t = transform.Find(childName);
            ParticleSystem ps;
            if (t != null && (ps = t.GetComponent<ParticleSystem>()) != null)
            {
                ps.Clear();   // salinan hasil belah: pakai ulang, buang partikel lama
                return ps;
            }
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, centerHeight, 0f);
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
            main.startSpeed = 0f;
            main.startSize = sizePx * pixel;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;   // jejak tertinggal saat slime bergerak
            main.maxParticles = 40;
            main.gravityModifier = 0f;

            var em = ps.emission;
            em.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;   // lingkaran di bidang XY (2D)
            shape.radius = radius;
            shape.radiusThickness = 1f;

            if (drift != Vector2.zero)
            {
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(-0.05f + drift.x, 0.05f + drift.x);
                vel.y = new ParticleSystem.MinMaxCurve(drift.y * 0.7f, drift.y);
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            }

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = particleMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            var anim = GetComponent<SlimeSpriteAnimator>();
            if (anim != null && anim.bodyRenderer != null)
            {
                r.sortingLayerID = anim.bodyRenderer.sortingLayerID;
                r.sortingOrder = anim.bodyRenderer.sortingOrder + 2;
            }
            ps.Play();
            return ps;
        }

        private void OnBodyChanged(SlimeBody body)
        {
            if (heat == null) return;
            var s = body.State;
            string temp = s.Get(TraitKind.Temperature);
            SetRate(heat, temp == "panas" ? heatRate : 0f);
            SetRate(cold, temp == "dingin" ? coldRate : 0f);
            SetRate(smell, s.Has(TraitKind.Smell) || s.SmellRadius > 0f ? smellRate : 0f);
            SetRate(shine, s.Get(TraitKind.Shine) == "berkilau" ? shineRate : 0f);
            heavy = s.Get(TraitKind.Weight) == "berat";
        }

        private static void SetRate(ParticleSystem ps, float rate)
        {
            var em = ps.emission;
            em.rateOverTime = rate;
        }

        private void OnHopPhase(SlimeMotor.HopPhase p)
        {
            if (heavy && dust != null && p == SlimeMotor.HopPhase.Land)
            {
                // debu keluar dari kaki, bukan dari tengah badan
                var ep = new ParticleSystem.EmitParams { position = (Vector3)piece.Center + Vector3.down * 0.15f, applyShapeToPosition = true };
                dust.Emit(ep, dustPerLanding);
            }
        }
    }
}
