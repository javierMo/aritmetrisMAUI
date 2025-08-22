using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Aritmetris.GameCore;

namespace Aritmetris.Rendering
{
    
    public class GameRenderer : SKCanvasView
    {
        private GameState? _game;

        public GameRenderer()
        {
            PaintSurface += OnPaintSurface;
        }

        public void BindGame(GameState game)
        {
            _game = game;
            InvalidateSurface();
        }

        private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;

            // Fondo blanco roto
            canvas.Clear(new SKColor(247, 247, 242)); // #F7F7F2

            if (_game == null) return;
            var board = _game.Board;

            float w = e.Info.Width;
            float h = e.Info.Height;
            float cell = Math.Min(w / board.Width, h / board.Height);
            float ox = (w - board.Width * cell) / 2f;
            float oy = (h - board.Height * cell) / 2f;

            using var border = new SKPaint { Color = new SKColor(120, 120, 120), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            using var numberText = new SKPaint { Color = SKColors.Black, TextAlign = SKTextAlign.Center, IsAntialias = true, TextSize = cell * 0.6f };
            using var opText = new SKPaint { Color = SKColors.White, TextAlign = SKTextAlign.Center, IsAntialias = true, TextSize = cell * 0.6f };
            using var opFill = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };

            // Paleta pastel por dígito 0..9
            SKColor[] pastelColors = new SKColor[]
            {
                new SKColor(253,226,228), // 0 - pink
                new SKColor(205,234,192), // 1 - green
                new SKColor(190,225,230), // 2 - blue
                new SKColor(255,241,182), // 3 - yellow
                new SKColor(234,215,242), // 4 - lavender
                new SKColor(251,196,171), // 5 - peach
                new SKColor(215,233,247), // 6 - sky
                new SKColor(246,223,235), // 7 - rose
                new SKColor(226,240,203), // 8 - lime
                new SKColor(255,229,180)  // 9 - apricot
            };

            // Celdas fijas del tablero
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var rect = new SKRect(
                        ox + x * cell,
                        oy + y * cell,
                        ox + (x + 1) * cell,
                        oy + (y + 1) * cell
                    );

                    var cellObj = board.Grid[y, x];
                    if (!cellObj.IsEmpty)
                    {
                        if (cellObj.Type == CellType.Operator)
                        {
                            canvas.DrawRect(rect, opFill);
                            canvas.DrawRect(rect, border);
                            canvas.DrawText(cellObj.Value, rect.MidX, rect.MidY + opText.TextSize / 3, opText);
                        }
                        else
                        {
                            int d; if (!int.TryParse(cellObj.Value, out d)) d = 0;
                            using var fill = new SKPaint { Color = pastelColors[Math.Abs(d) % pastelColors.Length], Style = SKPaintStyle.Fill };
                            canvas.DrawRect(rect, fill);
                            canvas.DrawRect(rect, border);
                            canvas.DrawText(cellObj.Value, rect.MidX, rect.MidY + numberText.TextSize / 3, numberText);
                        }
                    }
                    else
                    {
                        canvas.DrawRect(rect, border);
                    }
                }
            }

            // Pieza activa
            if (_game.CurrentPiece is Piece3 p)
            {
                foreach (var (dx, dy, c) in p.GetBlocks())
                {
                    int gx = p.X + dx, gy = p.Y + dy;
                    if (gy < 0) continue;

                    var rect = new SKRect(
                        ox + gx * cell,
                        oy + gy * cell,
                        ox + (gx + 1) * cell,
                        oy + (gy + 1) * cell
                    );

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
}
