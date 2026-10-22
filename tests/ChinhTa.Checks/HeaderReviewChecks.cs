using ChinhTa.Core;

public static class HeaderReviewChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var frame=new LineAnchorFrame(100,40,120,100,600);
        var target=new AdministrativePositionTarget(AdministrativePositionRole.Number,new(80,"Số:        /BC-ABC"),
            [new(10,"AGENCY TEST"),new(30,"UNIT TEST")],true);
        var number=new AdministrativeCenterEvidence(target,new(125,100,90,14,111),
            [new(110,50,100,14,61),new(130,70,60,14,81)],frame,[frame,frame]);
        var date=number with {Target=new(AdministrativePositionRole.Date,new(180,"Địa danh, ngày        tháng 10 năm 2026"),
            [new(110,"NATIONAL HEADING"),new(130,"MOTTO TEST")],true)};
        var centeredNumber=number with {Row=number.Row! with {Left=115}};
        var centeredDate=date with {Row=date.Row! with {Left=115}};
        AdministrativeCenterEvidence[] before=[number,date];
        bool Accept(AdministrativeCenterEvidence other)=>AdministrativeCentering.VerifyBatch(before,[centeredNumber,other],[AdministrativePositionRole.Number]);
        assert(Accept(date),"Repairing one row preserves the unselected row, even when its original offset remains a real warning");
        var collateral=date with {Row=date.Row! with {Baseline=112}};
        assert(AdministrativeCentering.Verify(number,centeredNumber) && !Accept(collateral),
            "Reproduced selected-only verification gap: a corrected number cannot conceal a moved unselected date baseline");
        foreach(var changed in new[]{date with {Row=date.Row! with {Left=126}},date with {Row=date.Row! with {Top=101}},
            date with {Row=date.Row! with {Width=89}},date with {Row=null},date with {Frame=null},
            date with {References=[date.References[0],null]},date with {ReferenceFrames=[frame,null]},
            date with {Target=date.Target with {ReferencesCentered=false}},
            date with {Target=date.Target with {Row=date.Target.Row with {Start=181}}},
            date with {Target=date.Target with {References=[date.Target.References[1],date.Target.References[0]]}},
            date with {References=[date.References[0]! with {Left=111},date.References[1]]},
            date with {ReferenceFrames=[frame,frame with {Width=121}]}})
            assert(!Accept(changed),"Unselected row geometry, bindings and available measurement evidence must survive a selected repair");
        assert(AdministrativeCentering.VerifyBatch(before,[centeredDate,centeredNumber],
            [AdministrativePositionRole.Number,AdministrativePositionRole.Date]),"A two-row batch accepts both real corrected centers independent of enumeration order");
        assert(!AdministrativeCentering.VerifyBatch(before,[centeredNumber],[AdministrativePositionRole.Number]) &&
            !AdministrativeCentering.VerifyBatch(before,[centeredNumber,date,date],[AdministrativePositionRole.Number]),
            "Disappearing or duplicate targets cannot turn a post-save review into a false pass");
        assert(!AdministrativeCentering.VerifyBatch(before,[centeredNumber,date],[]) &&
            !AdministrativeCentering.VerifyBatch(before,[centeredNumber,date],[AdministrativePositionRole.Number,AdministrativePositionRole.Number]) &&
            !AdministrativeCentering.VerifyBatch(before,[centeredNumber,date],[(AdministrativePositionRole)99]),
            "Empty, duplicate or invalid role selections cannot claim verified repair");
        var unknown=date with {Row=null,Frame=null,References=[null,null],ReferenceFrames=[null,null]};
        assert(AdministrativeCentering.VerifyBatch([number,unknown],[centeredNumber,unknown],[AdministrativePositionRole.Number]) &&
            !AdministrativeCentering.VerifyBatch([number,unknown],[centeredNumber,unknown],[AdministrativePositionRole.Date]),
            "An unchanged unmeasured unselected row remains unknown and can never prove a selected repair");
        assert(AdministrativeCentering.VerifyRestored(before,before) && !AdministrativeCentering.VerifyRestored(before,[centeredNumber,date]) &&
            !AdministrativeCentering.VerifyRestored(before,[number,collateral]),
            "Rollback verification requires both rows to return to their original measured geometry, including the unselected row");
        int restores=0;bool rollback=false;
        var item=target.Probe with {CanFix=true,CenterFix=new(target.Role)};
        try
        {
            AtomicTextBatch.Apply([item],i=>i.Original,i=>i.Replacement,()=>
            {
                if(!Accept(collateral))throw new InvalidOperationException("collateral geometry change");
            },()=>restores++);
        }
        catch(InvalidOperationException){rollback=true;}
        assert(rollback && restores==1,"A change in an unselected row rejects the commit and restores the complete position action once");

        const string original="Nội dung  thử.\r",expected="Nội dung thử.\r";
        var issue=Checker.Check([new(0,original)],DocumentProfile.Personal).Single();
        string current=original;var undo=new Stack<string>();int writes=0,scans=0;
        void Apply()=>AtomicTextBatch.Apply([issue],i=>current.Substring(i.Start,i.Length),i=>
        {
            writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;
        },()=>{if(current!=expected)throw new InvalidOperationException("wrong story");undo.Push(original);},()=>current=original);
        var exportFailure=new IOException("synthetic XPS export failure");bool scanFailed=false;
        try{EditThenScan.Run(Apply,()=>{scans++;throw exportFailure;});}
        catch(ScanAfterEditException error){scanFailed=ReferenceEquals(error.InnerException,exportFailure);}
        assert(scanFailed && writes==1 && scans==1 && current==expected && undo.Count==1,
            "An XPS review failure after a committed edit retains one edit and one Undo, with no silent rollback or replay");
        bool staleRejected=false;
        try{EditThenScan.Run(Apply,()=>scans++);}
        catch(InvalidOperationException error){staleRejected=error is not ScanAfterEditException;}
        assert(staleRejected && writes==1 && scans==1 && current==expected && undo.Count==1,
            "Reusing a stale selection after refresh failure is rejected before any second write or scan");
        int remaining=-1;
        EditThenScan.Run(()=>{},()=>{remaining=Checker.Check([new(0,current)],DocumentProfile.Personal).Count;scans++;});
        assert(remaining==0 && writes==1 && undo.Count==1,"A fresh review removes the corrected spacing warning without applying the edit again");
        var editFailure=new IOException("synthetic edit failure");bool unchangedError=false;
        try{EditThenScan.Run(()=>throw editFailure,()=>scans++);}
        catch(IOException error){unchangedError=ReferenceEquals(error,editFailure);}
        assert(unchangedError && scans==2,"An edit failure keeps the original error and cannot run the post-edit scanner");
    }
}
