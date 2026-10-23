using System.Globalization;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// Shared conservative prose boundaries for independent before/after sentence fixes.
internal static class SentenceSpaceContext
{
    private static readonly Regex DateWords=new(@"\bngày\b[^\r\n\a]*\btháng\b[^\r\n\a]*\bnăm\b",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    private static readonly HashSet<string> Abbreviations=new(StringComparer.OrdinalIgnoreCase)
        {"tp","tx","tt","tr","ts","ths","pgs","gs","dr","mr","mrs","ms","etc","vs","st","no","tel","fax","tnhh","http","https","ftp","file","mailto","data","urn"};
    private static readonly HashSet<string> Suffixes=new(StringComparer.OrdinalIgnoreCase)
        {"com","net","org","vn","gov","edu","io","dev","doc","docx","xls","xlsx","ppt","pptx","pdf","txt","exe","zip","rar","csv","xml","json","dll","log","jpg","png"};
    internal static bool Supported(string text)
    {
        return !(AdministrativeHeaderRules.IsCandidate(text) || DateWords.IsMatch(text) ||
            text.IndexOfAny(['\t','\r','\n','\a','\v','\f','\u00a0','@','/','\\','_','=','`','<','>','[',']','{','}','+','*','^','|'])>=0);
    }
    internal static bool Clear(string text,int leftEnd,int rightStart)
    {
        int lastStart=leftEnd;
        while(lastStart>0 && WordChar(text[lastStart-1]))lastStart--;
        if(!Word(text,lastStart,leftEnd) || text[lastStart..leftEnd].Any(c=>char.IsLetter(c) && !char.IsLower(c)) ||
            text[lastStart..leftEnd].All(c=>"ivxlcdm".Contains(c)) || Abbreviations.Contains(text[lastStart..leftEnd]) ||
            lastStart==0 || text[lastStart-1]!=' ')return false;
        int previousEnd=lastStart;
        while(previousEnd>0 && text[previousEnd-1]==' ')previousEnd--;
        int previousStart=previousEnd;
        while(previousStart>0 && WordChar(text[previousStart-1]))previousStart--;
        if(!Word(text,previousStart,previousEnd) || previousStart>0 && text[previousStart-1] is not (' ' or ',' or ';'))return false;
        int rightEnd=rightStart;
        while(rightEnd<text.Length && WordChar(text[rightEnd]))rightEnd++;
        if(!Word(text,rightStart,rightEnd) || !char.IsUpper(text[rightStart]) ||
            !text[(rightStart+1)..rightEnd].Any(char.IsLower) ||
            text[(rightStart+1)..rightEnd].Any(c=>char.IsLetter(c) && !char.IsLower(c)) ||
            Suffixes.Contains(text[rightStart..rightEnd]) ||
            rightEnd<text.Length && text[rightEnd]!=' ')return false;
        return true;
    }
    private static bool WordChar(char c)=>char.IsLetter(c) || char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
    private static bool Word(string text,int start,int end)=>start<end && char.IsLetter(text[start]) && text[start..end].Count(char.IsLetter)>=2;
}
