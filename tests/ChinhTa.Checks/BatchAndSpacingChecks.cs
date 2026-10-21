using ChinhTa.Core;

public static class BatchAndSpacingChecks
{
    public static void Run(Action<bool,string> assert)
    {
        IReadOnlyList<Issue> Spaces(string s)=>Checker.Check([new(41,s)]).Where(i=>i.Kind==FixKind.Text && i.Replacement==" ").ToArray();
        assert(Spaces("Số:        /BC-ABCD\r").Count==0,"Blank document-number field preserves intentional spaces");
        assert(Spaces("Ninh Bình, ngày        tháng 10 năm 2026\r").Count==0,"Blank day in an administrative date preserves intentional spaces");
        assert(Spaces("ngày        tháng 10 năm 2026\r").Count==0,"Standalone blank-day date template is recognized");
        assert(Spaces("Số:  123/BC-ABCD\r").Count==1,"Filled document number still reports excess spaces");
        assert(Spaces("Ninh Bình, ngày  12 tháng 10 năm 2026\r").Count==1,"Filled day still reports excess spaces");
        assert(Spaces("Số:        /BC-ABCD  (bản sao)\r").Count==1,"Only blank-number field is exempt; other excess spaces remain errors");
        assert(Spaces("Ninh Bình, ngày        tháng  10 năm 2026\r").Count==1,"Only blank-day field is exempt; spacing at the month remains checked");
        assert(Spaces("Văn bản ghi ngày    tháng 10 năm 2026 trong nội dung.\r").Count==1,"Body-text date wording does not trigger the template exemption");
        assert(Spaces("Nội dung  thử nghiệm.\r").Count==1 && Spaces("Số:  /BC-ABCD  nội dung.\r")[0].Start==41+"Số:  /BC-ABCD".Length,
            "Ordinary excess spaces retain exact Word offsets after an exempt field");
        foreach(var role in new[]{LineRole.Agency,LineRole.Subject})
        {
            var target=new LineTarget(role,0,"x",0,2,true);
            var reference=new LineReference(100,100,210);
            var fix=ShapeLineRules.Evaluate(target,reference,null,null,LinePolicy.Decree30).Fix;
            assert(Math.Abs(fix.Width-90)<.001,$"{role} auto-created length is 3/7");
            assert(ShapeLineRules.Evaluate(target,reference,new(170,106,70),null,LinePolicy.Decree30).Errors.Length==0 &&
                ShapeLineRules.Evaluate(target,reference,new(152.5,106,105),null,LinePolicy.Decree30).Errors.Length==0,
                $"{role} accepts both 1/3 and 1/2 without forcing the creation target");
            assert(ShapeLineRules.Evaluate(target,reference,new(171,106,68),null,LinePolicy.Decree30).Errors.Any(e=>e.Contains("Độ dài")),
                $"{role} reports a line shorter than 1/3 outside measurement tolerance");
        }
        var subject=new LineTarget(LineRole.Subject,0,"x",0,2,true);
        var initialReference=new LineReference(190.375,143.78,221.4);
        var oldFix=ShapeLineRules.Evaluate(subject,initialReference,null,null,new(1d/3,.5,5d/12)).Fix;
        var oldGeometry=new LineGeometry(oldFix.Left,oldFix.Top,oldFix.Width);
        var finalReference=initialReference with {Width=228.5};
        var finalAssessment=ShapeLineRules.Evaluate(subject,finalReference,oldGeometry,null,LinePolicy.Decree30);
        assert(finalAssessment.Errors.Any(e=>e.Contains("lệch tâm")),"A widened final reference reproduces the reported 3.55pt residual center error");
        var originalIssue=subject.Probe with {CanFix=true,LineFix=oldFix};
        var desiredIssue=originalIssue with {LineFix=finalAssessment.Fix};
        var encoded=LinePlacementFeedback.Correct([originalIssue],[],[oldGeometry])!;
        var rebased=LinePlacementFeedback.Rebase(encoded,[originalIssue],[desiredIssue])[0].LineFix!;
        assert(ShapeLineRules.Evaluate(subject,finalReference,new(rebased.Left,rebased.Top,rebased.Width),null,LinePolicy.Decree30).Errors.Length==0 &&
            Math.Abs(rebased.Width-228.5*3/7)<.001,"Rebase uses the final reference and the requested 3/7 creation target before reporting success");
        var intended=ShapeLineRules.Evaluate(subject,finalReference,null,42,LinePolicy.Decree30).Fix;
        var tooClose=new LineGeometry(intended.Left,intended.Top-.75,intended.Width);
        var actualAssessment=RenderedLineVerifier.Assess(subject,finalReference,intended,[tooClose],LinePolicy.Decree30);
        assert(actualAssessment.Actual is not null && actualAssessment.Errors.Any(e=>e.Contains("Khoảng cách")) &&
            ShapeLineRules.Evaluate(subject,finalReference,new(intended.Left,intended.Top,intended.Width),42,LinePolicy.Decree30).Errors.Length==0,
            "A stroke within 1pt of the proposal can still violate the actual gap; verification must report it");
        assert(RenderedLineVerifier.Assess(subject,finalReference,intended,[],LinePolicy.Decree30).Errors.Length>0,
            "Rendered assessment cannot succeed without an actual stroke");
        var unrelated=new LineGeometry(30,70,50);
        var preserve=RenderedLineVerifier.RetainOtherStrokes([unrelated,tooClose],[tooClose]);
        var subpointIssue=originalIssue with {LineFix=intended};
        var correction=LinePlacementFeedback.Correct([subpointIssue],preserve,[unrelated,tooClose])![0].LineFix!;
        assert(preserve.SequenceEqual(new[]{unrelated}) && Math.Abs(correction.Top-intended.Top-.75)<.001,
            "Updating an owned stroke by 0.75pt excludes only that old stroke and permits measured feedback");
        assert(RenderedLineVerifier.RetainOtherStrokes([unrelated],[tooClose]).SequenceEqual(new[]{unrelated}),
            "Replacing an invisible owned Line must retain every visible baseline stroke");
        bool ambiguousBaseline=false;
        try{RenderedLineVerifier.RetainOtherStrokes([tooClose,tooClose],[tooClose]);}
        catch(InvalidOperationException){ambiguousBaseline=true;}
        assert(ambiguousBaseline,"Overlapping old strokes cannot be silently excluded from preservation checks");
        string folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());Directory.CreateDirectory(folder);
        try
        {
            string current=Path.Combine(folder,"current.docx"),before=Path.Combine(folder,"before.docx");
            File.WriteAllText(current,"original");File.Copy(current,before);
            Issue Make(LineRole role)=>new((int)role,1,"x","x","",false,FixKind.ShapeLine,LineFix:new(null,10,20,30,role));
            var plan=new[]{Make(LineRole.Motto),Make(LineRole.Subject),Make(LineRole.Agency)};
            var order=new List<LineRole>();int restores=0;
            AtomicLineBatch.Apply(current,before,plan,(item,step)=>
            {
                if(File.ReadAllText(step)!=File.ReadAllText(current))throw new Exception("stale step backup");
                order.Add(item.LineFix!.Role);File.AppendAllText(current,"/"+item.LineFix.Role);
            },()=>{restores++;File.Copy(before,current,true);});
            assert(order.SequenceEqual(new[]{LineRole.Agency,LineRole.Subject,LineRole.Motto}) && restores==0 &&
                File.ReadAllText(current)=="original/Agency/Subject/Motto","One grouped action applies each Line once in a supported order");
            assert(!Directory.GetFiles(folder,"*.step.docx").Any() && File.ReadAllText(before)=="original","Intermediate backups are cleaned while the single undo checkpoint remains intact");
            File.Copy(before,current,true);order.Clear();bool failed=false;
            try
            {
                AtomicLineBatch.Apply(current,before,plan,(item,_)=>
                {
                    order.Add(item.LineFix!.Role);File.AppendAllText(current,"/"+item.LineFix.Role);
                    if(item.LineFix.Role==LineRole.Motto)throw new InvalidOperationException("third Line failed");
                },()=>{restores++;File.Copy(before,current,true);});
            }
            catch(InvalidOperationException){failed=true;}
            assert(failed && restores==1 && order.Count==3 && File.ReadAllText(current)=="original",
                "Failure on the third Line restores the whole batch, including the first two");
            bool finalFailed=false;
            try{AtomicLineBatch.Apply(current,before,plan,(item,_)=>File.AppendAllText(current,"/"+item.LineFix!.Role),
                ()=>File.Copy(before,current,true),()=>throw new InvalidOperationException("final reference changed"));}
            catch(InvalidOperationException){finalFailed=true;}
            assert(finalFailed && File.ReadAllText(current)=="original","Final whole-group verification failure restores the original despite individually successful steps");
            File.Copy(before,current,true);order.Clear();int rescans=0;
            var repairs=plan.Where(i=>i.LineFix!.Role!=LineRole.Agency).Select(i=>i with {LineFix=i.LineFix! with {ShapeId=42}}).ToArray();
            AtomicLineBatch.Apply(current,before,plan,(item,_)=>
                {order.Add(item.LineFix!.Role);File.AppendAllText(current,"/"+item.LineFix.Role);},
                ()=>File.Copy(before,current,true),()=>{rescans++;if(order.Count!=5)throw new Exception("residual");},
                ()=>{rescans++;return repairs;});
            assert(order.SequenceEqual(new[]{LineRole.Agency,LineRole.Subject,LineRole.Motto,LineRole.Subject,LineRole.Motto}) && rescans==2,
                "Three-Line action can settle exactly the two owned residuals and verify them before returning");
            assert(File.ReadAllText(before)=="original" && !Directory.GetFiles(folder,"*.step.docx").Any(),
                "The settling pass retains the single original Undo checkpoint and cleans all step backups");
            File.Copy(before,current,true);int writes=0;bool unsettled=false;
            try{AtomicLineBatch.Apply(current,before,plan,(_,_)=>{writes++;File.AppendAllText(current,"/edit");},
                ()=>File.Copy(before,current,true),()=>throw new InvalidOperationException("still residual"),()=>repairs);}
            catch(InvalidOperationException){unsettled=true;}
            assert(unsettled && writes==5 && File.ReadAllText(current)=="original",
                "Persistent residuals stop after one settling pass and roll back the entire original action");
            foreach(var unsafeRepairs in new[]{new[]{repairs[0] with {Start=999}},new[]{repairs[0] with {CanFix=false}},
                new[]{repairs[0] with {LineFix=repairs[0].LineFix! with {ShapeId=null}}},new[]{repairs[0],repairs[0]}})
            {
                File.Copy(before,current,true);writes=0;bool blocked=false;
                try{AtomicLineBatch.Apply(current,before,plan,(_,_)=>{writes++;File.AppendAllText(current,"/edit");},
                    ()=>File.Copy(before,current,true),remainingIssues:()=>unsafeRepairs);}
                catch(InvalidOperationException){blocked=true;}
                assert(blocked && writes==3 && File.ReadAllText(current)=="original",
                    "Ambiguous, unselected, manual or unidentified residuals cannot trigger automatic edits");
            }
            bool aggregate=false;
            try{AtomicLineBatch.Apply(current,before,plan,(_,_)=>throw new IOException("write failed"),()=>throw new IOException("restore failed"));}
            catch(AggregateException e){aggregate=e.InnerExceptions.Count==2;}
            assert(aggregate && Directory.GetFiles(folder,"*.step.docx").Length==1 && File.Exists(before),
                "Rollback failure preserves original and step backups for recovery");
        }
        finally{Directory.Delete(folder,true);}
    }
}
