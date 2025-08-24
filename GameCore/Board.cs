
namespace Aritmetris.GameCore;

public sealed class Board
{
    public int Width { get; }
    public int Height { get; }
    public Cell[,] Grid { get; }
    public int TargetValue { get; set; } = 10;

    public Board(int width = 10, int height = 20)
    {
        Width = width;
        Height = height;
        Grid = new Cell[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                Grid[y, x] = Cell.Empty();
    }

    public bool Fits(Piece3 piece, int nx, int ny)
    {
        foreach (var (dx, dy, cell) in piece.GetBlocks())
        {
            int x = nx + dx, y = ny + dy;
            if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
            if (!Grid[y, x].IsEmpty) return false;
        }
        return true;
    }

    // Mantén tu Place existente si lo usas en otros sitios
    public void Place(Piece3 piece)
    {
        foreach (var (dx, dy, cell) in piece.GetBlocks())
        {
            int x = piece.X + dx, y = piece.Y + dy;
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                Grid[y, x] = cell;
        }
    }

    /// <summary>
    /// "Rompe" la pieza: cada bloque cae independientemente.
    /// - Números: caen hasta encontrar cualquier celda NO vacía o el fondo.
    /// - Operadores: caen hasta quedar justo encima de un Número.
    ///   Si debajo hay vacío u operador, siguen cayendo; si no hay número, paran al fondo.
    /// </summary>
    public void PlaceWithBreak(Piece3 piece)
    {
        foreach (var (dx, dy, cell) in piece.GetBlocks())
        {
            int x = piece.X + dx;
            int y = piece.Y + dy;

            while (true)
            {
                int ny = y + 1;
                if (ny >= Height) break;

                var below = Grid[ny, x];
                if (cell.Type == CellType.Operator)
                {
                    // Operador: busca quedar sobre un número
                    if (below.Type == CellType.Number) break;
                    if (below.Type == CellType.Empty) { y = ny; continue; }
                    // debajo hay operador -> detén para no apilar operadores eternamente
                    break;
                }
                else
                {
                    // Número: detén sobre cualquier celda no vacía; si vacío, sigue
                    if (!below.IsEmpty) break;
                    y = ny;
                }
            }

            if (x >= 0 && x < Width && y >= 0 && y < Height)
                Grid[y, x] = cell;
        }
    }

    /// <summary>
    /// Asienta la pieza con caída por grupos, sin distinguir tipos de celda:
    /// - En cada paso, las celdas que pueden bajar bajan 1.
    /// - Las que no pueden bajar se fijan.
    /// - Los fragmentos conectados de las que sí pueden bajar continúan cayendo.
    /// </summary>
    public void SettleWithFallingGroups(Piece3 piece)
    {
        // Grupo inicial con las celdas situadas en el tablero
        var startCells = piece.GetBlocks()
                              .Select(b => new FallingCell(piece.X + b.dx, piece.Y + b.dy, b.cell))
                              .ToList();

        var groups = new List<FallingGroup> { new FallingGroup(startCells) };

        bool anyProgress;
        do
        {
            anyProgress = false;
            var nextGroups = new List<FallingGroup>();
            var toLock = new List<FallingCell>();

            foreach (var g in groups)
            {
                var res = g.TryMoveDownWithBreak(this, groups);

                if (res.MovedAll)
                {
                    nextGroups.Add(g);
                    anyProgress = true;
                }
                else if (res.BlockedAll)
                {
                    toLock.AddRange(g.Cells.Select(c => new FallingCell(c.X, c.Y, c.Cell)));
                }
                else
                {
                    if (res.Locked.Count > 0) toLock.AddRange(res.Locked);
                    nextGroups.AddRange(res.Fragments);
                    anyProgress = true;
                }
            }

            // Fijar las celdas bloqueadas de este paso
            foreach (var lc in toLock)
            {
                if (lc.X >= 0 && lc.X < Width && lc.Y >= 0 && lc.Y < Height)
                    Grid[lc.Y, lc.X] = lc.Cell;
            }

            // Mantener solo grupos que aún tengan alguna celda en aire
            groups = nextGroups.Where(g => g.Cells.Any(c => Grid[c.Y, c.X].IsEmpty)).ToList();

        } while (groups.Count > 0 && anyProgress);

        // Si quedara algo sin fijar (sin progreso), fíjalo
        foreach (var g in groups)
            foreach (var c in g.Cells)
                if (c.X >= 0 && c.X < Width && c.Y >= 0 && c.Y < Height)
                    Grid[c.Y, c.X] = c.Cell;
    }

    private static readonly (int dx,int dy)[] Directions=new[]{ (1,0),(0,1),(1,1),(1,-1),(-1,0),(0,-1),(-1,-1),(-1,1) };

    public int CheckMatches()
    {
        var toClear = new HashSet<(int x,int y)>();
        int bonus = 0;

        // Horizontal scan
        for (int y = 0; y < Height; y++)
            bonus += ScanLineAndCollect(toClear, 0, y, 1, 0);

        // Vertical scan
        for (int x = 0; x < Width; x++)
            bonus += ScanLineAndCollect(toClear, x, 0, 0, 1);

        int baseCells = toClear.Count;

        foreach (var p in toClear)
            Grid[p.y, p.x] = Cell.Empty();

        if (toClear.Count > 0) Collapse();

        return baseCells + bonus;

        // ---- local helpers ----
        int ScanLineAndCollect(HashSet<(int x,int y)> clear, int startX, int startY, int stepX, int stepY)
        {
            // Build the line
            var xs = new List<int>();
            var ys = new List<int>();
            var types = new List<CellType>();
            var values = new List<string>();

            int x = startX, y = startY;
            while (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                xs.Add(x); ys.Add(y);
                types.Add(Grid[y,x].Type);
                values.Add(Grid[y,x].Value);
                x += stepX; y += stepY;
            }

            int localBonus = 0;

            for (int i = 0; i < xs.Count; i++)
            {
                if (types[i] != CellType.Number) continue;

                // Expand alternating N/O/N/...
                var numVals = new List<int>();
                var numPos = new List<(int x,int y)>();
                var opVals  = new List<string>();
                var opPos   = new List<(int x,int y)>();

                int idx = i;
                bool expectNumber = true;

                while (idx < xs.Count)
                {
                    if (expectNumber)
                    {
                        if (types[idx] != CellType.Number) break;
                        if (!int.TryParse(values[idx], out int nv)) break;
                        numVals.Add(nv);
                        numPos.Add((xs[idx], ys[idx]));
                    }
                    else
                    {
                        if (types[idx] != CellType.Operator) break;
                        opVals.Add(values[idx]);
                        opPos.Add((xs[idx], ys[idx]));
                    }

                    expectNumber = !expectNumber;
                    idx++;

                    // When we just consumed a number, we have an odd-length sequence: 3,5,7,...
                    if (!expectNumber && numVals.Count >= 2 && numVals.Count == opVals.Count + 1)
                    {
                        long product;
                        if (EvaluateChain(numVals, opVals, out product))
                        {
                            // mark all cells of this sub-chain
                            for (int k = 0; k < numPos.Count; k++)
                                clear.Add(numPos[k]);
                            for (int k = 0; k < opPos.Count; k++)
                                clear.Add(opPos[k]);

                            // bonus: product of all numbers * 5
                            checked { localBonus += (int)(product * 5); }
                        }
                    }
                }
            }

            return localBonus;
        }

        bool EvaluateChain(List<int> nums, List<string> ops, out long product)
        {
            product = 1;
            if (nums.Count < 2 || nums.Count != ops.Count + 1) return false;

            long acc = nums[0];
            product *= nums[0];

            for (int i = 0; i < ops.Count; i++)
            {
                string op = ops[i];
                int rhs = nums[i+1];
                product *= rhs;

                switch (op)
                {
                    case "+": acc += rhs; break;
                    case "-": acc -= rhs; break;
                    case "*": acc *= rhs; break;
                    case "/":
                        if (rhs == 0) return false;
                        if (acc % rhs != 0) return false;
                        acc /= rhs; break;
                    default: return false;
                }
            }
            return acc == TargetValue;
        }
    }

    private bool InBounds(int x,int y)=>x>=0&&x<Width&&y>=0&&y<Height;

    private bool IsValidOperation(string a,string op,string b)
    {
        if(!int.TryParse(a,out int n1)) return false;
        if(!int.TryParse(b,out int n2)) return false;
        return op switch{
            "+" => (n1+n2)==TargetValue,
            "-" => (n1-n2)==TargetValue,
            "*" => (n1*n2)==TargetValue,
            "/" => (n2!=0 && n1/n2==TargetValue && n1 % n2==0),
            _=>false};
    }

    private void Collapse()
    {
        for(int x=0;x<Width;x++)
        {
            int writeY=Height-1;
            for(int y=Height-1;y>=0;y--)
            {
                if(!Grid[y,x].IsEmpty)
                {
                    if(writeY!=y){ Grid[writeY,x]=Grid[y,x]; Grid[y,x]=Cell.Empty(); }
                    writeY--;
                }
            }
        }
    }
}
