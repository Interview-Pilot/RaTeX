using System.Drawing;

namespace RaTeX.Windows;

internal static class RaTeXFontCatalog
{
    private static readonly IReadOnlyDictionary<string, FontDescriptor> Fonts =
        new Dictionary<string, FontDescriptor>(StringComparer.Ordinal)
        {
            ["AMS-Regular"] = new("KaTeX_AMS-Regular.ttf", "KaTeX_AMS", FontStyle.Regular),
            ["Caligraphic-Bold"] = new("KaTeX_Caligraphic-Bold.ttf", "KaTeX_Caligraphic", FontStyle.Bold),
            ["Caligraphic-Regular"] = new("KaTeX_Caligraphic-Regular.ttf", "KaTeX_Caligraphic", FontStyle.Regular),
            ["Fraktur-Bold"] = new("KaTeX_Fraktur-Bold.ttf", "KaTeX_Fraktur", FontStyle.Bold),
            ["Fraktur-Regular"] = new("KaTeX_Fraktur-Regular.ttf", "KaTeX_Fraktur", FontStyle.Regular),
            ["Main-Bold"] = new("KaTeX_Main-Bold.ttf", "KaTeX_Main", FontStyle.Bold),
            ["Main-BoldItalic"] = new("KaTeX_Main-BoldItalic.ttf", "KaTeX_Main", FontStyle.Bold | FontStyle.Italic),
            ["Main-Italic"] = new("KaTeX_Main-Italic.ttf", "KaTeX_Main", FontStyle.Italic),
            ["Main-Regular"] = new("KaTeX_Main-Regular.ttf", "KaTeX_Main", FontStyle.Regular),
            ["Math-BoldItalic"] = new("KaTeX_Math-BoldItalic.ttf", "KaTeX_Math", FontStyle.Bold | FontStyle.Italic),
            ["Math-Italic"] = new("KaTeX_Math-Italic.ttf", "KaTeX_Math", FontStyle.Italic),
            ["SansSerif-Bold"] = new("KaTeX_SansSerif-Bold.ttf", "KaTeX_SansSerif", FontStyle.Bold),
            ["SansSerif-Italic"] = new("KaTeX_SansSerif-Italic.ttf", "KaTeX_SansSerif", FontStyle.Italic),
            ["SansSerif-Regular"] = new("KaTeX_SansSerif-Regular.ttf", "KaTeX_SansSerif", FontStyle.Regular),
            ["Script-Regular"] = new("KaTeX_Script-Regular.ttf", "KaTeX_Script", FontStyle.Regular),
            ["Size1-Regular"] = new("KaTeX_Size1-Regular.ttf", "KaTeX_Size1", FontStyle.Regular),
            ["Size2-Regular"] = new("KaTeX_Size2-Regular.ttf", "KaTeX_Size2", FontStyle.Regular),
            ["Size3-Regular"] = new("KaTeX_Size3-Regular.ttf", "KaTeX_Size3", FontStyle.Regular),
            ["Size4-Regular"] = new("KaTeX_Size4-Regular.ttf", "KaTeX_Size4", FontStyle.Regular),
            ["Typewriter-Regular"] = new("KaTeX_Typewriter-Regular.ttf", "KaTeX_Typewriter", FontStyle.Regular)
        };

    public static bool TryGet(string fontId, out FontDescriptor descriptor) =>
        Fonts.TryGetValue(fontId, out descriptor!);

    public static bool IsSystemFallback(string fontId) =>
        fontId is "CJK-Regular" or "CJK-Fallback" or "Emoji-Fallback";

    internal sealed record FontDescriptor(string FileName, string FamilyName, FontStyle Style);
}
