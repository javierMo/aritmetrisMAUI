using System.Collections.Generic;
using System.Linq;

namespace Aritmetris.GameCore
{
    public sealed class FallingCell
    {
        public int X; public int Y; public Cell Cell;
        public FallingCell(int x, int y, Cell cell) { X = x; Y = y; Cell = cell; }
    }

    public sealed class FallingGroup
    {
        public List<FallingCell> Cells { get; } = new();
        public FallingGroup(IEnumerable<FallingCell> cells) { Cells.AddRange(cells); }

        public bool CanMove(Board board, List<FallingGroup> groups, int dx, int dy)
        {
            foreach (var c in Cells)
            {
                int nx = c.X + dx, ny = c.Y + dy;
                if (nx < 0 || nx >= board.Width || ny < 0 || ny >= board.Height) return false;
                if (!board.Grid[ny, nx].IsEmpty) return false;
                if (CollidesWithGroups(groups, this, nx, ny)) return false;
            }
            return true;
        }

        public sealed class MoveResult
        {
            public bool MovedAll;
            public bool BlockedAll;
            public List<FallingCell> Locked = new();
            public List<FallingGroup> Fragments = new();
        }

        public MoveResult TryMoveDownWithBreak(Board board, List<FallingGroup> groups)
        {
            var want = Cells.Select(c => new { c, nx = c.X, ny = c.Y + 1 }).ToList();
            string Key(int x, int y) => $"{x},{y}";
            var blockedSet = new HashSet<string>();

            // Bloqueo real: sale del tablero, choca con tablero, o choca con otros grupos
            foreach (var w in want)
            {
                bool bloqueado = w.ny >= board.Height || !board.Grid[w.ny, w.nx].IsEmpty || CollidesWithGroups(groups, this, w.nx, w.ny);
                if (bloqueado) blockedSet.Add(Key(w.c.X, w.c.Y));
            }

            // Propagación de bloqueo interna al grupo
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var w in want)
                {
                    var fromKey = Key(w.c.X, w.c.Y);
                    if (blockedSet.Contains(fromKey)) continue;

                    bool targetIsBlockedOfGroup = Cells.Any(o =>
                        !ReferenceEquals(o, w.c) &&
                        o.X == w.nx && o.Y == w.ny &&
                        blockedSet.Contains(Key(o.X, o.Y))
                    );

                    if (targetIsBlockedOfGroup)
                    {
                        blockedSet.Add(fromKey);
                        changed = true;
                    }
                }
            }

            var blocked = Cells.Where(c => blockedSet.Contains(Key(c.X, c.Y))).ToList();
            var movable = Cells.Where(c => !blockedSet.Contains(Key(c.X, c.Y))).ToList();

            var res = new MoveResult();
            if (blocked.Count == 0)
            {
                foreach (var c in Cells) c.Y += 1;
                res.MovedAll = true;
                return res;
            }
            if (blocked.Count == Cells.Count)
            {
                res.BlockedAll = true;
                return res;
            }

            res.Locked = blocked.Select(c => new FallingCell(c.X, c.Y, c.Cell)).ToList();

            var comps = ConnectedComponents(movable);
            foreach (var comp in comps)
            {
                var moved = comp.Select(c => new FallingCell(c.X, c.Y + 1, c.Cell)).ToList();
                res.Fragments.Add(new FallingGroup(moved));
            }

            return res;
        }

        private static bool CollidesWithGroups(List<FallingGroup> groups, FallingGroup self, int nx, int ny)
        {
            foreach (var g in groups)
            {
                if (ReferenceEquals(g, self)) continue;
                foreach (var oc in g.Cells)
                    if (oc.X == nx && oc.Y == ny) return true;
            }
            return false;
        }

        private static List<List<FallingCell>> ConnectedComponents(List<FallingCell> cells)
        {
            var map = cells.ToDictionary(c => (c.X, c.Y), c => c);
            var seen = new HashSet<(int, int)>();
            var comps = new List<List<FallingCell>>();

            foreach (var c in cells)
            {
                var key = (c.X, c.Y);
                if (seen.Contains(key)) continue;

                var comp = new List<FallingCell>();
                var st = new Stack<FallingCell>();
                st.Push(c);
                seen.Add(key);

                while (st.Count > 0)
                {
                    var cur = st.Pop();
                    comp.Add(cur);
                    var nb = new (int x, int y)[] { (cur.X + 1, cur.Y), (cur.X - 1, cur.Y), (cur.X, cur.Y + 1), (cur.X, cur.Y - 1) };
                    foreach (var n in nb)
                    {
                        if (map.TryGetValue(n, out var nc) && !seen.Contains((nc.X, nc.Y)))
                        {
                            seen.Add((nc.X, nc.Y));
                            st.Push(nc);
                        }
                    }
                }

                comps.Add(comp);
            }

            return comps;
        }
    }
}
