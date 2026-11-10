using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // A* di grid tile (logika murni, bisa di-test). 8 arah; diagonal hanya boleh kalau dua sel tegaknya bebas
    // (tidak memotong sudut tembok).
    public static class GridPath
    {
        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        // Hasil: sel dari start (tidak termasuk) sampai goal (termasuk). false kalau tidak ada jalan.
        public static bool Find(Func<Vector2Int, bool> walkable, Vector2Int start, Vector2Int goal,
            List<Vector2Int> result, int maxNodes = 4000)
        {
            result.Clear();
            if (start == goal) return true;
            if (!walkable(goal)) return false;

            var open = new List<Vector2Int> { start };
            var came = new Dictionary<Vector2Int, Vector2Int>();
            var g = new Dictionary<Vector2Int, float> { [start] = 0f };
            var f = new Dictionary<Vector2Int, float> { [start] = H(start, goal) };
            var closed = new HashSet<Vector2Int>();
            int expanded = 0;

            while (open.Count > 0 && expanded++ < maxNodes)
            {
                int bi = 0;
                for (int i = 1; i < open.Count; i++)
                    if (f[open[i]] < f[open[bi]]) bi = i;
                var cur = open[bi];
                if (cur == goal)
                {
                    for (var c = goal; c != start; c = came[c]) result.Add(c);
                    result.Reverse();
                    return true;
                }
                open.RemoveAt(bi);
                closed.Add(cur);

                foreach (var d in Dirs)
                {
                    var n = cur + d;
                    if (closed.Contains(n) || !walkable(n)) continue;
                    bool diag = d.x != 0 && d.y != 0;
                    if (diag && (!walkable(new Vector2Int(cur.x + d.x, cur.y)) || !walkable(new Vector2Int(cur.x, cur.y + d.y))))
                        continue;   // jangan memotong sudut
                    float cost = g[cur] + (diag ? 1.4142f : 1f);
                    if (g.TryGetValue(n, out var old) && cost >= old) continue;
                    came[n] = cur;
                    g[n] = cost;
                    f[n] = cost + H(n, goal);
                    if (!open.Contains(n)) open.Add(n);
                }
            }
            return false;
        }

        // Jarak oktil (cocok untuk 8 arah).
        private static float H(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
            return Mathf.Max(dx, dy) + 0.4142f * Mathf.Min(dx, dy);
        }
    }
}
