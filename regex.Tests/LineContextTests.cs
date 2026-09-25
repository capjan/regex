using regex;

namespace regex.Tests;

public class LineContextTests
{
    private const string Text = "first line\nsecond line\nthird line";

    [Fact]
    public void GetStartIndex_MatchAtFileStart_ReturnsZero()
    {
        Assert.Equal(0, LineContext.GetStartIndex(Text, 0));
    }

    [Fact]
    public void GetStartIndex_MatchInLaterLine_ReturnsLineStart()
    {
        var index = Text.IndexOf("line", Text.IndexOf("second", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Equal(Text.IndexOf("second", StringComparison.Ordinal), LineContext.GetStartIndex(Text, index));
    }

    [Fact]
    public void GetStartIndex_LongLine_IsLimitedToHundredCharacters()
    {
        var content = new string('a', 500) + "MATCH";
        Assert.Equal(400, LineContext.GetStartIndex(content, 500));
    }

    [Fact]
    public void GetStartIndex_ShortLineWithoutBreak_StartsAtBeginning()
    {
        Assert.Equal(0, LineContext.GetStartIndex("abcMATCH", 3));
    }

    [Fact]
    public void GetStartIndex_MatchAtEndOfFile_DoesNotThrow()
    {
        Assert.Equal(0, LineContext.GetStartIndex("abc", 3));
    }

    [Fact]
    public void GetEndIndex_MatchInFirstLine_ReturnsLineEnd()
    {
        var matchEnd = "first".Length;
        Assert.Equal(Text.IndexOf('\n'), LineContext.GetEndIndex(Text, matchEnd));
    }

    [Fact]
    public void GetEndIndex_MatchAtFileEnd_ReturnsLength()
    {
        Assert.Equal(Text.Length, LineContext.GetEndIndex(Text, Text.Length));
    }

    [Fact]
    public void GetEndIndex_LongLine_IsLimitedToHundredCharacters()
    {
        var content = "MATCH" + new string('a', 500);
        Assert.Equal(105, LineContext.GetEndIndex(content, 5));
    }

    [Fact]
    public void GetEndIndex_ShortTailWithoutBreak_ReturnsLength()
    {
        var content = "MATCHabc";
        Assert.Equal(content.Length, LineContext.GetEndIndex(content, 5));
    }
}
