using UnityEngine;

namespace GooNapped
{
    // Grid gerak per tile. 1 tile = 1 unit = 48 px (cell sprite).
    // Posisi objek (pivot sprite = tengah bawah cell) untuk tile (x, y) = Origin + (x + 0.5, y),
    // jadi cell sprite 48x48 menutup tile itu persis.
    public static class GridUtil
    {
        // Sudut kiri-bawah tile (0, 0) di dunia. Diisi LevelGrid di tiap scene.
        public static Vector2 Origin = Vector2.zero;

        public static readonly Vector2Int[] Directions =
        {
            Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left,
        };

        public static Vector3 CellToWorld(Vector2Int cell) =>
            new Vector3(Origin.x + cell.x + 0.5f, Origin.y + cell.y, 0f);

        public static Vector2 CellCenter(Vector2Int cell) =>
            new Vector2(Origin.x + cell.x + 0.5f, Origin.y + cell.y + 0.5f);

        public static Vector2Int WorldToCell(Vector2 world) =>
            new Vector2Int(Mathf.FloorToInt(world.x - Origin.x), Mathf.RoundToInt(world.y - Origin.y));

        // Input analog -> satu arah tile (sumbu dominan). (0,0) kalau di bawah ambang.
        public static Vector2Int ToDirection(Vector2 input, float threshold = 0.5f)
        {
            if (Mathf.Abs(input.x) < threshold && Mathf.Abs(input.y) < threshold) return Vector2Int.zero;
            return Mathf.Abs(input.x) > Mathf.Abs(input.y)
                ? new Vector2Int(input.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, input.y > 0 ? 1 : -1);
        }
    }
}
