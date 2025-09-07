namespace Aritmetris.GameCore
{
    public sealed class Piece3
    {
        // Cells[0] = A (número izquierda en orientación base)
        // Cells[1] = Op (operador) -> PIVOTE
        // Cells[2] = B (número derecha en orientación base)
        public Cell[] Cells { get; } = new Cell[3];

        // Posición del PIVOTE en el tablero (coordenadas de la celda Operador)
        public int X { get; set; }
        public int Y { get; set; }

        // 0=horizontal (A-Op-B), 1=vertical (A arriba, B abajo),
        // 2=horizontal invertida (B-Op-A), 3=vertical invertida (B arriba, A abajo)
        public int Rotation { get; set; }

        public Piece3(string leftNum, string op, string rightNum)
        {
            Cells[0] = new Cell(CellType.Number, leftNum); // A
            Cells[1] = new Cell(CellType.Operator, op);    // Op (pivote)
            Cells[2] = new Cell(CellType.Number, rightNum);// B
            Rotation = 0;
        }

        public void Rotate()
        {
            Rotation = (Rotation + 1) & 3; // 0..3
        }

        /// <summary>
        /// Devuelve los bloques con offsets relativos al PIVOTE (0,0).
        /// Suma (X+dx, Y+dy) para obtener posiciones de tablero.
        /// </summary>
        public (int dx, int dy, Cell cell)[] GetBlocks()
        {
            // Orientación base (rotación 0):
            // A(-1,0)  Op(0,0)  B(1,0)
            var local = new (int x, int y, Cell cell)[]
            {
                (-1, 0, Cells[0]),  // A
                ( 0, 0, Cells[1]),  // Op (pivote)
                ( 1, 0, Cells[2])   // B
            };

            // Aplica rotación CCW Rotation veces: (x,y) -> (-y, x)
            var result = new (int dx, int dy, Cell cell)[local.Length];
            for (int i = 0; i < local.Length; i++)
            {
                int x = local[i].x;
                int y = local[i].y;

                switch (Rotation & 3)
                {
                    case 0: // 0°
                        result[i] = (x, y, local[i].cell);
                        break;
                    case 1: // 90° CCW
                        result[i] = (-y, x, local[i].cell);
                        break;
                    case 2: // 180°
                        result[i] = (-x, -y, local[i].cell);
                        break;
                    case 3: // 270° CCW
                        result[i] = (y, -x, local[i].cell);
                        break;
                }
            }
            return result;
        }

        /// <summary>
        /// Si te viene bien tener las posiciones ya absolutas en tablero.
        /// </summary>
        public (int x, int y, Cell cell)[] GetWorldBlocks()
        {
            var blocks = GetBlocks();
            var world = new (int x, int y, Cell cell)[blocks.Length];
            for (int i = 0; i < blocks.Length; i++)
            {
                world[i] = (X + blocks[i].dx, Y + blocks[i].dy, blocks[i].cell);
            }
            return world;
        }

        /// <summary>
        /// Útil si necesitas el AABB para spawns o validaciones.
        /// Devuelve (minDx,maxDx,minDy,maxDy) relativos al pivote.
        /// </summary>
        public (int minDx, int maxDx, int minDy, int maxDy) GetLocalBounds()
        {
            var blocks = GetBlocks();
            int minDx = int.MaxValue, maxDx = int.MinValue;
            int minDy = int.MaxValue, maxDy = int.MinValue;
            foreach (var (dx, dy, _) in blocks)
            {
                if (dx < minDx) minDx = dx;
                if (dx > maxDx) maxDx = dx;
                if (dy < minDy) minDy = dy;
                if (dy > maxDy) maxDy = dy;
            }
            return (minDx, maxDx, minDy, maxDy);
        }
    }
}
