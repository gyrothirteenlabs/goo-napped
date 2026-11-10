using UnityEngine;

namespace GooNapped
{
    // Menu utama + pilih level (level terkunci sampai level sebelumnya selesai).
    // Tampilan OnGUI = PLACEHOLDER sampai UI dari tim desainer.
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string gameTitle = "GOO-NAPPED";
        [SerializeField] private string subtitle = "(nama kerja) - Game Off 2026: SHIFT";

        private bool selecting;
        private GUIStyle titleStyle, subStyle, button;

        private void Start() => Time.timeScale = 1f;

        private void OnGUI()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 46, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                titleStyle.normal.textColor = new Color(0.7f, 0.95f, 0.6f);
                subStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
                subStyle.normal.textColor = new Color(0.75f, 0.78f, 0.85f);
                button = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            }
            GUI.color = new Color(0.04f, 0.05f, 0.08f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = 280, h = 44, x = (Screen.width - w) / 2f, y = Screen.height * 0.36f;
            GUI.Label(new Rect(0, y - 150, Screen.width, 60), gameTitle, titleStyle);
            GUI.Label(new Rect(0, y - 92, Screen.width, 30), subtitle, subStyle);

            if (!selecting)
            {
                string cont = GameFlow.UnlockedIndex > 0 ? "Lanjut: " + GameFlow.Levels[GameFlow.UnlockedIndex] : "Mulai";
                if (GUI.Button(new Rect(x, y, w, h), cont, button)) GameFlow.Load(GameFlow.Levels[GameFlow.UnlockedIndex]);
                if (GUI.Button(new Rect(x, y + 56, w, h), "Pilih level", button)) selecting = true;
                if (GUI.Button(new Rect(x, y + 112, w, h), "Keluar", button)) Application.Quit();
                return;
            }

            for (int i = 0; i < GameFlow.Levels.Length; i++)
            {
                bool open = GameFlow.IsUnlocked(i);
                GUI.enabled = open;
                string label = GameFlow.Levels[i].Replace('_', ' ') + (open ? "" : "  (terkunci)");
                if (GUI.Button(new Rect(x, y + i * 50, w, 42), label, button)) GameFlow.Load(GameFlow.Levels[i]);
            }
            GUI.enabled = true;
            float by = y + GameFlow.Levels.Length * 50 + 10;
            if (GUI.Button(new Rect(x, by, w * 0.48f, 38), "Kembali", button)) selecting = false;
            if (GUI.Button(new Rect(x + w * 0.52f, by, w * 0.48f, 38), "Reset progres", button)) GameFlow.ResetProgress();
        }
    }
}
