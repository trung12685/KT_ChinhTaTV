namespace ChinhTa.Core;

// Inner ASCII edge spaces and a proven missing gap after a simple “...” pair.
// Keep quote marks, all words/runs, internal and outside spacing unchanged.
public static class QuoteSpaceRules
{
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text.TrimEnd('\r','\n','\a');
        if(!SentenceSpaceContext.Supported(text))return [];
        var pairs=ProseDelimiterContext.Quotes(text);if(pairs is null)return [];
        var result=new List<Issue>();
        foreach(var pair in pairs)
        {
            bool missingBefore=pair.Open>0 && text[pair.Open-1] is not (' ' or '(') && ProseDelimiterContext.ClearBefore(text,pair,true);
            if(pair.Open>0 && text[pair.Open-1] is not (' ' or '(') && !missingBefore)continue;
            int first=pair.Open+1,last=pair.Close;
            while(first<last && text[first]==' ')first++;
            while(last>first && text[last-1]==' ')last--;
            bool outside=ProseDelimiterContext.Outside(text,pair.Close+1);
            bool missingAfter=!outside && ParenthesisSpaceRules.ClearAfterDelimitedText(text,pair.Open,pair.Close,first,last,true);
            if(!outside && !missingAfter || !ProseDelimiterContext.Prose(text,first,last,2) || !missingBefore && !missingAfter && first==pair.Open+1 && last==pair.Close)continue;
            if(!paragraph.TextEditable)return [new(paragraph.Start,text.Length,text,text,
                "Khoảng trắng quanh ngoặc kép cần duyệt ở vùng chưa đối chiếu chắc; kiểm tra thủ công.",
                false,FixKind.QuoteSpace,CanFix:false,Title:"Kiểm tra khoảng trắng quanh ngoặc kép")];
            if(missingBefore)result.Add(new(paragraph.Start+pair.Open,1,pair.Opening.ToString()," "+pair.Opening,
                "Thêm một dấu cách trước ngoặc kép mở; giữ chữ, dấu ngoặc và khoảng chưa chọn.",false,FixKind.QuoteSpace,Title:"Thiếu khoảng trắng trước dấu mở ngoặc kép"));
            if(first>pair.Open+1)Add(pair.Open+1,first,"Thừa khoảng trắng sau dấu mở ngoặc kép");
            if(last<pair.Close)Add(last,pair.Close,"Thừa khoảng trắng trước dấu đóng ngoặc kép");
            if(missingAfter)result.Add(new(paragraph.Start+pair.Close,1,pair.Closing.ToString(),pair.Closing+" ",
                "Thêm một dấu cách ASCII sau ngoặc kép đóng; giữ dấu/chữ, mép trong và kiểu/cỡ chưa chọn. Cần duyệt cả câu.",
                false,FixKind.QuoteSpace,Title:"Thiếu khoảng trắng sau dấu đóng ngoặc kép"));
        }
        return result;
        void Add(int start,int end,string title)=>result.Add(new(paragraph.Start+start,end-start,text[start..end],"",
            "Xóa dấu cách ASCII sát trong ngoặc kép; giữ dấu ngoặc, chữ và khoảng cách chưa chọn. Cần duyệt cả câu.",
            false,FixKind.QuoteSpace,Title:title));
    }
}
