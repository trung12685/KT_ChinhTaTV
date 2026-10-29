using System.Text;
using System.Text.RegularExpressions;
using ChinhTa.Core;

public static class PlaceholderFieldSpacingChecks
{
    private static Issue[] Spaces(string text,int cp=0,DocumentProfile profile=DocumentProfile.State)=>
        Checker.Check([new(cp,text)],profile).Where(i=>i.CanFix && i.Kind==FixKind.Text && i.Replacement==" " &&
            i.Original.All(c=>c==' ')).ToArray();
    private static string Apply(string text,IEnumerable<Issue> edits,int cp)=>
        AtomicTextBatch.ExpectedStory(BatchPlan.Create(edits.ToArray()),text,i=>text[..(i.Start-cp)]);
    private static Paragraph[] Header(string number)
    {
        int offset=173;
        return new[]{Checker.Heading,HeadingRules.Motto,number}.Select(text=>
        {var paragraph=new Paragraph(offset,text+"\r",InTable:true);offset+=paragraph.Text.Length;return paragraph;}).ToArray();
    }
    private static Issue[] RowEdits(Paragraph[] rows)=>Checker.Check(rows).Where(i=>i.CanFix && !i.IsStyleOnly &&
        i.Start>=rows[2].Start && (long)i.Start+i.Length<=rows[2].Start+rows[2].Text.Length).ToArray();

    public static void Run(Action<bool,string> assert)
    {
        foreach(string text in new[]{"Số:    .../BC-ABC","Số: ...    /BC-ABC","Số:    …    /BC-ABC",
            "Số:    ____    /BC-ABC","Số: … /BC-ABC","Số:        /BC-ABC"})
        {
            foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                string raw=text.Normalize(form);
                assert(Spaces(raw,91,profile).Length==0,
                    "An unassigned serial slot preserves its raw NFC/NFD label and every padding span before/after placeholder dots through the slash");
            }
            var rows=Header(text);var row=rows[2];
            assert(AdministrativeHeaderRules.Identify(rows,DocumentProfile.State).Any(p=>p.Start==row.Start) && RowEdits(rows).Length==0,
                "The actual identified State-header parser emits no edit into a blank serial placeholder or its pre-slash padding");
        }
        foreach(var sample in new (string Source,string Expected)[]
        {
            ("Số:    ...    /   BC – ABC","Số:    ...    /BC-ABC"),
            ("Số:    …    /   BC-ABC","Số:    …    /BC-ABC"),
            ("Số:        /   BC-ABC","Số:        /BC-ABC"),
            ("Số:  123    /   BC – ABC","Số: 123/BC-ABC")
        })
        {
            var rows=Header(sample.Source);var row=rows[2];var edits=RowEdits(rows);
            int slash=sample.Source.IndexOf('/');
            assert(edits.Length>0 && edits.All(i=>i.Original==row.Text.Substring(i.Start-row.Start,i.Length)) &&
                Apply(row.Text,edits,row.Start)==sample.Expected+"\r" &&
                (sample.Source.Contains("123",StringComparison.Ordinal) || edits.All(i=>i.Start>=row.Start+slash+1)),
                "Header cleanup removes post-slash/code spacing without touching an unassigned slot; a filled serial retains ordinary compact separator normalization");
        }
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            string raw="Số:    …    /BC-ABC  (bản sao)".Normalize(form);
            string expected="Số:    …    /BC-ABC (bản sao)".Normalize(form);
            var edits=Spaces(raw,97,profile);
            assert(edits.Length==1 && Apply(raw,edits,97)==expected && edits[0].Original==raw.Substring(edits[0].Start-97,edits[0].Length),
                "Serial placeholder protection is local: independent annotation gaps retain exact repairs after decomposed labels");
        }
        foreach(string source in new[]{"Ngày sinh:    /    /…","Ngày sinh:    /    /____",
            "Ngày sinh:    /    /....","Ngày: …    /____    /        ","Sinh ngày:    /    /____\r\a"})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
            assert(Spaces(source.Normalize(form),83,profile).Length==0,
                "Explicit slash-date forms retain blank/placeholder padding in every day/month/year slot without normalizing source accents");

        foreach(var sample in new (string Source,string Expected)[]
        {
            ("Ngày sinh:  12/____    /…","Ngày sinh: 12/____    /…"),
            ("Ngày sinh: …    /  10/____","Ngày sinh: …    / 10/____"),
            ("Ngày sinh: ____    /…    /  2026","Ngày sinh: ____    /…    / 2026"),
            ("Ngày sinh:    /    /____; ghi  chú.","Ngày sinh:    /    /____; ghi chú.")
        })
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var edits=Spaces(sample.Source,73,profile);
            assert(edits.Length==1 && edits.All(i=>i.Original==sample.Source.Substring(i.Start-73,i.Length)) &&
                Apply(sample.Source,edits,73)==sample.Expected,
                "Only all-placeholder date slots retain padding; filled values and text outside the form still receive their independent spacing repair");
        }
        foreach(string source in new[]{"Tỉ lệ:    /    /____","Ngày sinh:    /tháng/____",
            "Ngày sinh:    /    /....sai","Ngày sinh:    /    /____sai","Ngày sinh:    /    /2026x",
            "Số liệu:    …    /BC-ABC","Số:  123/BC-ABC"})
            assert(Regex.Matches(source,@" {2,}").All(m=>!SpacingRules.IsBlankField(source,m.Index,m.Length)) &&
                (Spaces(source).Length>0 || Checker.Check([new(0,source)]).Any(i=>!i.CanFix)),
                "Unsupported labels/values and filled serials gain no blank-field exception; independent technical-context warnings remain manual");

        const string previous="Ngày sinh:    /  10/2026";
        const string current="Ngày sinh:    /  … /2026";
        var old=Spaces(previous);bool blocked=false;
        try{CurrentTextPlan.Validate(old,Checker.Check([new(0,current)]));}catch(InvalidOperationException){blocked=true;}
        assert(old.Length==1 && old[0].Original=="  " && current.Substring(old[0].Start,old[0].Length)==old[0].Original && blocked,
            "The same whitespace span and CP cannot replay a filled-date repair after the value becomes a placeholder");
        var oldHeader=Header("Số: 123    /BC-ABC");var newHeader=Header("Số: ...    /BC-ABC");
        var slashEdit=RowEdits(oldHeader).Single();blocked=false;
        try{CurrentTextPlan.Validate([slashEdit],Checker.Check(newHeader));}catch(InvalidOperationException){blocked=true;}
        assert(newHeader[2].Text.Substring(slashEdit.Start-newHeader[2].Start,slashEdit.Length)==slashEdit.Original && blocked,
            "An unchanged old compact-slash payload loses permission when its same-width serial becomes an unassigned placeholder");
    }
}
