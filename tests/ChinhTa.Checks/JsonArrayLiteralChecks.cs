using System.Text;
using ChinhTa.Core;

public static class JsonArrayLiteralChecks
{
    private const int Cp=319;
    private static bool Mechanical(Issue issue)=>issue.CanFix && !issue.IsStyleOnly && issue.Original!=issue.Replacement;

    public static void Run(Action<bool,string> assert)
    {
        foreach(string source in new[]
        {
            "[\"Báo  cáo\", \"kế hoạch kế hoạch\", \"con đo\"]",
            "  [ \"Nội dung  báo cáo\" ]  ",
            "[\"😀 Báo  cáo\", [\"kế hoạch kế hoạch\"], [1,  2]]",
            "[1, true, null, \"Nội dung  báo cáo\"]",
            "[\n\"Báo  cáo\",\n\"kế hoạch kế hoạch\"\n]",
            "[[[\"kế hoạch kế hoạch con đo\"]]]"
        })
        foreach(var normalization in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(string ending in new[]{"","\r","\r\a"})
        {
            string text=source.Normalize(normalization)+ending;
            string whole=text.TrimEnd('\r','\a','\n');
            assert(TechnicalTextContext.IsTechnical(text),
                "A complete JSON-array paragraph retains literal data without normalizing Unicode, whitespace or Word endings");
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var issues=Checker.Check([new(Cp,text)],profile);
                assert(!issues.Any(Mechanical) && issues.Any(i=>!i.CanFix && !i.BatchSafe && i.Start==Cp &&
                    i.Length==whole.Length && i.Original==whole && i.Replacement==whole),
                    "The actual checker retains an exact manual paragraph notice for spacing, repetition and demo-spelling candidates inside JSON values: "+profile+"/"+source);
            }
        }

        foreach(string text in new[]{"[]","[1, true, false, null]","[\"Nội dung đã duyệt.\"]"})
            assert(TechnicalTextContext.IsTechnical(text) && Checker.Check([new(Cp,text)],DocumentProfile.Personal).Count==0,
                "Recognizing a valid JSON array without mechanical errors does not invent a manual warning");

        foreach(string text in new[]
        {
            "[Nội dung  báo cáo]",
            "[“Nội dung  báo cáo”]",
            "Nội dung [\"Báo  cáo\"] cần duyệt.",
            "[\"Nội dung  báo cáo\"] thêm nội dung.",
            "[\"Nội dung  báo cáo\",]",
            "[\"Nội dung  báo cáo\"; \"kế hoạch kế hoạch\"]",
            "[\"Nội dung  báo cáo\"",
            "[\"Nội dung  báo cáo\"] [\"kế hoạch kế hoạch\"]",
            "[\v\"Nội dung  báo cáo\"]",
            "[\u00a0\"Nội dung  báo cáo\"]"
        })
            assert(!TechnicalTextContext.IsTechnical(text) && Checker.Check([new(Cp,text)],DocumentProfile.Personal).Any(Mechanical),
                "Square-bracket prose, embedded quotations, malformed shallow arrays and non-JSON whitespace do not gain the complete-array exception");

        foreach(int depth in new[]{63,64,65,129,4096})
        {
            string text=new string('[',depth)+"\"Báo  cáo kế hoạch kế hoạch con đo\""+new string(']',depth);
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var current=Checker.Check([new(Cp,text)],profile);
                assert(TechnicalTextContext.IsTechnical(text) && current.Count==1 && !current.Any(Mechanical) &&
                    current[0].Original==text && current[0].Start==Cp,
                    "A valid deeply nested array cannot regain mechanical edit permission when streaming recognition reaches its depth budget");
            }
        }
        string uncertain=new string('[',65)+"\"Nội dung  báo cáo\",oops"+new string(']',65);
        assert(TechnicalTextContext.IsTechnical(uncertain) && !Checker.Check([new(Cp,uncertain)],DocumentProfile.Personal).Any(Mechanical),
            "After a deeply nested valid JSON prefix reaches the bounded reader budget, the unparsed remainder stays conservatively manual");

        foreach(var normalization in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            string old="(\"😀 Nội dung  báo cáo\")".Normalize(normalization);
            string current="[\"😀 Nội dung  báo cáo\"]".Normalize(normalization);
            var selected=Checker.Check([new(Cp,old)],profile).Where(Mechanical).ToArray();
            bool rejected=false;
            try{CurrentTextPlan.Validate(selected,Checker.Check([new(Cp,current)],profile));}
            catch(InvalidOperationException){rejected=true;}
            assert(selected.Length==1 && selected[0].Original=="  " &&
                current.Substring(selected[0].Start-Cp,selected[0].Length)==selected[0].Original && rejected,
                "A same-width switch from prose parentheses to a JSON array invalidates an unchanged old whitespace span before writing, including NFD and surrogate prefixes");
        }

        string guarded="[\"Nội dung  kế hoạch kế hoạch con đo\"]";
        var paragraph=new Paragraph(Cp,guarded,TextEditable:false);
        var blocked=Checker.Check([paragraph],DocumentProfile.Personal);
        assert(blocked.Count==1 && !blocked.Any(Mechanical) && blocked[0].Start==Cp && blocked[0].Original==guarded,
            "The JSON scope cannot bypass the independent XML permission gate or replace a structured region with a guessed substring");

        var neighbors=new[]{new Paragraph(17,"Nội dung đầu đã  duyệt."),new Paragraph(Cp,guarded),new Paragraph(2071,"Nội dung cuối đã  duyệt.")};
        var edits=Checker.Check(neighbors,DocumentProfile.Personal).Where(Mechanical).ToArray();
        assert(edits.Length==2 && edits[0].Start<Cp && edits[1].Start>=2071 && edits.All(i=>i.Original=="  " && i.Replacement==" "),
            "Independent prose before and after a literal array keeps its actual repair anchors while JSON values stay manual");
    }
}
