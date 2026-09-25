namespace regex;

/// <summary>
/// Bestimmt den Text vor und nach einem Treffer, der zur Ausgabe im Zeilenkontext angezeigt wird.
/// Es werden maximal <see cref="MaxContextLength"/> Zeichen vor bzw. nach dem Treffer berücksichtigt.
/// </summary>
internal static class LineContext
{
    public const int MaxContextLength = 100;

    private static readonly char[] LineBreaks = { '\n', '\r' };

    /// <summary>
    /// Index des ersten Zeichens vor dem Treffer, das ausgegeben wird (Zeilenanfang, höchstens 100 Zeichen zurück).
    /// </summary>
    public static int GetStartIndex(string content, int matchIndex)
    {
        var windowStart = Math.Max(0, matchIndex - MaxContextLength);
        var lastBreak = content.AsSpan(windowStart, matchIndex - windowStart).LastIndexOfAny(LineBreaks);
        return lastBreak == -1 ? windowStart : windowStart + lastBreak + 1;
    }

    /// <summary>
    /// Index hinter dem letzten Zeichen nach dem Treffer, das ausgegeben wird (Zeilenende, höchstens 100 Zeichen vor).
    /// </summary>
    public static int GetEndIndex(string content, int matchEnd)
    {
        var windowEnd = Math.Min(content.Length, matchEnd + MaxContextLength);
        var nextBreak = content.AsSpan(matchEnd, windowEnd - matchEnd).IndexOfAny(LineBreaks);
        return nextBreak == -1 ? windowEnd : matchEnd + nextBreak;
    }
}
