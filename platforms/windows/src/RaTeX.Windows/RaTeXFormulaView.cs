using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RaTeX.Windows;

public sealed class RaTeXFormulaView : Grid
{
    private static readonly RaTeXWin2DRenderer Renderer = new();
    private CanvasControl? canvas;
    private RaTeXFormula? formula;

    public RaTeXFormulaView()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsHitTestVisible = false;
        EnsureCanvas();
    }

    public RaTeXFormula? Formula
    {
        get => formula;
        set
        {
            if (ReferenceEquals(formula, value))
            {
                return;
            }

            formula = value;
            Width = value?.Width ?? 0;
            Height = value?.Height ?? 0;
            canvas?.Invalidate();
        }
    }

    private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (formula is not null)
        {
            Renderer.Draw(args.DrawingSession, formula);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        EnsureCanvas();
        canvas?.Invalidate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (canvas is null)
        {
            return;
        }

        canvas.Draw -= OnDraw;
        canvas.RemoveFromVisualTree();
        Children.Clear();
        canvas = null;
    }

    private void EnsureCanvas()
    {
        if (canvas is not null)
        {
            return;
        }

        canvas = new CanvasControl
        {
            UseSharedDevice = true,
            IsHitTestVisible = false
        };
        canvas.Draw += OnDraw;
        Children.Add(canvas);
    }
}
