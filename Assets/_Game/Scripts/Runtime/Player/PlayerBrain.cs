using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GooNapped
{
    // "Otak" pemain: satu-satunya tempat input dibaca. Mengendalikan SATU potongan slime pada satu waktu.
    //   Gerak        : analog (WASD / stick)
    //   Makan        : bahan terdekat dalam jangkauan (sekali tekan)
    //   Belah/Gabung : satu tombol. Menyentuh potongan lain = gabung, kalau tidak = belah ke arah hadap
    //   Shift        : pindah kendali ke potongan berikutnya; yang ditinggal otomatis nyamar
    public class PlayerBrain : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMap = "Player";
        [SerializeField] private string moveAction = "Move";
        [SerializeField] private string eatAction = "Interact";
        [SerializeField] private string splitAction = "Jump";
        [SerializeField] private string switchAction = "Sprint";
        [Tooltip("Tahan untuk nyamar jadi barang di tempat (tidak bisa bergerak).")]
        [SerializeField] private string hideAction = "Crouch";
        [SerializeField] private string vomitAction = "Vomit";
        [SerializeField] private string throwAction = "Throw";

        [Header("Skill: lempar gumpalan (pengalih suara)")]
        [SerializeField] private Sprite blobSprite;
        [SerializeField] private float throwDistance = 3.2f;
        [Tooltip("Radius suara gumpalan mendarat: penjaga di dalamnya datang memeriksa.")]
        [SerializeField] private float throwNoiseRadius = 4.5f;
        [SerializeField] private float throwCooldown = 5f;

        [Tooltip("Potongan awal. Kosong = semua SlimePiece di scene.")]
        [SerializeField] private List<SlimePiece> pieces = new List<SlimePiece>();
        [SerializeField, Min(1)] private int maxPieces = 3;
        [Tooltip("Jarak munculnya potongan baru dari potongan aktif (unit).")]
        [SerializeField] private float splitDistance = 0.8f;
        [Tooltip("Celah maksimum antar tepi dua potongan supaya dianggap bersentuhan (unit).")]
        [SerializeField] private float mergeGap = 0.15f;

        [Tooltip("Jarak interaksi E ke toples/teman (dari pusat collider).")]
        [SerializeField] private float interactRadius = 0.85f;
        [Tooltip("Jarak mengintip isi toples.")]
        [SerializeField] private float peekRadius = 1.3f;

        [Header("Jeda aksi (anti spam)")]
        [Tooltip("Jeda minimum antar Shift (detik).")]
        [SerializeField] private float switchCooldown = 0.7f;
        [Tooltip("Lama tidak bisa bergerak setelah Shift (potongan baru 'bangun').")]
        [SerializeField] private float switchLock = 0.3f;
        [Tooltip("Ancang-ancang sebelum potongan keluar (detik). Selama itu diam dan masih bisa ketahuan.")]
        [SerializeField] private float splitWindup = 0.3f;
        [Tooltip("Jeda minimum antar belah (detik).")]
        [SerializeField] private float splitCooldown = 1.2f;
        [Tooltip("Lama tidak bisa bergerak setelah gabung.")]
        [SerializeField] private float mergeLock = 0.35f;

        private float lockTimer, nextSwitch, nextSplit, nextThrow;
        private InputAction move, eat, split, switchPiece, hide, vomit, throwBlob;

        // 0 = siap, >0 = detik sampai lempar berikutnya (untuk HUD).
        public float ThrowCooldownLeft => Mathf.Max(0f, nextThrow - Time.time);
        private int activeIndex;
        private float messageTimer;
        private LevelGoal goal;
        private readonly Collider2D[] hits = new Collider2D[8];
        private readonly Collider2D[] near = new Collider2D[16];

        // Toples yang sedang diintip (dekat potongan aktif), atau null.
        public Jar NearbyJar { get; private set; }
        // Pesan singkat untuk HUD (alasan aksi ditolak, hasil tukar).
        public string Message { get; private set; } = "";

        public IReadOnlyList<SlimePiece> Pieces => pieces;
        public SlimePiece Active => pieces.Count > 0 ? pieces[Mathf.Clamp(activeIndex, 0, pieces.Count - 1)] : null;
        public event Action<SlimePiece> ActiveChanged;

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError("PlayerBrain: Input Actions belum diisi (Assets/Settings/InputSystem_Actions).", this);
                enabled = false;
                return;
            }
            var map = actions.FindActionMap(actionMap, true);
            move = map.FindAction(moveAction, true);
            eat = map.FindAction(eatAction, true);
            split = map.FindAction(splitAction, true);
            switchPiece = map.FindAction(switchAction, true);
            hide = map.FindAction(hideAction, true);
            vomit = map.FindAction(vomitAction, false);
            throwBlob = map.FindAction(throwAction, false);
            map.Enable();
        }

        private void Start()
        {
            pieces.RemoveAll(p => p == null);
            if (pieces.Count == 0)
                pieces.AddRange(FindObjectsByType<SlimePiece>(FindObjectsSortMode.None));
            goal = FindFirstObjectByType<LevelGoal>();
            SetActive(0);
        }

        private void Update()
        {
            var a = Active;
            if (a == null) return;
            if (goal != null && (goal.IsFailed || goal.IsComplete)) { a.Move(Vector2.zero); return; }
            if (messageTimer > 0f && (messageTimer -= Time.deltaTime) <= 0f) Message = "";
            // jeda setelah aksi (belah, gabung, Shift): tidak bisa bergerak/aksi lain, supaya tidak bisa di-spam
            if (lockTimer > 0f) { lockTimer -= Time.deltaTime; a.Move(Vector2.zero); return; }
            if (switchPiece.WasPressedThisFrame() && Time.time >= nextSwitch) { SwitchNext(); return; }
            a.HideRequested = hide.IsPressed();
            if (a.IsTransforming) { a.Move(Vector2.zero); return; }
            if (!a.IsEating)
            {
                if (split.WasPressedThisFrame() && SplitOrMerge()) return;
                if (eat.WasPressedThisFrame() && Interact()) return;
                if (vomit != null && vomit.WasPressedThisFrame())
                {
                    if (a.TryVomit()) { Say("Bahan terakhir dimuntahkan."); return; }
                    Say("Perut kosong.");
                }
                if (throwBlob != null && throwBlob.WasPressedThisFrame() && TryThrow()) return;
            }
            a.Move(move.ReadValue<Vector2>());

            // Objektif 1: mendekat ke toples = mengintip isinya.
            NearbyJar = FindNear<Jar>(a, peekRadius);
            if (NearbyJar != null) NearbyJar.Peek();
        }

        private T FindNear<T>(SlimePiece a, float radius) where T : Component
        {
            var filter = new ContactFilter2D { useTriggers = true };
            int n = Physics2D.OverlapCircle(a.Center, radius, filter, near);
            T best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = near[i].GetComponentInParent<T>();
                if (c == null) continue;
                float d = ((Vector2)near[i].bounds.center - a.Center).sqrMagnitude;
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }

        private void Say(string text)
        {
            Message = text;
            messageTimer = 3f;
        }

        // Tombol E. Urutan: telan teman -> tukar dengan isi toples -> makan bahan.
        public bool Interact()
        {
            var a = Active;
            if (a == null || a.IsEating) return false;

            var friend = FindNear<FriendSlime>(a, interactRadius);
            if (friend != null)
            {
                var r = friend.Swallow();
                a.Carried.Add(r);
                Say(r != null ? r.FriendName + " ditelan. Bawa ke exit." : "Teman ditelan.");
                return true;
            }

            var jar = FindNear<Jar>(a, interactRadius);
            if (jar != null && jar.HasFriend) return SwapWithJar(a, jar);

            return a.TryEat();
        }

        // Objektif 4: potongan aktif masuk toples sebagai tiruan, teman keluar di tempat potongan itu berdiri.
        private bool SwapWithJar(SlimePiece a, Jar jar)
        {
            if (pieces.Count < 2)
            {
                Say("Belah dulu (Space): satu potongan masuk toples, potongan lain membawa teman.");
                return true;
            }
            if (a.Carried.Count > 0)
            {
                Say("Potongan ini sedang membawa teman. Pakai potongan lain (Shift).");
                return true;
            }
            int miss = jar.MismatchOf(a.Body.State);
            jar.Swap(a.Body.State, a.transform.position);
            pieces.Remove(a);
            Destroy(a.gameObject);
            SetActive(0);
            Say(miss == 0 ? "Tiruan sempurna. Telan temanmu (E) lalu kabur."
                          : "Tiruan masuk, tapi " + miss + " ciri salah: penjaga akan lebih cepat curiga.");
            return true;
        }

        // Potongan lain yang sedang bersentuhan dengan potongan aktif (paling dekat), atau null.
        public SlimePiece TouchingPiece()
        {
            var a = Active;
            if (a == null) return null;
            SlimePiece best = null;
            float bestD = float.MaxValue;
            foreach (var p in pieces)
            {
                if (p == null || p == a) continue;
                float d = Vector2.Distance(a.Center, p.Center);
                if (d <= a.Radius + p.Radius + mergeGap && d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        public bool SplitOrMerge()
        {
            var other = TouchingPiece();
            if (other != null) return Merge(other);
            var a = Active;
            if (!CanSplit(a))
            {
                if (a != null && pieces.Count >= maxPieces) Say("Maksimal " + maxPieces + " potongan.");
                return false;
            }
            if (Time.time < nextSplit) return false;
            StartCoroutine(SplitRoutine(a));
            return true;
        }

        private bool CanSplit(SlimePiece a)
        {
            if (a == null || a.IsEating || a.IsTransforming || pieces.Count >= maxPieces) return false;
            return !IsBlocked(a, a.Center + (Vector2)a.Facing * splitDistance);
        }

        // Ancang (badan menggembung-memipih, diam) -> potongan keluar. Masih bisa ketahuan selama ancang.
        private System.Collections.IEnumerator SplitRoutine(SlimePiece a)
        {
            nextSplit = Time.time + splitCooldown;
            lockTimer = splitWindup + 0.2f;
            a.Motor.Stop();
            if (a.Jelly != null) a.Jelly.Hold(0.22f);
            AudioDirector.Play(AudioDirector.Sfx.Split, a.Center);
            yield return new WaitForSeconds(splitWindup * 0.6f);
            if (a.Jelly != null) a.Jelly.Hold(-0.28f);
            yield return new WaitForSeconds(splitWindup * 0.4f);
            if (a.Jelly != null) a.Jelly.Release();
            if (a != null && a == Active) TrySplit();
        }

        // Belah langsung (tanpa ancang). Dipakai SplitRoutine dan test.
        public bool TrySplit()
        {
            var a = Active;
            if (!CanSplit(a)) return false;
            Vector2 offset = (Vector2)a.Facing * splitDistance;

            if (a.Jelly != null) a.Jelly.ResetPose();     // salinan jangan ikut pose gepeng/terangkat
            var from = a.transform.position;
            var to = from + (Vector3)offset;
            var go = Instantiate(a.gameObject, from, Quaternion.identity, a.transform.parent);
            go.name = "Slime_Potongan_" + pieces.Count;
            var piece = go.GetComponent<SlimePiece>();
            piece.Body.SetState(a.Body.State.Clone());   // mewarisi sifat tubuh saat ini
            piece.Carried.Clear();
            piece.Face(a.Facing);
            piece.SetControlled(false);                  // nyamar setelah mendarat
            piece.PlaySplitArc(from, to);
            if (a.Jelly != null) a.Jelly.Kick(-9f);      // induk memipih saat "memuntahkan" potongan
            a.Motor.Stop();
            pieces.Add(piece);
            return true;
        }

        // Lempar gumpalan lendir ke arah hadap; mendarat = bunyi yang memancing penjaga. Cooldown supaya tidak di-spam.
        public bool TryThrow()
        {
            var a = Active;
            if (a == null || a.IsEating || a.IsTransforming) return false;
            if (Time.time < nextThrow)
            {
                Say("Gumpalan belum siap (" + Mathf.CeilToInt(ThrowCooldownLeft) + " dtk).");
                return false;
            }
            Vector2 dir = a.Facing;
            Vector2 from = a.Center;
            float dist = throwDistance;
            foreach (var h in Physics2D.RaycastAll(from, dir, throwDistance))
            {
                if (h.collider.isTrigger || h.collider.GetComponentInParent<SlimePiece>() != null) continue;
                dist = Mathf.Min(dist, h.distance - 0.3f);
            }
            if (dist < 0.6f) { Say("Terlalu dekat tembok untuk melempar."); return false; }
            var color = a.GetComponent<SlimeSpriteAnimator>().bodyRenderer.sharedMaterial.HasProperty("_Band3")
                ? a.GetComponent<SlimeSpriteAnimator>().bodyRenderer.sharedMaterial.GetColor("_Band3") : Color.green;
            GooBlob.Spawn(blobSprite, color, from, from + dir * dist, throwNoiseRadius);
            nextThrow = Time.time + throwCooldown;
            lockTimer = 0.25f;
            a.Motor.Stop();
            if (a.Jelly != null) a.Jelly.Kick(6f);
            AudioDirector.Play(AudioDirector.Sfx.Split, from, 0.5f, 0.2f);
            return true;
        }

        // Ada dinding/prop di tempat potongan baru akan muncul?
        private bool IsBlocked(SlimePiece a, Vector2 center)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            int n = Physics2D.OverlapCircle(center, a.Radius * 0.9f, filter, hits);
            for (int i = 0; i < n; i++)
                if (hits[i].GetComponentInParent<SlimePiece>() != a) return true;
            // dinding tipis (edge collider) di antara potongan aktif dan tempat muncul
            foreach (var h in Physics2D.LinecastAll(a.Center, center))
                if (h.collider != null && !h.collider.isTrigger && h.collider.GetComponentInParent<SlimePiece>() != a) return true;
            return false;
        }

        // Potongan aktif menyerap potongan lain yang bersentuhan. Ciri potongan aktif yang menang.
        public bool Merge(SlimePiece other)
        {
            var a = Active;
            if (a == null || other == null || other == a) return false;
            if (Vector2.Distance(a.Center, other.Center) > a.Radius + other.Radius + mergeGap) return false;
            a.Body.Absorb(other.Body.State);
            a.Carried.AddRange(other.Carried);           // teman yang dibawa ikut pindah
            pieces.Remove(other);
            activeIndex = pieces.IndexOf(a);
            other.PlayAbsorbInto(a);                     // tersedot lalu hilang (animasi)
            a.Motor.Stop();
            lockTimer = mergeLock;
            AudioDirector.Play(AudioDirector.Sfx.Merge, a.Center);
            return true;
        }

        public void SwitchNext()
        {
            if (pieces.Count < 2) return;
            var old = Active;
            if (old != null && old.IsEating) return;
            SetActive((activeIndex + 1) % pieces.Count);
            // yang ditinggal meringkuk jadi barang, yang baru "bangun" (lepas samaran) dulu sebelum bisa bergerak
            nextSwitch = Time.time + switchCooldown;
            lockTimer = switchLock;
            AudioDirector.Play(AudioDirector.Sfx.Switch, Active.Center, 0.7f);
        }

        public void SetActive(int index)
        {
            if (pieces.Count == 0) return;
            activeIndex = Mathf.Clamp(index, 0, pieces.Count - 1);
            for (int i = 0; i < pieces.Count; i++)
                if (pieces[i] != null) pieces[i].SetControlled(i == activeIndex);
            ActiveChanged?.Invoke(Active);
        }
    }
}
