using System.Runtime.InteropServices;
using ChinhTa.Core;

public static class SpacingDiagnosticFailureChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var raw=new InvalidOperationException("PRIVATE-DOCUMENT-XML-AND-SELECTED-TEXT");
        foreach(var code in Enum.GetValues<SpacingDiagnosticFailureCode>())
        {
            var error=new SpacingDiagnosticException(code,raw);
            assert(error.Code==code && ReferenceEquals(error.InnerException,raw) && !error.Message.Contains("PRIVATE") && error.RecoveryPath is null,
                "A typed spacing failure keeps the internal cause while its fixed public message exposes no document text: "+code);
        }
        var unknown=new SpacingDiagnosticException((SpacingDiagnosticFailureCode)12345,raw);
        assert(unknown.Code==SpacingDiagnosticFailureCode.OperationFailed && unknown.Message==new SpacingDiagnosticException(SpacingDiagnosticFailureCode.OperationFailed).Message,
            "Unknown failure categories cannot manufacture an unsupported-evidence fallback or arbitrary user message");
        string owned=Path.Combine(Path.GetTempPath(),"ChinhTa-Isolated-"+Guid.NewGuid().ToString("N"));
        var retained=new SpacingDiagnosticException(SpacingDiagnosticFailureCode.CleanupFailed,raw,owned);
        assert(retained.RecoveryPath==Path.GetFullPath(owned) && !retained.Message.Contains(owned),
            "A retained owned temporary folder is separate safe recovery metadata, not mixed into the fixed exception message");
        foreach(string path in new[]{"PRIVATE-DOC-PATH",owned+".docx",Path.Combine(Path.GetTempPath(),"ChinhTa-Isolated-not-a-guid"),
            Path.Combine(owned,"ChinhTa-Isolated-"+Guid.NewGuid().ToString("N")),"https://example.invalid/"+Path.GetFileName(owned),owned+"\nPRIVATE-DOC"})
            assert(new SpacingDiagnosticException(SpacingDiagnosticFailureCode.CleanupFailed,raw,path).RecoveryPath is null,
                "Unowned, remote, nested or control-bearing paths cannot leak through spacing recovery metadata");

        int measures=0,fallbacks=0;
        int value=SpacingDiagnosticGuard.Optional(()=>{measures++;return 17;},()=>{fallbacks++;return -1;});
        assert(value==17 && measures==1 && fallbacks==0,"Successful optional gap evidence never invokes its unavailable fallback");
        measures=fallbacks=0;
        value=SpacingDiagnosticGuard.Optional<int>(()=>{measures++;throw new SpacingDiagnosticException(SpacingDiagnosticFailureCode.UnsupportedEvidence);},()=>{fallbacks++;return 23;});
        assert(value==23 && measures==1 && fallbacks==1,"Only explicitly unsupported optional evidence invokes its fallback once");
        Exception[] mustPropagate=[new InvalidOperationException("ordinary operation failure"),new COMException("Word disconnected",unchecked((int)0x800706BA)),
            new IOException("candidate failed"),new SpacingDiagnosticException(SpacingDiagnosticFailureCode.IntegrityFailed),
            new SpacingDiagnosticException(SpacingDiagnosticFailureCode.CleanupFailed),new SpacingDiagnosticException(SpacingDiagnosticFailureCode.SourceChanged),
            new SpacingDiagnosticException(SpacingDiagnosticFailureCode.OperationFailed),new SpacingDiagnosticException(SpacingDiagnosticFailureCode.ReferenceMissing),
            new SpacingDiagnosticException(SpacingDiagnosticFailureCode.ReferenceStale)];
        foreach(var failure in mustPropagate)
        {
            Exception? observed=null;fallbacks=0;
            try{SpacingDiagnosticGuard.Optional<int>(()=>throw failure,()=>{fallbacks++;return 0;});}catch(Exception error){observed=error;}
            assert(ReferenceEquals(observed,failure) && fallbacks==0,"A real operation, integrity or lifecycle fault cannot be silently downgraded to optional unknown evidence: "+failure.GetType().Name);
        }
        var fallbackFailure=new SpacingDiagnosticException(SpacingDiagnosticFailureCode.IntegrityFailed);Exception? raised=null;
        try{SpacingDiagnosticGuard.Optional<int>(()=>throw new SpacingDiagnosticException(SpacingDiagnosticFailureCode.UnsupportedEvidence),()=>throw fallbackFailure);}
        catch(Exception error){raised=error;}
        assert(ReferenceEquals(raised,fallbackFailure),"A failing fallback is preserved as a fault rather than recursively hidden");
        foreach(bool missingMeasure in new[]{true,false})
        {
            bool rejected=false;
            try{SpacingDiagnosticGuard.Optional<int>(missingMeasure?null!:(()=>1),missingMeasure?(()=>0):null!);}
            catch(ArgumentNullException){rejected=true;}
            assert(rejected,"An incomplete optional-evidence operation fails before invoking any callback");
        }
    }
}
