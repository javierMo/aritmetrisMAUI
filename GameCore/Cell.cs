
namespace Aritmetris.GameCore;

public enum CellType { Empty, Number, Operator }

public sealed class Cell
{
    public CellType Type { get; }
    public string Value { get; }  // "1","2","+","-","*","/"

    public Cell(CellType type, string value)
    {
        Type = type;
        Value = value;
    }

    public static Cell Empty() => new(CellType.Empty, "");
    public bool IsEmpty => Type == CellType.Empty;
}
