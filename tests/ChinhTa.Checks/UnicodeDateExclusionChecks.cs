using System.Text;
using ChinhTa.Core;

public static class UnicodeDateExclusionChecks
{
    private const int Cp=317;
    private sealed record Sample(string Text,FixKind Kind);
    private static readonly Sample[] Dates=
    [
        new("Ninh Bình ( bản nháp ), ngày 03 tháng 10 năm 2026",FixKind.ParenthesisSpace),
        new("Nhận ngày 03 tháng 10 năm 2026; ghi chú hoàn thành .",FixKind.SentenceEndSpace),
        new("Nhận ngày 03 tháng 10 năm 2026; ghi chú:nội dung.",FixKind.ColonSpace),
        new("Nhận ngày 03 tháng 10 năm 2026; đã hoàn thành . Xong.",FixKind.SentenceInnerSpace),
        new("Nhận ngày 03 tháng 10 năm 2026; đã hoàn thành.Xong.",FixKind.SentenceAfterSpace),
        new("Nhận ngày 03 tháng 10 năm 2026; đã hoàn thành.   ",FixKind.ParagraphEndSpace),
        new("Nhận ngày 03 tháng 10 năm 2026; theo mẫu “ nội dung ”.",FixKind.QuoteSpace),
        new("Nhận ngày 03 tháng 10 năm 2026; theo mẫu (nội dung)đã duyệt.",FixKind.ParenthesisSpace),
        new("Nhận ngày 03 tháng 10 năm 2026; theo mẫu (nội dung) ,báo cáo.",FixKind.PunctuationSpace)
    ];
    private static IReadOnlyList<Issue> Direct(Paragraph row,FixKind kind)=>kind switch
    {
        FixKind.ParenthesisSpace=>ParenthesisSpaceRules.Check(row),
        FixKind.SentenceEndSpace=>SentenceEndSpaceRules.Check(row),
        FixKind.ColonSpace=>ColonSpaceRules.Check(row),
        FixKind.SentenceInnerSpace=>SentenceInnerSpaceRules.Check(row),
        FixKind.SentenceAfterSpace=>SentenceAfterSpaceRules.Check(row),
        FixKind.ParagraphEndSpace=>ParagraphEndSpaceRules.Check(row),
        FixKind.QuoteSpace=>QuoteSpaceRules.Check(row),
        FixKind.PunctuationSpace=>PunctuationSpaceRules.Check(row),
        _=>throw new InvalidOperationException("Unknown synthetic prose family")
    };
    private static bool Reject(Action action)
    {try{action();return false;}catch(InvalidOperationException){return true;}}

    public static void Run(Action<bool,string> assert)
    {
        foreach(var sample in Dates)
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            string text=("😀 "+sample.Text).Normalize(form)+"\r\a";
            var row=new Paragraph(Cp,text);var current=Checker.Check([row],profile);
            assert(Direct(row,sample.Kind).Count==0 && !current.Any(i=>i.CanFix) && row.Text==text,
                "The existing date exclusion is identical for NFC/NFD prose annotations, punctuation and Word endings without rewriting source text: "+sample.Kind+"/"+profile);
        }

        foreach(bool dayNfd in new[]{false,true})
        foreach(bool monthNfd in new[]{false,true})
        foreach(bool yearNfd in new[]{false,true})
        foreach(bool uppercase in new[]{false,true})
        {
            string Word(string word,bool decomposed)=>(uppercase?word.ToUpperInvariant():word)
                .Normalize(decomposed?NormalizationForm.FormD:NormalizationForm.FormC);
            string text=$"Theo mẫu ( nội dung ), {Word("ngày",dayNfd)} 03 {Word("tháng",monthNfd)} 10 {Word("năm",yearNfd)} 2026.";
            assert(ParenthesisSpaceRules.Check(new(Cp,text)).Count==0,
                "Each original date label independently accepts composed/decomposed spelling and case without a normalization pass or new date writer");
        }

