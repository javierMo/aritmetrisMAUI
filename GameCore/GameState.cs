using System.Linq;

namespace Aritmetris.GameCore;

public sealed class GameState
{
    public Board Board { get; }
    public Piece3? CurrentPiece { get; private set; }
    public Piece3? NextPiece { get; private set; }
    public LevelConfig Config { get; private set; }

    public int Score { get; private set; }
    public int Level { get; private set; }=1;
    public int Target { get; private set; }=10;
    public bool GameOver { get; private set; }

    public event EventHandler<int>? OnScoreChanged;
    public event EventHandler<int>? OnLevelChanged;
    public event EventHandler<int>? OnTargetChanged;

    public record LevelUpDiff(int Level,int[] AddedNumbers,string[] AddedOps);
    public event EventHandler<LevelUpDiff>? OnLevelUpDiff;
    public event EventHandler? OnGameOver;

    private readonly Random _rng=new();

    public GameState(int width=10,int height=20)
    {
        Board=new Board(width,height);
        Reset();
    }

    public void Reset()
    {
        Score=0;
        Level=1;
        Config=LevelRules.Get(Level);
        Target=RandomTarget();
        Board.TargetValue=Target;
        GameOver=false;
        CurrentPiece=null;
        NextPiece=GenerateRandomPiece();
        SpawnNextPiece();
        FireAllEvents();
    }

    void FireAllEvents()
    {
        OnScoreChanged?.Invoke(this,Score);
        OnLevelChanged?.Invoke(this,Level);
        OnTargetChanged?.Invoke(this,Target);
    }

    public void Tick()
    {
        if(GameOver||CurrentPiece==null) return;
        if(Board.Fits(CurrentPiece,CurrentPiece.X,CurrentPiece.Y+1))
        {
            CurrentPiece.Y++;
        }
        else
        {
            //Cambio: usar PlaceWithBreak para "romper" la pieza al fijar
            //Board.PlaceWithBreak(CurrentPiece);
            //Board.Place(CurrentPiece);
            Board.SettleWithFallingGroups(CurrentPiece);

            int gained=Board.CheckMatches();
            if(gained>0)
            {
                Score+=gained;
                OnScoreChanged?.Invoke(this,Score);
                if(Score>=Config.Req)
                {
                    var prevNums = Config.Numbers.ToArray();
                    var prevOps  = Config.Ops.ToArray();
                    Level++;
                    var newCfg = LevelRules.Get(Level);
                    var addedNums = newCfg.Numbers.Except(prevNums).ToArray();
                    var addedOps  = newCfg.Ops.Except(prevOps).ToArray();
                    Config = newCfg;
                    Target = RandomTarget();
                    Board.TargetValue = Target;
                    OnLevelChanged?.Invoke(this,Level);
                    OnLevelUpDiff?.Invoke(this,new LevelUpDiff(Level,addedNums,addedOps));
                    OnTargetChanged?.Invoke(this,Target);
                }
            }
            SpawnNextPiece();
        }
    }

    void SpawnNextPiece()
    {
        CurrentPiece=NextPiece;
        if(CurrentPiece==null) return;
        CurrentPiece.X=Board.Width/2-1;
        CurrentPiece.Y=0;
        if(!Board.Fits(CurrentPiece,CurrentPiece.X,CurrentPiece.Y))
        {
            GameOver=true; OnGameOver?.Invoke(this,EventArgs.Empty);
        }
        NextPiece=GenerateRandomPiece();
    }

    Piece3 GenerateRandomPiece()
    {
        int n1=Config.Numbers[_rng.Next(Config.Numbers.Length)];
        int n2=Config.Numbers[_rng.Next(Config.Numbers.Length)];
        string op=Config.Ops[_rng.Next(Config.Ops.Length)];
        return new Piece3(n1.ToString(),op,n2.ToString());
    }

    int RandomTarget()=>_rng.Next(Config.TargetMin,Config.TargetMax+1);

    public void MoveLeft(){ if(CurrentPiece!=null && Board.Fits(CurrentPiece,CurrentPiece.X-1,CurrentPiece.Y)) CurrentPiece.X--; }
    public void MoveRight(){ if(CurrentPiece!=null && Board.Fits(CurrentPiece,CurrentPiece.X+1,CurrentPiece.Y)) CurrentPiece.X++; }
    public void Rotate(){ if(CurrentPiece!=null){ CurrentPiece.Rotate(); if(!Board.Fits(CurrentPiece,CurrentPiece.X,CurrentPiece.Y)) CurrentPiece.Rotate(); } }
    public void HardDrop(){ if(CurrentPiece!=null){ while(Board.Fits(CurrentPiece,CurrentPiece.X,CurrentPiece.Y+1)) CurrentPiece.Y++; Tick(); } }
}
