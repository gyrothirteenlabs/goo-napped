using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Satu urutan cerita: cutscene bergambar dan/atau dialog. Aset di Assets/_Game/Data/Story/.
    // Penulis cerita cukup mengisi halaman di Inspector (teks + gambar), tanpa kode.
    [CreateAssetMenu(menuName = "GooNapped/Cerita (Story Sequence)", fileName = "Story_Baru")]
    public class StorySequence : ScriptableObject
    {
        public enum Layout
        {
            ImageLeft,   // gambar tinggi di kiri, baris teks muncul bertumpuk di kanan
            ImageTop,    // gambar lebar di atas, teks di bawah
            ImageOnly,   // gambar saja di tengah (teks opsional di bawah)
            Dialogue,    // kotak dialog bawah dengan potret; game tetap terlihat di belakang
        }

        [Serializable]
        public class Line
        {
            [Tooltip("Nama pembicara (opsional).")]
            public string speaker;
            [TextArea(2, 5)] public string text;
            [Tooltip("Potret untuk layout Dialogue (opsional).")]
            public Sprite portrait;
        }

        [Serializable]
        public class Page
        {
            public Layout layout = Layout.ImageLeft;
            [Tooltip("Gambar cutscene (tidak dipakai di layout Dialogue).")]
            public Sprite image;
            public List<Line> lines = new List<Line>();
            [Tooltip("> 0: halaman lanjut otomatis setelah sekian detik (teks selesai diketik). 0 = tunggu tombol.")]
            public float autoAdvance;
        }

        public List<Page> pages = new List<Page>();
        [Tooltip("Hentikan permainan (penjaga, pemain) selama cerita.")]
        public bool pauseGame = true;
        [Tooltip("Boleh dilewati dengan Esc.")]
        public bool skippable = true;

        // Bikin dialog cepat dari kode: StoryPlayer.Say(...) memakai ini.
        public static StorySequence Dialogue(string speaker, string text, Sprite portrait = null)
        {
            var s = CreateInstance<StorySequence>();
            s.name = "Dialog_" + speaker;
            var page = new Page { layout = Layout.Dialogue };
            page.lines.Add(new Line { speaker = speaker, text = text, portrait = portrait });
            s.pages.Add(page);
            return s;
        }
    }
}
