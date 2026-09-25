using System.CommandLine;

namespace regex;

/// <summary>
/// Beschreibt die Kommandozeile (Argumente und Optionen) und liefert die geparsten <see cref="CliOptions"/>.
/// </summary>
internal static class CliDefinition
{
    public const string ProgramName = "regex";

    public static RootCommand Create(Func<CliOptions, int> run)
    {
        var pattern = new Argument<string?>("pattern")
        {
            Description = "The search pattern as .NET Regular Expression (RegEx).",
            Arity = ArgumentArity.ZeroOrOne
        };
        var paths = new Argument<string[]>("path")
        {
            Description = "File or directory to operate. (A directory must end with a directory separator)",
            Arity = ArgumentArity.ZeroOrMore
        };

        var replace = new Option<string?>("--replace", "-R") { Description = "replacement Pattern (regex)" };
        var caseSensitive = new Option<bool>("--case-sensitive", "-c")
        {
            Description = "enables case-sensitive behavior - btw. disables the by default enabled ignore-case option"
        };
        var filter = new Option<string>("--filter", "-f")
        {
            Description = "wildcard based file filter, e.g. *.txt",
            DefaultValueFactory = _ => "*.*"
        };
        var recursive = new Option<bool>("--recursive", "-r") { Description = "progress all subdirectories" };
        var offsetWidth = new Option<int>("--offset-width")
        {
            Description = "output-formatting: set the count of characters used for the offset column",
            DefaultValueFactory = _ => 6
        };
        var onlyMatching = new Option<bool>("--only-matching", "-o") { Description = "prints only the match" };
        var maxCount = new Option<int?>("--max-count", "-m") { Description = "limit matches to the given count" };
        var verbose = new Option<bool>("--verbose", "-v") { Description = "show additional information" };
        var version = new Option<bool>("--version", "-V") { Description = "show version information" };

        var root = new RootCommand(
            "regex is a CLI frontend for the .NET Regular Expression Engine.\n" +
            "It searches for a given search pattern in the file contents of every given input file.\n" +
            "Optionally you can set a replace pattern that will be applied on every match.\n\n" +
            "Examples:\n" +
            "  regex \"Name:(?<name>[A-Za-z]+)\" --replace \"id=${name}\" names.txt\n" +
            "  regex --recursive --filter *.txt Hello ./");

        // Die eingebaute --version Option durch eine eigene mit Kurzform -V ersetzen.
        foreach (var builtIn in root.Options.Where(o => o.Name == "--version").ToList())
            root.Options.Remove(builtIn);

        root.Arguments.Add(pattern);
        root.Arguments.Add(paths);
        foreach (var option in new Option[]
                 { replace, caseSensitive, filter, recursive, offsetWidth, onlyMatching, maxCount, verbose, version })
            root.Options.Add(option);

        root.SetAction(parseResult => run(new CliOptions
        {
            Pattern = parseResult.GetValue(pattern),
            Paths = parseResult.GetValue(paths) ?? [],
            Replace = parseResult.GetValue(replace),
            CaseSensitive = parseResult.GetValue(caseSensitive),
            Filter = parseResult.GetValue(filter) ?? "*.*",
            Recursive = parseResult.GetValue(recursive),
            OffsetColumnWidth = parseResult.GetValue(offsetWidth),
            OnlyMatching = parseResult.GetValue(onlyMatching),
            MaxMatchesCount = parseResult.GetValue(maxCount) ?? int.MaxValue,
            Verbose = parseResult.GetValue(verbose),
            ShowVersion = parseResult.GetValue(version)
        }));

        return root;
    }
}
