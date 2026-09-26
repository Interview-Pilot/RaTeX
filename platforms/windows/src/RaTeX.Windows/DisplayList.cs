namespace RaTeX.Windows;

public sealed record DisplayList(
    int Version,
    double Width,
    double Height,
    double Depth,
    IReadOnlyList<DisplayItem> Items);

public abstract record DisplayItem
{
    private DisplayItem()
    {
    }

    public sealed record GlyphPath(
        double X,
        double Y,
        double Scale,
        string Font,
        int CharCode,
        RaTeXColor Color) : DisplayItem;

    public sealed record Line(
        double X,
        double Y,
        double Width,
        double Thickness,
        RaTeXColor Color,
        bool Dashed) : DisplayItem;

    public sealed record Rect(
        double X,
        double Y,
        double Width,
        double Height,
        RaTeXColor Color) : DisplayItem;

    public sealed record Path(
        double X,
        double Y,
        IReadOnlyList<PathCommand> Commands,
        bool Fill,
        RaTeXColor Color) : DisplayItem;
}

public abstract record PathCommand
{
    private PathCommand()
    {
    }

    public sealed record MoveTo(double X, double Y) : PathCommand;

    public sealed record LineTo(double X, double Y) : PathCommand;

    public sealed record CubicTo(
        double X1,
        double Y1,
        double X2,
        double Y2,
        double X,
        double Y) : PathCommand;

    public sealed record QuadTo(
        double X1,
        double Y1,
        double X,
        double Y) : PathCommand;

    public sealed record Close : PathCommand;
}

public readonly record struct RaTeXColor(float R, float G, float B, float A)
{
    public static RaTeXColor Black { get; } = new(0, 0, 0, 1);
}
