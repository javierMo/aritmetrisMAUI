
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Aritmetris.GameCore;

namespace Aritmetris.Rendering;

public class NextPieceView : SKCanvasView
{
    private GameState? _game;
    public void BindGame(GameState game){ _game=game; InvalidateSurface(); }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);
        var canvas=e.Surface.Canvas;
        canvas.Clear(new SKColor(30,30,30));
        if(_game?.NextPiece==null) return;
        var next=_game.NextPiece;
        float w=e.Info.Width,h=e.Info.Height;
        float cell=Math.Min(w/3f,h);
        float ox=(w-cell*3)/2f, oy=(h-cell)/2f;
        using var border=new SKPaint{Color=new SKColor(60,60,60),Style=SKPaintStyle.Stroke,StrokeWidth=1};
        using var numPaint=new SKPaint{Color=SKColors.LightGreen,TextAlign=SKTextAlign.Center,IsAntialias=true,TextSize=cell*0.6f};
        using var opPaint=new SKPaint{Color=SKColors.Orange,TextAlign=SKTextAlign.Center,IsAntialias=true,TextSize=cell*0.6f};
        for(int i=0;i<3;i++)
        {
            var rect=new SKRect(ox+i*cell,oy,ox+(i+1)*cell,oy+cell);
            canvas.DrawRect(rect,border);
            var c=next.Cells[i];
            var paint=c.Type==CellType.Number?numPaint:opPaint;
            canvas.DrawText(c.Value,rect.MidX,rect.MidY+numPaint.TextSize/3,paint);
        }
    }
}
