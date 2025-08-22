using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Aritmetris.GameCore;

namespace Aritmetris.Rendering
{
    
    public class NextPieceView : SKCanvasView
    {
        private GameState? _game;

        public void BindGame(GameState game)
        {
            _game = game;
            InvalidateSurface();
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);
            var canvas = e.Surface.Canvas;

            // Fondo blanco roto
            canvas.Clear(new SKColor(247, 247, 242));

            if (_game?.NextPiece == null) return;
            var next = _game.NextPiece;

            float w = e.Info.Width;
            float h = e.Info.Height;
            float cell = Math.Min(w / 3f, h);
            float ox = (w - cell * 3f) / 2f;
            float oy = (h - cell) / 2f;

            using var border = new SKPaint { Color = new SKColor(120, 120, 120), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            using var numberText = new SKPaint { Color = SKColors.Black, TextAlign = SKTextAlign.Center, IsAntialias = true, TextSize = cell * 0.6f };
            using var opText = new SKPaint { Color = SKColors.White, TextAlign = SKTextAlign.Center, IsAntialias = true, TextSize = cell * 0.6f };
            using var opFill = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };

            SKColor[] pastelColors = new SKColor[]
            {
                new SKColor(253,226,228), new SKColor(205,234,192),
                new SKColor(190,225,230), new SKColor(255,241,182),
                new SKColor(234,215,242), new SKColor(251,196,171),
                new SKColor(215,233,247), new SKColor(246,223,235),
                new SKColor(226,240,203), new SKColor(255,229,180)
            };

            for (int i = 0; i < 3; i++)
            {
                var rect = new SKRect(ox + i * cell, oy, ox + (i + 1) * cell, oy + cell);
                var c = next.Cells[i];

                if (c.Type == CellType.Operator)
                {
                    canvas.DrawRect(rect, opFill);
                    canvas.DrawRect(rect, border);
                    canvas.DrawText(c.Value, rect.MidX, rect.MidY + opText.TextSize / 3, opText);
                }
                else
                {
                    int d; if (!int.TryParse(c.Value, out d)) d = 0;
                    using var fill = new SKPaint { Color = pastelColors[Math.Abs(d) % pastelColors.Length], Style = SKPaintStyle.Fill };
                    canvas.DrawRect(rect, fill);
                    canvas.DrawRect(rect, border);
                    canvas.DrawText(c.Value, rect.MidX, rect.MidY + numberText.TextSize / 3, numberText);
                }
            }
        }
    }
}
