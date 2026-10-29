using System.Text;
using ChinhTa.Core;

public static class RoundOptionSpacingChecks
{
    private const int Cp=137;
    private static Issue[] Spaces(string text,DocumentProfile profile=DocumentProfile.Personal)=>Checker.Check([new(Cp,text)],profile)
        .Where(i=>i.CanFix && i.Kind==FixKind.Text && i.Replacement==" " && i.Original.All(c=>c==' ')).ToArray();
    private static string Apply(string text,IReadOnlyList<Issue> edits)=>
        AtomicTextBatch.ExpectedStory(BatchPlan.Create(edits),text,i=>text[..(i.Start-Cp)]);

    public static void Run(Action<bool,string> assert)
    {
        foreach(char mark in new[]{'X','x','✓','✔'})
        foreach(var padding in new[]{("    ","    "),("\u00a0    ","    "),("    ","    \u202f")})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            string text=($"😀 Giới tính: Nam ({padding.Item1}{mark}{padding.Item2}) Nữ (        ).\r\a").Normalize(form);
            assert(Spaces(text,profile).Length==0,
                "Balanced marked round options retain their exact ASCII/NBSP/NNBSP width in all profiles without changing NFC/NFD or Word endings");
            string square=text.Replace('(','[').Replace(')',']');
            assert(Spaces(square,profile).Length==0,
                "The existing square marked and blank selection fields retain the same protected padding");
        }
        foreach(char mark in new[]{'X','x','✓','✔'})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            string raw=($"Tích chọn (    {mark}    )  theo  mẫu.").Normalize(form);
            var edits=Spaces(raw);
            assert(edits.Length==2 && Apply(raw,edits)==($"Tích chọn (    {mark}    ) theo mẫu.").Normalize(form) &&
                edits.All(i=>raw.Substring(i.Start-Cp,i.Length)==i.Original),
                "Only gaps outside the marked round option remain repairable at exact raw source CPs");
        }
        foreach(string text in new[]{"Tích chọn (    Y    ) theo mẫu.","Tích chọn (    XX    ) theo mẫu.",
            "Tích chọn (    X ✓    ) theo mẫu.","Tích chọn (    X    ] theo mẫu.","Tích chọn [    X    ) theo mẫu."})
            assert(Spaces(text).Length>=2,
                "Unknown or multiple marks and mismatched delimiter pairs gain no round-option blank-field exception");

        const string prose="Theo mẫu (    nội dung    ) đã duyệt.";
        var parentheses=Checker.Check([new(Cp,prose)],DocumentProfile.Personal).Where(i=>i.CanFix && i.Kind==FixKind.ParenthesisSpace).ToArray();
        assert(parentheses.Length==2 && Apply(prose,parentheses)=="Theo mẫu (nội dung) đã duyệt.",
            "Ordinary multiword parenthesis prose retains both independent inner-padding corrections");
        const string old="Tích chọn (    Y    ) theo mẫu.",current="Tích chọn (    X    ) theo mẫu.";
        var pending=Spaces(old);bool rejected=false;
        try{CurrentTextPlan.Validate(pending,Checker.Check([new(Cp,current)],DocumentProfile.Personal));}
        catch(InvalidOperationException){rejected=true;}
        assert(pending.Length==2 && rejected && pending.All(i=>current.Substring(i.Start-Cp,i.Length)==i.Original),
            "Unchanged old gaps cannot replay at the same CP after a single-letter placeholder becomes a marked round selection");
        assert(!Checker.Check([new(Cp,"Tích chọn (    X    )  theo mẫu.",TextEditable:false)]).Any(i=>i.CanFix),
            "Recognizing a round selection never bypasses the independent XML text-edit permission");
    }
}
