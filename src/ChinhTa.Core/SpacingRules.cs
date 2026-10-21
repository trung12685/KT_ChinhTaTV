using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public static class SpacingRules
{
    private static readonly Regex BlankNumber=new(@"^[ \t]*Số[ \t]*:(?<blank>[ \t]{2,})(?=/[\p{L}\p{N}])",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    private static readonly Regex BlankDay=new(@"^[ \t]*(?:[\p{L}\p{M}][\p{L}\p{M} .’'\-]*,[ \t]*)?ngày(?<blank>[ \t]{2,})tháng[ \t]+(?:\d{1,2}[ \t]+)?năm[ \t]+\d{4}[ .\t\r\a]*$",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    public static bool IsBlankField(string text,int start,int length,bool identifiedAdministrativeHeader=false)
    {
        if(identifiedAdministrativeHeader && AdministrativeHeaderRules.IsBlankTemplateSpacing(text,start,length))return true;
        foreach(var pattern in new[]{BlankNumber,BlankDay})
        {
            var match=pattern.Match(text);
            if(!match.Success)continue;
            var blank=match.Groups["blank"];
            if(start>=blank.Index && (long)start+length<=blank.Index+blank.Length)return true;
        }
        return false;
    }
}
