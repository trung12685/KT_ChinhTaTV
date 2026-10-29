using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// A boolean exclusion for prose rules, not an administrative date parser.
// Match the original NFC/NFD labels without normalizing text or translating CPs.
internal static class DateWordContext
{
    private static readonly Regex Words=new(@"\bng(?:à|a\u0300)y\b[^\r\n\a]*\bth(?:á|a\u0301)ng\b[^\r\n\a]*\bn(?:ă|a\u0306)m\b",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    internal static bool Contains(string text)=>Words.IsMatch(text);
}
