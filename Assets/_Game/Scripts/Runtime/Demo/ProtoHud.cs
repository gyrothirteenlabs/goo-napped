using System.Text;
using UnityEngine;

namespace GooNapped
{
    // HUD teks sementara untuk sandbox (OnGUI). BUKAN UI final: hanya supaya prototipe bisa dimainkan dan dicek.
    public class ProtoHud : MonoBehaviour
    {
        [SerializeField] private PlayerBrain brain;
        [SerializeField] private LevelGoal goal;
        [SerializeField] private int fontSize = 14;

        private GUIStyle style;
        private readonly StringBuilder sb = new StringBuilder();

        private void Awake()
        {
            if (brain == null) brain = FindFirstObjectByType<PlayerBrain>();
            if (goal == null) goal = FindFirstObjectByType<LevelGoal>();
            guards = FindObjectsByType<Guard>(FindObjectsSortMode.None);
        }

        private Guard[] guards = new Guard[0];

        private static string TraitsText(System.Collections.Generic.IEnumerable<Trait> traits)
        {
            var s = new StringBuilder();
            foreach (var t in traits) s.Append(s.Length > 0 ? ", " : "").Append(t.kind).Append(" ").Append(t.value);
            return s.Length > 0 ? s.ToString() : "netral";
        }

        private void OnGUI()
        {
            if (brain == null) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = true, wordWrap = true };
                style.normal.textColor = Color.white;
            }
            var a = brain.Active;
            sb.Length = 0;
            sb.AppendLine("WASD move | E eat/use | Space split/merge | Shift switch | C hide | F call friend | Q spit | R retry | Esc pause");
            if (a != null)
            {
                sb.Append("Tubuh: ").Append(TraitsText(a.Body.State.Traits));
                if (a.Body.State.EmitsLight) sb.Append("  [menyala]");
                if (a.Body.State.SpeedMultiplier < 1f) sb.Append("  [lambat]");
                if (!a.IsDisguised && LightExposure.At(a.Center) >= 0.35f) sb.Append("  <color=#ffc861>[kena cahaya: penjaga di dekat bisa melihat]</color>");
                if (a.Body.State.SmellRadius > 0f) sb.Append("  <color=#e6d36a>[berbau: meninggalkan jejak]</color>");
                if (a.IsDisguised) sb.Append("  [nyamar]");
                sb.AppendLine();
                sb.Append("Pieces: ").Append(brain.Pieces.Count).Append("   Size: ").Append(a.Mass)
                  .Append("   Friends safe: ").Append(goal != null ? goal.Saved : 0);
                if (goal != null) sb.Append("/").Append(goal.Required);
                sb.AppendLine();
                var jar = brain.NearbyJar;
                if (jar != null && jar.Recipe != null)
                {
                    if (jar.HasFriend)
                        sb.Append("Toples ").Append(jar.Recipe.FriendName).Append(": ").Append(TraitsText(jar.Recipe.Traits))
                          .Append("  (ciri salah: ").Append(jar.MismatchOf(a.Body.State)).AppendLine(")");
                    else
                        sb.Append("Toples berisi tiruan (ciri salah: ").Append(jar.DecoyMismatch).AppendLine(")");
                }
            }
            foreach (var g in guards)
            {
                if (g == null) continue;
                int n = Mathf.RoundToInt(g.Suspicion * 10);
                sb.Append(g.SeesTarget || g.Senses ? "<color=#ff5a5f>" : "<color=#8a92a0>").Append(g.GuardName).Append(" ")
                  .Append(new string('#', n)).Append(new string('.', 10 - n))
                  .Append(g.InLight ? "  (kamu di cahaya)" : g.Smells ? "  (kamu tercium)" : "").AppendLine("</color>");
            }
            if (!string.IsNullOrEmpty(brain.Message)) sb.Append("<color=#ffc861>").Append(brain.Message).AppendLine("</color>");
            if (goal != null && goal.IsComplete) sb.AppendLine("<color=#6fd27e><b>LEVEL SELESAI</b></color>");
            if (goal != null && goal.IsFailed) sb.Append("<color=#ff5a5f><b>").Append(goal.FailReason).AppendLine(" - ulang...</b></color>");
            GUI.Label(new Rect(10, 8, Screen.width - 20, 200), sb.ToString(), style);
        }
    }
}
