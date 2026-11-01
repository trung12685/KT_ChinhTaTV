using ChinhTa.WordProtocol;

internal static class EvaluationChecks
{
    public static void Run()
    {
        int checks=0;void Assert(bool ok,string label){if(!ok)throw new Exception("FAIL "+label);checks++;Console.WriteLine("PASS "+label);}
        Case Row(string text,params Edit[] edits)=>new("fixture","fixture","developer_fixture",text,edits,[]);
        FindingDto Finding(int start,int length,string text,params string[] suggestions)=>new(){Offset=start,Length=length,Original=text,Category="Spelling",Suggestions=suggestions};
        var row=Row("😀 Hom qua",new Edit(3,3,"Hom","Hôm",["Hòm"]));EvaluationScoring.Validate(row);
        var f=Finding(3,3,"Hom","Hòm");
        Assert(EvaluationScoring.Correct(f,row.Edits[0],row,f.Suggestions),"Every source-accepted alternative is scored, with original UTF-16 emoji offsets");
        string decomposed="Hôm".Normalize(System.Text.NormalizationForm.FormD);
        Assert(EvaluationScoring.Correct(f,row.Edits[0],row,[decomposed]),"Canonically equivalent suggestions match without changing input offsets");
        row=Row("ab cd",new Edit(0,2,"ab","AB",["A"]),new Edit(3,2,"cd","CD",["C"]));
        f=Finding(0,5,"ab cd","A C");
        Assert(row.Edits.All(e=>EvaluationScoring.Correct(f,e,row,f.Suggestions)),"A phrase suggestion must satisfy an accepted combination for every covered error");
        Assert(!EvaluationScoring.Correct(f,row.Edits[0],row,["A cd"]),"Correcting one word while leaving another covered error is not a correct phrase answer");
        f=Finding(1,4,"b cd","B CD");
        Assert(!EvaluationScoring.Correct(f,row.Edits[1],row,f.Suggestions),"A phrase crossing only part of an adjacent error cannot receive correction credit");
        row=Row("thừa",new Edit(0,4,"thừa",""));f=Finding(0,4,"thừa","");
        Assert(EvaluationScoring.Correct(f,row.Edits[0],row,f.Suggestions),"An explicitly accepted deletion is preserved as an empty-string answer");
        row=Row("Hom qua",new Edit(0,3,"Hom","Hôm")) with{CorrectSpans=[new(4,3,"qua")]};
        var measured=EvaluationScoring.Measure(row,[Finding(0,3,"Hom","Hôm"),Finding(4,3,"qua")]);
        Assert(measured.Targets.Single().Hit && measured.MatchedWarnings==1 && measured.ExtraWarnings==1 && measured.WarningsOnCorrectSpans==1 && measured.CorrectTokens==1,"Label agreement counts wrong targets and source-correct token warnings separately");
        measured=EvaluationScoring.Measure(row,[]);
        Assert(!measured.Targets.Single().Hit && measured.CorrectTokens==1,"Abstaining produces a missed target, not inflated recall");
        foreach(var bad in new[]{Row("x",new Edit(0,1,"y","z")),Row("abc",new Edit(0,2,"ab","x"),new Edit(1,2,"bc","y")),
            Row("abc",new Edit(0,1,"a","x")) with{CorrectSpans=[new(0,3,"abc")]},
            Row("abc") with{CorrectSpans=[new(0,3,"abc"),new(0,3,"abc")]}})
        {bool rejected=false;try{EvaluationScoring.Validate(bad);}catch(InvalidOperationException){rejected=true;}Assert(rejected,"Unbound or overlapping annotations cannot inflate the evaluation denominator");}
        Console.WriteLine(checks+" evaluation scoring checks passed.");
    }
}
