using ChinhTa.Core;

public static class RepairDependencyChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var font=new Issue(0,7,"Báo cáo","Báo cáo","",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var upper=font with {Kind=FixKind.DocumentTypeText,CharacterFormat=null,Replacement="BÁO CÁO"};
        var unrelated=new Issue(90,2,"  "," ","",false);
        var line=new Issue(20,8,"Nội dung","Nội dung","",false,FixKind.ShapeLine,LineFix:new(null,20,40,10,LineRole.Subject),Prerequisites:[font,upper]);
        var available=new[]{line,font,upper,unrelated};var selected=RepairDependencies.Expand([line],available);
        assert(selected.Count==3 && selected.Contains(font) && selected.Contains(upper) && !selected.Contains(unrelated),
            "Selecting a deferred Line expands only its exact current text/font prerequisites, not unrelated repairable findings");
        assert(StagedRepairBatch.Prepare(selected).Count==3 && selected.Count(i=>i.LineFix is not null)==1,
            "A complete dependency selection forms a valid text-to-Line batch despite shared title font/case spans");
        var second=line with {Start=40,LineFix=line.LineFix! with {Role=LineRole.Agency}};
        assert(RepairDependencies.Expand([line,second,font],[..available,second]).Count==4,
            "Two Line roles share a required font/case repair only once");
        bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
        assert(Reject(()=>StagedRepairBatch.Prepare([line])) && Reject(()=>StagedRepairBatch.Prepare([line,font])),
            "The Word-facing batch rejects missing prerequisites instead of silently applying unapproved extra edits");
        foreach(var items in new[]{new[]{line,upper},new[]{line,font with {CanFix=false},upper},
            new[]{line,font with {CharacterFormat=new(14,false,true)},upper},new[]{line,font,font,upper}})
            assert(Reject(()=>RepairDependencies.Expand([line],items)),"Missing/manual/changed/duplicate current prerequisites stop expansion before any writes");
        foreach(var malformed in new[]{line with {Prerequisites=[line]},line with {Prerequisites=[font with {Prerequisites=[upper]}]},
            font with {Prerequisites=[upper]}})
            assert(Reject(()=>StagedRepairBatch.Prepare([malformed,font,upper])),"Cycles, nested prerequisites and a non-Line dependency owner fail closed");
        var events=new List<string>();int restored=0,committed=0;double actualWidth=0;
        var requests=selected.Select(i=>new DeferredRepair(i,i.LineFix is null?i.Replacement:i.Original)).ToArray();
        var result=StagedRepairBatch.Apply(requests,stage=>events.Add("refresh "+stage),request=>
            request.Requested with {Prerequisites=null,LineFix=request.Requested.LineFix! with {Width=100}},
            plan=>{events.Add("apply "+StagedRepairBatch.Stage(plan[0]));if(plan[0].LineFix is not null)actualWidth=plan[0].LineFix!.Width;},
            ()=>events.Add("verify"),()=>committed++,()=>restored++);
        assert(result.Applied==3 && actualWidth==100 && events.SequenceEqual(new[]{"apply Text","refresh Line","apply Line","verify"}) && committed==1 && restored==0,
            "Deferred geometry uses the freshly measured 100pt Line after text/font, never the provisional 10pt payload");
        events.Clear();committed=restored=0;
        assert(Reject(()=>StagedRepairBatch.Apply(requests,_=>{},request=>request.Requested,
            _=>events.Add("apply"),()=>{},()=>committed++,()=>restored++)) && restored==1 && committed==0 && events.Count==1,
            "A fresh Line that still has prerequisites cannot be written; earlier text/font work rolls back as one action");
        committed=restored=0;int refreshes=0;
        var satisfied=StagedRepairBatch.Apply(requests,_=>refreshes++,request=>null,_=>{},()=>{},()=>committed++,()=>restored++);
        assert(satisfied.AlreadySatisfied==1 && satisfied.Applied==2 && refreshes==1 && committed==1 && restored==0,
            "A positively verified Line already valid after font changes is counted satisfied without an unnecessary Line write");
        var sample=ComponentChecks.Sample();var lower=sample.Layout.Select(p=>p.Text.StartsWith("ĐƠN VỊ BAN HÀNH")?
            p with {Text=p.Text.ToLowerInvariant()}:p).ToArray();
        assert(ShapeLineRules.Identify(lower,DocumentProfile.State).Single(t=>t.Role==LineRole.Agency).Text.StartsWith("đơn vị"),
            "A unique bounded agency above Số remains a Line reference while its capitalization repair is pending");
        var number=lower.Single(p=>p.Text.StartsWith("Số:"));
        assert(!ShapeLineRules.Identify(lower.Append(number with {Start=number.Start+1}).ToArray(),DocumentProfile.State).Any(t=>t.Role==LineRole.Agency),
            "Duplicate number anchors cannot authorize a guessed agency Line");
        assert(!ShapeLineRules.Identify(lower.Where(p=>p!=number).ToArray(),DocumentProfile.State).Any(t=>t.Role==LineRole.Agency),
            "Lowercase agency recognition requires the bounded number structure rather than guessing from lowercase text alone");
        var tooltip=IssueTooltipCatalog.Describe(line);
        assert(tooltip.Text.Split('\n').Length==3 && tooltip.Action.Contains("Sửa kèm 2") && tooltip.Action.Contains("đo lại") &&
            !tooltip.Action.Contains("10 pt"),"A deferred Line tooltip describes prerequisites/re-measurement without advertising provisional geometry as final");
        var report=DevelopmentReport.Build("0.1.61",DateTimeOffset.UnixEpoch);
        assert(report.Contains("B05") && report.Contains("chọn kèm đúng") && report.Contains("bỏ đọc CP") &&
            DevelopmentReport.Count(CapabilityMode.Reviewable)==33,"The report describes dependency-aware repair and actual measurement without inflating the capability count");
    }
}