        foreach(var sample in new (string Before,string After,FixKind Kind)[]
        {
            ("Theo mẫu ( nội dung ) đã duyệt.","Theo mẫu (nội dung) đã duyệt.",FixKind.ParenthesisSpace),
            ("Đã hoàn thành .","Đã hoàn thành.",FixKind.SentenceEndSpace),
            ("Ghi chú:nội dung","Ghi chú: nội dung",FixKind.ColonSpace),
            ("Đã hoàn thành . Xong.","Đã hoàn thành. Xong.",FixKind.SentenceInnerSpace),
            ("Đã hoàn thành.Xong.","Đã hoàn thành. Xong.",FixKind.SentenceAfterSpace),
            ("Đã hoàn thành.   ","Đã hoàn thành.",FixKind.ParagraphEndSpace),
            ("Theo mẫu “ nội dung ”.","Theo mẫu “nội dung”.",FixKind.QuoteSpace),
            ("Theo mẫu (nội dung) ,báo cáo.","Theo mẫu (nội dung), báo cáo.",FixKind.PunctuationSpace)
        })
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            string text=("😀 "+sample.Before).Normalize(form);var edits=Checker.Check([new(Cp,text)],DocumentProfile.Personal)
                .Where(i=>i.CanFix).ToArray();
            string expected=AtomicTextBatch.ExpectedStory(BatchPlan.Create(edits),text,i=>text[..(i.Start-Cp)]);
            assert(edits.Length>0 && edits.Any(i=>i.Kind==sample.Kind) && expected==("😀 "+sample.After).Normalize(form) &&
                edits.All(i=>text.Substring(i.Start-Cp,i.Length)==i.Original),
                "Ordinary NFC/NFD prose keeps exact positive-span repairs and source anchors when no date context is present: "+sample.Kind);
        }

        foreach(string labels in new[]{"côngngày 03 tháng 10 năm 2026","ngàyx 03 tháng 10 năm 2026",
            "ngày 03 thángx 10 năm 2026","ngày 03 tháng 10 nămx 2026","ngay 03 tháng 10 năm 2026",
            "ngày 03 thang 10 năm 2026","ngày 03 tháng 10 nam 2026","tháng 10 ngày 03 năm 2026",
            "ngày 03\ntháng 10 năm 2026","ngày 03\rtháng 10 năm 2026","ngày 03\atháng 10 năm 2026"})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            string text=("Theo mẫu ( nội dung ), nhắc "+labels+".").Normalize(form);
            assert(ParenthesisSpaceRules.Check(new(Cp,text)) is [{CanFix:true},{CanFix:true}],
                "Embedded labels, missing accents, changed order and paragraph/cell boundaries do not gain a date-word exclusion");
        }

        foreach(var sample in Dates)
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            string current=sample.Text.Normalize(NormalizationForm.FormD);
            string old=current.Replace("nga\u0300y","nga\u0301y",StringComparison.Ordinal);
            var selected=Checker.Check([new(Cp,old)],profile).Where(i=>i.CanFix && i.Kind==sample.Kind).ToArray();
            var findings=Checker.Check([new(Cp,current)],profile);
            assert(selected.Length>0 && selected.All(i=>current.Substring(i.Start-Cp,i.Length)==i.Original) &&
                Reject(()=>CurrentTextPlan.Validate(selected,findings)) && Reject(()=>RepairDependencies.Expand(selected,findings)),
                "An unchanged old punctuation span loses edit permission when its surrounding decomposed text becomes the excluded date context: "+sample.Kind+"/"+profile);
        }
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            string text="Nội dung ,báo cáo ngày 03 tháng 10 năm 2026.".Normalize(form);
            var found=PunctuationSpaceRules.Check(new(Cp,text));
            assert(found is [{CanFix:true,Original:" ,",Replacement:", "}] &&
                text.Substring(found[0].Start-Cp,found[0].Length)==found[0].Original,
                "The shared exclusion does not widen into an unrelated blanket comma ban: the existing simple-word branch retains its exact repair in both Unicode forms");
        }
        assert(ParenthesisSpaceRules.Check(new(Cp,"\ud800 Theo mẫu ( nội dung ), ngày 03 tháng 10 năm 2026.")).Count==0,
            "The boolean date exclusion does not normalize an unrelated invalid UTF-16 prefix or introduce a new normalization exception");
    }
}
