using regex;

namespace regex.Tests;

public class UnifiedDiffTests
{
    [Fact]
    public void IdenticalTexts_ReturnEmptyString()
    {
        Assert.Equal(string.Empty, UnifiedDiff.Create("a.txt", "a\nb\n", "a\nb\n"));
    }

    [Fact]
    public void ChangedLine_ProducesSingleHunkWithContext()
    {
        var diff = UnifiedDiff.Create("a.txt", "one\ntwo\nthree\n", "one\nTWO\nthree\n");

        Assert.Equal(
            "--- a.txt\n+++ a.txt\n@@ -1,3 +1,3 @@\n one\n-two\n+TWO\n three\n",
            diff);
    }

    [Fact]
    public void Context_IsLimitedToThreeLines()
    {
        var oldText = string.Join("", Enumerable.Range(1, 20).Select(i => $"line{i}\n"));
        var newText = oldText.Replace("line10\n", "changed\n");

        var diff = UnifiedDiff.Create("a.txt", oldText, newText);

        Assert.Contains("@@ -7,7 +7,7 @@", diff);
        Assert.Contains(" line7\n", diff);
        Assert.DoesNotContain("line6", diff);
        Assert.DoesNotContain("line14", diff);
    }

    [Fact]
    public void DistantChanges_ProduceSeparateHunks()
    {
        var oldText = string.Join("", Enumerable.Range(1, 30).Select(i => $"line{i}\n"));
        var newText = oldText.Replace("line2\n", "a\n").Replace("line28\n", "b\n");

        var diff = UnifiedDiff.Create("a.txt", oldText, newText);

        Assert.Equal(2, diff.Split('\n').Count(l => l.StartsWith("@@")));
    }

    [Fact]
    public void NearbyChanges_AreMergedIntoOneHunk()
    {
        var oldText = string.Join("", Enumerable.Range(1, 12).Select(i => $"line{i}\n"));
        var newText = oldText.Replace("line3\n", "a\n").Replace("line8\n", "b\n");

        var diff = UnifiedDiff.Create("a.txt", oldText, newText);

        Assert.Equal(1, diff.Split('\n').Count(l => l.StartsWith("@@")));
    }

    [Fact]
    public void AddedLines_AreMarkedWithPlus()
    {
        var diff = UnifiedDiff.Create("a.txt", "a\nc\n", "a\nb\nc\n");

        Assert.Contains("@@ -1,2 +1,3 @@\n a\n+b\n c\n", diff);
    }

    [Fact]
    public void RemovedFileContent_UsesZeroCountForNewSide()
    {
        var diff = UnifiedDiff.Create("a.txt", "a\nb\n", "");

        Assert.Contains("@@ -1,2 +0,0 @@\n-a\n-b\n", diff);
    }

    [Fact]
    public void MissingTrailingNewline_IsAnnotated()
    {
        var diff = UnifiedDiff.Create("a.txt", "a\nb", "a\nB");

        Assert.Contains("-b\n\\ No newline at end of file\n+B\n\\ No newline at end of file\n", diff);
    }
}
