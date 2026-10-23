namespace ChinhTa.Core;

// A nonempty punctuation span allows a checked Word write without a guessed
// zero-length insertion anchor. The next word and spaces before the mark stay out.
public static class SentenceAfterSpaceRules
{
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text.TrimEnd('\r','\n','\a');
        if(!SentenceSpaceContext.Supported(text))return Array.Empty<Issue>();
        var result=new List<Issue>();
        for(int mark=0;mark+1<text.Length;mark++)
        {
            if(text[mark] is not ('.' or '?' or '!') || !char.IsUpper(text[mark+1]))continue;
            int leftEnd=mark;
            while(leftEnd>0 && text[leftEnd-1]==' ')leftEnd--;
            if(!SentenceSpaceContext.Clear(text,leftEnd,mark+1))continue;
            if(!paragraph.TextEditable)
                return [new(paragraph.Start,text.Length,text,text,
                    "Có dấu kết câu thiếu dấu cách sau trong vùng chưa cho phép sửa chữ. Kiểm tra thủ công.",
                    false,FixKind.SentenceAfterSpace,CanFix:false,Title:"Thiếu dấu cách sau dấu kết câu trong vùng có cấu trúc")];
            string punctuation=text[mark].ToString();
            result.Add(new(paragraph.Start+mark,1,punctuation,punctuation+" ",
                "Đề xuất thêm một dấu cách ASCII sau dấu kết câu; giữ chữ, dấu câu và khoảng trước chưa chọn. Cần duyệt cả câu.",
                false,FixKind.SentenceAfterSpace,Title:"Thiếu khoảng trắng sau dấu kết câu"));
        }
        return result;
    }
}
