using System;
using UnityEngine;

namespace GooNapped
{
    // Tubuh slime di scene: menyimpan SlimeBodyState dan menampilkan akibatnya (palet body).
    // Efek nyala/uap/bau (particle, Light 2D) menyusul; sementara cukup baca State.EmitsLight / SmellRadius.
    public class SlimeBody : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        [Tooltip("Material saat belum makan bahan pewarna. Kosong = material bodyRenderer saat Awake.")]
        [SerializeField] private Material defaultMaterial;

        public SlimeBodyState State { get; private set; } = new SlimeBodyState();

        // Dipanggil tiap tubuh berubah (makan, state diganti saat belah/gabung).
        public event Action<SlimeBody> Changed;

        private void Awake()
        {
            if (defaultMaterial == null && bodyRenderer != null) defaultMaterial = bodyRenderer.sharedMaterial;
        }

        public void Eat(IngredientData ingredient)
        {
            if (ingredient == null) return;
            State.Eat(ingredient);
            Refresh();
        }

        public void SetState(SlimeBodyState state)
        {
            State = state ?? new SlimeBodyState();
            Refresh();
        }

        public IngredientData Vomit()
        {
            var i = State.Vomit();
            if (i != null) Refresh();
            return i;
        }

        // Gabung: serap ciri potongan lain (ciri sendiri tidak tertimpa).
        public void Absorb(SlimeBodyState other)
        {
            State.Absorb(other);
            Refresh();
        }

        private void Refresh()
        {
            if (bodyRenderer != null)
            {
                var m = State.BodyMaterial;
                bodyRenderer.sharedMaterial = m != null ? m : defaultMaterial;
            }
            Changed?.Invoke(this);
        }
    }
}
