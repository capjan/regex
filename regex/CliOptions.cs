using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Core.Parser.Arguments;

namespace regex
{
    public class CliOptions
    {

        public CliOptions(IEnumerable<string> args)
        {
            _programName = Assembly.GetExecutingAssembly().GetName().Name;
            _options = new OptionSet
            {
                { "R|replace=", "replacement Pattern (regex)", v => Replace = v },
                { "c|case-sensitive", "enables case-sensitive behavior - btw. disables the by default enabled ignore-case option", v => CaseSensitive = (v != null)},
                { "f|filter=", "wildcard based file filter (default *.*)\ne.g. *.txt", v => Filter = v},
                { "r|recursive", "progress all subdirectories", v => Recursive = (v != null)},
                { "offset-width=", "output-formatting:\nset the count of characters used for the offset column (default 6)", v => OffsetColumnWidth = int.Parse(v)},
                { "o|only-matching", "Prints only the match", v => OnlyMatching = v != null },
                { "v|verbose" , "show additional information", v => Verbose = (v != null)},
                { "V|version" , "show version information", v => ShowVersion = (v != null)},
                { "h|help", "shows this help", v => ShowHelp = (v != null) }
            };

            RegExOptions = CreateRegexOptions();
            Extra = _options.Parse(args).ToArray();
        }


        public int OffsetColumnWidth { get; private set; } = 6;
        public string Replace { get; private set; }
        public bool ShowHelp { get; private set; }
        public bool Verbose { get; private set; }
        public bool CaseSensitive { get; private set; }
        public bool Recursive { get; private set; }

        public bool OnlyMatching { get; private set; }
        public string Filter { get; private set; } = "*.*";
        public bool ShowVersion { get; private set; }
        public RegexOptions RegExOptions { get; private set; }
        public string[] Extra { get; private set; }

        public void WriteHelp(TextWriter writer)
        {
            writer.WriteLine(@"Usage:");
            writer.WriteLine();
            writer.Write(@"  ");
            WriteUsage(writer);
            writer.WriteLine();
            writer.WriteLine(@"Arguments:");
            writer.WriteLine(@"  pattern           The search pattern as .NET Regular Expression (RegEx).");
            writer.WriteLine(@"  file              File to operate.");
            writer.WriteLine(@"  directory         Directory to operate. (Must end with a directory separator)");
            writer.WriteLine();
            writer.WriteLine(@"Options:");
            _options.WriteOptionDescriptions(writer);
            writer.WriteLine();
            writer.WriteLine(@"Description:");
            writer.WriteLine(@"  " + _programName + @" is a CLI frontend for the .NET Regular Expression Engine.");
            writer.WriteLine(@"  It searches for a given search pattern in the file contents");
            writer.WriteLine(@"  in every given input file.");
            writer.WriteLine(@"  Optionally you can set a replace pattern that will be applied");
            writer.WriteLine(@"  on every match.");
            writer.WriteLine();
            writer.WriteLine(@"Examples:");
            writer.WriteLine(@"  1. Named groups:");
            writer.WriteLine();
            writer.WriteLine(@"    " + _programName + @" ""Name:(?<name>[A-Za-z]+)"" --replace ""id=${name}"" names.txt");
            writer.WriteLine();
            writer.WriteLine(@"  2. Find 'Hello' in all *.txt files in this folder and all subfolders");
            writer.WriteLine();
            writer.WriteLine(@"    " + _programName + @" --recursive --filter *.txt Hello ./");
        }

        public void WriteMinimalHelp(TextWriter writer)
        {

            writer.Write(@"Usage: ");
            WriteUsage(writer);
            writer.WriteLine(@"Type '" + _programName + @" --help' for more information.");
        }

        public void PrintVersionInformation()
        {
            var version      = Assembly.GetExecutingAssembly().GetName().Version;

            var copyright    = "";
            var copyrightAttributes =
                Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);
            if (copyrightAttributes.Length > 0)
            {
                copyright = ((AssemblyCopyrightAttribute)copyrightAttributes[0]).Copyright;
            }
            var companyName = "";
            var companyAttributes =
                Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), true);
            if (copyrightAttributes.Length > 0)
            {
                companyName = ((AssemblyCompanyAttribute)companyAttributes[0]).Company;
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(companyName + @" " + _programName + @" Version " + version);
            Console.ResetColor();
            Console.WriteLine(copyright);
        }

        private void WriteUsage(TextWriter writer)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            writer.Write(_programName);
            Console.ResetColor();
            writer.WriteLine(@" [option]... pattern (file|directory)...");
        }

        private readonly OptionSet _options;
        private readonly string _programName;


        private RegexOptions CreateRegexOptions()
        {
            var regexOptions = RegexOptions.None;

            if (!CaseSensitive)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }
            return regexOptions;
        }
    }
}
