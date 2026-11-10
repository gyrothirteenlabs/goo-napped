using UnityEngine;

namespace GooNapped
{
    // Satu potongan slime: badan yang bisa digerakkan. TIDAK membaca input.
    // Input ada di PlayerBrain, yang pindah antar potongan (tombol Shift).
    // Potongan yang tidak dikendalikan otomatis nyamar.
    [RequireComponent(typeof(GridMover), typeof(SlimeBody), typeof(SlimeSpriteAnimator))]
    public class SlimePiece : MonoBehaviour
    {
        [Tooltip("Lama animasi makan. Selama itu potongan diam (risiko ketahuan).")]
        [SerializeField] private float eatDuration = 0.6f;
        [SerializeField] private LayerMask pickupMask = ~0;

        private SlimeSpriteAnimator anim;
        private float eatTimer;
        private readonly Collider2D[] hits = new Collider2D[16];

        public GridMover Mover { get; private set; }
        public SlimeBody Body { get; private set; }
        public Vector2Int Facing { get; private set; } = Vector2Int.down;
        public bool IsControlled { get; private set; }
        public bool IsDisguised => !IsControlled;
        public bool IsEating => eatTimer > 0f;
        public bool IsBusy => Mover.IsMoving || IsEating;

        private void Awake()
        {
            Mover = GetComponent<GridMover>();
            Body = GetComponent<SlimeBody>();
            anim = GetComponent<SlimeSpriteAnimator>();
            Body.Changed += OnBodyChanged;
        }

        private void OnDestroy()
        {
            if (Body != null) Body.Changed -= OnBodyChanged;
        }

        private void OnBodyChanged(SlimeBody b) => Mover.SpeedMultiplier = b.State.SpeedMultiplier;

        public void SetControlled(bool on)
        {
            IsControlled = on;
            // Placeholder nyamar: wajah disembunyikan. Sprite "jadi barang" menyusul dari tim desainer.
            if (anim != null && anim.faceRenderer != null) anim.faceRenderer.enabled = on;
        }

        public void Face(Vector2Int dir)
        {
            if (dir != Vector2Int.zero) Facing = dir;
        }

        public bool Step(Vector2Int dir)
        {
            if (IsBusy) return false;
            Face(dir);
            return Mover.TryStep(dir);
        }

        // Bahan di tile sendiri atau tile di depan.
        public IngredientPickup FindPickup()
        {
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = pickupMask };
            int n = Physics2D.OverlapCircle(GridUtil.CellCenter(Mover.Cell), 1.5f, filter, hits);
            IngredientPickup front = null;
            for (int i = 0; i < n; i++)
            {
                var p = hits[i].GetComponentInParent<IngredientPickup>();
                if (p == null || p.Ingredient == null) continue;
                var c = GridUtil.WorldToCell(p.transform.position);
                if (c == Mover.Cell) return p;
                if (c == Mover.Cell + Facing) front = p;
            }
            return front;
        }

        public bool TryEat()
        {
            if (IsBusy) return false;
            var p = FindPickup();
            if (p == null) return false;
            Body.Eat(p.Consume());
            eatTimer = eatDuration;
            return true;
        }

        private void Update()
        {
            if (eatTimer > 0f) eatTimer -= Time.deltaTime;
            string dir = Facing.x != 0 ? "side" : (Facing.y > 0 ? "back" : "front");
            bool left = Facing.x != 0 ? Facing.x < 0 : anim.faceLeft;
            string a = IsDisguised ? "idle" : IsEating ? "eat" : Mover.IsMoving ? "move" : "idle";
            anim.Play(a, dir, left);
        }
    }
}
