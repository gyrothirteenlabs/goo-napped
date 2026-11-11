using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GooNapped.Tests
{
    // QA: gambaran grid satu level (dari objek scene), bisa dipakai di Edit mode maupun Play mode.
    // Sel = tile 1x1 unit. Dipakai validator level dan bot playtest untuk mencari jalan.
    public class LevelMap
    {
        public Tilemap floor, walls;
        public readonly List<SlimePiece> pieces = new List<SlimePiece>();
        public readonly List<Jar> jars = new List<Jar>();
        public readonly List<IngredientPickup> pickups = new List<IngredientPickup>();
        public readonly List<LevelExit> exits = new List<LevelExit>();
        public readonly List<Door> doors = new List<Door>();
        public readonly List<Lever> levers = new List<Lever>();
        public readonly List<PressurePlate> plates = new List<PressurePlate>();
        public readonly List<Crawlspace> crawlspaces = new List<Crawlspace>();
        public readonly List<Guard> guards = new List<Guard>();
        public readonly List<LevelGoal> goals = new List<LevelGoal>();
        public readonly List<PlayerBrain> brains = new List<PlayerBrain>();
        public readonly List<Extinguishable> lamps = new List<Extinguishable>();
        // v2: ventilasi (aman, checkpoint, exit) dan tempat sembunyi
        public readonly List<Vent> vents = new List<Vent>();
        public readonly List<HidingSpot> spots = new List<HidingSpot>();

        // Semua titik selesai level: LevelExit (v1) dan lubang ventilasi exit (v2).
        public IEnumerable<Component> ExitPoints()
        {
            foreach (var e in exits) yield return e;
            foreach (var v in vents) if (v.ExitsLevel) yield return v;
            foreach (var k in kingJars) yield return k;   // adegan final: toples raja menggantikan exit
        }
        public readonly List<KingJar> kingJars = new List<KingJar>();
        public readonly List<AuraValve> valves = new List<AuraValve>();

        // Sel yang ditutup prop padat (toples, lentera, ...): badan slime (r 0.34) tidak muat lewat.
        public readonly HashSet<Vector2Int> propBlocked = new HashSet<Vector2Int>();
        public readonly HashSet<Vector2Int> crawlCells = new HashSet<Vector2Int>();
        public readonly Dictionary<Vector2Int, Door> doorCells = new Dictionary<Vector2Int, Door>();
        // Pintu yang punya tuas/pelat (bisa dibuka pemain).
        public readonly HashSet<Door> switchedDoors = new HashSet<Door>();

        public static Vector2Int Cell(Vector2 world) => new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
        public static Vector2 CenterOf(Vector2Int c) => new Vector2(c.x + 0.5f, c.y + 0.5f);

        public static LevelMap From(IEnumerable<GameObject> roots)
        {
            var m = new LevelMap();
            var solids = new List<Collider2D>();
            foreach (var r in roots)
            {
                foreach (var tm in r.GetComponentsInChildren<Tilemap>(true))
                {
                    if (tm.GetComponent<TilemapCollider2D>() != null) m.walls = tm;
                    else if (tm.name == "Floor") m.floor = tm;
                }
                m.pieces.AddRange(r.GetComponentsInChildren<SlimePiece>(true));
                m.jars.AddRange(r.GetComponentsInChildren<Jar>(true));
                m.pickups.AddRange(r.GetComponentsInChildren<IngredientPickup>(true));
                m.exits.AddRange(r.GetComponentsInChildren<LevelExit>(true));
                m.doors.AddRange(r.GetComponentsInChildren<Door>(true));
                m.levers.AddRange(r.GetComponentsInChildren<Lever>(true));
                m.plates.AddRange(r.GetComponentsInChildren<PressurePlate>(true));
                m.crawlspaces.AddRange(r.GetComponentsInChildren<Crawlspace>(true));
                m.guards.AddRange(r.GetComponentsInChildren<Guard>(true));
                m.goals.AddRange(r.GetComponentsInChildren<LevelGoal>(true));
                m.brains.AddRange(r.GetComponentsInChildren<PlayerBrain>(true));
                m.lamps.AddRange(r.GetComponentsInChildren<Extinguishable>(true));
                m.vents.AddRange(r.GetComponentsInChildren<Vent>(true));
                m.kingJars.AddRange(r.GetComponentsInChildren<KingJar>(true));
                m.valves.AddRange(r.GetComponentsInChildren<AuraValve>(true));
                m.spots.AddRange(r.GetComponentsInChildren<HidingSpot>(true));
                foreach (var c in r.GetComponentsInChildren<Collider2D>(true))
                {
                    if (c.isTrigger || c is TilemapCollider2D || c is CompositeCollider2D) continue;
                    // includeInactive: bot mematikan penjaga; collider penjaga tetap bukan tembok
                    if (c.GetComponentInParent<SlimePiece>(true) != null || c.GetComponentInParent<Guard>(true) != null) continue;
                    if (!c.gameObject.activeInHierarchy && Application.isPlaying) continue;
                    if (c.GetComponent<Door>() != null || c.GetComponent<Crawlspace>() != null) continue;
                    if (c.name == "CameraBounds") continue;
                    solids.Add(c);
                }
            }
            foreach (var c in solids)
            {
                var b = WorldBounds(c);
                for (int x = Mathf.FloorToInt(b.min.x); x <= Mathf.FloorToInt(b.max.x - 1e-4f); x++)
                    for (int y = Mathf.FloorToInt(b.min.y); y <= Mathf.FloorToInt(b.max.y - 1e-4f); y++)
                        m.propBlocked.Add(new Vector2Int(x, y));
            }
            foreach (var d in m.doors) m.doorCells[d.Cell] = d;
            foreach (var cs in m.crawlspaces)
            {
                var box = cs.GetComponent<BoxCollider2D>();
                m.crawlCells.Add(Cell(cs.transform.TransformPoint(box.offset)));
            }
            foreach (var l in m.levers) foreach (var d in l.Doors) if (d != null) m.switchedDoors.Add(d);
            foreach (var p in m.plates) foreach (var d in p.Doors) if (d != null) m.switchedDoors.Add(d);
            return m;
        }

        // Bounds dari bentuk collider (tidak bergantung pada simulasi fisika, jadi jalan juga di Edit mode).
        public static Bounds WorldBounds(Collider2D c)
        {
            var t = c.transform;
            switch (c)
            {
                case BoxCollider2D box:
                {
                    var center = t.TransformPoint(box.offset);
                    var size = Vector2.Scale(box.size, t.lossyScale);
                    return new Bounds(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 0f));
                }
                case CircleCollider2D circle:
                {
                    var center = t.TransformPoint(circle.offset);
                    float r = circle.radius * Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
                    return new Bounds(center, new Vector3(r * 2f, r * 2f, 0f));
                }
                default:
                    return new Bounds(t.position, new Vector3(0.5f, 0.5f, 0f));
            }
        }

        public bool IsFloor(Vector2Int c)
        {
            var p = new Vector3Int(c.x, c.y, 0);
            if (walls != null && walls.HasTile(p)) return false;
            return floor == null || floor.HasTile(p);
        }

        // Sel bisa dilewati SLIME? heavy = tubuh berat (tidak muat celah). openSwitchedDoors = pintu bertuas/pelat dianggap bisa dibuka.
        public bool SlimeWalkable(Vector2Int c, bool heavy, bool openSwitchedDoors)
        {
            if (!IsFloor(c) || propBlocked.Contains(c)) return false;
            if (crawlCells.Contains(c)) return !heavy;
            if (doorCells.TryGetValue(c, out var d)) return d.IsOpen || d.StartsOpen || (openSwitchedDoors && switchedDoors.Contains(d));
            return true;
        }

        // Sel bisa dilewati PENJAGA (pintu sesuai keadaan awal, celah tertutup).
        public bool GuardWalkable(Vector2Int c)
        {
            if (!IsFloor(c) || propBlocked.Contains(c) || crawlCells.Contains(c)) return false;
            if (doorCells.TryGetValue(c, out var d)) return d.IsOpen || d.StartsOpen;
            return true;
        }

        // Garis pandang kasar di grid: terhalang tembok, celah, atau pintu tertutup.
        public bool LineOfSight(Vector2 a, Vector2 b)
        {
            float len = Vector2.Distance(a, b);
            int n = Mathf.CeilToInt(len / 0.1f);
            for (int i = 1; i < n; i++)
            {
                var c = Cell(Vector2.Lerp(a, b, i / (float)n));
                if (!IsFloor(c) || crawlCells.Contains(c)) return false;
                if (doorCells.TryGetValue(c, out var d) && !(d.IsOpen || d.StartsOpen)) return false;
            }
            return true;
        }

        // Semua sel yang terjangkau dari `start` (flood fill 4 arah).
        public HashSet<Vector2Int> Reachable(Vector2Int start, System.Func<Vector2Int, bool> walkable)
        {
            var seen = new HashSet<Vector2Int>();
            if (!walkable(start)) return seen;
            var q = new Queue<Vector2Int>();
            q.Enqueue(start);
            seen.Add(start);
            var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var d in dirs)
                {
                    var n = c + d;
                    if (seen.Contains(n) || !walkable(n)) continue;
                    if (seen.Count > 20000) return seen;
                    seen.Add(n);
                    q.Enqueue(n);
                }
            }
            return seen;
        }

        // Sel tempat sebuah objek "berdiri" (pusat collider, atau pivot + 0.25).
        public static Vector2Int CellOfObject(Component c)
        {
            var col = c.GetComponent<Collider2D>();
            if (col != null) return Cell(WorldBounds(col).center);
            return Cell((Vector2)c.transform.position + Vector2.up * 0.25f);
        }

        // Tetangga bebas (untuk berdiri di samping prop padat seperti toples).
        public IEnumerable<Vector2Int> Neighbors4(Vector2Int c)
        {
            yield return c + Vector2Int.down;
            yield return c + Vector2Int.left;
            yield return c + Vector2Int.right;
            yield return c + Vector2Int.up;
        }
    }
}
