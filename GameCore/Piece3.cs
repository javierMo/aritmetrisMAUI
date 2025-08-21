
namespace Aritmetris.GameCore;

public sealed class Piece3
{
    public Cell[] Cells { get; } = new Cell[3];
    public int X { get; set; }
    public int Y { get; set; }
    public int Rotation { get; private set; } // 0=horizontal,1=vertical

    public Piece3(string leftNum, string op, string rightNum)
    {
        Cells[0] = new Cell(CellType.Number, leftNum);
        Cells[1] = new Cell(CellType.Operator, op);
        Cells[2] = new Cell(CellType.Number, rightNum);
    }

    public void Rotate()
    {
        Rotation = (Rotation + 1) % 2;
    }

    public (int dx,int dy,Cell cell)[] GetBlocks()
    {
        if (Rotation == 0)
            return new[]{ (0,0,Cells[0]), (1,0,Cells[1]), (2,0,Cells[2]) };
        else
            return new[]{ (0,0,Cells[0]), (0,1,Cells[1]), (0,2,Cells[2]) };
    }
}
