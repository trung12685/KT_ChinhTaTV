using ChinhTa.Core;

public static class LinePlacementFeedbackChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue Make(LineRole role,double x,double y,double width)=>new((int)role,1,"x","x","",false,FixKind.ShapeLine,
            LineFix:new(null,x,y,width,role));
        var plan=new[]{Make(LineRole.Agency,145.17,92.68,70.90)};
        var corrected=LinePlacementFeedback.Correct(plan,[],[new(230.20,149.35,70.85)])!;
        assert(corrected.Count==1 && Math.Abs(corrected[0].LineFix!.Left-60.14)<.001 &&
            Math.Abs(corrected[0].LineFix!.Top-36.01)<.001 && corrected[0].LineFix!.Width==70.90,
            "Measured 30/20mm drift is compensated from XPS, without changing intended width");
        var f=corrected[0].LineFix!;
        assert(RenderedLineVerifier.Find(new(145.17,92.68,70.90),[new(f.Left+85.03,f.Top+56.67,70.85)]) is not null,
            "Margin-shift renderer reaches the original page target after one correction");
        assert(LinePlacementFeedback.Correct(plan,[],[]) is null &&
            LinePlacementFeedback.Correct(plan,[],[new(230,149,100)]) is null,
            "No correction for missing stroke or incorrect length");
        assert(LinePlacementFeedback.Correct(plan,[new(230.20,149.35,70.85)],[new(230.20,149.35,70.85)]) is null,
            "An unchanged existing border cannot count as a newly rendered Line");
        assert(LinePlacementFeedback.Correct(plan,[],[new(230.20,149.35,70.85),new(231,150,70.90)]) is null,
            "Ambiguous or extra new strokes block correction");
        assert(LinePlacementFeedback.Correct(plan,[],[new(double.NaN,149,70.90)]) is null,
            "Non-finite render evidence blocks correction");
        var multi=new[]{Make(LineRole.Subject,150,200,80),Make(LineRole.Agency,100,90,80),Make(LineRole.Motto,300,120,210)};
        var adjusted=LinePlacementFeedback.Correct(multi,[],[new(150,256,80),new(300,176,210),new(100,146,80)])!;
        assert(adjusted.Select(i=>i.LineFix!.Role).SequenceEqual(new[]{LineRole.Agency,LineRole.Motto,LineRole.Subject}),
            "Pair unique vertical order even when plan and XPS ordering differ");
        var sameY=new[]{Make(LineRole.Agency,100,90,80),Make(LineRole.Motto,300,90,210)};
        assert(LinePlacementFeedback.Correct(sameY,[],[new(100,146,80),new(300,146,210)]) is null,
            "Same-height Lines have no unambiguous vertical mapping");
        var intent=new LineGeometry(145.17,92.68,70.90);
        assert(LinePageIntent.Read(LinePageIntent.Format(intent))==intent && LinePageIntent.Read("User title") is null,
            "Intended page geometry survives metadata round trip without trusting arbitrary titles");
        bool invalid=false;
        try{LinePageIntent.Read("ChinhTaViet.PageLine.v1;NaN;0;70");}catch(InvalidDataException){invalid=true;}
        assert(invalid,"Malformed reserved intent metadata fails closed");
        var motto=new[]{Make(LineRole.Motto,333.67,93.76,175.94)};
        var existing=new[]{new LineGeometry(145.15,92.70,70.85),new LineGeometry(254.95,206.45,92.25)};
        var frame=new LineAnchorFrame(276.2,56.67,290.55,104,595.35);
        var hidden=LinePlacementFeedback.CorrectHiddenMotto(motto,existing,existing,frame)!;
        assert(hidden.Count==1 && Math.Abs(hidden[0].LineFix!.Left-57.47)<.001 && Math.Abs(hidden[0].LineFix!.Top-37.09)<.001,
            "Invisible right-column Motto uses measured cell origin when its initial point lies off page");
        var moved=hidden[0].LineFix!;
        assert(RenderedLineVerifier.Find(new(333.67,93.76,175.94),[new(moved.Left+frame.Left,moved.Top+frame.Top,moved.Width)]) is not null,
            "Cell-origin renderer reaches the original Motto target after the bounded correction");
        assert(LinePlacementFeedback.CorrectHiddenMotto(motto,existing,[existing[0]],frame) is null,
            "Missing old Line prevents hidden-Motto correction");
        assert(LinePlacementFeedback.CorrectHiddenMotto(motto,existing,[..existing,new(600,150,175.94)],frame) is null,
            "Extra visible stroke prevents treating a Motto as invisible");
        assert(LinePlacementFeedback.CorrectHiddenMotto(plan,existing,existing,frame) is null,
            "Hidden-Motto fallback does not change Agency behavior");
        assert(LinePlacementFeedback.CorrectHiddenMotto(motto,existing,existing,null) is null &&
            LinePlacementFeedback.CorrectHiddenMotto(motto,existing,existing,frame with {Width=600}) is null,
            "No hidden-Motto correction without a verified table frame");
        assert(LinePlacementFeedback.CorrectHiddenMotto(motto,existing,existing,frame with {PageWidth=1000}) is null,
            "No hidden-Motto correction when shifted starting point is still on page");
        assert(LinePlacementFeedback.CorrectHiddenMotto([motto[0] with{LineFix=motto[0].LineFix! with{ShapeId=2}}],existing,existing,frame) is null,
            "Hidden-Motto fallback cannot replace an existing shape");
        assert(LinePlacementFeedback.CorrectHiddenMotto(motto,[existing[0],existing[0]],[existing[0],existing[0]],frame) is null,
            "Ambiguous overlapping baseline strokes block the hidden-Motto fallback");
        string folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());Directory.CreateDirectory(folder);
        try
        {
            string current=Path.Combine(folder,"current"),candidate=Path.Combine(folder,"candidate");
            File.WriteAllText(current,"original");File.WriteAllText(candidate,"wrong");int opens=0;
            for(int attempt=0;attempt<2;attempt++)
            {
                try{CheckpointSwap.Apply(current,candidate,()=>{},()=>{if(++opens%2==1)throw new RenderedLineMismatchException([new(230.20,149.35,70.85)]);});}
                catch(RenderedLineMismatchException){}
            }
            assert(opens==4 && File.ReadAllText(current)=="original","A failed second verification restores the original after both attempts");
        }
        finally{Directory.Delete(folder,true);}
    }
}
