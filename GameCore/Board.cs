
namespace Aritmetris.GameCore;

public sealed class Board
{
    public int Width { get; }
    public int Height { get; }
    public Cell[,] Grid { get; }
    public int TargetValue { get; set; } = 10;

    public Board(int width=10,int height=20)
    {
        Width=width; Height=height;
        Grid=new Cell[height,width];
        for(int y=0;y<height;y++) for(int x=0;x<width;x++) Grid[y,x]=Cell.Empty();
    }

    public bool Fits(Piece3 piece,int nx,int ny)
    {
        foreach(var (dx,dy,cell) in piece.GetBlocks())
        {
            int x=nx+dx,y=ny+dy;
            if(x<0||x>=Width||y<0||y>=Height) return false;
            if(!Grid[y,x].IsEmpty) return false;
        }
        return true;
    }

    public void Place(Piece3 piece)
    {
        foreach(var (dx,dy,cell) in piece.GetBlocks())
        {
            int x=piece.X+dx,y=piece.Y+dy;
            if(x>=0&&x<Width&&y>=0&&y<Height)
                Grid[y,x]=cell;
        }
    }

    private static readonly (int dx,int dy)[] Directions=new[]{ (1,0),(0,1),(1,1),(1,-1),(-1,0),(0,-1),(-1,-1),(-1,1) };

    public int CheckMatches()
    {
        var toClear=new HashSet<(int,int)>();
        int score=0;
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
                    toClear.Add((x,y)); toClear.Add((x1,y1)); toClear.Add((x2,y2));
                    score+=TargetValue*10; // como en JS: puntos = matches * target * 10
                }
            }
        }
        foreach(var (cx,cy) in toClear) Grid[cy,cx]=Cell.Empty();
        if(toClear.Count>0) Collapse();
        return score;
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
