using ChinhTa.Core;

public static class AtomicTextBatchChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string original="Số: 3/BC-ABC\rHà Nội, ngày 4 tháng 1 năm 2026\rNội dung giữ nguyên.\r";
        int day=original.IndexOf("4 tháng",StringComparison.Ordinal),month=original.IndexOf("1 năm",StringComparison.Ordinal);
        Issue[] selected=[new(4,1,"3","03","",false,FixKind.AdministrativeNumber),
            new(day,1,"4","04","",false,FixKind.AdministrativeDate),new(month,1,"1","01","",false,FixKind.AdministrativeDate)];
        string expected=original.Replace("3/BC","03/BC",StringComparison.Ordinal).Replace("ngày 4","ngày 04",StringComparison.Ordinal)
            .Replace("tháng 1 năm","tháng 01 năm",StringComparison.Ordinal);
        assert(AtomicTextBatch.ExpectedStory(selected,original,i=>original[..i.Start])==expected,
            "Expected text changes only selected number/date spans and preserves all other text");
        var shifted=selected.Select(i=>i with {Start=i.Start+70}).ToArray();
        assert(AtomicTextBatch.ExpectedStory(shifted,original,i=>original[..(i.Start-70)])==expected,
            "Expected text uses the actual Word prefix rather than treating a CP as a string index");
        bool BadMap(Func<Issue,string> prefix)
        {
            try{AtomicTextBatch.ExpectedStory(selected,original,prefix);return false;}
            catch(InvalidOperationException){return true;}
        }
        assert(BadMap(_=>"unrelated"),"An unmatched prefix rejects the action before writing");
        Issue[] same=[selected[0],selected[0] with {Start=100}];
        bool duplicate=false;
        try{AtomicTextBatch.ExpectedStory(same,original,_=>original[..4]);}catch(InvalidOperationException){duplicate=true;}
        assert(duplicate,"Two distinct Word positions cannot collapse into one mapped string edit");
        bool reversed=false;
        try{AtomicTextBatch.ExpectedStory([new(0,1,"a","A","",false),new(1,1,"a","B","",false)],"aa",i=>i.Start==0?"a":"");}
        catch(InvalidOperationException){reversed=true;}
        assert(reversed,"Mapped prefixes must preserve Word range order even when the source characters are identical");
        string current=original;int writes=0,saves=0,restores=0;
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i)
        {
            writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;
        }
        void Restore(){restores++;current=original;}
        AtomicTextBatch.Apply(selected,Read,Write,()=>{saves++;if(current!=expected)throw new Exception("Unexpected final text");},Restore);
        assert(current==expected && writes==3 && saves==1 && restores==0,"Successful grouped text edit commits once in descending position order");
        foreach(int failAfter in new[]{1,2,3})
        {
            current=original;writes=saves=restores=0;bool failed=false;
            var injected=new IOException("injected write failure");
            try{AtomicTextBatch.Apply(selected,Read,i=>{var result=Write(i);if(writes==failAfter)throw injected;return result;},()=>saves++,Restore);}
            catch(IOException e){failed=ReferenceEquals(e,injected);}
            assert(failed && current==original && writes==failAfter && saves==0 && restores==1,
                "A failure after a partial/final write restores the whole selection once and preserves the actual error");
        }
        foreach(bool mismatch in new[]{false,true})
        {
            current=original;writes=saves=restores=0;bool failed=false;
            try
            {
                AtomicTextBatch.Apply(selected,Read,Write,()=>
                {
                    saves++;
                    if(!mismatch)throw new IOException("save failed");
                    current+="unexpected unselected change";
                    if(current!=expected)throw new InvalidOperationException("post-save verification failed");
                },Restore);
            }
            catch(Exception e) when(e is IOException or InvalidOperationException){failed=true;}
            assert(failed && current==original && writes==3 && saves==1 && restores==1,
                "Save failure or an unselected post-save text change rolls back every completed edit");
        }
        current=original;writes=saves=restores=0;bool wrongResult=false;
        try{AtomicTextBatch.Apply(selected,Read,i=>{Write(i);return "wrong observed text";},()=>saves++,Restore);}
        catch(InvalidOperationException){wrongResult=true;}
        assert(wrongResult && writes==1 && restores==1 && saves==0 && current==original,
            "A setter that does not yield the selected replacement cannot be reported as success");
        foreach(var unsafeSelection in new[]{new[]{selected[0] with {Original="x"}},new[]{selected[0] with {CanFix=false}},
            new[]{selected[0],selected[0] with {Length=2,Original="3/"}},
            new[]{selected[0] with {Kind=FixKind.ShapeLine,LineFix=new(null,10,10,20,LineRole.Agency)}}})
        {
            current=original;writes=saves=restores=0;bool rejected=false;
            try{AtomicTextBatch.Apply(unsafeSelection,Read,Write,()=>saves++,Restore);}
            catch(InvalidOperationException){rejected=true;}
            assert(rejected && current==original && writes==0 && saves==0 && restores==0,
                "Stale, manual, overlapping or Shape selections fail before mutation and require no rollback");
        }
        current=original;writes=saves=restores=0;int reads=0;bool changedDuringBatch=false;
        try{AtomicTextBatch.Apply(selected,i=>++reads>selected.Length?"changed":Read(i),Write,()=>saves++,Restore);}
        catch(InvalidOperationException){changedDuringBatch=true;}
        assert(changedDuringBatch && writes==0 && restores==1 && current==original,
            "A source change after preflight stops the batch and restores the checkpoint");
        current=original;writes=saves=restores=0;bool aggregate=false;
        var writeFailure=new IOException("write failed");var restoreFailure=new IOException("restore failed");
        try{AtomicTextBatch.Apply(selected,Read,i=>{Write(i);throw writeFailure;},()=>saves++,()=>{restores++;throw restoreFailure;});}
        catch(AggregateException e){aggregate=e.InnerExceptions.Count==2 && ReferenceEquals(e.InnerExceptions[0],writeFailure) && ReferenceEquals(e.InnerExceptions[1],restoreFailure);}
        assert(aggregate && writes==1 && restores==1 && saves==0,"Rollback failure preserves both errors and never replays a write");
        writes=saves=restores=0;
        AtomicTextBatch.Apply([],Read,Write,()=>saves++,Restore);
        assert(writes==0 && saves==0 && restores==0,"An empty selection creates no save or rollback operation");
        string folder=Path.Combine(Path.GetTempPath(),"TextBatchChecks-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string working=Path.Combine(folder,"working.docx"),before=Path.Combine(folder,"before.docx");
            File.WriteAllText(working,original);File.Copy(working,before);
            current=original;bool saveFailed=false;
            try
            {
                AtomicTextBatch.Apply(selected,Read,i=>{var result=Write(i);File.WriteAllText(working,current);return result;},
                    ()=>throw new IOException("failed after partial file save"),()=>CheckpointSwap.Apply(working,before,()=>{},()=>current=File.ReadAllText(working)));
            }
            catch(IOException){saveFailed=true;}
            assert(saveFailed && current==original && File.ReadAllText(working)==original && File.ReadAllText(before)==original,
                "Text transaction and real checkpoint swap restore both persisted and in-memory state after a save failure");
            assert(!Directory.GetFiles(folder,"*.rollback.docx").Any(),"A verified text rollback cleans temporary swaps while retaining the original checkpoint");
        }
        finally{Directory.Delete(folder,true);}
    }
}
