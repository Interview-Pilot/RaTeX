using System.Numerics;
using System.Text;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI.Text;

namespace RaTeX.Windows;

public sealed class RaTeXWin2DRenderer
{
    private const float GlyphLayoutExtent = 4096;

    public void Draw(
        CanvasDrawingSession drawingSession,
        RaTeXFormula formula,
        float x = 0,
        float y = 0,
        float scale = 1)
    {
        ArgumentNullException.ThrowIfNull(drawingSession);
        ArgumentNullException.ThrowIfNull(formula);
        if (!float.IsFinite(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }

        var fontSize = formula.FontSize * scale;
        using var fonts = new TextFormatCache(fontSize);
        foreach (var item in formula.DisplayList.Items)
        {
            switch (item)
            {
                case DisplayItem.GlyphPath glyph:
                    DrawGlyph(drawingSession, glyph, fonts, x, y, fontSize);
                    break;
                case DisplayItem.Line line:
                    DrawLine(drawingSession, line, x, y, fontSize);
                    break;
                case DisplayItem.Rect rect:
                    DrawRect(drawingSession, rect, x, y, fontSize);
                    break;
                case DisplayItem.Path path:
                    DrawPath(drawingSession, path, x, y, fontSize);
                    break;
            }
        }
    }

    private static void DrawGlyph(
        CanvasDrawingSession drawingSession,
        DisplayItem.GlyphPath glyph,
        TextFormatCache fonts,
        float originX,
        float originY,
        float fontSize)
    {
        if (!Rune.IsValid(glyph.CharCode))
        {
            return;
        }

        var format = fonts.Get(glyph.Font, fontSize * (float)glyph.Scale);
        if (format is null)
        {
            return;
        }

        using var layout = new CanvasTextLayout(
            drawingSession.Device,
            new Rune(glyph.CharCode).ToString(),
            format,
            GlyphLayoutExtent,
            GlyphLayoutExtent);
        var metrics = layout.LineMetrics;
        if (metrics.Length == 0)
        {
            return;
        }

        drawingSession.DrawTextLayout(
            layout,
            originX + Em(glyph.X, fontSize),
            originY + Em(glyph.Y, fontSize) - metrics[0].Baseline,
            ToWindowsColor(glyph.Color));
    }

    private static void DrawLine(
        CanvasDrawingSession drawingSession,
        DisplayItem.Line line,
        float originX,
        float originY,
        float fontSize)
    {
        var thickness = Math.Max(0.5f, Em(line.Thickness, fontSize));
        var left = originX + Em(line.X, fontSize);
        var centerY = originY + Em(line.Y, fontSize);
        var width = Em(line.Width, fontSize);
        var color = ToWindowsColor(line.Color);
        if (line.Dashed)
        {
            using var strokeStyle = new CanvasStrokeStyle
            {
                DashStyle = CanvasDashStyle.Dash,
                StartCap = CanvasCapStyle.Flat,
                EndCap = CanvasCapStyle.Flat
            };
            drawingSession.DrawLine(left, centerY, left + width, centerY, color, thickness, strokeStyle);
        }
        else
        {
            drawingSession.FillRectangle(left, centerY - (thickness / 2), width, thickness, color);
        }
    }

    private static void DrawRect(
        CanvasDrawingSession drawingSession,
        DisplayItem.Rect rect,
        float originX,
        float originY,
        float fontSize)
    {
        drawingSession.FillRectangle(
            originX + Em(rect.X, fontSize),
            originY + Em(rect.Y, fontSize),
            Math.Max(0.5f, Em(rect.Width, fontSize)),
            Math.Max(0.5f, Em(rect.Height, fontSize)),
            ToWindowsColor(rect.Color));
    }

    private static void DrawPath(
        CanvasDrawingSession drawingSession,
        DisplayItem.Path path,
        float originX,
        float originY,
        float fontSize)
    {
        using var geometry = BuildGeometry(drawingSession.Device, path, originX, originY, fontSize);
        var color = ToWindowsColor(path.Color);
        if (path.Fill)
        {
            drawingSession.FillGeometry(geometry, color);
        }
        else
        {
            drawingSession.DrawGeometry(geometry, color, 1);
        }
    }

    private static CanvasGeometry BuildGeometry(
        ICanvasResourceCreator resourceCreator,
        DisplayItem.Path path,
        float originX,
        float originY,
        float fontSize)
    {
        using var builder = new CanvasPathBuilder(resourceCreator);
        builder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
        var offsetX = originX + Em(path.X, fontSize);
        var offsetY = originY + Em(path.Y, fontSize);
        var figureOpen = false;
        foreach (var command in path.Commands)
        {
            switch (command)
            {
                case PathCommand.MoveTo move:
                    if (figureOpen)
                    {
                        builder.EndFigure(CanvasFigureLoop.Open);
                    }

                    builder.BeginFigure(Point(move.X, move.Y, offsetX, offsetY, fontSize));
                    figureOpen = true;
                    break;
                case PathCommand.LineTo line when figureOpen:
                    builder.AddLine(Point(line.X, line.Y, offsetX, offsetY, fontSize));
                    break;
                case PathCommand.CubicTo cubic when figureOpen:
                    builder.AddCubicBezier(
                        Point(cubic.X1, cubic.Y1, offsetX, offsetY, fontSize),
                        Point(cubic.X2, cubic.Y2, offsetX, offsetY, fontSize),
                        Point(cubic.X, cubic.Y, offsetX, offsetY, fontSize));
                    break;
                case PathCommand.QuadTo quad when figureOpen:
                    builder.AddQuadraticBezier(
                        Point(quad.X1, quad.Y1, offsetX, offsetY, fontSize),
                        Point(quad.X, quad.Y, offsetX, offsetY, fontSize));
                    break;
                case PathCommand.Close when figureOpen:
                    builder.EndFigure(CanvasFigureLoop.Closed);
                    figureOpen = false;
                    break;
            }
        }

        if (figureOpen)
        {
            builder.EndFigure(CanvasFigureLoop.Open);
        }

        return CanvasGeometry.CreatePath(builder);
    }

    private static Vector2 Point(
        double x,
        double y,
        float offsetX,
        float offsetY,
        float fontSize) =>
        new(offsetX + Em(x, fontSize), offsetY + Em(y, fontSize));

    private static float Em(double value, float fontSize) => checked((float)value * fontSize);

    private static Windows.UI.Color ToWindowsColor(RaTeXColor color) =>
        Windows.UI.Color.FromArgb(
            ToByte(color.A),
            ToByte(color.R),
            ToByte(color.G),
            ToByte(color.B));

    private static byte ToByte(float value) => (byte)MathF.Round(Math.Clamp(value, 0, 1) * 255);

    private sealed class TextFormatCache : IDisposable
    {
        private readonly Dictionary<(string FontId, float Size), CanvasTextFormat> formats = [];
        private readonly float defaultFontSize;

        public TextFormatCache(float defaultFontSize)
        {
            this.defaultFontSize = defaultFontSize;
        }

        public CanvasTextFormat? Get(string fontId, float size)
        {
            var normalizedSize = float.IsFinite(size) && size > 0 ? size : defaultFontSize;
            var key = (fontId, normalizedSize);
            if (formats.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var format = Create(fontId, normalizedSize);
            if (format is not null)
            {
                formats.Add(key, format);
            }

            return format;
        }

        public void Dispose()
        {
            foreach (var format in formats.Values)
            {
                format.Dispose();
            }
        }

        private static CanvasTextFormat? Create(string fontId, float size)
        {
            if (RaTeXFontCatalog.TryGet(fontId, out var descriptor))
            {
                return new CanvasTextFormat
                {
                    FontFamily = $"ms-appx:///RaTeX/Fonts/{descriptor.FileName}#{descriptor.FamilyName}",
                    FontSize = size,
                    FontStyle = descriptor.Style.HasFlag(System.Drawing.FontStyle.Italic)
                        ? FontStyle.Italic
                        : FontStyle.Normal,
                    FontWeight = descriptor.Style.HasFlag(System.Drawing.FontStyle.Bold)
                        ? FontWeights.Bold
                        : FontWeights.Normal
                };
            }

            if (RaTeXFontCatalog.IsSystemFallback(fontId))
            {
                return new CanvasTextFormat
                {
                    FontFamily = "Segoe UI",
                    FontSize = size,
                    FontStyle = FontStyle.Normal,
                    FontWeight = FontWeights.Normal
                };
            }

            return null;
        }
    }
}
