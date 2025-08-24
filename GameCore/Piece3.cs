namespace Aritmetris.GameCore;

public sealed class Piece3
{
    public Cell[] Cells { get; } = new Cell[3];
    public int X { get; set; }
    public int Y { get; set; }
    // 0=right, 1=down, 2=left, 3=up
    public int Rotation { get; set; }

    public Piece3(string leftNum, string op, string rightNum)
    {
        Cells[0] = new Cell(CellType.Number, leftNum);
        Cells[1] = new Cell(CellType.Operator, op);
        Cells[2] = new Cell(CellType.Number, rightNum);
        Rotation = 0;
    }

    public void Rotate()
    {
        Rotation = (Rotation + 1) & 3;
    }

    public (int dx,int dy,Cell cell)[] GetBlocks()
    {
        switch (Rotation & 3)
        {
            case 0: // right: [A][Op][B]
                return new[]{ (0,0,Cells[0]), (1,0,Cells[1]), (2,0,Cells[2]) };
            case 1: // down: A / Op / B
                return new[]{ (0,0,Cells[0]), (0,1,Cells[1]), (0,2,Cells[2]) };
            case 2: // left: [B][Op][A] (swap A and B)
                return new[]{ (0,0,Cells[2]), (1,0,Cells[1]), (2,0,Cells[0]) };
            default: // up: B / Op / A (swap A and B)
                return new[]{ (0,0,Cells[2]), (0,1,Cells[1]), (0,2,Cells[0]) };
        }
    }
}
