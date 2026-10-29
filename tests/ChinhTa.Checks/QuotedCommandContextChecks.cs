using System.Text;
using ChinhTa.Core;

public static class QuotedCommandContextChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const int cp=83;
        const string value="😀 Báo  cáo kế hoạch kế hoạch con đo";
        var commands=new List<string>();
        foreach(char quote in new[]{'"','\''})
        {
            foreach(string command in new[]{"echo","ECHO","printf","Write-Output","Write-Host","cmd /c echo","CMD\t/C\tprintf"})
                commands.Add(command+" "+quote+value+quote);
            commands.Add("printf '%s' "+quote+value+quote);
            foreach(string option in new[]{"--label","--file_name","--message-text","--Name2"})
            foreach(string separator in new[]{" ","\t","="})
                commands.Add("tool "+option+separator+quote+value+quote);
            commands.Add("--label "+quote+value+quote);
        }
        foreach(string source in commands)
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            string text=source.Normalize(form)+"\r\a";
            string whole=text.TrimEnd('\r','\a','\n');
            assert(TechnicalTextContext.IsTechnical(text),
                "A quoted long-option value or anchored known shell-output command retains explicit technical scope without normalizing its source");
            var direct=RepeatedTextRules.Check(new(cp,text));
            assert(direct is [{CanFix:false,Start:cp}] && direct[0].Original==whole && direct[0].Replacement==whole,
                "Repeated literal words in the actual command remain one source-exact manual paragraph notice");
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var found=Checker.Check([new(cp,text)],profile);
                assert(found.Any(i=>!i.CanFix && i.Start==cp && i.Original==whole && i.Replacement==whole) &&
                    !found.Any(i=>i.CanFix && !i.IsStyleOnly && i.Original!=i.Replacement),
                    "Every profile preserves significant command-value spaces, repetitions, emoji and raw NFC/NFD bytes at the nonzero source CP");
            }
            assert(!Checker.Check([new(cp,text,TextEditable:false)]).Any(i=>i.CanFix),
                "Command recognition cannot bypass the independently required structured-text permission");
        }
        foreach(string text in new[]{
            "Nội dung -- phần \"Báo  cáo\" cần duyệt.",
            "Nội dung -- nhãn 'kế hoạch kế hoạch' cần duyệt.",
            "Nội dung -label \"Báo  cáo\" cần duyệt.",
            "Theo mẫu \"kế hoạch kế hoạch\" cần duyệt.",
            "Nội dung echo \"Báo  cáo\" cần duyệt.",
            "Nội dung Write-Output \"Báo  cáo\" cần duyệt.",
            "Mẫu --label báo cáo kế hoạch kế hoạch.",
            "Mẫu ---label \"Báo  cáo\" cần duyệt.",
            "Mẫu --label: \"Báo  cáo\" cần duyệt.",
            "Chọn công tác ngày/đêm kế hoạch kế hoạch."})
        {
            assert(!TechnicalTextContext.IsTechnical(text),
                "Prose dashes, arbitrary quoted prose, embedded output-command words and unquoted options do not acquire command-literal ownership");
            foreach(var profile in Enum.GetValues<DocumentProfile>())
                assert(Checker.Check([new(cp,text)],profile).Any(i=>i.CanFix && !i.IsStyleOnly && i.Original!=i.Replacement),
                    "Ordinary prose outside the explicit command grammar retains its existing reviewed spacing or repetition correction");
        }
        const string prose="Nội dung  báo cáo.";
        var pending=Checker.Check([new(cp,prose)],DocumentProfile.Personal).Where(i=>i.CanFix).ToArray();
        foreach(string current in new[]{prose+" tool --label \"mẫu\"",prose+" tool --label='mẫu'"})
        {
            bool rejected=false;
            try{CurrentTextPlan.Validate(pending,Checker.Check([new(cp,current)],DocumentProfile.Personal));}
            catch(InvalidOperationException){rejected=true;}
            assert(rejected && current.Substring(pending.Single().Start-cp,pending[0].Length)==pending[0].Original,
                "An unchanged old spacing span loses write permission when its current paragraph acquires an explicit quoted command option");
        }
        const string annotation="Ghi: \"Nội dung  báo cáo\"",outputCommand="echo \"Nội dung  báo cáo\"";
        var oldAnnotation=Checker.Check([new(cp,annotation)],DocumentProfile.Personal).Where(i=>i.CanFix).ToArray();
        bool commandRejected=false;
        try{CurrentTextPlan.Validate(oldAnnotation,Checker.Check([new(cp,outputCommand)],DocumentProfile.Personal));}
        catch(InvalidOperationException){commandRejected=true;}
        assert(oldAnnotation.Length==1 && commandRejected &&
            outputCommand.Substring(oldAnnotation[0].Start-cp,oldAnnotation[0].Length)==oldAnnotation[0].Original,
            "An unchanged gap at the same CP cannot replay after an equally wide prose prefix becomes a recognized output command");
        foreach(string text in new[]{"echo \"Báo cáo\"","printf '%s' 'nội dung'","Write-Host 'nội dung'","tool --label=\"nội dung\""})
            assert(TechnicalTextContext.IsTechnical(text) && Checker.Check([new(cp,text)]).Count==0,
                "A command without an actual mechanical text proposal does not invent an advisory");
    }
}
