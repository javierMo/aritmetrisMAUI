
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
    /// - Los “fragmentos” conectados de las que sí pueden bajar continúan cayendo.
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
        var toClear=new HashSet<(int,int)>();
        int baseCellsCleared = 0;
        int bonusFromNumbers = 0;

        // Guardamos los pares de números implicados en cada match para calcular el bonus
        var matchNumberPairs = new List<(int n1,int n2)>();

        for(int y=0;y<Height;y++)
        for(int x=0;x<Width;x++)
        {
            var c=Grid[y,x];
            if(c.Type!=CellType.Number) continue;
            foreach(var (dx,dy) in Directions)
            {
                int x1=x+dx,y1=y+dy,x2=x+2*dx,y2=y+2*dy;
                if(!InBounds(x2,y2)) continue;
                var op=Grid[y1,x1];
                var n2=Grid[y2,x2];
                if(op.Type!=CellType.Operator||n2.Type!=CellType.Number) continue;
                if(IsValidOperation(c.Value,op.Value,n2.Value))
                {
                    // Añadimos las 3 celdas al set de limpieza
                    toClear.Add((x,y)); toClear.Add((x1,y1)); toClear.Add((x2,y2));

                    // Calculamos el bonus por números: (n1 * n2) * 5
                    if(int.TryParse(c.Value, out var v1) && int.TryParse(n2.Value, out var v2))
                        matchNumberPairs.Add((v1,v2));
                }
            }
        }

        baseCellsCleared = toClear.Count;
        foreach(var (n1,n2) in matchNumberPairs)
            bonusFromNumbers += (n1 * n2) * 5;

        // Limpiar y colapsar
        foreach(var (cx,cy) in toClear) Grid[cy,cx]=Cell.Empty();
        if(toClear.Count>0) Collapse();

        // Puntuación final: celdas eliminadas + suma((n1*n2)*5 por match)
        return baseCellsCleared + bonusFromNumbers;
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
