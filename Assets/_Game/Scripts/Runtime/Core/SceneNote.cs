using UnityEngine;

namespace GooNapped
{
    // Catatan untuk tim di Inspector: tujuan scene/objek, TODO, siapa yang pegang.
    // Tidak melakukan apa-apa saat runtime. Hapus kalau sudah tidak perlu.
    [AddComponentMenu("GooNapped/Scene Note")]
    public class SceneNote : MonoBehaviour
    {
        [TextArea(3, 20)] public string note;
    }
}
