using System.Text;

namespace regex;

/// <summary>
/// Erzeugt einen zeilenbasierten Unified Diff (wie <c>diff -u</c>) zwischen zwei Texten.
/// </summary>
internal static class UnifiedDiff
{
    public const int ContextLines = 3;

    /// <summary>
    /// Maximale Größe der LCS-Tabelle. Bei größeren geänderten Bereichen wird der Bereich
    /// vollständig als gelöscht und neu eingefügt dargestellt, statt viel Speicher zu belegen.
    /// </summary>
    private const long MaxTableCells = 4_000_000;

    private const char Keep = ' ';
    private const char Remove = '-';
    private const char Add = '+';

    /// <summary>
    /// Liefert den Diff als Text oder einen leeren String, wenn beide Texte gleich sind.
    /// </summary>
    public static string Create(string path, string oldText, string newText)
    {
        var ops = Compare(SplitLines(oldText), SplitLines(newText));

        // oldBefore[k] / newBefore[k]: Anzahl Zeilen von alt bzw. neu vor Operation k.
        var oldBefore = new int[ops.Count + 1];
        var newBefore = new int[ops.Count + 1];
        for (var k = 0; k < ops.Count; k++)
        {
            oldBefore[k + 1] = oldBefore[k] + (ops[k].Kind == Add ? 0 : 1);
            newBefore[k + 1] = newBefore[k] + (ops[k].Kind == Remove ? 0 : 1);
        }

        var changes = Enumerable.Range(0, ops.Count).Where(k => ops[k].Kind != Keep).ToList();
        if (changes.Count == 0) return string.Empty;

        var result = new StringBuilder();
        result.Append("--- ").Append(path).Append('\n');
        result.Append("+++ ").Append(path).Append('\n');

        var i = 0;
        while (i < changes.Count)
        {
            // Änderungen zu einem Hunk zusammenfassen, solange ihr Kontext überlappt.
            var last = changes[i];
            var j = i + 1;
            while (j < changes.Count && changes[j] - last - 1 <= 2 * ContextLines)
                last = changes[j++];

            var start = Math.Max(0, changes[i] - ContextLines);
            var end = Math.Min(ops.Count, last + ContextLines + 1);
            var oldCount = oldBefore[end] - oldBefore[start];
            var newCount = newBefore[end] - newBefore[start];
            // Bei einem leeren Bereich zeigt die Startzeile auf die Zeile davor.
            var oldStart = oldBefore[start] + (oldCount == 0 ? 0 : 1);
            var newStart = newBefore[start] + (newCount == 0 ? 0 : 1);

            result.Append($"@@ -{oldStart},{oldCount} +{newStart},{newCount} @@\n");
            for (var k = start; k < end; k++)
            {
                var (kind, line) = ops[k];
                result.Append(kind).Append(line);
                if (!line.EndsWith('\n'))
                    result.Append("\n\\ No newline at end of file\n");
            }

            i = j;
        }

        return result.ToString();
    }

    /// <summary>
    /// Teilt den Text in Zeilen, jeweils inklusive des abschließenden '\n' (außer bei der letzten Zeile).
    /// </summary>
    private static string[] SplitLines(string text)
    {
        var lines = new List<string>();
        var start = 0;
        while (start < text.Length)
        {
            var newline = text.IndexOf('\n', start);
            var end = newline == -1 ? text.Length : newline + 1;
            lines.Add(text[start..end]);
            start = end;
        }

        return lines.ToArray();
    }

    private static List<(char Kind, string Line)> Compare(string[] a, string[] b)
    {
        var ops = new List<(char, string)>();

        var prefix = 0;
        while (prefix < a.Length && prefix < b.Length && a[prefix] == b[prefix]) prefix++;
        var suffix = 0;
        while (suffix < a.Length - prefix && suffix < b.Length - prefix &&
               a[a.Length - 1 - suffix] == b[b.Length - 1 - suffix])
            suffix++;

        for (var k = 0; k < prefix; k++) ops.Add((Keep, a[k]));

        var n = a.Length - prefix - suffix;
        var m = b.Length - prefix - suffix;
        if ((long)(n + 1) * (m + 1) > MaxTableCells)
        {
            for (var x = 0; x < n; x++) ops.Add((Remove, a[prefix + x]));
            for (var y = 0; y < m; y++) ops.Add((Add, b[prefix + y]));
        }
        else
        {
            // lcs[x, y]: Länge der längsten gemeinsamen Teilfolge von a[x..] und b[y..] im mittleren Bereich.
            var lcs = new int[n + 1, m + 1];
            for (var x = n - 1; x >= 0; x--)
            for (var y = m - 1; y >= 0; y--)
                lcs[x, y] = a[prefix + x] == b[prefix + y]
                    ? lcs[x + 1, y + 1] + 1
                    : Math.Max(lcs[x + 1, y], lcs[x, y + 1]);

            int cx = 0, cy = 0;
            while (cx < n || cy < m)
            {
                if (cx < n && cy < m && a[prefix + cx] == b[prefix + cy])
                {
                    ops.Add((Keep, a[prefix + cx]));
                    cx++;
                    cy++;
                }
                else if (cx < n && (cy == m || lcs[cx + 1, cy] >= lcs[cx, cy + 1]))
                {
                    // Bei Gleichstand zuerst löschen, dann einfügen (wie diff -u).
                    ops.Add((Remove, a[prefix + cx++]));
                }
                else
                {
                    ops.Add((Add, b[prefix + cy++]));
                }
            }
        }

        for (var k = suffix; k > 0; k--) ops.Add((Keep, a[a.Length - k]));
        return ops;
    }
}
