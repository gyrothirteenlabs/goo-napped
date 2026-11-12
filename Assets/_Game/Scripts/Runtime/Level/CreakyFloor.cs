using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Lantai berderit (desain-v2 bagian 10): berbunyi kalau diinjak tubuh besar/berat (massa >= 3 atau ciri berat)
    // atau teman besar. Slime kecil lewat tanpa suara. Butuh Collider2D (trigger) seukuran tile.
    [RequireComponent(typeof(Collider2D))]
    public class CreakyFloor : MonoBehaviour
    {
        [SerializeField] private float noiseRadius = 3.5f;
        [Tooltip("Jeda minimum antar derit (detik).")]
        [SerializeField] private float cooldown = 0.6f;

        private readonly HashSet<SlimePiece> pieces = new HashSet<SlimePiece>();
        private readonly HashSet<FriendSlime> friends = new HashSet<FriendSlime>();
        private float next;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.GetComponentInParent<SlimePiece>();
            if (p != null) { pieces.Add(p); p.Motor.PhaseEntered += OnLand; Check(p); return; }
            var f = other.GetComponentInParent<FriendSlime>();
            if (f != null) friends.Add(f);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var p = other.GetComponentInParent<SlimePiece>();
            if (p != null && pieces.Remove(p)) p.Motor.PhaseEntered -= OnLand;
            var f = other.GetComponentInParent<FriendSlime>();
            if (f != null) friends.Remove(f);
        }

        private void OnDisable()
        {
            foreach (var p in pieces) if (p != null) p.Motor.PhaseEntered -= OnLand;
            pieces.Clear();
        }

        private void OnLand(SlimeMotor.HopPhase phase)
        {
            if (phase != SlimeMotor.HopPhase.Land) return;
            foreach (var p in pieces) if (p != null && p.IsControlled) Check(p);
        }

        private void Check(SlimePiece p)
        {
            if (p.IsHidden || !p.Body.State.IsHeavy) return;
            Creak();
        }

        private void Update()
        {
            foreach (var f in friends)
                if (f != null && f.IsMoving && f.Recipe != null && f.Recipe.Size >= 2) { Creak(); break; }
        }

        private void Creak()
        {
            if (Time.time < next) return;
            next = Time.time + cooldown;
            AudioDirector.Play(AudioDirector.Sfx.GolemStep, transform.position, 0.5f, 0.3f);
            NoiseBus.Emit(transform.position, noiseRadius);
        }
    }
}
