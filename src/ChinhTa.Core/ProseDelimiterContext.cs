using System.Globalization;

namespace ChinhTa.Core;

internal sealed record ProseDelimiterPair(int Open,int Close,char Opening,char Closing);

// Read-only character positions inside one XML-proven paragraph. A null result
// means ambiguous/unbalanced; it never grants an edit or survives another scan.
internal sealed class ProseDelimiterIndex(IReadOnlyList<ProseDelimiterPair> pairs)
{
    internal IReadOnlyDictionary<int,ProseDelimiterPair> Open {get;}=pairs.ToDictionary(p=>p.Open);
    internal IReadOnlyDictionary<int,ProseDelimiterPair> Close {get;}=pairs.ToDictionary(p=>p.Close);
}

internal static class ProseDelimiterContext
{
    internal static IReadOnlyList<ProseDelimiterPair>? Parentheses(string text)=>Pairs(text,'(',')');
    internal static IReadOnlyList<ProseDelimiterPair>? Quotes(string text)
    {
        bool straight=text.Contains('"'),curly=text.IndexOfAny(['“','”'])>=0;
        if(straight && curly || text.IndexOfAny(['«','»','„','‟','‘','’'])>=0)return null;
        var pairs=straight?Pairs(text,'"','"'):Pairs(text,'“','”');
        if(straight && pairs is not null)
        {
            bool supported=SentenceSpaceContext.Supported(text.TrimEnd('\r','\n','\a'));
            // Identical opening/closing symbols cannot express nesting. Reject
            // the whole ambiguous family if any apparent pair has bad borders.
            foreach(var pair in pairs)
            {
                int first=pair.Open+1,last=pair.Close;
                while(first<last && text[first]==' ')first++;
                while(last>first && text[last-1]==' ')last--;
                if(pair.Open>0 && text[pair.Open-1] is not (' ' or '(') && !ClearBefore(text,pair,supported) ||
                    !Outside(text,pair.Close+1) && !ParenthesisSpaceRules.ClearAfterDelimitedText(text,pair.Open,pair.Close,first,last,supported))return null;
            }
        }
        return pairs;
    }
    private static IReadOnlyList<ProseDelimiterPair>? Pairs(string text,char opening,char closing)
    {
        var result=new List<ProseDelimiterPair>();int open=-1;
        for(int i=0;i<text.Length;i++)
        {
            char c=text[i];
            if(c==opening && (opening!=closing || open<0))
            {if(open>=0)return null;open=i;}
            else if(c==closing)
            {if(open<0)return null;result.Add(new(open,i,opening,closing));open=-1;}
        }
        return open<0?result:null;
    }
    internal static bool Outside(string text,int position)=>position==text.Length || text[position] is ' ' or '\r' or '\n' or '\a' or ')' or ',' or ';' or '.' or ':' or '?' or '!';
    internal static bool Mark(char c)=>char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
    internal static bool WordChar(char c)=>char.IsLetter(c)||Mark(c);
    internal static bool Prefix(string text,int opening)
    {
        int end=opening;while(end>0 && text[end-1]==' ')end--;
        if(!SentenceSpaceContext.ClearEnding(text,end))return false;
        int start=end;while(start>0 && WordChar(text[start-1]))start--;
        return !ParenthesisSpaceRules.IsFunctionName(text[start..end]);
    }
    internal static bool ClearBefore(string text,ProseDelimiterPair pair,bool supported)
    {
        if(pair.Open==0 || !WordChar(text[pair.Open-1]) || !supported || !Prefix(text,pair.Open))return false;
        int first=pair.Open+1,last=pair.Close;
        while(first<last && text[first]==' ')first++;
        while(last>first && text[last-1]==' ')last--;
        if(!SimpleWords(text,first,last,true))return false;
        return Outside(text,pair.Close+1) || ParenthesisSpaceRules.ClearAfterDelimitedText(text,pair.Open,pair.Close,first,last,true);
    }
    internal static bool SimpleWords(string text,int start,int end,bool terminal=false)
    {
        if(terminal && end>start && text[end-1] is '.' or '?' or '!')
        {
            string sentence=text[start..(end-1)];
            if(!SentenceSpaceContext.ClearEnding(sentence,sentence.Length))return false;
            end--;
        }
        var words=text[start..end].Split(' ',StringSplitOptions.RemoveEmptyEntries);
        return words.Length>=2 && text[start..end].Any(char.IsLower) && words.All(w=>char.IsLetter(w[0]) && w.Count(char.IsLetter)>=2 && w.All(WordChar));
    }
    internal static bool Prose(string text,int start,int end,int minimumWords)
    {
        if(end>start && text[end-1] is '.' or '?' or '!')
        {
            string sentence=text[start..(end-1)];
            if(!SentenceSpaceContext.ClearEnding(sentence,sentence.Length))return false;
            end--;
        }
        if(start==end || !char.IsLetter(text[start]) || !(char.IsLetter(text[end-1]) || Mark(text[end-1])))return false;
        int words=0,letters=0;bool longWord=false,lower=false;
        for(int i=start;i<end;i++)
        {
            char c=text[i];
            if(char.IsLetter(c)){if(letters==0)words++;letters++;longWord|=letters>=2;lower|=char.IsLower(c);}
            else if(Mark(c))continue;
            else if(c is ' ' or ',' or ';' or '-' or '–' or '—')letters=0;
            else return false;
        }
        return words>=minimumWords && longWord && lower;
    }
}
