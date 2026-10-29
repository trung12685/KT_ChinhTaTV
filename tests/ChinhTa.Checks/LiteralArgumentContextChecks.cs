using ChinhTa.Core;

public static class LiteralArgumentContextChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string literal="Báo  cáo kế hoạch kế hoạch con đo con đo";
        var calls=new List<string>();
        foreach(string prefix in new[]{"@","$","$@","@$"})
            calls.Add("Console.WriteLine("+prefix+'"'+literal+"\");");
        foreach(string prefix in new[]{"r","R","f","F","b","B","u","U","br","rb","fr","rf","Fr","rB"})
        foreach(char quote in new[]{'"','\''})
        {
            // Python byte literals require ASCII source characters.
            string value=prefix.Contains('b',StringComparison.OrdinalIgnoreCase)?"Bao  cao ke hoach ke hoach":literal;
            calls.Add("print("+prefix+quote+value+quote+")");
        }
        calls.AddRange([
            "Console.WriteLine(42, \""+literal+"\");",
            "ghiNhan(maSo, \""+literal+"\")",
            "module.ghiNhan(duLieu.maSo, 42, r\""+literal+"\")",
            "ghiNhan(-12.5, +20, tenBien, f'"+literal+"')",
            "module.ghiNhan(\tmaSo,\t$@\""+literal+"\")\r\a",
            "😀 ghi\u0301Nhan(duLieu.maSo, u\"Ba\u0301o  ca\u0301o ta\u0300i lie\u0323\u0302u ta\u0300i lie\u0323\u0302u\")\r"
        ]);
        foreach(string text in calls)
        {
            string whole=text.TrimEnd('\r','\n','\a');
            assert(TechnicalTextContext.IsTechnical(text),
                "A call with an explicit C#/Python literal or quoted later simple argument retains technical scope");
            var direct=RepeatedTextRules.Check(new(83,text));
            assert(direct is [{CanFix:false,Start:83}] && direct[0].Original==whole && direct[0].Replacement==whole,
                "A repeated literal stays one source-exact manual paragraph notice without deleting its words");
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var issues=Checker.Check([new(83,text)],profile);
                assert(issues.Any(i=>!i.CanFix && i.Start==83 && i.Original==whole && i.Replacement==whole) &&
                    !issues.Any(i=>i.CanFix && !i.IsStyleOnly && i.Original!=i.Replacement),
                    "Every profile preserves literal spaces, repetitions, demo-spelling text, Unicode and Word endings");
            }
            assert(!Checker.Check([new(83,text,TextEditable:false)]).Any(i=>i.CanFix),
                "Literal recognition cannot bypass the independently required structured-text permission");
        }
        foreach(string text in new[]{"Cần duyệt (\"kế hoạch kế hoạch\") hôm nay.",
            "Nội dung (\"Báo  cáo\") cần duyệt.","Theo mẫu ( r\"Báo  cáo\" ) cần duyệt.",
            "Theo mẫu (42, \"Báo  cáo\") cần duyệt.","Nội dung kế hoạch kế hoạch đã duyệt.",
            "Nội dung / ta / tài liệu tài liệu đã duyệt.","Công tác ngày/đêm kế hoạch kế hoạch đã xong."})
        {
            assert(!TechnicalTextContext.IsTechnical(text),
                "Spaced prose parentheses, quotations and prose slashes do not acquire function-call ownership");
            foreach(var profile in Enum.GetValues<DocumentProfile>())
                assert(Checker.Check([new(83,text)],profile).Any(i=>i.CanFix && !i.IsStyleOnly && i.Original!=i.Replacement),
                    "Independent ordinary prose keeps its existing reviewed spacing or repetition correction");
        }
        const string prose="Nội dung  báo cáo.";
        var selected=Checker.Check([new(19,prose)],DocumentProfile.Personal).Where(i=>i.CanFix).ToArray();
        foreach(string suffix in new[]{" print(r\"mẫu\")"," Console.WriteLine(@\"mẫu\")",
            " ghiNhan(42, \"mẫu\")"," ghiNhan(maSo, f'mẫu')"})
        {
            bool rejected=false;
            try{CurrentTextPlan.Validate(selected,Checker.Check([new(19,prose+suffix)],DocumentProfile.Personal));}
            catch(InvalidOperationException){rejected=true;}
            assert(rejected,"An unchanged old spacing span loses write permission when its paragraph acquires an explicit literal call");
        }
        foreach(string literalOnly in new[]{"print(r\"Báo cáo\")","Console.WriteLine(42, @\"mẫu\");"})
            assert(TechnicalTextContext.IsTechnical(literalOnly) && Checker.Check([new(0,literalOnly)]).Count==0,
                "A literal without a real text proposal does not invent an advisory or repetition warning");
    }
}
