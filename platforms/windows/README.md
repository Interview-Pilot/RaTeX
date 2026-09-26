# RaTeX for Windows

`RaTeX.Windows` is the native Windows binding for the shared RaTeX Rust engine. It uses the same C ABI and versioned DisplayList JSON protocol as the Apple, Android, JVM, and Flutter bindings.

The binding contains:

- a typed C# wrapper around `ratex_ffi.dll`
- a forward-compatible DisplayList decoder
- a Win2D renderer for WinUI surfaces
- a GDI+ renderer for native Windows overlay surfaces
- bundled KaTeX fonts
- native `win-x64` and `win-arm64` runtime assets

There is no WebView, JavaScript runtime, subprocess, or second LaTeX parser.

## Build

From a Windows PowerShell prompt at the repository root:

```powershell
.\platforms\windows\build-windows.ps1
```

Use `-Architecture x64` or `-Architecture ARM64` for one architecture. Add `-Pack` to create the package after both native runtime assets and the managed tests succeed.

## Use

Parse once, then draw the resulting formula on the Windows surface that owns the pixels:

```csharp
var displayList = RaTeXEngine.Parse(@"\frac{1}{2}", displayMode: false);
var formula = new RaTeXFormula(displayList, fontSize: 18);
```

- Use `RaTeXFormulaView` inside WinUI content.
- Use `RaTeXGdiRenderer.Draw` inside an existing `System.Drawing.Graphics` render pass.

All dimensions are derived from the Rust DisplayList and remain in logical pixels at the supplied font size.
