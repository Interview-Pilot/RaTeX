using System.Runtime.InteropServices;

namespace RaTeX.Windows;

public static class RaTeXEngine
{
    public static DisplayList Parse(
        string latex,
        bool displayMode = true,
        RaTeXColor? color = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(latex);

        var nativeColor = color ?? RaTeXColor.Black;
        var colorPointer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeColor>());
        try
        {
            Marshal.StructureToPtr(
                new NativeColor(nativeColor.R, nativeColor.G, nativeColor.B, nativeColor.A),
                colorPointer,
                fDeleteOld: false);
            var options = new NativeOptions(
                (nuint)Marshal.SizeOf<NativeOptions>(),
                displayMode ? 1 : 0,
                colorPointer);
            var result = NativeMethods.ParseAndLayout(latex, in options);
            if (result.ErrorCode != 0 || result.Data == IntPtr.Zero)
            {
                var message = ReadLastError();
                if (result.Data != IntPtr.Zero)
                {
                    NativeMethods.FreeDisplayList(result.Data);
                }

                throw new RaTeXException(message);
            }

            try
            {
                var json = Marshal.PtrToStringUTF8(result.Data);
                if (string.IsNullOrEmpty(json))
                {
                    throw new RaTeXException("RaTeX returned an empty display list.");
                }

                return DisplayListDecoder.Decode(json);
            }
            catch (RaTeXException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is ArgumentException
                    or FormatException
                    or InvalidOperationException
                    or OverflowException
                    or System.Text.Json.JsonException)
            {
                throw new RaTeXException("RaTeX returned an invalid display list.", exception);
            }
            finally
            {
                NativeMethods.FreeDisplayList(result.Data);
            }
        }
        catch (RaTeXException)
        {
            throw;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            throw new RaTeXException("The RaTeX Windows native library could not be loaded.", exception);
        }
        finally
        {
            Marshal.FreeHGlobal(colorPointer);
        }
    }

    private static string ReadLastError()
    {
        var pointer = NativeMethods.GetLastError();
        return pointer == IntPtr.Zero
            ? "RaTeX could not render the formula."
            : Marshal.PtrToStringUTF8(pointer) ?? "RaTeX could not render the formula.";
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeColor
    {
        public NativeColor(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public readonly float R;
        public readonly float G;
        public readonly float B;
        public readonly float A;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeOptions
    {
        public NativeOptions(nuint structSize, int displayMode, IntPtr color)
        {
            StructSize = structSize;
            DisplayMode = displayMode;
            Color = color;
        }

        public readonly nuint StructSize;
        public readonly int DisplayMode;
        public readonly IntPtr Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeResult
    {
        public NativeResult(IntPtr data, int errorCode)
        {
            Data = data;
            ErrorCode = errorCode;
        }

        public readonly IntPtr Data;
        public readonly int ErrorCode;
    }

    private static class NativeMethods
    {
        private const string LibraryName = "ratex_ffi";

        [DllImport(LibraryName, EntryPoint = "ratex_parse_and_layout", CallingConvention = CallingConvention.Cdecl)]
        internal static extern NativeResult ParseAndLayout(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string latex,
            in NativeOptions options);

        [DllImport(LibraryName, EntryPoint = "ratex_free_display_list", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FreeDisplayList(IntPtr pointer);

        [DllImport(LibraryName, EntryPoint = "ratex_get_last_error", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetLastError();
    }
}
