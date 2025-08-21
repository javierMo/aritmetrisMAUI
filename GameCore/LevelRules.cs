
namespace Aritmetris.GameCore;

public record LevelConfig(int[] Numbers,string[] Ops,int TargetMin,int TargetMax,int DropMs,int Req);

public static class LevelRules
{
    private static readonly Dictionary<int,LevelConfig> Levels=new(){
        [1]= new LevelConfig(new[]{0,1,2},new[]{ "+" },0,4,1000,50),
        [2]= new LevelConfig(new[]{0,1,2,3},new[]{ "+" },0,6,1000,125),
        [3]= new LevelConfig(new[]{0,1,2,3,4},new[]{ "+","-" },0,8,800,250),
        [4]= new LevelConfig(new[]{0,1,2,3,4,5},new[]{ "+","-" },0,10,800,500),
        [5]= new LevelConfig(new[]{0,1,2,3,4,5,6},new[]{ "+","-" },0,12,700,750),
        [6]= new LevelConfig(new[]{0,1,2,3,4,5,6,7},new[]{ "+","-" },0,14,700,1000),
        [7]= new LevelConfig(new[]{0,1,2,3,4,5,6,7,8},new[]{ "+","-" },0,16,600,1250),
        [8]= new LevelConfig(new[]{0,1,2,3,4,5,6,7,8,9},new[]{ "+","-" },0,18,600,1750),
        [9]= new LevelConfig(new[]{0,1,2,3,4,5,6,7,8,9},new[]{ "+","-","*" },0,81,500,2500),
        [10]=new LevelConfig(new[]{0,1,2,3,4,5,6,7,8,9},new[]{ "+","-","*","/" },0,81,500,5000)
    };

    public static LevelConfig Get(int level)
    {
        if(Levels.ContainsKey(level)) return Levels[level];
        var baseCfg=Levels[10];
        int drop=Math.Max(220,baseCfg.DropMs-(level-10)*30);
        int req=baseCfg.Req+(level-10)*2000;
        return new LevelConfig(baseCfg.Numbers,baseCfg.Ops,baseCfg.TargetMin,baseCfg.TargetMax,drop,req);
    }
}
