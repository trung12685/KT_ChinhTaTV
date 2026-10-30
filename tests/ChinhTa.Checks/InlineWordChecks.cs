using System.Xml.Linq;
using ChinhTa.Core;

public static class InlineWordChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string text="Người dùng tự sửa nội dung này.";
        var row=new WordParagraphSnapshot(0,text.Length+1,text+"\r",1,false,false);
        var hit=new PreviewAnchor(1,text,text.IndexOf("dùng",StringComparison.Ordinal),4);
        var match=InlineWordLocator.Locate(hit,[row]);
        assert(match is {Offset:6,Word:"dùng"},"Clicking an unflagged rendered word binds to its exact native paragraph and offset without inventing an automatic issue");
        var target=ManualTextTarget.Prepare(ManualTextEditChecks.Fixture(),[row,new(text.Length+1,text.Length+23,"Đoạn khác giữ nguyên.\r",1,false,false)],6,4,"dùng");
        var draft=new InlineWordDraft(target,6,"dùng");var change=draft.Change("dụng");
        assert(change.Edited=="Người dụng tự sửa nội dung này." && change.ExpectedStory("prefix"+text+"\rsuffix","prefix")=="prefixNgười dụng tự sửa nội dung này.\rsuffix", "Inline replacement changes only the selected word and leaves every outside character and paragraph marker intact");
        assert(draft.Change("").Edited=="Người  tự sửa nội dung này." && draft.Change("dùng thử").Edited=="Người dùng thử tự sửa nội dung này.","A user may delete a selected word or replace it by a short phrase without a whole-paragraph text submission");
        bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
        foreach(string value in new[]{"dùng","x\ny","x\ty","x\ay","x\u202Ey",new string('a',257),"😀"})
            assert(Reject(()=>draft.Change(value)),"Unchanged, structural, hidden-format or over-limit inline input cannot create an edit and is not silently truncated");
        assert(Reject(()=>new InlineWordDraft(target,7,"dùng")) && Reject(()=>target.EditRange(-1,4,"x")) && Reject(()=>target.EditRange(text.Length,2,"x")),"An inline selection must match exact source bounds and text before any package mutation");
        assert(InlineWordLocator.Locate(hit,[row,row with {Start=100,End=100+row.Text.Length}]) is null && InlineWordLocator.Locate(hit,[row with {End=row.End-1}]) is null,
            "Repeated paragraphs or non-linear Word CP/string coordinates cannot choose the first apparent occurrence");
        assert(InlineWordLocator.Locate(hit,[row with {Page=2}]) is null && InlineWordLocator.Locate(hit with {Offset=0,Length=0},[row]) is null && InlineWordLocator.Locate(hit with {Offset=1,Length=2},[row]) is null,
            "A different page, empty hit or mid-word substring cannot authorize an inline word write");
        const string repeated="Sai đầu. Sai cuối.";var repeatedRow=new WordParagraphSnapshot(0,repeated.Length+1,repeated+"\r",1,false,false);
        assert(InlineWordLocator.Locate(new(1,repeated,9,3),[repeatedRow]) is {Offset:9},"Repeated words use unique surrounding page context to preserve the clicked occurrence");
        const string decomposed="Chữ tiếng Việt.";int at=decomposed.IndexOf("tie",StringComparison.Ordinal);const string word="tiếng";
        assert(InlineWordLocator.Locate(new(1,decomposed,at,word.Length),[new(50,51+decomposed.Length,decomposed+"\r",1,false,false)]) is {Offset:4,Word:word},"Decomposed Vietnamese keeps original Unicode and native offset rather than normalizing away combining marks");
        // Two equally spelled words in differently styled runs: select the second
        // explicitly; a minimal-diff algorithm must never move the write to the first.
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        string flat=ManualTextEditChecks.Fixture(root=>
        {
            var paragraph=root.Descendants(w+"p").First();paragraph.Elements(w+"r").Remove();
            paragraph.Add(new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"b")),new XElement(w+"t","aa ")),
                new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"i")),new XElement(w+"t","aa")));
        });
        var styled=ManualTextTarget.Prepare(flat,[new(0,6,"aa aa\r",1,false,false),new(6,28,"Đoạn khác giữ nguyên.\r",1,false,false)],3,2,"aa");
        var explicitChange=new InlineWordDraft(styled,3,"aa").Change("ab");
        explicitChange.VerifyLive(ManualTextEditChecks.Fixture(root=>
        {
            var paragraph=root.Descendants(w+"p").First();paragraph.Elements(w+"r").Remove();
            paragraph.Add(new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"b")),new XElement(w+"t","aa ")),
                new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"i")),new XElement(w+"t","ab")));
        }));
        assert(explicitChange.Edited=="aa ab","An explicit repeated-word selection retains both original styles and edits the intended run");
        assert(Reject(()=>styled.EditRange(1,3,"x")),"A selected word/range that crosses styles is refused before changing either run");
    }
}
