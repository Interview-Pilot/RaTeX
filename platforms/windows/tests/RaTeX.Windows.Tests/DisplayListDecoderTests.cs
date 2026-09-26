using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RaTeX.Windows.Tests;

[TestClass]
public sealed class DisplayListDecoderTests
{
    [TestMethod]
    public void Decode_RecognizesEveryCurrentDisplayItem()
    {
        const string json = """
            {
              "version": 1,
              "width": 3.5,
              "height": 1.2,
              "depth": 0.3,
              "items": [
                {"type":"GlyphPath","x":0,"y":1,"scale":1,"font":"Main-Regular","char_code":120,"color":{"r":0,"g":0,"b":0,"a":1}},
                {"type":"Line","x":0,"y":0.5,"width":2,"thickness":0.04,"color":{"r":1,"g":0,"b":0,"a":1}},
                {"type":"Rect","x":0,"y":0,"width":1,"height":1,"color":{"r":0,"g":1,"b":0,"a":1}},
                {"type":"Path","x":0,"y":0,"fill":true,"color":{"r":0,"g":0,"b":1,"a":1},"commands":[
                  {"type":"MoveTo","x":0,"y":0},
                  {"type":"LineTo","x":1,"y":0},
                  {"type":"QuadTo","x1":1,"y1":1,"x":0,"y":1},
                  {"type":"CubicTo","x1":0,"y1":1,"x2":0,"y2":0,"x":0,"y":0},
                  {"type":"Close"}
                ]}
              ]
            }
            """;

        var displayList = DisplayListDecoder.Decode(json);

        Assert.AreEqual(1, displayList.Version);
        Assert.AreEqual(3.5, displayList.Width);
        Assert.AreEqual(4, displayList.Items.Count);
        Assert.IsInstanceOfType(displayList.Items[0], typeof(DisplayItem.GlyphPath));
        Assert.IsInstanceOfType(displayList.Items[1], typeof(DisplayItem.Line));
        Assert.IsInstanceOfType(displayList.Items[2], typeof(DisplayItem.Rect));
        var path = (DisplayItem.Path)displayList.Items[3];
        Assert.AreEqual(5, path.Commands.Count);
    }

    [TestMethod]
    public void Decode_IgnoresUnknownItemsCommandsAndFields()
    {
        const string json = """
            {
              "version": 2,
              "width": 1,
              "height": 1,
              "depth": 0,
              "future": true,
              "items": [
                {"type":"FutureItem","value":1},
                {"type":"Path","x":0,"y":0,"fill":false,"color":{"r":0,"g":0,"b":0,"a":1},"commands":[
                  {"type":"MoveTo","x":0,"y":0},
                  {"type":"FutureCommand","value":1},
                  {"type":"LineTo","x":1,"y":1}
                ]}
              ]
            }
            """;

        var displayList = DisplayListDecoder.Decode(json);

        Assert.AreEqual(1, displayList.Items.Count);
        var path = (DisplayItem.Path)displayList.Items[0];
        Assert.AreEqual(2, path.Commands.Count);
    }

    [TestMethod]
    public void Decode_DefaultsMissingOptionalProtocolFields()
    {
        const string json = """
            {
              "width": 1,
              "height": 1,
              "depth": 0,
              "items": [
                {"type":"Line","x":0,"y":0.5,"width":1,"thickness":0.04,"color":{"r":0,"g":0,"b":0,"a":1}}
              ]
            }
            """;

        var displayList = DisplayListDecoder.Decode(json);

        Assert.AreEqual(0, displayList.Version);
        Assert.IsFalse(((DisplayItem.Line)displayList.Items[0]).Dashed);
    }
}
