namespace ChinhTa.Core;

// Optional evidence may be absent without invalidating a separate proven
// measurement. An operation/integrity/resource failure is not absent evidence.
public static class SpacingDiagnosticGuard
{
    public static T Optional<T>(Func<T> measure,Func<T> unsupported)
    {
        ArgumentNullException.ThrowIfNull(measure);ArgumentNullException.ThrowIfNull(unsupported);
        try{return measure();}
        catch(SpacingDiagnosticException error) when(error.Code==SpacingDiagnosticFailureCode.UnsupportedEvidence)
        {return unsupported();}
    }
}
