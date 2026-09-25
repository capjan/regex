using System.CommandLine;
using System.Text.RegularExpressions;
using regex;

namespace regex.Tests;

public class CliDefinitionTests
{
    private static (CliOptions? Options, int ExitCode) Parse(params string[] args)
    {
        CliOptions? captured = null;
        var root = CliDefinition.Create(o =>
        {
            captured = o;
            return 0;
        });

        var config = new InvocationConfiguration { Output = TextWriter.Null, Error = TextWriter.Null };
        var exitCode = root.Parse(args).Invoke(config);
        return (captured, exitCode);
    }

    [Fact]
    public void Defaults_AreIgnoreCaseAndAllFiles()
    {
        var (options, exitCode) = Parse("hello", "a.txt");

        Assert.Equal(0, exitCode);
        Assert.NotNull(options);
        Assert.Equal("hello", options.Pattern);
        Assert.Equal(["a.txt"], options.Paths);
        Assert.Equal("*.*", options.Filter);
        Assert.Equal(6, options.OffsetColumnWidth);
        Assert.Equal(int.MaxValue, options.MaxMatchesCount);
        Assert.False(options.CaseSensitive);
        Assert.Equal(RegexOptions.IgnoreCase, options.RegExOptions);
        Assert.Null(options.Replace);
        Assert.False(options.DryRun);
        Assert.False(options.Diff);
    }

    [Theory]
    [InlineData("--dry-run", true, false)]
    [InlineData("-n", true, false)]
    [InlineData("--diff", false, true)]
    [InlineData("-d", false, true)]
    public void DryRunAndDiffFlags_AreParsed(string flag, bool dryRun, bool diff)
    {
        var (options, exitCode) = Parse(flag, "-R", "x", "hello", "a.txt");

        Assert.Equal(0, exitCode);
        Assert.NotNull(options);
        Assert.Equal(dryRun, options.DryRun);
        Assert.Equal(diff, options.Diff);
    }

    [Theory]
    [InlineData("-c")]
    [InlineData("--case-sensitive")]
    public void CaseSensitiveFlag_DisablesIgnoreCase(string flag)
    {
        var (options, _) = Parse(flag, "hello", "a.txt");

        Assert.NotNull(options);
        Assert.True(options.CaseSensitive);
        Assert.Equal(RegexOptions.None, options.RegExOptions);
    }

    [Fact]
    public void AllOptions_AreParsed()
    {
        var (options, exitCode) = Parse(
            "-R", "id=${name}", "-f", "*.txt", "-r", "-o", "-v", "-m", "3", "--offset-width", "10",
            "Name:(?<name>[A-Za-z]+)", "names.txt", "dir/");

        Assert.Equal(0, exitCode);
        Assert.NotNull(options);
        Assert.Equal("id=${name}", options.Replace);
        Assert.Equal("*.txt", options.Filter);
        Assert.True(options.Recursive);
        Assert.True(options.OnlyMatching);
        Assert.True(options.Verbose);
        Assert.Equal(3, options.MaxMatchesCount);
        Assert.Equal(10, options.OffsetColumnWidth);
        Assert.Equal("Name:(?<name>[A-Za-z]+)", options.Pattern);
        Assert.Equal(["names.txt", "dir/"], options.Paths);
    }

    [Theory]
    [InlineData("-V")]
    [InlineData("--version")]
    public void VersionFlag_IsRecognizedWithoutPattern(string flag)
    {
        var (options, exitCode) = Parse(flag);

        Assert.Equal(0, exitCode);
        Assert.NotNull(options);
        Assert.True(options.ShowVersion);
        Assert.Null(options.Pattern);
    }

    [Fact]
    public void InvalidMaxCount_IsAParseError()
    {
        var (options, exitCode) = Parse("-m", "abc", "hello", "a.txt");

        Assert.Null(options);
        Assert.NotEqual(0, exitCode);
    }
}
