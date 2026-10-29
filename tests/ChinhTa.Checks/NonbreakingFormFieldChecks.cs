using System.Text;
using System.Text.RegularExpressions;
using ChinhTa.Core;

public static class NonbreakingFormFieldChecks
{
    private const int Cp=271;
    private static Issue[] Spaces(string text,DocumentProfile profile=DocumentProfile.State)=>
        Checker.Check([new(Cp,text)],profile).Where(i=>i.CanFix && i.Kind==FixKind.Text &&
            i.Replacement==" " && i.Original.All(c=>c==' ')).ToArray();
    private static string Apply(string text,IReadOnlyList<Issue> edits)=>
        AtomicTextBatch.ExpectedStory(BatchPlan.Create(edits),text,i=>text[..(i.Start-Cp)]);
    private static Paragraph[] Header(string text)
    {
        int cp=Cp;
        return new[]{Checker.Heading,HeadingRules.Motto,text}.Select(t=>
        {var p=new Paragraph(cp,t+"\r",InTable:true);cp+=p.Text.Length;return p;}).ToArray();
    }
    public static void Run(Action<bool,string> assert)
    {
        foreach(char gap in new[]{'\u00A0','\u202F'})
        {
            foreach(string text in new[]{
                $"Ngày sinh:{gap}    /    /____",
                $"Sinh{gap}ngày:    /{gap}    /…",
                $"Số:{gap}    ...    /BC-ABC",
                $"Số:    ...{gap}    /BC-ABC",
                $"ngày{gap}    tháng    năm    ",
                $"ngày    tháng{gap}    năm    ",
                $"Hà Nội,{gap}ngày    tháng    năm{gap}    "})
            foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
            foreach(var profile in Enum.GetValues<DocumentProfile>())
                assert(Spaces(text.Normalize(form),profile).Length==0,
                    "Recognized blank serial/date slots retain ASCII padding when Word NBSP or narrow NBSP is mixed into the same field, in raw NFC/NFD and every profile");

            foreach(var sample in new (string Source,string Expected,int Count)[]{
                ($"Ngày sinh:{gap}    /  10/____; ghi  chú.",$"Ngày sinh:{gap}    / 10/____; ghi chú.",2),
                ($"ngày{gap}    tháng  10 năm    ",$"ngày{gap}    tháng 10 năm    ",1),
                ($"Số:{gap}    ...    /BC-ABC  (bản sao)",$"Số:{gap}    ...    /BC-ABC (bản sao)",1),
                ($"Chọn [  {gap}X  ]  ghi chú.",$"Chọn [  {gap}X  ] ghi chú.",1),
                ($"Chọn [{gap}  ✓  {gap}]  ghi chú.",$"Chọn [{gap}  ✓  {gap}] ghi chú.",1)})
            foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                string raw=sample.Source.Normalize(form);var edits=Spaces(raw,profile);
                assert(edits.Length==sample.Count && Apply(raw,edits)==sample.Expected.Normalize(form) &&
                    edits.All(i=>raw.Substring(i.Start-Cp,i.Length)==i.Original),
                    "Nonbreaking-field protection stays local: filled slots and outside prose gaps still repair at the exact raw source CP while NBSP and blank widths remain unchanged");
            }

            foreach(string text in new[]{
                $"Số:{gap}    ...    /BC-ABC",$"Số:    ...{gap}    /BC-ABC",
                $"Hà Nội, ngày{gap}    tháng    năm    "})
            {
                var header=Header(text);var row=header[2];
                var edits=Checker.Check(header).Where(i=>i.CanFix && !i.IsStyleOnly && i.Format is null &&
                    i.Start>=row.Start && (long)i.Start+i.Length<=row.Start+row.Text.Length).ToArray();
                assert(AdministrativeHeaderRules.Identify(header,DocumentProfile.State).Any(p=>p.Start==row.Start) &&
                    edits.Length==0,
                    "An identified State header containing nonbreaking blank padding gains no administrative or C01 write into its unassigned serial/date slots; unsupported edit grammar stays manual");
            }

            foreach(string text in new[]{
                $"Mã số:{gap}    ...    /BC-ABC",$"Ngày sinh:{gap}    /tháng/____",
                $"Ngày sinh:{gap}    /    /____x",$"Số liệu:{gap}    ...    /BC-ABC",
                $"Ngày sinh:{gap}12/  10/2026"})
                assert(Regex.Matches(text,@" {2,}").All(m=>!SpacingRules.IsBlankField(text,m.Index,m.Length)),
                    "Unsupported labels, malformed values and filled slots gain no new NBSP blank-field exception");

            string previous=$"Ngày sinh:{gap}12/  10/2026",current=$"Ngày sinh:{gap}12/  … /2026";
            var old=Spaces(previous);bool blocked=false;
            try{CurrentTextPlan.Validate(old,Checker.Check([new(Cp,current)]));}
            catch(InvalidOperationException){blocked=true;}
            assert(old.Length==1 && current.Substring(old[0].Start-Cp,old[0].Length)==old[0].Original && blocked,
                "A same-width filled-date repair cannot replay after its unchanged ASCII gap becomes padding of a nonbreaking placeholder field");
        }

        foreach(char unsupported in new[]{'\v','\f','\n','\r','\a','\u2003'})
        {
            string text=$"Ngày sinh:{unsupported}    /    /____";
            assert(Regex.Matches(text,@" {2,}").All(m=>!SpacingRules.IsBlankField(text,m.Index,m.Length)),
                "Field recognition does not admit line/page/cell controls or all Unicode whitespace as blank horizontal padding");
        }
    }
}
