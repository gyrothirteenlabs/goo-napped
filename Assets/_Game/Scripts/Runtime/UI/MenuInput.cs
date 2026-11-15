using UnityEngine;
using UnityEngine.InputSystem;

namespace GooNapped
{
    // Input navigasi menu bersama: tekan = 1 langkah, tahan = ulang otomatis (jeda awal lalu cepat), roda mouse = naik/turun.
    // Dihitung sekali per frame supaya beberapa pemakai di frame yang sama dapat hasil yang sama.
    // Dipakai MainMenuController, PauseMenu, OptionsPanel. Waktu nyata (jalan saat pause).
    public static class MenuInput
    {
        private const float FirstDelay = 0.32f, RepeatEvery = 0.075f;

        private static int frame = -1;
        private static bool up, down, left, right, ok, back;
        private static readonly float[] next = new float[4];
        private static readonly bool[] wasHeld = new bool[4];

        public static bool Up { get { Tick(); return up; } }
        public static bool Down { get { Tick(); return down; } }
        public static bool Left { get { Tick(); return left; } }
        public static bool Right { get { Tick(); return right; } }
        public static bool Ok { get { Tick(); return ok; } }
        public static bool Back { get { Tick(); return back; } }
        public static bool AnyMove { get { Tick(); return up || down || left || right; } }

        private static void Tick()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            up = down = left = right = ok = back = false;
            var kb = Keyboard.current;
            float now = Time.realtimeSinceStartup;
            if (kb != null)
            {
                up = Repeat(0, kb.upArrowKey.isPressed || kb.wKey.isPressed, now);
                down = Repeat(1, kb.downArrowKey.isPressed || kb.sKey.isPressed, now);
                left = Repeat(2, kb.leftArrowKey.isPressed || kb.aKey.isPressed, now);
                right = Repeat(3, kb.rightArrowKey.isPressed || kb.dKey.isPressed, now);
                ok = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame;
                back = kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame;
            }
            // roda mouse: satu notch = satu langkah
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float s = mouse.scroll.ReadValue().y;
                if (s > 0.01f) up = true;
                else if (s < -0.01f) down = true;
            }
        }

        private static bool Repeat(int i, bool held, float now)
        {
            bool fire = false;
            if (held && !wasHeld[i]) { fire = true; next[i] = now + FirstDelay; }
            else if (held && now >= next[i]) { fire = true; next[i] = now + RepeatEvery; }
            wasHeld[i] = held;
            return fire;
        }
    }
}
