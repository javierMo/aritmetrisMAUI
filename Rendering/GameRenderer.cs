
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Aritmetris.GameCore;

namespace Aritmetris.Rendering;

public class GameRenderer : SKCanvasView
{
    private GameState? _game;
    public void BindGame(GameState game){ _game=game; InvalidateSurface(); }
    public GameRenderer(){ PaintSurface+=OnPaintSurface; }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas=e.Surface.Canvas;
        canvas.Clear(new SKColor(18,18,18));
        if(_game==null) return;
        var board=_game.Board;
        float w=e.Info.Width,h=e.Info.Height;
        float cell=Math.Min(w/board.Width,h/board.Height);
        float ox=(w-board.Width*cell)/2f, oy=(h-board.Height*cell)/2f;
        using var border=new SKPaint{Color=new SKColor(60,60,60),Style=SKPaintStyle.Stroke,StrokeWidth=1};
        using var numPaint=new SKPaint{Color=SKColors.LightGreen,TextAlign=SKTextAlign.Center,IsAntialias=true,TextSize=cell*0.6f};
        using var opPaint=new SKPaint{Color=SKColors.Orange,TextAlign=SKTextAlign.Center,IsAntialias=true,TextSize=cell*0.6f};
        for(int y=0;y<board.Height;y++)
        for(int x=0;x<board.Width;x++)
        {
            var rect=new SKRect(ox+x*cell,oy+y*cell,ox+(x+1)*cell,oy+(y+1)*cell);
            canvas.DrawRect(rect,border);
            var cellObj=board.Grid[y,x];
            if(!cellObj.IsEmpty)
            {
                var p=cellObj.Type==CellType.Number?numPaint:opPaint;
                canvas.DrawText(cellObj.Value,rect.MidX,rect.MidY+numPaint.TextSize/3,p);
            }
        }
        if(_game.CurrentPiece is Piece3 pce)
        {
            foreach(var (dx,dy,c) in pce.GetBlocks())
            {
                int gx=pce.X+dx,gy=pce.Y+dy; if(gy<0) continue;
                var rect=new SKRect(ox+gx*cell,oy+gy*cell,ox+(gx+1)*cell,oy+(gy+1)*cell);
                canvas.DrawRect(rect,border);
                var paint=c.Type==CellType.Number?numPaint:opPaint;
                canvas.DrawText(c.Value,rect.MidX,rect.MidY+numPaint.TextSize/3,paint);
            }
        }
    }
}
