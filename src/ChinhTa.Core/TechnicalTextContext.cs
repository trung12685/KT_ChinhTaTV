using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// Whitespace and repeated words can be significant inside paths, addresses and
// literal/code fragments. This is a conservative paragraph scope, not a parser:
// recognize explicit technical forms without treating every prose slash as code.
public static class TechnicalTextContext
{
    private const RegexOptions Options=RegexOptions.CultureInvariant|RegexOptions.IgnoreCase|RegexOptions.NonBacktracking;
    private static readonly Regex Paths=new(
        "(?:^|[\\s\"'“‘(\\[])(?:[a-z]:[\\\\/]|\\\\\\\\[^\\s\\\\/]+[\\\\/]|\\.{1,2}[\\\\/]|~[\\\\/]|/[\\p{L}\\p{M}\\p{N}_.-][\\p{L}\\p{M}\\p{N}_. -]*/)",Options);
    private static readonly Regex Schemes=new(
        @"\b(?:[a-z][a-z0-9+.-]{1,31}://|mailto:|file:|urn:)",Options);
    private static readonly Regex Email=new(
        @"[^\s@]+@[\p{L}\p{N}][\p{L}\p{N}.-]*\.[\p{L}]{2,}",Options);
    private static readonly Regex Assignment=new(
        @"(?:^|[\s({;])[\p{L}_][\p{L}\p{N}_.-]*\s*(?:=|:=|=>)",Options);
    private static readonly Regex MarkupOrObject=new(
        "(?:<[a-z_][a-z0-9_.:-]*(?:[\\s/>])|[\\{,]\\s*\"[^\"\\r\\n]+\"\\s*:)",Options);
    private static readonly Regex FilenameOrDomain=new(
        @"[\p{L}\p{N}_.-]\.(?:docx?|xlsx?|pptx?|pdf|txt|csv|json|xml|ya?ml|ini|cfg|log|zip|rar|7z|exe|dll|png|jpe?g)(?:$|[^\p{L}\p{N}_])|[a-z0-9][a-z0-9.-]*\.(?:vn|com|net|org|io|dev|gov|edu)/",Options);

    public static bool IsTechnical(string text)=>text.Contains('`') || text.Contains('\\') || Paths.IsMatch(text) || Schemes.IsMatch(text) ||
        Email.IsMatch(text) || Assignment.IsMatch(text) || MarkupOrObject.IsMatch(text) || FilenameOrDomain.IsMatch(text);
}
