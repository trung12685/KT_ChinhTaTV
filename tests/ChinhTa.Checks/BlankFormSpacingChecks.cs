using ChinhTa.Core;

public static class BlankFormSpacingChecks
{
    private static Issue[] Spaces(string text,int cp=0,DocumentProfile profile=DocumentProfile.State)=>
        Checker.Check([new(cp,text)],profile).Where(i=>i.Kind==FixKind.Text && i.CanFix && i.Replacement==" " &&
            i.Original.Length>0 && i.Original.All(c=>c==' ')).ToArray();
    private static string Apply(string text,IReadOnlyList<Issue> edits,int cp=0)=>
        AtomicTextBatch.ExpectedStory(BatchPlan.Create(edits),text,i=>text[..(i.Start-cp)]);

    public static void Run(Action<bool,string> assert)
    {
        foreach(string text in new[]{"Tích chọn [        ] nếu đồng ý.","[        ]", "Tích chọn [  ] hoặc [    ]."})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            assert(Spaces(text,83,profile).Length==0,
                "An empty square checkbox retains its ASCII width in every profile rather than becoming a one-space text repair");
            int start=text.IndexOf('[',StringComparison.Ordinal)+1,end=text.IndexOf(']',start);
            assert(SpacingRules.IsBlankField(text,start,end-start),
                "The shared blank-field boundary recognizes only the exact empty checkbox interior");
        }
        foreach(string text in new[]{"Tích chọn [  nội dung  ] nếu đồng ý.","Tích chọn [        chưa rõ.","Tích chọn [        ) chưa rõ."})
            assert(Spaces(text).Length>0,"A nonempty, unmatched or mismatched bracket is not silently exempted as an empty checkbox");
        foreach(string text in new[]{"Ngày sinh:    /    /2026","Ngày:    /    /2026\r\a", "Sinh ngày:    /    /2026",
            "NGÀY SINH:    /    /2026", "Nga\u0300y sinh:    /    /2026", "Ngày sinh:    /    /        "})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
            assert(Spaces(text,91,profile).Length==0,
                "A clearly labelled slash-date form preserves blank day/month/year fields including decomposed labels and Word endings");

        foreach(var sample in new[]{
            ("Ngày sinh:  12/    /2026","Ngày sinh: 12/    /2026"),
            ("Ngày sinh:    /  10/2026","Ngày sinh:    / 10/2026"),
            ("Ngày sinh:    /    /  2026","Ngày sinh:    /    / 2026"),
            ("Ngày sinh:  12/  10/  2026","Ngày sinh: 12/ 10/ 2026"),
            ("Ngày sinh:    /10/        ; ghi chú.","Ngày sinh:    /10/        ; ghi chú.")})
        {
            var edits=Spaces(sample.Item1,73);
            assert(Apply(sample.Item1,edits,73)==sample.Item2 && edits.All(i=>i.Original==sample.Item1.Substring(i.Start-73,i.Length)),
                "Filled date fields retain ordinary exact-span spacing repairs while separate empty fields keep their width");
        }
        foreach(var sample in new[]{
            ("😀 Tích  chọn [        ] nếu  đồng ý.\r\a","😀 Tích chọn [        ] nếu đồng ý.\r\a"),
            ("Ngày  sinh:    /    /2026  (bản sao)\r\a","Ngày sinh:    /    /2026 (bản sao)\r\a"),
            ("Nga\u0300y  sinh:    /    /2026  (bản sao)","Nga\u0300y sinh:    /    /2026 (bản sao)")})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var edits=Spaces(sample.Item1,93,profile);
            assert(edits.Length==2 && edits.All(i=>i.Start>=93 && i.Original==sample.Item1.Substring(i.Start-93,i.Length)) &&
                Apply(sample.Item1,edits,93)==sample.Item2,
                "Blank protection is local: independent outer spaces remain repairable at exact CPs after emoji, combining marks and cell endings");
            CurrentTextPlan.Validate(edits,Checker.Check([new(93,sample.Item1)],profile));
        }
        foreach(string text in new[]{"Công tác ngày/đêm  cần duyệt.","Tỉ lệ:    /    /2026", "Văn bản ghi ngày    /    /2026",
            "Ngày sinh:    /    /hai nghìn", "Ngày sinh:    |    |2026"})
            assert(Spaces(text).Length>0,"A prose slash, unrelated label or unsupported date shape cannot exempt arbitrary whitespace");
        const string old="Ngày sinh:    12/    /2026";
        var previous=Spaces(old);
        const string changed="Ngày sinh:    /    /2026";
        bool rejected=false;
        try{CurrentTextPlan.Validate(previous,Checker.Check([new(0,changed)]));}catch(InvalidOperationException){rejected=true;}
        assert(previous.Length==1 && previous[0].Start==10 && previous[0].Original=="    " &&
            changed.Substring(previous[0].Start,previous[0].Length)==previous[0].Original && rejected,
            "An identical whitespace span and CP loses its old spacing-write permission when a filled day becomes a blank field");
        foreach(string text in new[]{"Số:        /BC-ABC\r","Ninh Bình, ngày        tháng 10 năm 2026\r", "Điện thoại: (        )"})
            assert(Spaces(text).Length==0,"Existing number/date and empty-round-field padding remain protected");
    }
}
