using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GooNapped
{
    // "Otak" pemain: satu-satunya tempat input dibaca. Mengendalikan SATU potongan slime pada satu waktu.
    // Kontrol v2 (desain-v2 bagian 15):
    //   WASD  gerak
    //   E     makan esens · tukar di toples · lentera · tuas
    //   Space belah / gabung. Di depan toples = membelah langsung ke dalam toples (tiruan)
    //   Shift pindah kendali ke potongan lain
    //   C     masuk / keluar tempat sembunyi (HidingSpot) terdekat
    //   F     perintah teman yang dikawal: tunggu / ikut
    //   Q     muntahkan esens terakhir
    public class PlayerBrain : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMap = "Player";
        [SerializeField] private string moveAction = "Move";
        [SerializeField] private string eatAction = "Interact";
        [SerializeField] private string splitAction = "Jump";
        [SerializeField] private string switchAction = "Sprint";
        [Tooltip("Masuk / keluar tempat sembunyi terdekat.")]
        [SerializeField] private string hideAction = "Crouch";
        [SerializeField] private string vomitAction = "Vomit";
        [Tooltip("Perintah teman yang dikawal: tunggu / ikut.")]
        [SerializeField] private string commandAction = "Command";

        [Tooltip("Sprite titik jejak bau (ScentTrail).")]
        [SerializeField] private Sprite blobSprite;

        [Tooltip("Potongan awal. Kosong = semua SlimePiece di scene.")]
        [SerializeField] private List<SlimePiece> pieces = new List<SlimePiece>();
        [Tooltip("Maksimal potongan sekaligus (desain-v2: 2).")]
        [SerializeField, Min(1)] private int maxPieces = 2;
        [Tooltip("Jarak munculnya potongan baru dari potongan aktif (unit).")]
        [SerializeField] private float splitDistance = 0.8f;
        [Tooltip("Celah maksimum antar tepi dua potongan supaya dianggap bersentuhan (unit).")]
        [SerializeField] private float mergeGap = 0.15f;

        [Tooltip("Jarak interaksi E/Space ke toples, lentera, tuas (dari pusat collider).")]
        [SerializeField] private float interactRadius = 0.85f;
        [Tooltip("Jarak mendekat ke toples: aura dan ukuran teman masuk HUD, teman bicara.")]
        [SerializeField] private float peekRadius = 1.6f;
        [Tooltip("Jarak ke tempat sembunyi supaya bisa masuk (C).")]
        [SerializeField] private float hideRadius = 0.9f;
        [Tooltip("Teman yang mengikuti dalam jarak ini ikut masuk tempat sembunyi/ventilasi bersama pemain.")]
        [SerializeField] private float friendJoinRadius = 1.6f;

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

        private float lockTimer, nextSwitch, nextSplit;
        private bool ventStick;
        private InputAction move, eat, split, switchPiece, hide, vomit, command;
        private int activeIndex;
        private float messageTimer;
        private LevelGoal goal;
        private readonly Collider2D[] hits = new Collider2D[8];
        private readonly Collider2D[] near = new Collider2D[16];

        // Toples terdekat yang sedang didekati potongan aktif, atau null.
        public Jar NearbyJar { get; private set; }
        // Pesan singkat untuk HUD (alasan aksi ditolak, kata-kata teman). Bahasa Inggris.
        public string Message { get; private set; } = "";

        public IReadOnlyList<SlimePiece> Pieces => pieces;
        public int MaxPieces => maxPieces;
        public SlimePiece Active => pieces.Count > 0 ? pieces[Mathf.Clamp(activeIndex, 0, pieces.Count - 1)] : null;
        public event Action<SlimePiece> ActiveChanged;
        // Transformasi terjadi: belah, gabung, tiruan masuk toples, potongan bangun (Shift), masuk/keluar tempat sembunyi.
        // Penjaga yang melihatnya jadi curiga (desain-v2 bagian 4). spot != null untuk tempat sembunyi, entering = masuk.
        public event Action<Vector2, HidingSpot, bool> Transformed;

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
            command = map.FindAction(commandAction, false);
            map.Enable();
        }

        private void Start()
        {
            pieces.RemoveAll(p => p == null);
            if (pieces.Count == 0)
                pieces.AddRange(FindObjectsByType<SlimePiece>(FindObjectsSortMode.None));
            goal = FindFirstObjectByType<LevelGoal>();
            if (ScentTrail.MarkSprite == null) ScentTrail.MarkSprite = blobSprite;
            SetActive(0);
        }

        private void Update()
        {
            var a = Active;
            if (a == null) return;
            if (goal != null && (goal.IsFailed || goal.IsComplete)) { a.Move(Vector2.zero); return; }
            if (StoryPlayer.IsPlaying) { a.Move(Vector2.zero); return; }   // dialog/cutscene: input terkunci
            if (messageTimer > 0f && (messageTimer -= Time.deltaTime) <= 0f) Message = "";
            // jeda setelah aksi (belah, gabung, Shift): tidak bisa bergerak/aksi lain, supaya tidak bisa di-spam
            if (lockTimer > 0f) { lockTimer -= Time.deltaTime; a.Move(Vector2.zero); return; }
            if (switchPiece.WasPressedThisFrame() && Time.time >= nextSwitch) { SwitchNext(); return; }
            if (a.IsTransforming) { a.Move(Vector2.zero); return; }
            if (hide.WasPressedThisFrame() && !a.IsEating) { ToggleHide(a); a.Move(Vector2.zero); return; }
            if (a.HidingIn != null)
            {
                // di dalam tempat sembunyi: diam, hanya C (keluar), Shift, dan F. Di ventilasi: A/D pindah lubang.
                var vent = Vent.Of(a.HidingIn);
                float x = move.ReadValue<Vector2>().x;
                if (vent != null && Mathf.Abs(x) > 0.5f && !ventStick) { ventStick = true; TravelVent(a, vent, x < 0f ? -1 : 1); }
                if (Mathf.Abs(x) <= 0.5f) ventStick = false;
                if (command != null && command.WasPressedThisFrame()) Command();
                else if (split.WasPressedThisFrame() || eat.WasPressedThisFrame())
                    Say(vent != null ? "You're in the vent. A/D: other openings. C: come out." : "You're hiding. Press C to come out.");
                a.Move(Vector2.zero);
                return;
            }
            if (!a.IsEating)
            {
                if (split.WasPressedThisFrame() && SplitOrMerge()) return;
                if (eat.WasPressedThisFrame() && Interact()) return;
                if (vomit != null && vomit.WasPressedThisFrame()) Vomit(a);
                if (command != null && command.WasPressedThisFrame()) Command();
            }
            a.Move(move.ReadValue<Vector2>());

            // mendekati toples: aura dan ukuran teman masuk HUD, teman bicara sekali (desain-v2 jawaban 27, pilihan C)
            NearbyJar = FindNear<Jar>(a, peekRadius);
            if (NearbyJar != null && !NearbyJar.IsPeeked)
            {
                NearbyJar.Peek();
                var r = NearbyJar.Recipe;
                if (r != null && !string.IsNullOrEmpty(r.JarLine)) Say(r.FriendName + ": \"" + r.JarLine + "\"", 4f);
            }
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

        // Pesan HUD dari objek level (pintu terkunci, celah terlalu sempit, ...).
        public void Notify(string text) => Say(text);

        private void Say(string text, float seconds = 3f)
        {
            Message = text;
            messageTimer = seconds;
        }

        // Tombol E. Urutan: makan esens -> tiruan ke toples -> lentera -> tuas.
        // Esens di dekat toples tetap bisa dimakan dulu (masuk toples juga bisa lewat Space).
        public bool Interact()
        {
            var a = Active;
            if (a == null || a.IsEating) return false;

            var pickup = a.FindPickup();
            if (pickup != null)
            {
                if (!a.Body.State.CanEat) { Say("Too full. Split (Space) or spit one out (Q) first."); return true; }
                if (a.TryEat()) return true;
            }

            var jar = FindNear<Jar>(a, interactRadius);
            if (jar != null && jar.HasFriend) return InsertIntoJar(a, jar);

            // padamkan lentera: butuh tubuh dingin
            var lamp = FindNear<Extinguishable>(a, interactRadius);
            if (lamp != null && lamp.IsLit)
            {
                if (a.Body.State.Get(TraitKind.Temperature) != "dingin")
                {
                    Say("You need a cold body (Frost or Dew essence) to put this out.");
                    return true;
                }
                lamp.Extinguish();
                Say("The lantern hisses out. Nearby guards heard it!");
                return true;
            }

            // tarik tuas: buka/tutup pintu yang tersambung (berisik)
            var lever = FindNear<Lever>(a, interactRadius);
            if (lever != null)
            {
                lever.Pull();
                Say(lever.Message);
                return true;
            }
            return false;
        }

        // Muntahkan esens terakhir (koreksi urutan makan).
        private void Vomit(SlimePiece a)
        {
            var st = a.Body.State;
            var last = st.Eaten.Count > 0 ? st.Eaten[st.Eaten.Count - 1] : null;
            if (a.TryVomit()) { Say("Spat out " + (last != null ? last.DisplayName : "essence") + "."); return; }
            Say(st.Eaten.Count == 0 ? "Nothing to spit out." : "Too small to spit anything out.");
        }

        // Tombol F:
        //   ada teman yang menunggu (atau mengikuti potongan lain) dalam jangkauan bisik -> mereka ikut potongan ini
        //   (berbaris di belakang teman yang sudah mengikuti);
        //   kalau tidak ada -> semua teman yang mengikuti potongan ini disuruh menunggu di tempat.
        public bool Command()
        {
            var a = Active;
            if (a == null) return false;
            var called = new List<FriendSlime>();
            bool anyFar = false;
            foreach (var f in FriendSlime.All)
            {
                if (f == null) continue;
                bool idle = f.State == FriendSlime.Mode.Waiting || (f.State == FriendSlime.Mode.Following && f.Leader != a);
                if (!idle) continue;
                if (f.CanHear(a)) called.Add(f); else anyFar |= f.State == FriendSlime.Mode.Waiting;
            }
            if (called.Count > 0)
            {
                called.Sort((x, y) => Vector2.Distance(x.Center, a.Center).CompareTo(Vector2.Distance(y.Center, a.Center)));
                foreach (var f in called) f.Follow(a);
                Say(called.Count == 1 ? called[0].Name + ": \"Right behind you!\"" : "\"Right behind you!\"");
                return true;
            }
            var waiting = new List<FriendSlime>();
            foreach (var f in FriendSlime.All)
                if (f != null && f.State == FriendSlime.Mode.Following && f.Leader == a) waiting.Add(f);
            if (waiting.Count > 0)
            {
                foreach (var f in waiting) f.Wait();
                Say(waiting.Count == 1 ? waiting[0].Name + ": \"I'll wait here.\"" : "\"We'll wait here.\"");
                return true;
            }
            Say(anyFar ? "Too far away. Get closer and whisper (F)." : "Nobody here to call.");
            return false;
        }

        // Space di depan toples: membelah langsung ke dalam toples. Potongan bermassa = ukuran teman menjadi tiruan,
        // esens paling baru ikut ke tiruan (aura = esens terakhir), pemain menyimpan sisanya.
        private bool InsertIntoJar(SlimePiece a, Jar jar)
        {
            if (Time.time < nextSplit) return false;
            int need = jar.Size;
            var st = a.Body.State;
            string who = jar.Recipe != null ? jar.Recipe.FriendName : "Your friend";
            if (st.Mass < need + 1)
            {
                Say(who + " needs a copy of size " + need + ". You are size " + st.Mass + ": eat " + (need + 1 - st.Mass) +
                    " more essence first.");
                return true;
            }
            StartCoroutine(InsertRoutine(a, jar, need));
            return true;
        }

        private System.Collections.IEnumerator InsertRoutine(SlimePiece a, Jar jar, int need)
        {
            nextSplit = Time.time + splitCooldown;
            lockTimer = splitWindup + 0.45f;
            a.Motor.Stop();
            if (a.Jelly != null) a.Jelly.Hold(0.22f);
            AudioDirector.Play(AudioDirector.Sfx.Split, a.Center);
            yield return new WaitForSeconds(splitWindup * 0.6f);
            if (a.Jelly != null) a.Jelly.Hold(-0.28f);
            yield return new WaitForSeconds(splitWindup * 0.4f);
            if (a.Jelly != null) a.Jelly.Release();
            if (a == null || jar == null || !jar.HasFriend) yield break;

            var decoy = a.Body.State.Split(need);
            if (decoy == null) yield break;
            a.Body.SetState(a.Body.State);
            int miss = jar.MismatchOf(decoy);
            Transformed?.Invoke(a.Center, null, false);

            // gumpalan tiruan melompat masuk ke mulut toples
            Vector3 mouth = jar.transform.position + Vector3.up * 0.25f * jar.transform.localScale.y;
            var blob = SpawnLoosePiece(a, decoy, a.transform.position);
            blob.PlaySplitArc(a.transform.position, mouth, 0.3f);
            if (a.Jelly != null) a.Jelly.Kick(-9f);
            yield return new WaitForSeconds(0.28f);
            if (blob != null) Destroy(blob.gameObject);

            // teman keluar di sisi toples yang menghadap pemain
            Vector2 jarPos = jar.transform.position;
            Vector2 dir = (Vector2)a.transform.position - jarPos;
            dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector2.down;
            Vector2 exit = jarPos + dir * 0.6f * jar.transform.localScale.x;
            var friend = jar.Swap(decoy, exit);
            string who = jar.Recipe != null ? jar.Recipe.FriendName : "Your friend";
            Say(miss == 0 ? who + " is free! Press F so " + who + " follows you."
                          : who + " is free, but the copy has the wrong aura. Guards will get suspicious faster.", 4f);
            if (friend == null) Debug.LogWarning("Jar: friendPrefab kosong, teman tidak muncul.", jar);
        }

        // Potongan lepas di luar daftar `pieces` (untuk animasi tiruan masuk toples).
        private SlimePiece SpawnLoosePiece(SlimePiece a, SlimeBodyState state, Vector3 at)
        {
            if (a.Jelly != null) a.Jelly.ResetPose();
            var go = Instantiate(a.gameObject, at, Quaternion.identity, a.transform.parent);
            go.name = "Slime_Tiruan";
            var piece = go.GetComponent<SlimePiece>();
            piece.Body.SetState(state);
            piece.Face(a.Facing);
            piece.SetControlled(false);
            return piece;
        }

        // Tombol C: keluar kalau sedang sembunyi, masuk ke tempat sembunyi terdekat kalau tidak.
        // Teman yang mengikuti ikut masuk kalau muat (berdua = lebih cepat dicurigai), kalau tidak menunggu di sebelahnya.
        public bool ToggleHide(SlimePiece a)
        {
            if (a == null || a.IsTransforming) return false;
            if (a.HidingIn != null)
            {
                var spot = a.HidingIn;
                if (!a.ExitHiding()) return false;
                Transformed?.Invoke(spot.Center, spot, false);
                var inside = new List<FriendSlime>(spot.Friends);
                Vector2 at = spot.ExitPoint(a.transform.position, 0.25f);
                foreach (var f in inside) { spot.Leave(f); f.Unhide(a, at); }
                return true;
            }
            var target = FindNear<HidingSpot>(a, hideRadius);
            if (target == null) { Say("Nothing to hide in here."); return false; }
            if (!target.HasRoom()) { Say("No room in there."); return false; }
            if (!a.EnterHiding(target)) return false;
            Transformed?.Invoke(a.Center, target, true);
            var vent = Vent.Of(target);
            int evacuated = 0;
            foreach (var f in new List<FriendSlime>(FriendSlime.All))
            {
                if (f == null || f.State != FriendSlime.Mode.Following || f.Leader != a) continue;
                // hanya teman yang tepat di belakang yang ikut masuk sekarang; yang jauh menyusul lewat jejak
                bool close = Vector2.Distance(f.transform.position, a.transform.position) <= friendJoinRadius;
                if (vent != null) { if (close) { f.Save(); evacuated++; } }   // ventilasi: teman dievakuasi = selamat
                else if (!close) f.Wait();
                else if (target.HasRoom()) { target.Enter(f); f.Hide(target.transform.position); }
                else { f.Wait(); Say(f.Name + ": \"No room for me! I'll stay low here.\""); }
            }
            if (vent != null) EnteredVent(a, vent, evacuated);
            return true;
        }

        // Masuk ventilasi: checkpoint, pesan, dan exit level kalau ini lubang exit.
        private void EnteredVent(SlimePiece a, Vent vent, int evacuated)
        {
            if (vent.SavesCheckpoint) Checkpoint.Save(this);
            if (vent.ExitsLevel && goal != null)
            {
                if (goal.TryComplete(a)) return;
                int left = goal.Required - goal.Saved;
                Say(left == 1 ? "Someone is still trapped. Bring them here first." : left + " friends are still trapped.");
                return;
            }
            Say(evacuated > 0 ? "Safe in the vent. Your friend will wait here for you." : "Safe in the vent. A/D: other openings. C: come out.", 4f);
        }

        // A/D di dalam ventilasi: pindah ke lubang lain di jaringan yang sama (kamera ikut).
        private void TravelVent(SlimePiece a, Vent from, int dir)
        {
            var to = from.Neighbor(dir);
            if (to == null) { Say("This vent doesn't lead anywhere else."); return; }
            if (!a.MoveHiddenTo(to.Spot)) return;
            AudioDirector.Play(AudioDirector.Sfx.SlimeStep, a.Center, 0.4f, 0.1f);
            if (to.ExitsLevel && goal != null) goal.TryComplete(a);
        }

        // Dipanggil Checkpoint: susun ulang potongan sesuai keadaan saat checkpoint.
        // Potongan pertama di scene dipakai ulang, potongan lain dibuat dari salinannya.
        public void RestorePieces(List<Checkpoint.PieceState> states, List<HidingSpot> spots)
        {
            if (states == null || states.Count == 0) return;
            pieces.RemoveAll(p => p == null);
            if (pieces.Count == 0) return;
            var template = pieces[0];
            for (int i = pieces.Count - 1; i >= 1; i--) { Destroy(pieces[i].gameObject); pieces.RemoveAt(i); }
            int active = 0;
            for (int i = 0; i < states.Count; i++)
            {
                var s = states[i];
                SlimePiece p = i == 0 ? template
                    : Instantiate(template.gameObject, s.position, Quaternion.identity, template.transform.parent).GetComponent<SlimePiece>();
                if (i > 0) { p.name = "Slime_Potongan_" + i; pieces.Add(p); }
                p.Body.SetState(SlimeBodyState.From(s.clearUnits, s.eaten));
                var rb = p.GetComponent<Rigidbody2D>();
                p.transform.position = s.position;
                if (rb != null) rb.position = s.position;
                if (s.active) active = i;
                var spot = i < spots.Count ? spots[i] : null;
                if (spot != null) p.EnterHidingInstant(spot);
            }
            SetActive(active);
            Say("Back in the vent. Press C to come out.", 4f);
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
            if (a == null) return false;
            var jar = FindNear<Jar>(a, interactRadius);
            if (jar != null && jar.HasFriend) return InsertIntoJar(a, jar);
            // toples berisi teman di dekat tapi belum terjangkau: jangan belah sembarangan di sebelahnya
            var close = FindNear<Jar>(a, peekRadius);
            if (close != null && close.HasFriend)
            {
                Say("Get closer to the jar, then split (Space).");
                return false;
            }
            if (!CanSplit(a))
            {
                if (a.IsEating || a.IsTransforming) return false;
                if (!a.Body.State.CanSplit) Say("Too small to split. Eat some essence first.");
                else if (pieces.Count >= maxPieces) Say("You can only split in two. Touch your other half to merge (Space).");
                else Say("No room to split here.");
                return false;
            }
            if (Time.time < nextSplit) return false;
            StartCoroutine(SplitRoutine(a));
            return true;
        }

        private bool CanSplit(SlimePiece a)
        {
            if (a == null || a.IsEating || a.IsTransforming || pieces.Count >= maxPieces || !a.Body.State.CanSplit) return false;
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

        // Belah langsung (tanpa ancang). Potongan baru mengambil setengah massa (dibulatkan ke atas) dari unit TERAKHIR:
        // esens paling baru ikut ke potongan baru (umpan menyala/berbau). Dipakai SplitRoutine dan test.
        public bool TrySplit()
        {
            var a = Active;
            if (!CanSplit(a)) return false;
            int k = Mathf.CeilToInt(a.Body.State.Mass / 2f);
            var newState = a.Body.State.Split(k);
            if (newState == null) return false;
            a.Body.SetState(a.Body.State);
            Vector2 offset = (Vector2)a.Facing * splitDistance;

            var from = a.transform.position;
            var to = from + (Vector3)offset;
            var piece = SpawnLoosePiece(a, newState, from);
            piece.name = "Slime_Potongan_" + pieces.Count;
            piece.PlaySplitArc(from, to);
            if (a.Jelly != null) a.Jelly.Kick(-9f);      // induk memipih saat "memuntahkan" potongan
            a.Motor.Stop();
            pieces.Add(piece);
            Transformed?.Invoke(a.Center, null, false);
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

        // Potongan aktif menyerap potongan lain yang bersentuhan. Massa dijumlah, aura potongan aktif yang menang.
        public bool Merge(SlimePiece other)
        {
            var a = Active;
            if (a == null || other == null || other == a) return false;
            if (Vector2.Distance(a.Center, other.Center) > a.Radius + other.Radius + mergeGap) return false;
            if (a.Body.State.Mass + other.Body.State.Mass > SlimeBodyState.MaxMass)
            {
                Say("Too big to merge back (max size " + SlimeBodyState.MaxMass + ").");
                return false;
            }
            a.Body.Absorb(other.Body.State);
            // teman yang mengikuti potongan yang diserap pindah mengikuti potongan aktif
            foreach (var f in FriendSlime.All)
                if (f != null && f.State == FriendSlime.Mode.Following && f.Leader == other) f.Follow(a);
            pieces.Remove(other);
            activeIndex = pieces.IndexOf(a);
            other.PlayAbsorbInto(a);                     // tersedot lalu hilang (animasi)
            a.Motor.Stop();
            lockTimer = mergeLock;
            AudioDirector.Play(AudioDirector.Sfx.Merge, a.Center);
            Transformed?.Invoke(a.Center, null, false);
            return true;
        }

        public void SwitchNext()
        {
            if (pieces.Count < 2) return;
            var old = Active;
            if (old != null && old.IsEating) return;
            SetActive((activeIndex + 1) % pieces.Count);
            // yang ditinggal diam di tempat, yang baru "bangun" dulu sebelum bisa bergerak
            nextSwitch = Time.time + switchCooldown;
            lockTimer = switchLock;
            AudioDirector.Play(AudioDirector.Sfx.Switch, Active.Center, 0.7f);
            Transformed?.Invoke(Active.Center, null, false);
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
