using UnityEngine;

namespace GooNapped
{
    // Satu per scene level: menentukan di mana grid tile berada (sudut kiri-bawah tile (0,0)).
    [DefaultExecutionOrder(-1000)]
    [ExecuteAlways]
    public class LevelGrid : MonoBehaviour
    {
        [Tooltip("Sudut kiri-bawah tile (0,0) di dunia. Samakan dengan sudut tile lantai.")]
        [SerializeField] private Vector2 origin;

        private void Awake() => Apply();
        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        private void Apply() => GridUtil.Origin = origin;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
            for (int x = -8; x <= 8; x++)
                for (int y = -6; y <= 6; y++)
                    Gizmos.DrawWireCube(GridUtil.CellCenter(new Vector2Int(x, y)), Vector3.one);
        }
    }
}
