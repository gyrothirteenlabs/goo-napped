using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GooNapped.Tests
{
    public class GridPathTests
    {
        // Peta: baris atas = y terbesar. '#' tembok.
        static System.Func<Vector2Int, bool> Map(params string[] rows)
        {
            int h = rows.Length;
            return c => c.y >= 0 && c.y < h && c.x >= 0 && c.x < rows[h - 1 - c.y].Length && rows[h - 1 - c.y][c.x] != '#';
        }

        [Test]
        public void Jalur_MemutariTembok()
        {
            var walk = Map(
                ".....",
                ".###.",
                ".....");
            var path = new List<Vector2Int>();
            Assert.IsTrue(GridPath.Find(walk, new Vector2Int(0, 1), new Vector2Int(4, 1), path));
            Assert.AreEqual(new Vector2Int(4, 1), path[path.Count - 1]);
            foreach (var c in path) Assert.IsTrue(walk(c), "tidak lewat tembok: " + c);
        }

        [Test]
        public void Diagonal_TidakMemotongSudut()
        {
            var walk = Map(
                "..",
                "#.");
            var path = new List<Vector2Int>();
            // dari (0,1) ke (1,0): diagonal memotong sudut tembok (0,0) -> harus lewat (1,1)
            Assert.IsTrue(GridPath.Find(walk, new Vector2Int(0, 1), new Vector2Int(1, 0), path));
            Assert.AreEqual(2, path.Count);
            Assert.AreEqual(new Vector2Int(1, 1), path[0]);
        }

        [Test]
        public void TidakAdaJalan_False()
        {
            var walk = Map(
                ".#.",
                ".#.");
            var path = new List<Vector2Int>();
            Assert.IsFalse(GridPath.Find(walk, new Vector2Int(0, 0), new Vector2Int(2, 0), path));
        }
    }
}
