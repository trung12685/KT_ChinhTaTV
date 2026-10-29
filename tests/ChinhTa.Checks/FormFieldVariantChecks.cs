using System.Text;
using ChinhTa.Core;

public static class FormFieldVariantChecks
{
    private static Issue[] Spaces(string text,int cp=0,DocumentProfile profile=DocumentProfile.State)=>
        Checker.Check([new(cp,text)],profile).Where(i=>i.Kind==FixKind.Text && i.CanFix && i.Replacement==" " &&
            i.Original.Length>0 && i.Original.All(c=>c==' ')).ToArray();
    private static string Apply(string text,IReadOnlyList<Issue> edits,int cp=0)=>
        AtomicTextBatch.ExpectedStory(BatchPlan.Create(edits),text,i=>text[..(i.Start-cp)]);

    public static void Run(Action<bool,string> assert)
    {
        foreach(string mark in new[]{"X","x","✓","✔"})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            string text="Tích chọn [    "+mark+"    ] nếu đồng ý.";
            assert(Spaces(text,83,profile).Length==0,
                "A single conventional square-checkbox mark retains both ASCII padding spans in every document profile");
            int left=text.IndexOf('[',StringComparison.Ordinal)+1,center=left+4;
            assert(SpacingRules.IsBlankField(text,left,4) && SpacingRules.IsBlankField(text,center+1,4) &&
                !SpacingRules.IsBlankField(text,center,1),
                "Marked-checkbox protection covers only its padding, never grants ownership over the mark itself");
        }
        foreach(string text in new[]{"Tích chọn [    XX    ] nếu đồng ý.","Tích chọn [    ?    ] nếu đồng ý.",
            "Tích chọn [    ✓✓    ] nếu đồng ý.","Tích chọn [    nội dung    ] nếu đồng ý.",
            "Tích chọn [    X    ) nếu đồng ý.","Tích chọn [    X chưa rõ."})
            assert(Spaces(text).Length>0,
                "Multiple marks, ordinary bracketed prose and unmatched shapes are not guessed to be checked form boxes");

        const string marked="😀 Tích  chọn [    X    ] hoặc [   ✔   ] nếu  đồng ý.\r\a";
        const string markedExpected="😀 Tích chọn [    X    ] hoặc [   ✔   ] nếu đồng ý.\r\a";
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var edits=Spaces(marked,93,profile);
            assert(edits.Length==2 && edits.All(i=>i.Original==marked.Substring(i.Start-93,i.Length)) &&
                Apply(marked,edits,93)==markedExpected,
                "Checked boxes keep their complete width while independent outer gaps retain exact CP repairs after emoji and cell endings");
        }

        foreach(string text in new[]{"Số:        /BC-ABC","Ninh Bình, ngày        tháng 10 năm 2026",
            "Ngày 03 tháng        năm 2026","ngày        tháng        năm        ",
            "Ngày:        tháng        năm        ","ngày 03 tháng 10 năm        (chưa điền)",
            "Ngày 03 tháng …    năm ____ (chưa điền)","NGÀY 03 THÁNG        NĂM 2026"})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            string raw=text.Normalize(form);
            assert(Spaces(raw,91,profile).Length==0,
                "NFC and decomposed number/date labels preserve every empty or placeholder day/month/year field outside an administrative header");
        }
        foreach(var sample in new (string Source,string Expected)[]
        {
            ("Ngày  03 tháng        năm 2026  (bản sao)\r\a","Ngày 03 tháng        năm 2026 (bản sao)\r\a"),
            ("Ninh Bình, ngày        tháng  10 năm 2026","Ninh Bình, ngày        tháng 10 năm 2026"),
            ("Số:        /BC-ABC  (bản sao)","Số:        /BC-ABC (bản sao)"),
            ("Ngày 03 tháng  10 năm        (chưa điền)","Ngày 03 tháng 10 năm        (chưa điền)")
        })
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            string raw=sample.Source.Normalize(form),expected=sample.Expected.Normalize(form);
            var edits=Spaces(raw,93,profile);
            assert(edits.Length>0 && edits.All(i=>i.Original==raw.Substring(i.Start-93,i.Length)) && Apply(raw,edits,93)==expected,
                "Blank-slot recognition keeps raw label accents and field offsets; filled slots and outer annotations still receive their independent repairs");
            CurrentTextPlan.Validate(edits,Checker.Check([new(93,raw)],profile));
        }
        foreach(string text in new[]{"Trong ngày 03 tháng        năm 2026", "Ghi chú ngày        tháng 10 năm 2026",
            "Ngày 03 tháng tám    năm 2026","Ngày 03 thang        năm 2026", "Ngay 03 tháng        năm 2026",
            "Ngày 03 tháng        năm 2026x", "Ngày / tháng        năm 2026", "Số liệu:        /BC-ABC",
            "Hà Nội, ngày        tháng 10 năm 2026 (dự thảo)\r"})
            assert(Spaces(text).Length>0,
                "Unlabelled prose, malformed date words and unsupported field values cannot exempt arbitrary paragraph spaces");

        foreach(var sample in new (string Old,string Current)[]
        {
            ("Tích chọn [    Y    ] nếu đồng ý.","Tích chọn [    X    ] nếu đồng ý."),
            ("Ngày 03 tháng    10 năm 2026","Ngày 03 tháng    năm 2026"),
            ("Ngày 03 tháng    10 năm 2026".Normalize(NormalizationForm.FormD),"Ngày 03 tháng    năm 2026".Normalize(NormalizationForm.FormD))
        })
        {
            var old=Spaces(sample.Old);bool rejected=false;
            try{CurrentTextPlan.Validate(old,Checker.Check([new(0,sample.Current)]));}
            catch(InvalidOperationException){rejected=true;}
            assert(old.Length>0 && old.All(i=>sample.Current.Substring(i.Start,i.Length)==i.Original) && rejected,
                "Unchanged whitespace bytes and CPs cannot replay an old repair after the surrounding field becomes a checked box or an empty date slot");
        }
    }
}
