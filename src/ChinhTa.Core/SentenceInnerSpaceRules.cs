namespace ChinhTa.Core;

// Only delete ASCII spaces before a single .?! between two clear prose spans.
// Require two adjacent left words and a title-case right word; after-space is independent.
public static class SentenceInnerSpaceRules
{
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text.TrimEnd('\r','\n','\a');
        if(!SentenceSpaceContext.Supported(text))return Array.Empty<Issue>();
        var result=new List<Issue>();
        for(int mark=0;mark+1<text.Length;mark++)
        {
            if(text[mark] is not ('.' or '?' or '!'))continue;
            int start=mark;
            while(start>0 && text[start-1]==' ')start--;
            if(start==mark)continue;
            int rightStart=mark+1;
            while(rightStart<text.Length && text[rightStart]==' ')rightStart++;
            if(!SentenceSpaceContext.Clear(text,start,rightStart))continue;
            if(!paragraph.TextEditable)
                return [new(paragraph.Start,text.Length,text,text,
                    "Có khoảng trắng trước dấu kết câu giữa đoạn trong vùng chưa cho phép sửa chữ. Kiểm tra thủ công.",
                    false,FixKind.SentenceInnerSpace,CanFix:false,Title:"Dấu kết câu giữa đoạn trong vùng có cấu trúc")];
            result.Add(new(paragraph.Start+start,mark-start,text[start..mark],"",
                "Đề xuất xóa dấu cách trước dấu kết câu giữa đoạn; giữ chữ, dấu câu và khoảng trắng sau. Cần duyệt cả câu.",
                false,FixKind.SentenceInnerSpace,Title:"Khoảng trắng trước dấu kết câu giữa đoạn"));
        }
        return result;
    }
}
