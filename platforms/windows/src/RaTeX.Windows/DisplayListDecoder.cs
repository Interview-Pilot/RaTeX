using System.Text.Json;

namespace RaTeX.Windows;

internal static class DisplayListDecoder
{
    public static DisplayList Decode(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            MaxDepth = 128
        });
        var root = document.RootElement;
        var items = new List<DisplayItem>();
        foreach (var element in Required(root, "items").EnumerateArray())
        {
            if (TryDecodeItem(element, out var item))
            {
                items.Add(item);
            }
        }

        return new DisplayList(
            OptionalInt(root, "version"),
            RequiredDouble(root, "width"),
            RequiredDouble(root, "height"),
            RequiredDouble(root, "depth"),
            items);
    }

    private static bool TryDecodeItem(JsonElement element, out DisplayItem item)
    {
        item = null!;
        if (!element.TryGetProperty("type", out var typeElement))
        {
            return false;
        }

        var type = typeElement.GetString();
        switch (type)
        {
            case "GlyphPath":
                item = new DisplayItem.GlyphPath(
                    RequiredDouble(element, "x"),
                    RequiredDouble(element, "y"),
                    RequiredDouble(element, "scale"),
                    RequiredString(element, "font"),
                    RequiredInt(element, "char_code"),
                    DecodeColor(Required(element, "color")));
                return true;
            case "Line":
                item = new DisplayItem.Line(
                    RequiredDouble(element, "x"),
                    RequiredDouble(element, "y"),
                    RequiredDouble(element, "width"),
                    RequiredDouble(element, "thickness"),
                    DecodeColor(Required(element, "color")),
                    OptionalBool(element, "dashed"));
                return true;
            case "Rect":
                item = new DisplayItem.Rect(
                    RequiredDouble(element, "x"),
                    RequiredDouble(element, "y"),
                    RequiredDouble(element, "width"),
                    RequiredDouble(element, "height"),
                    DecodeColor(Required(element, "color")));
                return true;
            case "Path":
                item = new DisplayItem.Path(
                    RequiredDouble(element, "x"),
                    RequiredDouble(element, "y"),
                    DecodeCommands(Required(element, "commands")),
                    Required(element, "fill").GetBoolean(),
                    DecodeColor(Required(element, "color")));
                return true;
            default:
                return false;
        }
    }

    private static IReadOnlyList<PathCommand> DecodeCommands(JsonElement array)
    {
        var commands = new List<PathCommand>();
        foreach (var element in array.EnumerateArray())
        {
            if (!element.TryGetProperty("type", out var typeElement))
            {
                continue;
            }

            PathCommand? command = typeElement.GetString() switch
            {
                "MoveTo" => new PathCommand.MoveTo(
                    RequiredDouble(element, "x"),
                    RequiredDouble(element, "y")),
                "LineTo" => new PathCommand.LineTo(
                    RequiredDouble(element, "x"),
                    RequiredDouble(element, "y")),
                "CubicTo" => new PathCommand.CubicTo(
                    RequiredDouble(element, "x1"),
                    RequiredDouble(element, "y1"),
                    RequiredDouble(element, "x2"),
                    RequiredDouble(element, "y2"),
                    RequiredDouble(element, "x"),
                    RequiredDouble(element, "y")),
                "QuadTo" => new PathCommand.QuadTo(
                    RequiredDouble(element, "x1"),
                    RequiredDouble(element, "y1"),
                    RequiredDouble(element, "x"),
                    RequiredDouble(element, "y")),
                "Close" => new PathCommand.Close(),
                _ => null
            };
            if (command is not null)
            {
                commands.Add(command);
            }
        }

        return commands;
    }

    private static RaTeXColor DecodeColor(JsonElement element)
    {
        return new RaTeXColor(
            (float)RequiredDouble(element, "r"),
            (float)RequiredDouble(element, "g"),
            (float)RequiredDouble(element, "b"),
            (float)RequiredDouble(element, "a"));
    }

    private static JsonElement Required(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            throw new JsonException($"RaTeX display list is missing '{name}'.");
        }

        return value;
    }

    private static double RequiredDouble(JsonElement element, string name)
    {
        var value = Required(element, name).GetDouble();
        if (!double.IsFinite(value))
        {
            throw new JsonException($"RaTeX display list contains a non-finite '{name}'.");
        }

        return value;
    }

    private static int RequiredInt(JsonElement element, string name) => Required(element, name).GetInt32();

    private static string RequiredString(JsonElement element, string name) =>
        Required(element, name).GetString() ?? throw new JsonException($"RaTeX display list has a null '{name}'.");

    private static int OptionalInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.GetInt32() : 0;

    private static bool OptionalBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.GetBoolean();
}
