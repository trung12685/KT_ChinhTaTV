using System.Text;
using System.Text.Json;
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
    private const string Identifier=@"[\p{L}_][\p{L}\p{M}\p{N}_]*";
    private const string QualifiedIdentifier=Identifier+@"(?:\."+Identifier+@")*";
    // Literal markers belong to the argument, not prose quotation prefixes.
    // Accept C# verbatim/interpolated and the established Python prefixes only
    // after an adjacent call '('. Earlier arguments must be simple identifiers
    // or decimal numbers followed by commas; nested expressions are not parsed.
    private const string LiteralPrefix=@"(?:@\$?|\$@?|br|rb|fr|rf|r|f|b|u)?";
    // Spaces before '(' are ordinary prose too, so they grant no call scope.
    private static readonly Regex QuotedFunctionArgument=new(
        @"(?:^|[^\p{L}\p{M}\p{N}_])"+QualifiedIdentifier+@"\([ \t]*(?:(?:"+QualifiedIdentifier+
        @"|[+-]?[0-9]+(?:\.[0-9]+)?)[ \t]*,[ \t]*)*"+LiteralPrefix+"[\"']",Options);
    // Explicit long-option syntax owns its quoted value even when the command
    // itself is unfamiliar. A spaced prose dash or an unquoted option does not.
    private static readonly Regex QuotedLongOption=new(
        """(?:^|[ \t])--[a-z][a-z0-9_-]*(?:[ \t]+|=)["']""",Options);
    // These known output commands use quoted data without call parentheses.
    // Keep the recognition anchored; arbitrary prose followed by a quotation
    // or a single-dash word does not establish a shell command.
    private static readonly Regex QuotedOutputCommand=new(
        """^[ \t]*(?:cmd(?:\.exe)?[ \t]+/c[ \t]+)?(?:echo|printf|Write-Output|Write-Host)[ \t]+["']""",Options);
    private static readonly Regex FilenameOrDomain=new(
        @"[\p{L}\p{M}\p{N}_.-]\.(?:docx?|docm|dotx?|dotm|rtf|odt|ott|xlsx?|xlsm|xlsb|xltx?|xltm|ods|ots|pptx?|pptm|potx?|potm|ppsx?|ppsm|odp|otp|pdf|txt|csv|json|xml|ya?ml|ini|cfg|log|zip|rar|7z|exe|dll|png|jpe?g)(?:$|[^\p{L}\p{M}\p{N}_])|[a-z0-9][a-z0-9.-]*\.(?:vn|com|net|org|io|dev|gov|edu)/",Options);

    private static bool IsJsonArray(string text)
    {
        int start=0,end=text.Length;
        while(start<end && text[start] is ' ' or '\t' or '\r' or '\n')start++;
        while(end>start && text[end-1] is ' ' or '\t' or '\r' or '\n' or '\a')end--;
        // Only a complete array-shaped paragraph establishes this scope; a
        // quoted bracket embedded in prose remains outside this recognition.
        if(end-start<2 || text[start]!='[' || text[end-1]!=']')return false;
        byte[] bytes=Encoding.UTF8.GetBytes(text[start..end]);
        const int depthBudget=64;
        var reader=new Utf8JsonReader(bytes,new JsonReaderOptions {MaxDepth=depthBudget+1});
        try
        {
            while(reader.Read())
                // A valid JSON prefix this deep is already technical. Do not
                // let a parser-depth limit restore permission to edit values;
                // the unparsed remainder is conservatively manual.
                if(reader.CurrentDepth>=depthBudget && reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)return true;
            return reader.TokenType==JsonTokenType.EndArray && reader.BytesConsumed==bytes.Length;
        }
        catch(JsonException){return false;}
    }

    public static bool IsTechnical(string text)=>text.Contains('`') || text.Contains('\\') || Paths.IsMatch(text) || RootFilename.IsMatch(text) || Schemes.IsMatch(text) ||
        Email.IsMatch(text) || Assignment.IsMatch(text) || MarkupOrObject.IsMatch(text) || QuotedFunctionArgument.IsMatch(text) ||
        QuotedLongOption.IsMatch(text) || QuotedOutputCommand.IsMatch(text) || FilenameOrDomain.IsMatch(text) || IsJsonArray(text);
}
