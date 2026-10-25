using System.Globalization;

namespace ChinhTa.Core;

// Inner ASCII edge spaces only, in a proven simple “...” prose pair.
// Keep quote marks, all words/runs, internal and outside spacing unchanged.
public static class QuoteSpaceRules
{
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text.TrimEnd('\r','\n','\a');
        if(!SentenceSpaceContext.Supported(text) || text.IndexOfAny(['"','«','»','„','‟','‘','’'])>=0)return [];
        var pairs=new List<(int Open,int Close)>();int open=-1;
        for(int i=0;i<text.Length;i++)
            if(text[i]=='“')
            {
                if(open>=0)return [];
                open=i;
            }
            else if(text[i]=='”')
            {
                if(open<0)return [];
                pairs.Add((open,i));open=-1;
            }
        if(open>=0)return [];
        var result=new List<Issue>();
        foreach(var pair in pairs)
        {
            if(pair.Open>0 && text[pair.Open-1] is not (' ' or '(') || pair.Close+1<text.Length &&
                text[pair.Close+1] is not (' ' or ')' or ',' or ';' or '.' or ':' or '?' or '!'))continue;
            int first=pair.Open+1,last=pair.Close;
            while(first<last && text[first]==' ')first++;
            while(last>first && text[last-1]==' ')last--;
            if(first==pair.Open+1 && last==pair.Close || !Prose(text,first,last))continue;
            if(!paragraph.TextEditable)return [new(paragraph.Start,text.Length,text,text,
                "Có khoảng trắng sát trong ngoặc kép ở vùng chưa đối chiếu chắc; kiểm tra thủ công.",
                false,FixKind.QuoteSpace,CanFix:false,Title:"Kiểm tra khoảng trắng trong ngoặc kép")];
            if(first>pair.Open+1)Add(pair.Open+1,first,"Thừa khoảng trắng sau dấu mở ngoặc kép");
            if(last<pair.Close)Add(last,pair.Close,"Thừa khoảng trắng trước dấu đóng ngoặc kép");
        }
        return result;
        void Add(int start,int end,string title)=>result.Add(new(paragraph.Start+start,end-start,text[start..end],"",
            "Xóa dấu cách ASCII sát trong ngoặc kép; giữ dấu ngoặc, chữ và khoảng cách chưa chọn. Cần duyệt cả câu.",
            false,FixKind.QuoteSpace,Title:title));
    }
    private static bool Mark(char c)=>char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
    private static bool Prose(string text,int start,int end)
    {
        // One attached sentence-ending mark is retained. Validate its words on
        // the quote interior so a missing left-edge gap cannot change the role.
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
        return words>=2 && longWord && lower;
    }
}
