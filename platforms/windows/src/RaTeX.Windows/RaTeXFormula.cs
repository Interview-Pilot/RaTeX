namespace RaTeX.Windows;

public sealed class RaTeXFormula
{
    public RaTeXFormula(DisplayList displayList, float fontSize)
    {
        ArgumentNullException.ThrowIfNull(displayList);
        if (!float.IsFinite(fontSize) || fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize));
        }

        DisplayList = displayList;
        FontSize = fontSize;
    }

    public DisplayList DisplayList { get; }

    public float FontSize { get; }

    public float Width => checked((float)DisplayList.Width * FontSize);

    public float Height => checked((float)(DisplayList.Height + DisplayList.Depth) * FontSize);
}
