using System.Text;

namespace DcrDetailBlazor.Models;

/// <summary>
/// Sanitizes free-form, attacker-influenced text (e.g. Azure resource names,
/// connector display values, stream/table names) before it is concatenated into
/// a Mermaid diagram definition.
///
/// Mermaid node labels are emitted as <c>id["LABEL"]:::class</c>. The single most
/// important defense is neutralizing the double quote, which is the only character
/// that can break out of the quoted label and inject diagram structure. Angle
/// brackets, backticks and Mermaid shape delimiters are additionally removed as
/// defense-in-depth so the label stays safe even if the renderer configuration
/// (securityLevel / htmlLabels) regresses.
///
/// Legitimate Azure names — <c>Custom-MyTable_CL</c>, <c>Microsoft-WindowsEvent</c>,
/// <c>Security Events (via AMA)</c> — pass through unchanged and human-readable.
/// </summary>
public static class MermaidLabelHelper
{
    /// <summary>Fallback shown for null / empty / fully-stripped input.</summary>
    public const string Placeholder = "—";

    /// <summary>Defensive cap on label length; longer values are truncated with an ellipsis.</summary>
    public const int MaxLength = 120;

    // Characters removed entirely: markup angle brackets and Mermaid node-shape
    // delimiters. None of these appear in valid Azure resource names, so removing
    // them never harms readability but eliminates structure-injection vectors.
    private static readonly char[] StrippedChars = { '<', '>', '[', ']', '{', '}' };

    /// <summary>
    /// Returns a Mermaid-safe, human-readable version of <paramref name="value"/>.
    /// </summary>
    public static string EscapeLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Placeholder;
        }

        var builder = new StringBuilder(value.Length);
        var lastWasSpace = false;

        foreach (var ch in value)
        {
            // Collapse all whitespace (including CR/LF/TAB) to a single space so
            // labels stay on one line and cannot smuggle in control characters.
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                    lastWasSpace = true;
                }
                continue;
            }

            // Drop other non-printable control characters outright.
            if (char.IsControl(ch))
            {
                continue;
            }

            // Strip markup / Mermaid-structure delimiters.
            if (Array.IndexOf(StrippedChars, ch) >= 0)
            {
                continue;
            }

            // Neutralize breakout characters by downgrading them to a single quote.
            // The double quote is the critical one — it terminates the label wrapper.
            // The backtick is removed of its HTML/markdown significance the same way.
            var safe = ch switch
            {
                '"' => '\'',
                '`' => '\'',
                _ => ch
            };

            builder.Append(safe);
            lastWasSpace = false;
        }

        var result = builder.ToString().Trim();

        if (result.Length == 0)
        {
            return Placeholder;
        }

        if (result.Length > MaxLength)
        {
            result = result[..MaxLength].TrimEnd() + "…";
        }

        return result;
    }
}
