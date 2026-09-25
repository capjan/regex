using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace regex;

static class Program
{
    static int Main(string[] args)
    {
        var root = CliDefinition.Create(Run);
        return root.Parse(args).Invoke();
    }

    private static int Run(CliOptions options)
    {
        try
        {
            if (options.ShowVersion)
            {
                PrintVersionInformation();
                return 0;
            }

            if (options.Pattern == null)
                throw new ArgumentException("Missing regular expression pattern");

            if ((options.DryRun || options.Diff) && options.Replace == null)
                throw new ArgumentException("--dry-run and --diff require --replace");

            var filelist = new List<string>();

            foreach (var itm in options.Paths)
            {
                if (itm.EndsWith(Path.DirectorySeparatorChar) || itm.EndsWith(Path.AltDirectorySeparatorChar))
                {
                    // directory
                    filelist.AddRange(Directory.GetFiles(
                        itm,
                        options.Filter,
                        options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
                }
                else
                {
                    filelist.Add(itm);
                }
            }

            // Ausführen
            foreach (var filePath in filelist)
            {
                ProcessInputFile(filePath, options);
            }

            return 0;
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(e.Message);
            Console.ResetColor();
            Console.Error.WriteLine($"Type '{CliDefinition.ProgramName} --help' for more information.");
            return 1;
        }
    }

    private static void PrintVersionInformation()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString(3);
        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
        var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "";

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"{company} {CliDefinition.ProgramName} Version {version}");
        Console.ResetColor();
        Console.WriteLine(copyright);
    }

    #region Hilfsfunktionen

    /// <summary>
    /// Verarbeitet eine Datei mit den geg. Parametern
    /// </summary>
    /// <param name="filePath">Pfad zur Eingabedatei</param>
    /// <param name="options">Suchpattern, Ersetzungsmuster und Ausgabeoptionen</param>
    private static void ProcessInputFile(string filePath, CliOptions options)
    {
        var pattern = options.Pattern!;
        var offsetColumnWidth = options.OffsetColumnWidth;

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine(@"File not found error: " + filePath);
            return;
        }

        // Bei --diff bleibt stdout dem Patch vorbehalten, Zusatzinformationen gehen nach stderr.
        var info = options.Diff ? Console.Error : Console.Out;

        if (options.Verbose)
        {
            info.WriteLine(@"Progressing: " + filePath);
        }

        var fileContent = File.ReadAllText(filePath);

        if (options.Replace == null)
        {
            // search mode
            var countOfPrintedMatches = 0;
            foreach (Match m in Regex.Matches(fileContent, pattern, options.RegExOptions))
            {
                if (!options.OnlyMatching)
                    Console.Write(@"Offset:" + m.Index.ToString(CultureInfo.InvariantCulture).PadRight(offsetColumnWidth) + @" ");
                PrintMatch(fileContent, m, options.OnlyMatching);
                Console.WriteLine();
                countOfPrintedMatches++;
                if (countOfPrintedMatches == options.MaxMatchesCount) break;
            }

            if (!options.OnlyMatching && (options.Verbose || countOfPrintedMatches > 0))
            {
                PrintMatchResult(filePath, countOfPrintedMatches);
            }
        }
        else
        {
            // replace mode
            var matchCount = 0;
            var replaced = Regex.Replace(fileContent, pattern,
                match =>
                {
                    matchCount++;
                    var result = match.Result(options.Replace);
                    if (options.Verbose)
                    {
                        info.WriteLine(@"Offset:" + match.Index.ToString(CultureInfo.InvariantCulture).PadRight(offsetColumnWidth) + @" " + match.Value + @"->" + result);
                    }
                    return result;
                }, options.RegExOptions);

            var writeChanges = !options.DryRun && !options.Diff;

            // Dateien ohne Treffer nicht neu schreiben (Encoding und Zeitstempel bleiben erhalten).
            if (matchCount > 0)
            {
                if (options.Diff)
                    PrintDiff(UnifiedDiff.Create(filePath, fileContent, replaced));
                if (writeChanges)
                    File.WriteAllText(filePath, replaced);
            }

            // Bei --diff besteht die Ausgabe nur aus dem Diff, damit sie sich an patch weiterreichen lässt.
            if (!options.Diff && (options.Verbose || matchCount > 0))
            {
                PrintReplacementResult(filePath, matchCount, options.DryRun);
            }
        }
    }

    private static void PrintMatch(string fileContent, Match m, bool onlyMatches)
    {
        var firstIndexAfterMatch = m.Index + m.Length;

        // PreString nach stdout schreiben
        if (!onlyMatches)
        {
            var indexOfPreString = LineContext.GetStartIndex(fileContent, m.Index);
            Console.Write(fileContent.AsSpan(indexOfPreString, m.Index - indexOfPreString));
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(m.Value);
        Console.ResetColor();

        // PostString nach stdout schreiben, sofern es einen gibt.
        if (onlyMatches) return;
        var indexOfNextNewLine = LineContext.GetEndIndex(fileContent, firstIndexAfterMatch);
        Console.Write(fileContent.AsSpan(firstIndexAfterMatch, indexOfNextNewLine - firstIndexAfterMatch));
    }

    private static void PrintMatchResult(string filePath, int matchCount)
    {
        if (matchCount == 1)
        {
            Console.WriteLine(@"{0}: found 1 match", filePath);
        }
        else
        {
            Console.WriteLine(@"{0}: found {1} matches", filePath, matchCount);
        }
    }

    private static void PrintReplacementResult(string filePath, int replacementCount, bool dryRun)
    {
        var verb = dryRun ? "would do" : "did";
        if (replacementCount == 1)
        {
            Console.WriteLine(@"{0}: {1} 1 replacement", filePath, verb);
        }
        else
        {
            Console.WriteLine(@"{0}: {1} {2:n0} replacements", filePath, verb, replacementCount);
        }
    }

    private static void PrintDiff(string diff)
    {
        // Treffer, die den Text nicht verändern (z. B. foo -> foo), ergeben keinen Diff.
        if (diff.Length == 0) return;

        // Farben nur im Terminal, damit umgeleitete Ausgabe ein gültiger Patch bleibt.
        var useColor = !Console.IsOutputRedirected;
        foreach (var line in diff[..^1].Split('\n'))
        {
            if (useColor)
            {
                if (line.StartsWith("+++") || line.StartsWith("---")) Console.ForegroundColor = ConsoleColor.White;
                else if (line.StartsWith('+')) Console.ForegroundColor = ConsoleColor.Green;
                else if (line.StartsWith('-')) Console.ForegroundColor = ConsoleColor.Red;
                else if (line.StartsWith("@@")) Console.ForegroundColor = ConsoleColor.Cyan;
            }

            Console.Write(line);
            if (useColor) Console.ResetColor();
            Console.Write('\n');
        }
    }
    #endregion
}
