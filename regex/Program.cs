using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace regex
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var options = new CliOptions(args);

            try
            {

                if (options.ShowVersion)
                {
                    options.PrintVersionInformation();
                    Environment.Exit(0);
                }

                if (options.ShowHelp)
                {
                    options.WriteHelp(Console.Out);
                    Environment.Exit(0);
                }


                if (options.Extra.Length == 0)
                    throw new ArgumentException("Missing regular expression pattern");


                var pattern = options.Extra[0];

                var filelist = new List<string>();

                for (var i = 1; i < options.Extra.Length; i++)
                {
                    var itm = options.Extra[i];

                    if (itm.EndsWith(new string(Path.DirectorySeparatorChar, 1)) ||
                        itm.EndsWith(new string(Path.AltDirectorySeparatorChar, 1)))
                    {
                        // directory
                        filelist.AddRange(options.Recursive
                                              ? Directory.GetFiles(itm.Substring(0, itm.Length - 1), options.Filter, SearchOption.AllDirectories)
                                              : Directory.GetFiles(itm.Substring(0, itm.Length - 1), options.Filter, SearchOption.TopDirectoryOnly));
                    }
                    else
                    {
                        filelist.Add(itm);
                    }
                }

                // Ausführen
                foreach (var filePath in filelist)
                {
                    ProcessInputFile(
                        filePath,
                        pattern,
                        options.RegExOptions,
                        options.Replace,
                        options.Verbose,
                        options.OffsetColumnWidth);
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e.Message);
                Console.ResetColor();
                options.WriteMinimalHelp(Console.Out);
                // options.WriteHelp(Console.Out);
            }

        }

        #region Hilfsfunktionen



        /// <summary>
        /// Verarbeitet eine Datei mit den geg. Parametern
        /// </summary>
        /// <param name="filePath">Pfad zur Eingabedatei</param>
        /// <param name="pattern">Suchpattern als regulärer Ausdruck</param>
        /// <param name="regexOptions">Verwendete Optionen für die Suche mit dem regulären Ausdruck</param>
        /// <param name="replace">Ersetzungsmuster als regulärer Ausdruck (Optional) Null wenn nicht ersetzt werden soll</param>
        /// <param name="verbose">Sollen zusätzliche Informationen angezeigt werden?</param>
        /// <param name="offsetColumnWidth">Zeichen Anzahl für die Offset Spalte in der Ausgabe</param>
        private static void ProcessInputFile(string filePath,
                                             string pattern,
                                             RegexOptions regexOptions,
                                             string replace,
                                             bool verbose,
                                             int offsetColumnWidth)
        {

            if (!File.Exists(filePath))
            {
                Console.WriteLine(@"File not found error: " + filePath);
                return;
            }

            if (verbose)
            {
                Console.WriteLine(@"Progressing: " + filePath);
            }

            var matchCount = 0;
            var fileContent = File.ReadAllText(filePath);

            if (replace == null)
            {
                // search mode
                MatchCollection mc = Regex.Matches(fileContent, pattern, regexOptions);

                foreach (Match m in mc)
                {
                    Console.Write(@"Offset:" + m.Index.ToString(CultureInfo.InvariantCulture).PadRight(offsetColumnWidth) + @" ");
                    PrintMatch(fileContent, m);
                    Console.WriteLine();
                }

                if (verbose || mc.Count > 0)
                {
                    PrintMatchResult(filePath, mc.Count);
                }

            }
            else
            {
                // replace mode
                fileContent = Regex.Replace(fileContent, pattern,
                    match =>
                    {
                        matchCount++;
                        var result = match.Result(replace);
                        if (verbose)
                        {
                            Console.WriteLine(@"Offset:" + match.Index.ToString(CultureInfo.InvariantCulture).PadRight(offsetColumnWidth) + @" " + match.Value + @"->" + result);
                        }
                        return result;
                    }, regexOptions
                    );

                File.WriteAllText(filePath, fileContent);

                if (verbose || matchCount > 0)
                {
                    PrintReplacementResult(filePath, matchCount);
                }

            }
        }

        private static void PrintMatch(string fileContent, Match m)
        {
            string preString = null;
            string postString = null;

            // Anfang der Zeile suchen. Max. 100 Zeichen
            var startIndexOfMatch = m.Index;
            var firstIndexAfterMatch = (m.Index + m.Length);

            var indexOfNextNewLine = GetIndexOfNextNewlineOrEofIndex(fileContent, m);
            var indexOfPreString = GetStartIndexOfPreString(m, fileContent);

            // PreString nach stdout schreiben
            if ((indexOfPreString != -1) && indexOfPreString < startIndexOfMatch)
            {
                preString = fileContent.Substring(indexOfPreString, (m.Index - indexOfPreString));
                Console.Write(preString);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(m.Value);
            Console.ResetColor();

            // Wenn es keinen Poststring gibt Abbrechen,
            // Ansonsten den Poststring nach stdout schreiben.
            if (indexOfNextNewLine <= firstIndexAfterMatch) return;
            postString = fileContent.Substring(firstIndexAfterMatch, (indexOfNextNewLine - firstIndexAfterMatch));
            Console.Write(postString);
        }

        private static int GetIndexOfNextNewlineOrEofIndex(string fileContent, Match m)
        {
            var firstIndexAfterMatch = (m.Index + m.Length);
            var indexOfNextNewLine = fileContent.IndexOfAny(new [] { '\n', '\r' }, firstIndexAfterMatch, Math.Min(100, fileContent.Length - firstIndexAfterMatch));

            if (indexOfNextNewLine == -1 && fileContent.Length > firstIndexAfterMatch)
            {
                indexOfNextNewLine = Math.Min(fileContent.Length, indexOfNextNewLine + 100);
            }
            return indexOfNextNewLine;
        }

        private static int GetStartIndexOfPreString(Match m, string fileContent)
        {
            var indexOfLastNewline = fileContent.LastIndexOfAny(new [] { '\n', '\r' }, m.Index, Math.Min(m.Index, 100));

            int indexOfPreString;
            if (indexOfLastNewline != -1)
            {
                indexOfPreString = ++indexOfLastNewline;
            }
            else if (indexOfLastNewline == -1 && m.Index > 0)
            {
                indexOfPreString = Math.Max(0, indexOfLastNewline - 100);
            }
            else
            {
                indexOfPreString = -1;
            }
            return indexOfPreString;
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

        private static void PrintReplacementResult(string filePath, int replacementCount)
        {
            if (replacementCount == 1)
            {
                Console.WriteLine(@"{0}: did 1 replacement", filePath);
            }
            else
            {
                Console.WriteLine(@"{0}: did {1:n0} replacements", filePath, replacementCount);
            }
        }
        #endregion
    }
}
