using UnityEngine;
using UnityEngine.InputSystem;

namespace GooNapped
{
    // Pause (Esc / Start) dan ulang level cepat (R / Select). Tampilan OnGUI = PLACEHOLDER sampai UI dari tim desainer.
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMap = "Player";
        [SerializeField] private string pauseAction = "Pause";
        [SerializeField] private string restartAction = "Restart";

        private InputAction pause, restart;
        private bool paused;
        private GUIStyle title, button;

        public bool IsPaused => paused;

        private void OnEnable()
        {
            if (actions == null) return;
            var map = actions.FindActionMap(actionMap, true);
            pause = map.FindAction(pauseAction, false);
            restart = map.FindAction(restartAction, false);
            map.Enable();
        }

        private void OnDisable()
        {
            if (paused) SetPaused(false);
        }

        private void Update()
        {
            if (StoryPlayer.IsPlaying) return;   // Esc dipakai untuk melewati cerita
            if (pause != null && pause.WasPressedThisFrame()) SetPaused(!paused);
            if (restart != null && restart.WasPressedThisFrame()) GameFlow.ReloadCurrent();
        }

        public void SetPaused(bool on)
        {
            paused = on;
            Time.timeScale = on ? 0f : 1f;
            AudioListener.pause = on;
        }

        private void OnGUI()
        {
            if (!paused) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                title.normal.textColor = Color.white;
                button = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            }
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float w = 260, h = 44, x = (Screen.width - w) / 2f, y = Screen.height * 0.3f;
            GUI.Label(new Rect(0, y - 70, Screen.width, 50), "PAUSED", title);
            if (GUI.Button(new Rect(x, y, w, h), "Resume (Esc)", button)) SetPaused(false);
            if (GUI.Button(new Rect(x, y + 56, w, h), "Retry (R)", button)) GameFlow.ReloadCurrent();
            if (GUI.Button(new Rect(x, y + 112, w, h), "Main menu", button)) GameFlow.LoadMainMenu();
        }
    }
}
