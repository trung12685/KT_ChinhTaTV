using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public static class SpacingRules
{
    // Blank fields may mix Word nonbreaking spaces with ASCII padding. Recognize
    // only NBSP/narrow NBSP as additional horizontal padding; never normalize
    // the paragraph or accept line/page controls as field whitespace.
    // Match canonical label spellings directly. Normalizing the paragraph
    // would change source offsets before Word has approved any edit.
    private const string NumberLabel=@"S(?:ố|o\u0302\u0301)";
    private const string DayLabel=@"ng(?:à|a\u0300)y";
    private const string MonthLabel=@"th(?:á|a\u0301)ng";
    private const string YearLabel=@"n(?:ă|a\u0306)m";
    private static readonly Regex BlankNumber=new(@"^[ \t\u00A0\u202F]*"+NumberLabel+@"[ \t\u00A0\u202F]*:(?<blank>[._… \t\u00A0\u202F]*)(?=/[ \t\u00A0\u202F]*[\p{L}\p{N}])",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    private static readonly Regex MarkedCheckbox=new(@"\[(?<left>[ \u00A0\u202F]*)(?:[Xx✓✔])(?<right>[ \u00A0\u202F]*)\]",
        RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    // Date words establish the field shape, including a partially filled form
    // outside an identified administrative header. Only blank/placeholder slots
    // keep their padding; annotations and filled slots retain ordinary review.
    private static readonly Regex WordDateFields=new(@"^[ \t\u00A0\u202F]*(?:(?<place>[\p{L}\p{M}][\p{L}\p{M} .’'\-]*),[ \t\u00A0\u202F]*)?"+
        DayLabel+@":?(?<day>[0-9._… \t\u00A0\u202F]*)"+MonthLabel+@"(?<month>[0-9._… \t\u00A0\u202F]*)"+YearLabel+
        @"(?<year>[0-9._… \t\u00A0\u202F]*)(?=$|[\r\n\a(,;.])",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    // A labelled date form, not an arbitrary slash in prose. Capture actual
    // placeholder characters too: a period cannot stand in as a successful
    // boundary before an unparsed year. Filled slots retain ordinary review.
    // Raw captures preserve both NFC/NFD label offsets without normalization.
    private static readonly Regex SlashDateFields=new(@"^[ \t\u00A0\u202F]*(?:ng(?:à|a\u0300)y(?:[ \t\u00A0\u202F]+sinh)?|sinh[ \t\u00A0\u202F]+ng(?:à|a\u0300)y)[ \t\u00A0\u202F]*:(?<day>[0-9._… \t\u00A0\u202F]*)/(?<month>[0-9._… \t\u00A0\u202F]*)/(?<year>[0-9._… \t\u00A0\u202F]*)(?=$|[\r\n\a(,;])",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    public static bool IsBlankField(string text,int start,int length,bool identifiedAdministrativeHeader=false)
    {
        // Empty delimiters may be a form/checkbox field, never collapse their width.
        if(length>0 && start>0 && (long)start+length<text.Length && (text[start-1]=='(' && text[start+length]==')' || text[start-1]=='[' && text[start+length]==']' || text[start-1]=='“' && text[start+length]=='”' || text[start-1]=='"' && text[start+length]=='"') &&
            text.AsSpan(start,length).IndexOfAnyExcept(' ')<0)return true;
        foreach(Match checkbox in MarkedCheckbox.Matches(text))
            foreach(string name in new[]{"left","right"})
            {
                var padding=checkbox.Groups[name];
                if(length>0 && start>=padding.Index && (long)start+length<=padding.Index+padding.Length)return true;
            }
        if(identifiedAdministrativeHeader && AdministrativeHeaderRules.IsBlankTemplateSpacing(text,start,length))return true;
        var number=BlankNumber.Match(text);
        if(number.Success)
        {
            var blank=number.Groups["blank"];
            if(start>=blank.Index && (long)start+length<=blank.Index+blank.Length)return true;
        }
        var wordDate=WordDateFields.Match(text);
        // An annotated place/date row belongs to the identified-header proof
        // above. Do not extend that exception to an arbitrary body paragraph.
        bool dateScope=wordDate.Success && (!wordDate.Groups["place"].Success ||
            text[(wordDate.Index+wordDate.Length)..].All(c=>c is ' ' or '\t' or '\u00A0' or '\u202F' or '.' or '\r' or '\n' or '\a'));
        if(dateScope && length>0)
            foreach(string name in new[]{"day","month","year"})
            {
                var field=wordDate.Groups[name];
                if(field.Value.All(c=>c is ' ' or '\t' or '\u00A0' or '\u202F' or '.' or '_' or '…') && start>=field.Index &&
                    (long)start+length<=field.Index+field.Length)return true;
            }
        var date=SlashDateFields.Match(text);
        if(date.Success && length>0)
            foreach(string name in new[]{"day","month","year"})
            {
                var field=date.Groups[name];
                if(field.Length>0 && field.Value.All(c=>c is ' ' or '\t' or '\u00A0' or '\u202F' or '.' or '_' or '…') && start>=field.Index &&
                    (long)start+length<=field.Index+field.Length)return true;
            }
        return false;
    }
}
