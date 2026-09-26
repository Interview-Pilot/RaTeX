namespace RaTeX.Windows;

public sealed class RaTeXException : Exception
{
    public RaTeXException(string message)
        : base(message)
    {
    }

    public RaTeXException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
