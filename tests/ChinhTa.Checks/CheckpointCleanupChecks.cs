using ChinhTa.Core;

public static class CheckpointCleanupChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-checkpoint-cleanup-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string current=Path.Combine(folder,"working.docx"),candidate=Path.Combine(folder,"candidate.docx");
        try
        {
            void Reset(){File.WriteAllText(current,"before");File.WriteAllText(candidate,"after");}
            Reset();int closes=0,opens=0,deletes=0;
            var normal=CheckpointSwap.Apply(current,candidate,()=>closes++,()=>opens++);
            assert(normal is null && closes==1 && opens==1 && File.ReadAllText(current)=="after" &&
                Directory.GetFiles(folder,"*.rollback.docx").Length==0,
                "Normal checkpoint replacement still closes, replaces, reopens and deletes only its own backup");
            foreach(Exception cleanupError in new Exception[]{new IOException("delete locked"),new UnauthorizedAccessException("delete denied")})
            {
                Reset();closes=opens=deletes=0;
                var warning=CheckpointSwap.Apply(current,candidate,()=>closes++,()=>
                {opens++;if(File.ReadAllText(current)!="after")throw new Exception("bad candidate");},path=>
                {deletes++;if(File.ReadAllText(path)!="before")throw new Exception("lost backup");throw cleanupError;});
                assert(warning is not null && ReferenceEquals(warning.Error,cleanupError) && File.ReadAllText(warning.RetainedPath)=="before" &&
                    File.ReadAllText(current)=="after" && closes==1 && opens==1 && deletes==1,
                    "A locked/denied cleanup preserves the verified candidate and backup without retrying close, open, copy or cleanup");
                File.Delete(warning!.RetainedPath);
            }
            foreach(bool failClose in new[]{true,false})
            {
                Reset();closes=opens=deletes=0;
                var primary=new IOException(failClose?"close failed":"candidate open failed");
                var cleanupError=new UnauthorizedAccessException("cleanup also denied");Exception? caught=null;
                try
                {
                    CheckpointSwap.Apply(current,candidate,()=>{closes++;if(failClose)throw primary;},()=>
                    {if(++opens==1)throw primary;},_=>{deletes++;throw cleanupError;});
                }
                catch(IOException error){caught=error;}
                var warning=caught?.Data[CheckpointSwap.CleanupDataKey] as CheckpointCleanup;
                assert(ReferenceEquals(caught,primary) && warning is not null && ReferenceEquals(warning.Error,cleanupError) &&
                    File.ReadAllText(current)=="before" && File.ReadAllText(warning.RetainedPath)=="before" && deletes==1 &&
                    (failClose?closes==1 && opens==0:closes==2 && opens==2),
                    "Close/open failure retains its original exception and restored bytes even if backup cleanup also fails");
                File.Delete(warning!.RetainedPath);
            }
            Reset();closes=opens=deletes=0;
            var mismatch=new RenderedLineMismatchException([]);bool mismatchPreserved=false;
            try
            {
                CheckpointSwap.Apply(current,candidate,()=>closes++,()=>{if(++opens==1)throw mismatch;},_=>throw new IOException("cleanup"));
            }
            catch(RenderedLineMismatchException error){mismatchPreserved=ReferenceEquals(error,mismatch) &&
                error.Data[CheckpointSwap.CleanupDataKey] is CheckpointCleanup;}
            assert(mismatchPreserved && opens==2 && closes==2 && File.ReadAllText(current)=="before",
                "A rendered-line mismatch keeps its exact type and evidence for existing bounded feedback; cleanup cannot replace it");
            foreach(var path in Directory.GetFiles(folder,"*.rollback.docx"))File.Delete(path);
            Reset();deletes=0;var candidateFailure=new IOException("candidate failed");var restoreFailure=new IOException("restore failed");
            AggregateException? aggregate=null;opens=0;
            try{CheckpointSwap.Apply(current,candidate,()=>{},()=>{throw ++opens==1?candidateFailure:restoreFailure;},_=>deletes++);}
            catch(AggregateException error){aggregate=error;}
            var kept=Directory.GetFiles(folder,"*.rollback.docx");
            assert(aggregate is not null && aggregate.InnerExceptions.Count==2 && ReferenceEquals(aggregate.InnerExceptions[0],candidateFailure) &&
                ReferenceEquals(aggregate.InnerExceptions[1],restoreFailure) && deletes==0 && kept.Length==1 && File.ReadAllText(kept[0])=="before",
                "A genuine restoration failure retains both causes and its backup; it never enters cleanup or reports success");
            File.Delete(kept[0]);
            Reset();File.Delete(candidate);Exception? copyFailure=null;
            try{CheckpointSwap.Apply(current,candidate,()=>{},()=>{},_=>throw new IOException("cleanup"));}
            catch(FileNotFoundException error){copyFailure=error;}
            assert(copyFailure is not null && copyFailure.Data[CheckpointSwap.CleanupDataKey] is CheckpointCleanup && File.ReadAllText(current)=="before",
                "Candidate copy failure restores the original and cannot be hidden behind a subsequent cleanup exception");
            foreach(var path in Directory.GetFiles(folder,"*.rollback.docx"))File.Delete(path);

            // Model the exact Undo boundary: consume the stack entry only after the
            // real file swap returns, including the successful-reopen/cleanup-failure case.
            string first=Path.Combine(folder,"first.docx"),zero=Path.Combine(folder,"zero.docx");
            File.WriteAllText(first,"first edit");File.WriteAllText(zero,"original");File.WriteAllText(current,"second edit");
            var stack=new Stack<string>();stack.Push(zero);stack.Push(first);opens=0;
            var undoWarning=CheckpointSwap.Apply(current,stack.Peek(),()=>{},()=>opens++,_=>throw new IOException("cleanup"));stack.Pop();
            assert(undoWarning is not null && File.ReadAllText(current)=="first edit" && stack.Count==1 && opens==1,
                "Undo with retained backup consumes exactly the completed action instead of leaving the same checkpoint selected");
            CheckpointSwap.Apply(current,stack.Peek(),()=>{},()=>opens++);stack.Pop();
            assert(File.ReadAllText(current)=="original" && stack.Count==0 && opens==2,
                "The next Undo reaches the preceding action rather than replaying the already completed Undo");
            File.Delete(undoWarning!.RetainedPath);

            if(OperatingSystem.IsWindows())
            {
                Reset();FileStream? held=null;CheckpointCleanup? actual=null;
                try
                {
                    actual=CheckpointSwap.Apply(current,candidate,()=>{},()=>
                    {
                        string rollback=Directory.GetFiles(folder,"*.rollback.docx").Single();
                        held=new FileStream(rollback,FileMode.Open,FileAccess.Read,FileShare.Read);
                    });
                    assert(actual is not null && actual.Error is IOException or UnauthorizedAccessException && File.ReadAllText(current)=="after" &&
                        File.Exists(actual.RetainedPath),"Real Windows delete-denying share lock cannot turn a completed checkpoint swap into failure");
                }
                finally{held?.Dispose();if(actual is not null)File.Delete(actual.RetainedPath);}
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
