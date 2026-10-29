using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public static class SpacingRules
{
    private static readonly Regex BlankNumber=new(@"^[ \t]*Số[ \t]*:(?<blank>[ \t]{2,})(?=/[\p{L}\p{N}])",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    private static readonly Regex BlankDay=new(@"^[ \t]*(?:[\p{L}\p{M}][\p{L}\p{M} .’'\-]*,[ \t]*)?ngày(?<blank>[ \t]{2,})tháng[ \t]+(?:\d{1,2}[ \t]+)?năm[ \t]+\d{4}[ .\t\r\a]*$",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    // A labelled date form, not an arbitrary slash in prose. Keep only fields
    // consisting entirely of spaces/tabs; a filled field still gets its usual
    // spacing review. Match both spellings of the accent without normalizing
    // the source, so all captured offsets remain Word/string offsets.
    private static readonly Regex SlashDateFields=new(@"^[ \t]*(?:ng(?:à|a\u0300)y(?:[ \t]+sinh)?|sinh[ \t]+ng(?:à|a\u0300)y)[ \t]*:(?<day>[0-9 \t]*)/(?<month>[0-9 \t]*)/(?<year>[0-9 \t]*)(?=$|[\r\n\a(,;.])",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    public static bool IsBlankField(string text,int start,int length,bool identifiedAdministrativeHeader=false)
    {
        // Empty delimiters may be a form/checkbox field, never collapse their width.
        if(length>0 && start>0 && (long)start+length<text.Length && (text[start-1]=='(' && text[start+length]==')' || text[start-1]=='[' && text[start+length]==']' || text[start-1]=='“' && text[start+length]=='”' || text[start-1]=='"' && text[start+length]=='"') &&
            text.AsSpan(start,length).IndexOfAnyExcept(' ')<0)return true;
        if(identifiedAdministrativeHeader && AdministrativeHeaderRules.IsBlankTemplateSpacing(text,start,length))return true;
        foreach(var pattern in new[]{BlankNumber,BlankDay})
        {
            var match=pattern.Match(text);
            if(!match.Success)continue;
            var blank=match.Groups["blank"];
            if(start>=blank.Index && (long)start+length<=blank.Index+blank.Length)return true;
        }
        var date=SlashDateFields.Match(text);
        if(date.Success && length>0)
            foreach(string name in new[]{"day","month","year"})
            {
                var field=date.Groups[name];
                if(field.Length>0 && field.Value.All(c=>c is ' ' or '\t') && start>=field.Index &&
                    (long)start+length<=field.Index+field.Length)return true;
            }
        return false;
    }
}
