namespace ChinhTa.Core;

// Only ASCII spaces after a single final sentence/list mark in proven prose.
// Keep the mark, any unselected spaces before it, and every paragraph/cell end.
public static class ParagraphEndSpaceRules
{
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text.TrimEnd('\r','\n','\a');
        if(!SentenceSpaceContext.Supported(text))return [];
        int start=text.Length;
        while(start>0 && text[start-1]==' ')start--;
        if(start==text.Length || start==0 || text[start-1] is not ('.' or '?' or '!' or ';'))return [];
        int leftEnd=start-1;
        while(leftEnd>0 && text[leftEnd-1]==' ')leftEnd--;
        if(!SentenceSpaceContext.ClearEnding(text,leftEnd) && !ClearDelimitedEnding(text,leftEnd))return [];
        if(!paragraph.TextEditable)
            return [new(paragraph.Start,text.Length,text,text,
                "Có dấu cách cuối đoạn sau dấu kết câu/chấm phẩy trong vùng chưa cho phép sửa chữ. Kiểm tra thủ công.",
                false,FixKind.ParagraphEndSpace,CanFix:false,Title:"Khoảng trắng cuối đoạn trong vùng có cấu trúc")];
        return [new(paragraph.Start+start,text.Length-start,text[start..],"",
            "Đề xuất xóa dấu cách ASCII sau dấu kết câu/chấm phẩy cuối đoạn; giữ chữ, dấu câu, khoảng trước chưa chọn và dấu kết đoạn/ô. Cần duyệt cả câu.",
            false,FixKind.ParagraphEndSpace,Title:"Thừa khoảng trắng cuối đoạn")];
    }
    private static bool ClearDelimitedEnding(string text,int leftEnd)
    {
        if(leftEnd==0 || TechnicalTextContext.IsTechnical(text))return false;
        char closing=text[leftEnd-1];
        IReadOnlyList<ProseDelimiterPair>? pairs;
        if(closing==')')
        {
            if(text.IndexOfAny(['\"','\'','“','”','«','»','„','‟','‘','’'])>=0)return false;
            pairs=ProseDelimiterContext.Parentheses(text);
        }
        else if(closing is '\"' or '”')
        {
            if(text.IndexOfAny(['(',')','\''])>=0)return false;
            pairs=ProseDelimiterContext.Quotes(text);
        }
        else return false;
        // This extension proves one simple final prose pair. Other delimiter
        // families, nesting, calls and unselected surrounding spaces stay out
        // of its authority; it deletes only the already-located trailing gap.
        if(pairs is not {Count:1})return false;
        var pair=pairs[0];
        if(pair.Close!=leftEnd-1 || pair.Open==0 || text[pair.Open-1]!=' ' || !ProseDelimiterContext.Prefix(text,pair.Open))return false;
        int first=pair.Open+1,last=pair.Close;
        while(first<last && text[first]==' ')first++;
        while(last>first && text[last-1]==' ')last--;
        if(!ProseDelimiterContext.SimpleWords(text,first,last))return false;
        string inside=text[first..last];
        return inside.All(c=>!char.IsLetter(c) || char.IsLower(c)) && SentenceSpaceContext.ClearEnding(inside,inside.Length);
    }
}
