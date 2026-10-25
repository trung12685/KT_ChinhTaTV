using ChinhTa.Core;

public static class QuoteSentenceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.QuoteSpace && i.CanFix).ToArray();
        string Apply(string text,IEnumerable<Issue>? edits=null)=>AtomicTextBatch.ExpectedStory(BatchPlan.Create(edits??Find(text)),text,i=>text[..i.Start]);
        foreach(string sentence in new[]{"Đã hoàn thành.","Kết quả thế nào?","Báo cáo!","Ke\u0302\u0301 hoa\u0323ch đã duyệt."})
        foreach(var gaps in new[]{("  "," "),("","  "),("  ","")})
        {
            string before="Đã ghi: “"+gaps.Item1+sentence+gaps.Item2+"”",after="Đã ghi: “"+sentence+"”";
            var edits=Find(before);
            assert(edits.Length==(gaps.Item1.Length==0 || gaps.Item2.Length==0?1:2) && Apply(before)==after && !Find(after).Any(),
                "A quoted sentence with one attached .?! gets only existing inner-edge deletions, independently of which edge is already correct");
            var source=new CharacterStyleEvidence(before,before.Select((c,i)=>char.IsWhiteSpace(c)?null:
                new ObservedCharacterStyle(new("Times New Roman",i%2==0?13:14,i%2==0?0:-1,i%3==0?-1:0,0),i%2)),new(0,0,0,36,new(0,6,0,12,0,0)));
            var expected=source.Project(edits,i=>before[..i.Start]);int oldMark=before.IndexOf(sentence[^1]),newMark=after.IndexOf(sentence[^1]);
            assert(expected.Text==after && expected.Styles[newMark]==source.Styles[oldMark] && expected.Styles[after.IndexOf('“')]==source.Styles[before.IndexOf('“')] &&
                expected.Styles[after.IndexOf('”')]==source.Styles[before.IndexOf('”')] && expected.Paragraph==source.Paragraph,
                "Quote-edge repair retains the sentence-ending mark and both quotes with their exact source size, emphasis, underline and paragraph state");
        }
        foreach(string text in new[]{"“ Đã hoàn thành... ”","“ Đã hoàn thành?! ”","“ Đã hoàn thành!? ”","“ Đã hoàn thành. Tiếp tục báo cáo. ”",
            "“ Đã hoàn thành . ”","“ Nội dung TP. ”","“ nội dung tp. ”","“ Xong. ”","“ nội dung ABC. ”","“ 12.5 ”",
            "“ Nội dung. ” “ phụ “ thêm ” ”","“ Đã hoàn thành. ” “ còn thiếu","“ Đã hoàn thành.\u00a0”",
            "“ Đã hoàn thành. ”tiếp","“ Đã hoàn thành. ” ngày 03 tháng 10 năm 2026"})
            assert(Find(text).Length==0,"Unmeasured abbreviation/mark sequence, multiple sentences, gap before the mark, unknown boundary or unsupported context does not grant quote-edge writes: "+text);
        const string mixed="Nội dung  “  kế hoạch kế hoạch đã duyệt.  ” đã xong;hoàn thành.";
        const string result="Nội dung “kế hoạch đã duyệt.” đã xong; hoàn thành.";
        var all=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();var edge=Find(mixed).OrderBy(i=>i.Start).First();
        assert(Apply(mixed,all)==result && !Checker.Check([new(0,result)]).Any(i=>i.CanFix),
            "Sentence-ending quote edges compose in one nonoverlapping batch with repetition, ordinary spaces and semicolon spacing");
        string one=Apply(mixed,[edge]);
        assert(one=="Nội dung  “kế hoạch kế hoạch đã duyệt.  ” đã xong;hoàn thành." && Find(one).Length==1 &&
            Checker.Check([new(0,one)]).Any(i=>i.CanFix && i.Kind==FixKind.RepeatedText),
            "Selecting one edge retains the other edge, the original ending mark and every unselected text error");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var edits=Checker.Check([new(75,"“ Đã hoàn thành. ”\r\a")],profile).Where(i=>i.Kind==FixKind.QuoteSpace).ToArray();
            assert(edits.Length==2 && edits.All(i=>i.Start>=76 && i.Original.All(c=>c==' ')) &&
                !Checker.Check([new(75,"“ Đã hoàn thành. ”",TextEditable:false)],profile).Any(i=>i.CanFix),
                "All profiles preserve real CP offsets and paragraph/cell terminators while unproven XML revokes quoted-sentence writes");
        }
        bool rejected=false;try{CurrentTextPlan.Validate(Find("“ nội dung. ”"),Checker.Check([new(0,"“ nội dung!?”")]));}
        catch(InvalidOperationException){rejected=true;}
        assert(rejected,"A changed quote/terminal context invalidates a stale inner-edge selection before mutation");
    }
}
