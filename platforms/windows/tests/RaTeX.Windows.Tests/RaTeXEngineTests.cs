using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RaTeX.Windows.Tests;

[TestClass]
public sealed class RaTeXEngineTests
{
    [TestMethod]
    [DataRow(@"\boxed{\frac{1}{2}}")]
    [DataRow(@"\text{D&A}")]
    public void Parse_ProducesFiniteDisplayList(string latex)
    {
        var displayList = RaTeXEngine.Parse(latex, displayMode: true);

        Assert.IsTrue(double.IsFinite(displayList.Width));
        Assert.IsTrue(double.IsFinite(displayList.Height));
        Assert.IsTrue(double.IsFinite(displayList.Depth));
        Assert.IsTrue(displayList.Width > 0);
        Assert.IsTrue(displayList.Height + displayList.Depth > 0);
        Assert.IsTrue(displayList.Items.Count > 0);
    }

    [TestMethod]
    public void Formula_ConvertsEmDimensionsToLogicalPixels()
    {
        var displayList = new DisplayList(
            Version: 1,
            Width: 2.5,
            Height: 1.25,
            Depth: 0.25,
            Items: []);

        var formula = new RaTeXFormula(displayList, fontSize: 20);

        Assert.AreEqual(50f, formula.Width);
        Assert.AreEqual(30f, formula.Height);
    }

    [TestMethod]
    public void GdiRenderer_DrawsVisibleFormulaContent()
    {
        var formula = new RaTeXFormula(
            RaTeXEngine.Parse(@"\boxed{x^2}", displayMode: true),
            fontSize: 32);
        var width = Math.Max(1, (int)Math.Ceiling(formula.Width));
        var height = Math.Max(1, (int)Math.Ceiling(formula.Height));
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);

        new RaTeXGdiRenderer().Draw(graphics, formula);

        var containsInk = false;
        for (var y = 0; y < bitmap.Height && !containsInk; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() != Color.White.ToArgb())
                {
                    containsInk = true;
                    break;
                }
            }
        }

        Assert.IsTrue(containsInk);
    }
}
