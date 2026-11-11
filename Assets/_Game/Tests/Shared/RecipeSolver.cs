using System.Collections.Generic;
using System.Linq;

namespace GooNapped.Tests
{
    // QA: cari urutan makan (dan muntah) terpendek supaya tubuh cocok dengan resep, dari bahan yang ada di level.
    // Dipakai validator level (EditMode) dan bot playtest (PlayMode).
    public static class RecipeSolver
    {
        public enum StepKind { Eat, Vomit }

        public struct Step
        {
            public StepKind kind;
            public IngredientData ingredient;   // Eat: bahan yang dimakan. Vomit: bahan yang keluar.
            public override string ToString() => kind == StepKind.Eat ? "makan " + ingredient.DisplayName : "muntah " + ingredient.DisplayName;
        }

        public class Result
        {
            public List<Step> steps = new List<Step>();
            public List<IngredientData> eaten = new List<IngredientData>();
            public List<IngredientData> pool = new List<IngredientData>();
            public override string ToString() => steps.Count == 0 ? "(tanpa langkah)" : string.Join(", ", steps);
        }

        private class Node
        {
            public List<IngredientData> eaten, pool;
            public List<Step> steps;
        }

        public static SlimeBodyState StateOf(IEnumerable<IngredientData> eaten)
        {
            var s = new SlimeBodyState();
            foreach (var i in eaten) s.Eat(i);
            return s;
        }

        // BFS. `eatenSoFar` = isi perut sekarang, `pool` = bahan yang masih tergeletak. null = tidak ada solusi.
        public static Result Solve(IEnumerable<IngredientData> eatenSoFar, IEnumerable<IngredientData> pool, RecipeData recipe,
            int maxDepth = 6, bool allowVomit = true)
        {
            var start = new Node { eaten = eatenSoFar.ToList(), pool = pool.ToList(), steps = new List<Step>() };
            var queue = new Queue<Node>();
            var seen = new HashSet<string>();
            queue.Enqueue(start);
            seen.Add(Key(start));
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                if (StateOf(n.eaten).MismatchCount(recipe.Traits) == 0)
                    return new Result { steps = n.steps, eaten = n.eaten, pool = n.pool };
                if (n.steps.Count >= maxDepth) continue;
                foreach (var ing in n.pool.Distinct())
                {
                    var c = new Node { eaten = new List<IngredientData>(n.eaten) { ing }, pool = new List<IngredientData>(n.pool), steps = new List<Step>(n.steps) };
                    c.pool.Remove(ing);
                    c.steps.Add(new Step { kind = StepKind.Eat, ingredient = ing });
                    if (seen.Add(Key(c))) queue.Enqueue(c);
                }
                if (allowVomit && n.eaten.Count > 0)
                {
                    var last = n.eaten[n.eaten.Count - 1];
                    var c = new Node { eaten = new List<IngredientData>(n.eaten), pool = new List<IngredientData>(n.pool) { last }, steps = new List<Step>(n.steps) };
                    c.eaten.RemoveAt(c.eaten.Count - 1);
                    c.steps.Add(new Step { kind = StepKind.Vomit, ingredient = last });
                    if (seen.Add(Key(c))) queue.Enqueue(c);
                }
            }
            return null;
        }

        // Beberapa toples berurutan: tubuh setelah toples pertama jadi titik awal toples kedua (potongan kedua
        // adalah salinan tubuh yang sama). Daftar hasil per toples, atau null kalau ada yang tidak bisa.
        public static List<Result> SolveSequence(IEnumerable<IngredientData> pool, IEnumerable<RecipeData> recipes)
        {
            var list = new List<Result>();
            IEnumerable<IngredientData> eaten = new List<IngredientData>();
            foreach (var r in recipes)
            {
                // tanpa muntah dulu (lebih mudah untuk pemain), baru dengan muntah
                var res = Solve(eaten, pool, r, allowVomit: false) ?? Solve(eaten, pool, r, allowVomit: true);
                if (res == null) return null;
                list.Add(res);
                eaten = res.eaten;
                pool = res.pool;
            }
            return list;
        }

        private static string Key(Node n) =>
            string.Join(",", n.eaten.Select(i => i.name)) + "|" + string.Join(",", n.pool.Select(i => i.name).OrderBy(x => x));
    }
}
