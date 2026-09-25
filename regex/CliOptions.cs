using System.Text.RegularExpressions;

namespace regex;

/// <summary>
/// Geparste Kommandozeilenoptionen.
/// </summary>
internal sealed class CliOptions
{
    public string? Pattern { get; init; }
    public string[] Paths { get; init; } = [];
    public string? Replace { get; init; }
    public bool DryRun { get; init; }
    public bool Diff { get; init; }
    public bool CaseSensitive { get; init; }
    public string Filter { get; init; } = "*.*";
    public bool Recursive { get; init; }
    public int OffsetColumnWidth { get; init; } = 6;
    public bool OnlyMatching { get; init; }
    public int MaxMatchesCount { get; init; } = int.MaxValue;
    public bool Verbose { get; init; }
    public bool ShowVersion { get; init; }

    public RegexOptions RegExOptions => CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
}
