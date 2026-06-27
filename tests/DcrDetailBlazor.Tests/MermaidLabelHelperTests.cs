using DcrDetailBlazor.Models;
using Xunit;

namespace DcrDetailBlazor.Tests;

public class MermaidLabelHelperTests
{
    [Theory]
    [InlineData("Custom-MyTable_CL", "Custom-MyTable_CL")]
    [InlineData("Microsoft-WindowsEvent", "Microsoft-WindowsEvent")]
    [InlineData("Security Events (via AMA)", "Security Events (via AMA)")]
    [InlineData("dcr-app.prod", "dcr-app.prod")]
    public void EscapeLabel_LeavesLegitimateNamesReadable(string input, string expected)
    {
        Assert.Equal(expected, MermaidLabelHelper.EscapeLabel(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public void EscapeLabel_ReturnsPlaceholderForEmptyOrWhitespace(string? input)
    {
        Assert.Equal(MermaidLabelHelper.Placeholder, MermaidLabelHelper.EscapeLabel(input));
    }

    [Fact]
    public void EscapeLabel_NeutralizesDoubleQuoteBreakout()
    {
        // The double quote is the only character that can terminate the quoted
        // Mermaid label wrapper (id["LABEL"]). It must never survive.
        var result = MermaidLabelHelper.EscapeLabel("\"], attacker[\"pwned");

        Assert.DoesNotContain('"', result);
        Assert.DoesNotContain('[', result);
        Assert.DoesNotContain(']', result);
    }

    [Fact]
    public void EscapeLabel_StripsAngleBracketMarkup()
    {
        var result = MermaidLabelHelper.EscapeLabel("<script>alert(1)</script>");

        Assert.DoesNotContain('<', result);
        Assert.DoesNotContain('>', result);
        // Inner text remains, but inert.
        Assert.Contains("script", result);
        Assert.Contains("alert(1)", result);
    }

    [Fact]
    public void EscapeLabel_StripsMermaidNodeShapeDelimiters()
    {
        var result = MermaidLabelHelper.EscapeLabel("a{b}c[d]e");

        Assert.DoesNotContain('{', result);
        Assert.DoesNotContain('}', result);
        Assert.DoesNotContain('[', result);
        Assert.DoesNotContain(']', result);
        Assert.Equal("abcde", result);
    }

    [Fact]
    public void EscapeLabel_RemovesBacktick()
    {
        var result = MermaidLabelHelper.EscapeLabel("name`with`ticks");

        Assert.DoesNotContain('`', result);
    }

    [Fact]
    public void EscapeLabel_CollapsesNewlinesAndControlCharsToSingleSpace()
    {
        var result = MermaidLabelHelper.EscapeLabel("line1\r\n\tline2");

        Assert.DoesNotContain('\r', result);
        Assert.DoesNotContain('\n', result);
        Assert.DoesNotContain('\t', result);
        Assert.Equal("line1 line2", result);
    }

    [Fact]
    public void EscapeLabel_TruncatesOverlongInput()
    {
        var input = new string('a', MermaidLabelHelper.MaxLength + 50);

        var result = MermaidLabelHelper.EscapeLabel(input);

        Assert.True(result.Length <= MermaidLabelHelper.MaxLength + 1); // +1 for the ellipsis
        Assert.EndsWith("…", result);
    }

    [Fact]
    public void EscapeLabel_DoesNotTruncateInputAtLimit()
    {
        var input = new string('a', MermaidLabelHelper.MaxLength);

        var result = MermaidLabelHelper.EscapeLabel(input);

        Assert.Equal(input, result);
        Assert.DoesNotContain('…', result);
    }

    [Fact]
    public void EscapeLabel_ReturnsPlaceholderWhenInputIsEntirelyStripped()
    {
        var result = MermaidLabelHelper.EscapeLabel("<<>>[]{}");

        Assert.Equal(MermaidLabelHelper.Placeholder, result);
    }
}
