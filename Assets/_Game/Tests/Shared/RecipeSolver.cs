using System.Collections.Generic;
using System.Linq;

namespace GooNapped.Tests
{
    // QA (desain-v2 bagian 5): cari urutan makan (dan muntah) terpendek supaya TIRUAN cocok dengan teman di toples.
    // Tiruan = potongan yang dibelah langsung ke toples = `ukuran teman` unit TERAKHIR tubuh (SlimeBodyState.Split).
    // Jadi massa tubuh harus >= ukuran + 1, dan esens terakhir menentukan aura tiruan.
    // Setelah tiruan masuk, sisa tubuh jadi titik awal toples berikutnya. Dipakai validator level dan bot playtest.
    public static class RecipeSolver
    {
        public enum StepKind { Eat, Vomit }

        public struct Step
        {
            public StepKind kind;
            public IngredientData ingredient;   // Eat: esens yang dimakan. Vomit: esens yang keluar.
            public override string ToString() => kind == StepKind.Eat ? "makan " + ingredient.DisplayName : "muntah " + ingredient.DisplayName;
        }

        public class Result
        {
            public List<Step> steps = new List<Step>();
            // tubuh tepat sebelum membelah ke toples
            public int clearBefore;
            public List<IngredientData> eatenBefore = new List<IngredientData>();
            // sisa tubuh setelah tiruan masuk toples
            public int clearAfter;
            public List<IngredientData> eatenAfter = new List<IngredientData>();
            public List<IngredientData> pool = new List<IngredientData>();
            public override string ToString() => steps.Count == 0 ? "(tanpa langkah)" : string.Join(", ", steps);
        }

        private class Node
        {
            public int clear;
            public List<IngredientData> eaten, pool;
            public List<Step> steps;
        }

        public static SlimeBodyState StateOf(int clear, IEnumerable<IngredientData> eaten) => SlimeBodyState.From(clear, eaten);

        // Tubuh awal pemain (1 unit bening) setelah makan `eaten`.
        public static SlimeBodyState StateOf(IEnumerable<IngredientData> eaten) => SlimeBodyState.From(1, eaten);

        // Tiruan untuk teman ini dari tubuh (null kalau massa kurang).
        public static SlimeBodyState CopyFor(SlimeBodyState body, RecipeData friend) => body.Clone().Split(friend.Size);

        public static bool CopyMatches(SlimeBodyState body, RecipeData friend)
        {
            var copy = CopyFor(body, friend);
            return copy != null && copy.MismatchCount(friend.Traits) == 0;
        }

        // BFS dari tubuh (clear, eatenSoFar) dengan esens `pool` di lantai. null = tidak ada solusi.
        public static Result Solve(int clear, IEnumerable<IngredientData> eatenSoFar, IEnumerable<IngredientData> pool, RecipeData friend,
            int maxDepth = 6, bool allowVomit = true)
        {
            var start = new Node { clear = clear, eaten = eatenSoFar.ToList(), pool = pool.ToList(), steps = new List<Step>() };
            var queue = new Queue<Node>();
            var seen = new HashSet<string>();
            queue.Enqueue(start);
            seen.Add(Key(start));
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                var body = StateOf(n.clear, n.eaten);
                if (CopyMatches(body, friend))
                {
                    var after = body.Clone();
                    after.Split(friend.Size);
                    return new Result
                    {
                        steps = n.steps, clearBefore = n.clear, eatenBefore = n.eaten,
                        clearAfter = after.ClearUnits, eatenAfter = after.Eaten.ToList(), pool = n.pool,
                    };
                }
                if (n.steps.Count >= maxDepth) continue;
                if (body.CanEat)
                    foreach (var ing in n.pool.Distinct())
                    {
                        var c = new Node { clear = n.clear, eaten = new List<IngredientData>(n.eaten) { ing }, pool = new List<IngredientData>(n.pool), steps = new List<Step>(n.steps) };
                        c.pool.Remove(ing);
                        c.steps.Add(new Step { kind = StepKind.Eat, ingredient = ing });
                        if (seen.Add(Key(c))) queue.Enqueue(c);
                    }
                if (allowVomit && body.CanVomit)
                {
                    var last = n.eaten[n.eaten.Count - 1];
                    var c = new Node { clear = n.clear, eaten = new List<IngredientData>(n.eaten), pool = new List<IngredientData>(n.pool) { last }, steps = new List<Step>(n.steps) };
                    c.eaten.RemoveAt(c.eaten.Count - 1);
                    c.steps.Add(new Step { kind = StepKind.Vomit, ingredient = last });
                    if (seen.Add(Key(c))) queue.Enqueue(c);
                }
            }
            return null;
        }

        // Beberapa toples berurutan dari tubuh awal (1 unit bening). Daftar hasil per toples, atau null kalau ada yang tidak bisa.
        public static List<Result> SolveSequence(IEnumerable<IngredientData> pool, IEnumerable<RecipeData> friends)
        {
            var list = new List<Result>();
            int clear = 1;
            IEnumerable<IngredientData> eaten = new List<IngredientData>();
            foreach (var f in friends)
            {
                // tanpa muntah dulu (lebih mudah untuk pemain), baru dengan muntah
                var res = Solve(clear, eaten, pool, f, allowVomit: false) ?? Solve(clear, eaten, pool, f, allowVomit: true);
                if (res == null) return null;
                list.Add(res);
                clear = res.clearAfter;
                eaten = res.eatenAfter;
                pool = res.pool;
            }
            return list;
        }

        private static string Key(Node n) =>
            n.clear + "|" + string.Join(",", n.eaten.Select(i => i.name)) + "|" + string.Join(",", n.pool.Select(i => i.name).OrderBy(x => x));
    }
}
