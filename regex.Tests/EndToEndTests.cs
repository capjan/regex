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
