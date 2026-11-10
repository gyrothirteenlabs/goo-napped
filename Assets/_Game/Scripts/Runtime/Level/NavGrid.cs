using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace GooNapped
{
    // Peta jalan penjaga untuk scene aktif: sel boleh dilewati = ada tile lantai (Grid/Floor) dan tidak ada tile tembok
    // (Grid/Walls). Dibaca sekali per scene. Scene tanpa Tilemap (sandbox) = tidak ada peta, penjaga jalan lurus.
    public static class NavGrid
    {
        private static Tilemap floor, walls;
        private static bool built;
        private static readonly List<Vector2Int> cells = new List<Vector2Int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            built = false;
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        private static void OnLoaded(Scene s, LoadSceneMode m) => built = false;

        private static void Build()
        {
            built = true;
            floor = walls = null;
            foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (tm.GetComponent<TilemapCollider2D>() != null) walls = tm;
                else if (tm.name == "Floor") floor = tm;
            }
        }

        public static bool Available
        {
            get
            {
                if (!built || walls == null) Build();
                return walls != null;
            }
        }

        public static Vector2Int CellOf(Vector2 world) => new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
        public static Vector2 CenterOf(Vector2Int cell) => new Vector2(cell.x + 0.5f, cell.y + 0.5f);

        public static bool Walkable(Vector2Int c)
        {
            var p = new Vector3Int(c.x, c.y, 0);
            if (walls.HasTile(p)) return false;
            return floor == null || floor.HasTile(p);
        }

        // Jalur dari `from` ke `to` (posisi dunia, titik tengah badan) sebagai titik dunia tengah sel.
        public static bool FindPath(Vector2 from, Vector2 to, List<Vector2> result)
        {
            result.Clear();
            if (!Available) return false;
            var start = CellOf(from);
            var goal = CellOf(to);
            if (!Walkable(goal)) goal = NearestWalkable(goal);
            if (!GridPath.Find(Walkable, start, goal, cells)) return false;
            foreach (var c in cells) result.Add(CenterOf(c));
            if (result.Count > 0) result[result.Count - 1] = Walkable(CellOf(to)) ? to : result[result.Count - 1];
            return true;
        }

        private static Vector2Int NearestWalkable(Vector2Int c)
        {
            for (int r = 1; r <= 2; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        var n = new Vector2Int(c.x + dx, c.y + dy);
                        if (Walkable(n)) return n;
                    }
            return c;
        }
    }
}
