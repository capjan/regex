using System.Diagnostics;

namespace regex.Tests;

/// <summary>
/// Startet das Tool als eigenen Prozess gegen temporäre Dateien und prüft Ausgabe und Exit-Code.
/// </summary>
public sealed class EndToEndTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("regex-tests-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string WriteFile(string name, string content)
    {
        var path = Path.Combine(_dir, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private static (int ExitCode, string Stdout, string Stderr) Run(params string[] args)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "regex.dll"));
        foreach (var arg in args) startInfo.ArgumentList.Add(arg);

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, stdout.Result, stderr.Result);
    }

    [Fact]
    public void Search_IgnoresCaseByDefault()
    {
        var file = WriteFile("a.txt", "Hello world\nhello again\nbye\n");

        var (exitCode, stdout, _) = Run("hello", file);

        Assert.Equal(0, exitCode);
        Assert.Contains("Hello world", stdout);
        Assert.Contains("hello again", stdout);
        Assert.Contains("found 2 matches", stdout);
    }

    [Fact]
    public void Search_CaseSensitive_OnlyFindsExactCase()
    {
        var file = WriteFile("a.txt", "Hello world\nhello again\n");

        var (_, stdout, _) = Run("-c", "hello", file);

        Assert.DoesNotContain("Hello world", stdout);
        Assert.Contains("hello again", stdout);
        Assert.Contains("found 1 match", stdout);
    }

    [Fact]
    public void Search_OnlyMatching_PrintsJustTheMatches()
    {
        var file = WriteFile("a.txt", "foo123 bar\nbaz45\n");

        var (_, stdout, _) = Run("-o", @"\d+", file);

        Assert.Equal(["123", "45"], stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()));
    }

    [Fact]
    public void Search_MaxCount_LimitsPrintedMatches()
    {
        var file = WriteFile("a.txt", "x x x x x\n");

        var (_, stdout, _) = Run("-m", "2", "x", file);

        Assert.Contains("found 2 matches", stdout);
    }

    [Fact]
    public void Search_LongLine_ContextIsLimited()
    {
        var file = WriteFile("a.txt", new string('a', 300) + "NEEDLE" + new string('b', 300) + "\n");

        var (_, stdout, _) = Run("NEEDLE", file);

        var line = stdout.Split('\n')[0];
        Assert.Contains(new string('a', 100) + "NEEDLE" + new string('b', 100), line);
        Assert.DoesNotContain(new string('a', 101) + "NEEDLE", line);
        Assert.DoesNotContain("NEEDLE" + new string('b', 101), line);
    }

    [Fact]
    public void Search_DirectoryRecursive_UsesFilter()
    {
        WriteFile("top.txt", "Hello\n");
        WriteFile("sub/nested.txt", "Hello\n");
        WriteFile("sub/other.md", "Hello\n");

        var (exitCode, stdout, _) = Run("-r", "-f", "*.txt", "Hello", _dir + Path.DirectorySeparatorChar);

        Assert.Equal(0, exitCode);
        Assert.Contains("top.txt", stdout);
        Assert.Contains("nested.txt", stdout);
        Assert.DoesNotContain("other.md", stdout);
    }

    [Fact]
    public void Replace_WithNamedGroup_RewritesFile()
    {
        var file = WriteFile("names.txt", "Name:Bob and Name:Ann\n");

        var (exitCode, stdout, _) = Run("-R", "id=${name}", "Name:(?<name>[A-Za-z]+)", file);

        Assert.Equal(0, exitCode);
        Assert.Contains("did 2 replacements", stdout);
        Assert.Equal("id=Bob and id=Ann\n", File.ReadAllText(file));
    }

    [Fact]
    public void Replace_WithoutMatches_LeavesFileUntouched()
    {
        var file = WriteFile("a.txt", "nothing here\n");
        var before = File.GetLastWriteTimeUtc(file);
        Thread.Sleep(50);

        var (exitCode, _, _) = Run("-R", "x", "absent", file);

        Assert.Equal(0, exitCode);
        Assert.Equal(before, File.GetLastWriteTimeUtc(file));
    }

    [Fact]
    public void Replace_DryRun_ReportsButDoesNotWrite()
    {
        var file = WriteFile("names.txt", "Name:Bob and Name:Ann\n");

        var (exitCode, stdout, _) = Run("--dry-run", "-R", "id=${name}", "Name:(?<name>[A-Za-z]+)", file);

        Assert.Equal(0, exitCode);
        Assert.Contains("would do 2 replacements", stdout);
        Assert.DoesNotContain("did 2", stdout);
        Assert.Equal("Name:Bob and Name:Ann\n", File.ReadAllText(file));
    }

    [Fact]
    public void Replace_Diff_PrintsUnifiedDiffAndDoesNotWrite()
    {
        var file = WriteFile("names.txt", "first\nName:Bob\nlast\n");

        var (exitCode, stdout, _) = Run("--diff", "-R", "id=${name}", "Name:(?<name>[A-Za-z]+)", file);

        Assert.Equal(0, exitCode);
        Assert.Equal(
            $"--- {file}\n+++ {file}\n@@ -1,3 +1,3 @@\n first\n-Name:Bob\n+id=Bob\n last\n",
            stdout.ReplaceLineEndings("\n"));
        Assert.Equal("first\nName:Bob\nlast\n", File.ReadAllText(file));
    }

    [Fact]
    public void Replace_Diff_WithoutMatches_PrintsNothing()
    {
        var file = WriteFile("a.txt", "nothing here\n");

        var (exitCode, stdout, _) = Run("--diff", "-R", "x", "absent", file);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stdout);
    }

    [Fact]
    public void Replace_Diff_WithUnchangedResult_PrintsNothingAndSucceeds()
    {
        var file = WriteFile("a.txt", "foo\n");

        var (exitCode, stdout, stderr) = Run("--diff", "-R", "foo", "foo", file);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Equal(string.Empty, stderr);
    }

    [Fact]
    public void Replace_DiffVerbose_KeepsStdoutAValidPatch()
    {
        var file = WriteFile("a.txt", "foo\n");

        var (exitCode, stdout, stderr) = Run("--diff", "-v", "-R", "bar", "foo", file);

        Assert.Equal(0, exitCode);
        Assert.StartsWith("--- ", stdout);
        Assert.DoesNotContain("Progressing", stdout);
        Assert.DoesNotContain("->", stdout);
        Assert.Contains("Progressing", stderr);
    }

    [Fact]
    public void Replace_Diff_PreservesCarriageReturns()
    {
        var file = WriteFile("crlf.txt", "a\r\nb\r\n");

        var (_, stdout, _) = Run("--diff", "-R", "x", "a", file);

        Assert.Contains("-a\r\n", stdout);
        Assert.Contains("+x\r\n", stdout);
        Assert.Contains(" b\r\n", stdout);
    }

    [Fact]
    public void Replace_DryRunAndDiff_PrintOnlyTheDiffAndDoNotWrite()
    {
        var file = WriteFile("a.txt", "foo\n");

        var (exitCode, stdout, _) = Run("-n", "-d", "-R", "bar", "foo", file);

        Assert.Equal(0, exitCode);
        Assert.StartsWith("--- ", stdout);
        Assert.DoesNotContain("would do", stdout);
        Assert.Equal("foo\n", File.ReadAllText(file));
    }

    [Fact]
    public void Replace_Diff_MultipleFiles_PrintsOneDiffPerFile()
    {
        var first = WriteFile("a.txt", "foo\n");
        var second = WriteFile("b.txt", "foo\n");

        var (exitCode, stdout, _) = Run("--diff", "-R", "bar", "foo", first, second);

        Assert.Equal(0, exitCode);
        var text = stdout.ReplaceLineEndings("\n");
        Assert.Equal(
            $"--- {first}\n+++ {first}\n@@ -1,1 +1,1 @@\n-foo\n+bar\n" +
            $"--- {second}\n+++ {second}\n@@ -1,1 +1,1 @@\n-foo\n+bar\n",
            text);
    }

    [Fact]
    public void Replace_Diff_FileWithoutTrailingNewline_IsAnnotated()
    {
        var file = WriteFile("a.txt", "foo");

        var (exitCode, stdout, _) = Run("--diff", "-R", "bar", "foo", file);

        Assert.Equal(0, exitCode);
        Assert.EndsWith("-foo\n\\ No newline at end of file\n+bar\n\\ No newline at end of file\n",
            stdout.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData("--dry-run")]
    [InlineData("--diff")]
    public void DryRunOrDiff_WithoutReplace_FailsWithExitCodeOne(string flag)
    {
        var file = WriteFile("a.txt", "x\n");

        var (exitCode, _, stderr) = Run(flag, "x", file);

        Assert.Equal(1, exitCode);
        Assert.Contains("require --replace", stderr);
    }

    [Fact]
    public void MissingFile_IsReportedOnStderr()
    {
        var (_, stdout, stderr) = Run("hello", Path.Combine(_dir, "missing.txt"));

        Assert.Contains("File not found error", stderr);
        Assert.DoesNotContain("File not found error", stdout);
    }

    [Fact]
    public void MissingPattern_FailsWithExitCodeOne()
    {
        var (exitCode, _, stderr) = Run();

        Assert.Equal(1, exitCode);
        Assert.Contains("Missing regular expression pattern", stderr);
    }

    [Fact]
    public void InvalidPattern_FailsWithExitCodeOne()
    {
        var file = WriteFile("a.txt", "x\n");

        var (exitCode, _, stderr) = Run("(", file);

        Assert.Equal(1, exitCode);
        Assert.Contains("Invalid pattern", stderr);
    }

    [Theory]
    [InlineData("--max-count")]
    [InlineData("-m")]
    [InlineData("--offset-width")]
    public void NonNumericIntegerOption_FailsWithReadableMessageOnStderr(string option)
    {
        var file = WriteFile("a.txt", "x\n");

        var (exitCode, stdout, stderr) = Run(option, "abc", "x", file);

        Assert.Equal(1, exitCode);
        Assert.Contains("requires a whole number, but got 'abc'", stderr);
        Assert.DoesNotContain("System.Nullable", stderr);
        Assert.DoesNotContain("found", stdout);
    }

    [Fact]
    public void OffsetWidth_ControlsTheOffsetColumn()
    {
        var file = WriteFile("a.txt", "x\n");

        var (exitCode, stdout, _) = Run("--offset-width", "2", "x", file);

        Assert.Equal(0, exitCode);
        Assert.StartsWith("Offset:0  x", stdout);
    }

    [Fact]
    public void Version_PrintsProgramNameAndVersion()
    {
        var (exitCode, stdout, _) = Run("-V");

        Assert.Equal(0, exitCode);
        Assert.Contains("regex Version", stdout);
    }
}
