using System;
using UnityEngine;

namespace GooNapped
{
    // Toples jahat berisi satu teman (desain-v2 bagian 5).
    // Alur v2: toples berpendar warna aura teman (terlihat dari jauh), ukurannya = ukuran teman.
    //   Pemain membelah di depan toples: potongan dengan MASSA = ukuran teman masuk sebagai tiruan, teman keluar.
    //   Aura tiruan salah: tetap masuk, tapi penjaga lebih cepat curiga (DecoyMismatch) dan alarm lebih cepat.
    //   Alarm penyadapan: mesin menyadap tiruan lalu sadar setelah beberapa detik -> semua penjaga waspada.
    public class Jar : MonoBehaviour
    {
        [SerializeField] private RecipeData recipe;
        [SerializeField] private FriendSlime friendPrefab;
        [Tooltip("Renderer isi toples (body), supaya tiruan terlihat sesuai warna tubuhnya.")]
        [SerializeField] private SpriteRenderer contentRenderer;

        [Header("Alarm penyadapan")]
        [Tooltip("Detik sampai mesin sadar tiruan disadap, kalau aura tiruan BENAR. 0 = tanpa alarm.")]
        [SerializeField, Min(0f)] private float alarmDelay = 90f;
        [Tooltip("Detik sampai alarm kalau aura tiruan SALAH.")]
        [SerializeField, Min(0f)] private float wrongAlarmDelay = 30f;

        [Header("Pendar aura")]
        [Tooltip("Material tanpa cahaya (Vfx_Particle) untuk pendar isi toples: terlihat dari jauh walau gelap, " +
                 "tapi tidak menerangi slime di dekatnya. Kosong = tanpa pendar.")]
        [SerializeField] private Material glowMaterial;
        [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.5f;

        private SpriteRenderer glow;
        private float alarmLeft = -1f;

        public RecipeData Recipe => recipe;
        // Massa tiruan yang dibutuhkan.
        public int Size => recipe != null ? recipe.Size : 1;
        public bool IsPeeked { get; private set; }
        public bool HasFriend { get; private set; } = true;
        public bool HasDecoy => DecoyState != null;
        public SlimeBodyState DecoyState { get; private set; }
        // Jumlah ciri tiruan yang salah (0 = sempurna). -1 kalau belum ada tiruan.
        public int DecoyMismatch => HasDecoy && recipe != null ? DecoyState.MismatchCount(recipe.Traits) : -1;
        // Detik sampai alarm (-1 = belum ada tiruan atau tanpa alarm).
        public float AlarmTimeLeft => alarmLeft;
        public bool AlarmRaised { get; private set; }

        public event Action<Jar> Changed;
        // Alarm penyadapan berbunyi (didengar semua penjaga).
        public static event Action<Jar> Alarm;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Alarm = null;

        // Skala toples per ukuran teman (art satu, cukup diskalakan).
        public static float SizeScale(int size) => size <= 1 ? 1f : size == 2 ? 1.25f : 1.45f;

        private void Awake()
        {
            if (contentRenderer != null && recipe != null && recipe.BodyMaterial != null)
                contentRenderer.sharedMaterial = recipe.BodyMaterial;
            transform.localScale = Vector3.one * SizeScale(Size);
            if (glowMaterial != null && contentRenderer != null && recipe != null)
            {
                var go = new GameObject("Aura_Glow");
                go.transform.SetParent(contentRenderer.transform, false);
                go.transform.localScale = Vector3.one * 1.12f;
                glow = go.AddComponent<SpriteRenderer>();
                glow.sharedMaterial = glowMaterial;
                glow.sortingLayerID = contentRenderer.sortingLayerID;
                glow.sortingOrder = contentRenderer.sortingOrder;
                glow.sprite = contentRenderer.sprite;
            }
        }

        private void Update()
        {
            if (glow != null)
            {
                glow.sprite = contentRenderer.sprite;
                // berdenyut pelan; aura tiruan (bening/salah) tetap berpendar sesuai tubuh tiruan
                string aura = HasDecoy ? DecoyState.Aura : recipe.Aura;
                var c = TraitNames.AuraColor(aura);
                c.a = glowAlpha * (0.7f + 0.3f * Mathf.Sin(Time.time * 2.4f));
                glow.color = c;
            }
            if (alarmLeft > 0f && !AlarmRaised)
            {
                alarmLeft -= Time.deltaTime;
                if (alarmLeft <= 0f) RaiseAlarm();
            }
        }

        // Pemain mendekat: aura dan ukuran masuk HUD, teman bicara sekali (desain-v2 jawaban 27, pilihan C).
        public void Peek()
        {
            if (IsPeeked) return;
            IsPeeked = true;
            Changed?.Invoke(this);
        }

        // Berapa ciri tubuh ini yang masih salah terhadap teman (untuk petunjuk HUD).
        public int MismatchOf(SlimeBodyState body) => recipe != null ? body.MismatchCount(recipe.Traits) : 0;

        // Alasan tiruan bermassa `mass` tidak bisa masuk, atau null kalau pas.
        public string RejectReason(int mass)
        {
            if (!HasFriend) return "This jar is already empty.";
            if (mass < Size) return "Too small to fill this jar.";
            if (mass > Size) return "Too big for this jar.";
            return null;
        }

        // Tiruan masuk, teman keluar di `exitPosition` (menunggu diajak). Mengembalikan teman yang keluar.
        public FriendSlime Swap(SlimeBodyState decoy, Vector3 exitPosition)
        {
            if (!HasFriend || decoy == null) return null;
            HasFriend = false;
            IsPeeked = true;
            DecoyState = decoy.Clone();
            if (contentRenderer != null)
                contentRenderer.sharedMaterial = DecoyState.BodyMaterial != null ? DecoyState.BodyMaterial
                    : recipe != null && recipe.BodyMaterial != null ? recipe.BodyMaterial : contentRenderer.sharedMaterial;
            float delay = DecoyMismatch == 0 ? alarmDelay : wrongAlarmDelay;
            alarmLeft = delay > 0f ? delay : -1f;

            FriendSlime friend = null;
            if (friendPrefab != null)
            {
                friend = Instantiate(friendPrefab, exitPosition, Quaternion.identity, transform.parent);
                friend.Init(recipe);
            }
            Changed?.Invoke(this);
            return friend;
        }

        // Mesin sadar tiruan disadap (atau Mata Penyihir membongkar tiruan salah): semua penjaga waspada.
        public void RaiseAlarm()
        {
            if (AlarmRaised || !HasDecoy) return;
            AlarmRaised = true;
            alarmLeft = 0f;
            AudioDirector.Play(AudioDirector.Sfx.Caught, transform.position, 0.8f);
            Alarm?.Invoke(this);
            Changed?.Invoke(this);
        }
    }
}
