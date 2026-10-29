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
    // A root-level filename without an extension needs an explicit boundary or
    // path introduction. A slash after arbitrary whitespace can be the blank
    // document-number field "Số: /BC-ABCD", so it grants no technical scope.
    private static readonly Regex RootFilename=new(
        """(?:^[ \t]*|["'“‘(\[]|(?:^|[\s"'“‘(\[])(?:mở|tệp|file|đường[ \t]+dẫn)(?:[ \t]*:)?[ \t]+)/[\p{L}\p{M}\p{N}_.-][\p{L}\p{M}\p{N}_. -]*(?:$|["'”’)\]\r\n\a\v\f,;!?])""",Options);
    private static readonly Regex Schemes=new(
        @"\b(?:[a-z][a-z0-9+.-]{1,31}://|mailto:|file:|urn:)",Options);
    private static readonly Regex Email=new(
        @"[^\s@]+@[\p{L}\p{N}][\p{L}\p{M}\p{N}.-]*\.[\p{L}]\p{M}*(?:[\p{L}]\p{M}*)+",Options);
    private static readonly Regex Assignment=new(
        @"(?:^|[\s({;])[\p{L}_][\p{L}\p{N}_.-]*\s*(?:=|:=|=>)",Options);
    private static readonly Regex MarkupOrObject=new(
        "(?:<[a-z_][a-z0-9_.:-]*(?:[\\s/>])|[\\{,]\\s*\"[^\"\\r\\n]+\"\\s*:)",Options);
    // Explicit quoted first argument, with '(' adjacent to the identifier.
    // Spaces before '(' are ordinary prose too, so this is deliberately not a
    // parser for every call syntax or programming language.
    private static readonly Regex QuotedFunctionArgument=new(
        """(?:^|[^\p{L}\p{M}\p{N}_])(?:[\p{L}_][\p{L}\p{M}\p{N}_]*\.)*[\p{L}_][\p{L}\p{M}\p{N}_]*\([ \t]*["']""",Options);
    private static readonly Regex FilenameOrDomain=new(
        @"[\p{L}\p{M}\p{N}_.-]\.(?:docx?|docm|dotx?|dotm|rtf|odt|ott|xlsx?|xlsm|xlsb|xltx?|xltm|ods|ots|pptx?|pptm|potx?|potm|ppsx?|ppsm|odp|otp|pdf|txt|csv|json|xml|ya?ml|ini|cfg|log|zip|rar|7z|exe|dll|png|jpe?g)(?:$|[^\p{L}\p{M}\p{N}_])|[a-z0-9][a-z0-9.-]*\.(?:vn|com|net|org|io|dev|gov|edu)/",Options);

    public static bool IsTechnical(string text)=>text.Contains('`') || text.Contains('\\') || Paths.IsMatch(text) || RootFilename.IsMatch(text) || Schemes.IsMatch(text) ||
        Email.IsMatch(text) || Assignment.IsMatch(text) || MarkupOrObject.IsMatch(text) || QuotedFunctionArgument.IsMatch(text) || FilenameOrDomain.IsMatch(text);
}
