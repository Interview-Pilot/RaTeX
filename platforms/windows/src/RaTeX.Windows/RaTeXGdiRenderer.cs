using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text;

namespace RaTeX.Windows;

public sealed class RaTeXGdiRenderer
{
    private static readonly Lazy<GdiFontStore> FontStore = new(
        () => new GdiFontStore(Path.Combine(AppContext.BaseDirectory, "RaTeX", "Fonts")),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public void Draw(
        Graphics graphics,
        RaTeXFormula formula,
        float x = 0,
        float y = 0,
        float scale = 1)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(formula);
        if (!float.IsFinite(scale) || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }

        var fontSize = formula.FontSize * scale;
        var state = graphics.Save();
        try
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using var fonts = new FontCache(fontSize);
            foreach (var item in formula.DisplayList.Items)
            {
                switch (item)
                {
                    case DisplayItem.GlyphPath glyph:
                        DrawGlyph(graphics, glyph, fonts, x, y, fontSize);
                        break;
                    case DisplayItem.Line line:
                        DrawLine(graphics, line, x, y, fontSize);
                        break;
                    case DisplayItem.Rect rect:
                        DrawRect(graphics, rect, x, y, fontSize);
                        break;
                    case DisplayItem.Path path:
                        DrawPath(graphics, path, x, y, fontSize);
                        break;
                }
            }
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private static void DrawGlyph(
        Graphics graphics,
        DisplayItem.GlyphPath glyph,
        FontCache fonts,
        float originX,
        float originY,
        float fontSize)
    {
        if (!Rune.IsValid(glyph.CharCode))
        {
            return;
        }

        var font = fonts.Get(glyph.Font, fontSize * (float)glyph.Scale);
        if (font is null)
        {
            return;
        }

        var style = font.Style;
        var family = font.FontFamily;
        var ascent = family.GetCellAscent(style) * font.Size / family.GetEmHeight(style);
        using var brush = new SolidBrush(ToDrawingColor(glyph.Color));
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        graphics.DrawString(
            new Rune(glyph.CharCode).ToString(),
            font,
            brush,
            originX + Em(glyph.X, fontSize),
            originY + Em(glyph.Y, fontSize) - ascent,
            format);
    }

    private static void DrawLine(
        Graphics graphics,
        DisplayItem.Line line,
        float originX,
        float originY,
        float fontSize)
    {
        var thickness = Math.Max(0.5f, Em(line.Thickness, fontSize));
        var left = originX + Em(line.X, fontSize);
        var centerY = originY + Em(line.Y, fontSize);
        var width = Em(line.Width, fontSize);
        var color = ToDrawingColor(line.Color);
        if (line.Dashed)
        {
            using var pen = new Pen(color, thickness)
            {
                DashStyle = DashStyle.Dash,
                StartCap = LineCap.Flat,
                EndCap = LineCap.Flat
            };
            graphics.DrawLine(pen, left, centerY, left + width, centerY);
        }
        else
        {
            using var brush = new SolidBrush(color);
            graphics.FillRectangle(brush, left, centerY - (thickness / 2), width, thickness);
        }
    }

    private static void DrawRect(
        Graphics graphics,
        DisplayItem.Rect rect,
        float originX,
        float originY,
        float fontSize)
    {
        using var brush = new SolidBrush(ToDrawingColor(rect.Color));
        graphics.FillRectangle(
            brush,
            originX + Em(rect.X, fontSize),
            originY + Em(rect.Y, fontSize),
            Math.Max(0.5f, Em(rect.Width, fontSize)),
            Math.Max(0.5f, Em(rect.Height, fontSize)));
    }

    private static void DrawPath(
        Graphics graphics,
        DisplayItem.Path path,
        float originX,
        float originY,
        float fontSize)
    {
        using var drawingPath = BuildPath(path, originX, originY, fontSize);
        var color = ToDrawingColor(path.Color);
        if (path.Fill)
        {
            using var brush = new SolidBrush(color);
            graphics.FillPath(brush, drawingPath);
        }
        else
        {
            using var pen = new Pen(color, 1);
            graphics.DrawPath(pen, drawingPath);
        }
    }

    private static GraphicsPath BuildPath(
        DisplayItem.Path path,
        float originX,
        float originY,
        float fontSize)
    {
        var drawingPath = new GraphicsPath(FillMode.Winding);
        var offsetX = originX + Em(path.X, fontSize);
        var offsetY = originY + Em(path.Y, fontSize);
        var current = new PointF(offsetX, offsetY);
        var figureStart = current;
        foreach (var command in path.Commands)
        {
            switch (command)
            {
                case PathCommand.MoveTo move:
                    drawingPath.StartFigure();
                    current = Point(move.X, move.Y, offsetX, offsetY, fontSize);
                    figureStart = current;
                    break;
                case PathCommand.LineTo line:
                    var lineEnd = Point(line.X, line.Y, offsetX, offsetY, fontSize);
                    drawingPath.AddLine(current, lineEnd);
                    current = lineEnd;
                    break;
                case PathCommand.CubicTo cubic:
                    var cubicEnd = Point(cubic.X, cubic.Y, offsetX, offsetY, fontSize);
                    drawingPath.AddBezier(
                        current,
                        Point(cubic.X1, cubic.Y1, offsetX, offsetY, fontSize),
                        Point(cubic.X2, cubic.Y2, offsetX, offsetY, fontSize),
                        cubicEnd);
                    current = cubicEnd;
                    break;
                case PathCommand.QuadTo quad:
                    var control = Point(quad.X1, quad.Y1, offsetX, offsetY, fontSize);
                    var quadEnd = Point(quad.X, quad.Y, offsetX, offsetY, fontSize);
                    var control1 = new PointF(
                        current.X + ((control.X - current.X) * 2 / 3),
                        current.Y + ((control.Y - current.Y) * 2 / 3));
                    var control2 = new PointF(
                        quadEnd.X + ((control.X - quadEnd.X) * 2 / 3),
                        quadEnd.Y + ((control.Y - quadEnd.Y) * 2 / 3));
                    drawingPath.AddBezier(current, control1, control2, quadEnd);
                    current = quadEnd;
                    break;
                case PathCommand.Close:
                    drawingPath.CloseFigure();
                    current = figureStart;
                    break;
            }
        }

        return drawingPath;
    }

    private static PointF Point(
        double x,
        double y,
        float offsetX,
        float offsetY,
        float fontSize) =>
        new(offsetX + Em(x, fontSize), offsetY + Em(y, fontSize));

    private static float Em(double value, float fontSize) => checked((float)value * fontSize);

    private static Color ToDrawingColor(RaTeXColor color) =>
        Color.FromArgb(
            ToByte(color.A),
            ToByte(color.R),
            ToByte(color.G),
            ToByte(color.B));

    private static int ToByte(float value) => (int)MathF.Round(Math.Clamp(value, 0, 1) * 255);

    private sealed class FontCache : IDisposable
    {
        private readonly Dictionary<(string FontId, float Size), Font> fonts = [];
        private readonly float defaultFontSize;

        public FontCache(float defaultFontSize)
        {
            this.defaultFontSize = defaultFontSize;
        }

        public Font? Get(string fontId, float size)
        {
            var normalizedSize = float.IsFinite(size) && size > 0 ? size : defaultFontSize;
            var key = (fontId, normalizedSize);
            if (fonts.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var font = FontStore.Value.Create(fontId, normalizedSize);
            if (font is not null)
            {
                fonts.Add(key, font);
            }

            return font;
        }

        public void Dispose()
        {
            foreach (var font in fonts.Values)
            {
                font.Dispose();
            }
        }
    }

    private sealed class GdiFontStore
    {
        private readonly Dictionary<string, LoadedFont> fonts = new(StringComparer.Ordinal);

        public GdiFontStore(string fontDirectory)
        {
            var missingFonts = new List<string>();
            foreach (var fontId in KnownFontIds)
            {
                if (!RaTeXFontCatalog.TryGet(fontId, out var descriptor))
                {
                    continue;
                }

                var path = Path.Combine(fontDirectory, descriptor.FileName);
                if (!File.Exists(path))
                {
                    missingFonts.Add(descriptor.FileName);
                    continue;
                }

                var collection = new PrivateFontCollection();
                collection.AddFontFile(path);
                if (collection.Families.Length > 0)
                {
                    fonts[fontId] = new LoadedFont(collection, collection.Families[0], descriptor.Style);
                }
                else
                {
                    collection.Dispose();
                    missingFonts.Add(descriptor.FileName);
                }
            }

            if (missingFonts.Count > 0)
            {
                throw new RaTeXException(
                    $"Missing RaTeX font assets: {string.Join(", ", missingFonts)}.");
            }
        }

        public Font? Create(string fontId, float size)
        {
            if (fonts.TryGetValue(fontId, out var loaded))
            {
                return new Font(loaded.Family, size, loaded.Style, GraphicsUnit.Pixel);
            }

            return RaTeXFontCatalog.IsSystemFallback(fontId)
                ? new Font("Segoe UI", size, FontStyle.Regular, GraphicsUnit.Pixel)
                : null;
        }

        private static IEnumerable<string> KnownFontIds => new[]
        {
            "AMS-Regular", "Caligraphic-Bold", "Caligraphic-Regular", "Fraktur-Bold",
            "Fraktur-Regular", "Main-Bold", "Main-BoldItalic", "Main-Italic",
            "Main-Regular", "Math-BoldItalic", "Math-Italic", "SansSerif-Bold",
            "SansSerif-Italic", "SansSerif-Regular", "Script-Regular", "Size1-Regular",
            "Size2-Regular", "Size3-Regular", "Size4-Regular", "Typewriter-Regular"
        };

        private sealed record LoadedFont(
            PrivateFontCollection Collection,
            FontFamily Family,
            FontStyle Style);
    }
}
