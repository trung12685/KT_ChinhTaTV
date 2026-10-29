using ChinhTa.Core;

public static class StagedDocumentOpenChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-open-transition-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        string current=Path.Combine(folder,"working.docx"),source=Path.Combine(folder,"incoming.docx"),undo=Path.Combine(folder,"undo.docx");
        int checkpoints=0,closes=0,opens=0;
        void Reset(bool previous=true)
        {
            foreach(string file in Directory.GetFiles(folder))File.Delete(file);
            if(previous)File.WriteAllText(current,"previous");
            File.WriteAllText(source,"incoming");File.WriteAllText(undo,"undo");checkpoints=closes=opens=0;
        }
        IReadOnlyList<CheckpointCleanup> Apply(string input,bool previous=true,Action? checkpoint=null,Action? close=null,Action? open=null,
            Action<Stream,Stream>? copy=null,Action<string>? delete=null)=>StagedDocumentOpen.Apply(current,input,[undo],previous,
                checkpoint??(()=>checkpoints++),close??(()=>closes++),open??(()=>opens++),copy,delete);
        StagedDocumentOpenException Failure(Action action)
        {try{action();}catch(StagedDocumentOpenException error){return error;}throw new Exception("Expected a failed open transition");}
        bool NoTemporary()=>Directory.GetFiles(folder,"*.open.docx").Length==0 && Directory.GetFiles(folder,"*.rollback.docx").Length==0;
        try
        {
            Reset();var missing=Failure(()=>Apply(Path.Combine(folder,"missing.docx")));
            assert(missing.CurrentIsVerified && missing.InnerException is FileNotFoundException && File.ReadAllText(current)=="previous" &&
                checkpoints==0 && closes==0 && opens==0 && NoTemporary(),
                "A missing incoming file does not checkpoint, close, replace or reopen the existing document");
            Reset();var directory=Failure(()=>Apply(folder));
            assert(directory.CurrentIsVerified && checkpoints==0 && closes==0 && opens==0 && File.ReadAllText(current)=="previous",
                "An incoming directory is rejected before any transition callback or previous-state mutation");
            foreach(string input in new[]{current,undo,Path.Combine(folder,".","working.docx")})
            {
                Reset();var own=Failure(()=>Apply(input));
                assert(own.CurrentIsVerified && checkpoints==0 && closes==0 && opens==0 && File.ReadAllText(current)=="previous" && File.ReadAllText(undo)=="undo",
                    "Working, Undo and normalized self paths cannot be reopened as incoming source or clobbered");
            }
            Reset();var partial=new IOException("incoming read failed after prefix");
            var failedCopy=Failure(()=>Apply(source,copy:(input,output)=>{output.WriteByte((byte)input.ReadByte());throw partial;}));
            assert(failedCopy.CurrentIsVerified && ReferenceEquals(failedCopy.InnerException,partial) && checkpoints==0 && closes==0 && opens==0 &&
                File.ReadAllText(current)=="previous" && File.ReadAllText(source)=="incoming" && NoTemporary(),
                "Partial incoming-copy failure leaves the old checkpoint and source bytes intact and never closes Word");
            Reset();var changedSource=Failure(()=>Apply(source,copy:(input,output)=>{input.CopyTo(output);File.WriteAllText(source,"concurrently changed");}));
            assert(changedSource.CurrentIsVerified && changedSource.InnerException is IOException && checkpoints==0 && closes==0 && opens==0 &&
                File.ReadAllText(current)=="previous" && NoTemporary(),
                "Concurrent incoming-source changes fail byte verification before touching the existing document");
            Reset();var success=Apply(source,checkpoint:()=>{checkpoints++;File.WriteAllText(current,"previous latest");},open:()=>
            {opens++;if(File.ReadAllText(current)!="incoming")throw new Exception("Wrong candidate");});
            assert(success.Count==0 && checkpoints==1 && closes==1 && opens==1 && File.ReadAllText(current)=="incoming" &&
                File.ReadAllText(source)=="incoming" && File.ReadAllText(undo)=="undo" && NoTemporary(),
                "A successful transition checkpoints the previous document, opens verified incoming bytes and cleans only its temporary files");
            Reset();var rejected=new IOException("Word rejected candidate");
            var restored=Failure(()=>Apply(source,checkpoint:()=>{checkpoints++;File.WriteAllText(current,"previous latest");},open:()=>
            {opens++;if(opens==1)throw rejected;if(File.ReadAllText(current)!="previous latest")throw new Exception("Wrong restore");}));
            assert(restored.CurrentIsVerified && ReferenceEquals(restored.InnerException,rejected) && closes==2 && opens==2 && checkpoints==1 &&
                File.ReadAllText(current)=="previous latest" && File.ReadAllText(source)=="incoming" && NoTemporary(),
                "Word rejection restores and reopens the latest previous checkpoint while preserving the incoming source");
            Reset();var aggregatePrimary=new AggregateException("ordinary Word-open aggregation",new IOException("one"),new IOException("two"));
            var aggregateRestored=Failure(()=>Apply(source,open:()=>{if(++opens==1)throw aggregatePrimary;}));
            assert(aggregateRestored.CurrentIsVerified && ReferenceEquals(aggregateRestored.InnerException,aggregatePrimary) &&
                closes==2 && opens==2 && File.ReadAllText(current)=="previous" && NoTemporary(),
                "An arbitrary AggregateException from candidate opening is still verified when the previous document actually reopens");
            Reset();var closeFailure=new IOException("old close rejected");
            var unchanged=Failure(()=>Apply(source,close:()=>{closes++;throw closeFailure;}));
            assert(unchanged.CurrentIsVerified && ReferenceEquals(unchanged.InnerException,closeFailure) && closes==1 && opens==0 &&
                File.ReadAllText(current)=="previous" && NoTemporary(),
                "A failed initial close cannot publish incoming bytes or consume the previous checkpoint");
            Reset();var restoreFailure=new IOException("previous reopen rejected");
            var uncertain=Failure(()=>Apply(source,open:()=>{throw ++opens==1?rejected:restoreFailure;}));
            string retained=Directory.GetFiles(folder,"*.rollback.docx").Single();
            assert(!uncertain.CurrentIsVerified && uncertain.InnerException is CheckpointRestoreException typed && typed.RollbackPath==retained &&
                File.ReadAllText(retained)=="previous" && opens==2 && closes==2,
                "A genuine previous-reopen failure marks recovery unverified and retains its exact previous-byte backup");
            Reset();var restoreClose=Failure(()=>Apply(source,close:()=>{if(++closes==2)throw closeFailure;},open:()=>{opens++;throw rejected;}));
            retained=Directory.GetFiles(folder,"*.rollback.docx").Single();
            assert(!restoreClose.CurrentIsVerified && File.ReadAllText(retained)=="previous" && File.ReadAllText(current)=="incoming" && opens==1 && closes==2,
                "Failure closing a rejected candidate keeps its backup and never pretends that old bytes were restored");
            Reset();var checkpointFailure=new IOException("cannot save latest old edits");
            var unsaved=Failure(()=>Apply(source,checkpoint:()=>{checkpoints++;throw checkpointFailure;}));
            assert(!unsaved.CurrentIsVerified && ReferenceEquals(unsaved.InnerException,checkpointFailure) && checkpoints==1 && closes==0 && opens==0 &&
                File.ReadAllText(current)=="previous" && NoTemporary(),
                "Unconfirmed latest edits block switching documents before close and do not get promoted to a verified checkpoint");
            Reset(false);var first=Apply(source,previous:false);
            assert(first.Count==0 && checkpoints==1 && closes==1 && opens==1 && File.ReadAllText(current)=="incoming" && NoTemporary(),
                "The first document opens from a verified private copy without manufacturing a previous backup");
            Reset(false);var firstRejected=Failure(()=>Apply(source,previous:false,open:()=>{opens++;throw rejected;}));
            assert(firstRejected.CurrentIsVerified && ReferenceEquals(firstRejected.InnerException,rejected) && !File.Exists(current) &&
                File.ReadAllText(source)=="incoming" && closes==2 && opens==1 && NoTemporary(),
                "A rejected first document is closed and removed without deleting or modifying its source");
            Reset(false);var firstUnclosed=Failure(()=>Apply(source,previous:false,close:()=>{if(++closes==2)throw closeFailure;},open:()=>{opens++;throw rejected;}));
            assert(!firstUnclosed.CurrentIsVerified && firstUnclosed.InnerException is AggregateException both && both.InnerExceptions.Count==2 &&
                File.ReadAllText(current)=="incoming" && File.ReadAllText(source)=="incoming" && closes==2 && opens==1,
                "A rejected first document that cannot be closed retains its recovery copy and both failure causes");
            Reset(false);File.WriteAllText(current,"unverified old recovery");var orphan=Failure(()=>Apply(source,previous:false));
            assert(!orphan.CurrentIsVerified && checkpoints==0 && closes==0 && opens==0 && File.ReadAllText(current)=="unverified old recovery",
                "An existing recovery file without a prior verified session is never overwritten by a first-document open");
            Reset();var warning=Apply(source,delete:_=>throw new UnauthorizedAccessException("cleanup denied"));
            assert(warning.Count==2 && warning.All(w=>File.Exists(w.RetainedPath)) && File.ReadAllText(current)=="incoming" && opens==1 && closes==1,
                "Backup/stage cleanup failures are returned separately after a successful verified open and do not replay the transition");
            Reset();var restoredWarnings=Failure(()=>Apply(source,open:()=>{if(++opens==1)throw rejected;},delete:_=>throw new IOException("cleanup locked")));
            assert(restoredWarnings.CurrentIsVerified && ReferenceEquals(restoredWarnings.InnerException,rejected) && restoredWarnings.CleanupWarnings.Count==2 &&
                restoredWarnings.CleanupWarnings.All(w=>File.Exists(w.RetainedPath)) && File.ReadAllText(current)=="previous" && opens==2,
                "Cleanup warnings cannot replace the primary Word error or hide a successful previous-document restoration");
            if(OperatingSystem.IsWindows())
            {
                Reset();using var held=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.None);
                var locked=Failure(()=>Apply(source));
                assert(locked.CurrentIsVerified && checkpoints==0 && closes==0 && opens==0 && File.ReadAllText(current)=="previous" && NoTemporary(),
                    "A real Windows read-denying source lock leaves the previous Word session untouched");
            }
            Reset();string link=Path.Combine(folder,"source-link.docx");
            try{File.CreateSymbolicLink(link,current);}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException or PlatformNotSupportedException){ }
            if(File.Exists(link))
            {
                var alias=Failure(()=>Apply(link));
                assert(alias.CurrentIsVerified && checkpoints==0 && closes==0 && opens==0 && File.ReadAllText(current)=="previous",
                    "A resolved input symlink to the working file is rejected before callbacks and never consumes its target");
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
