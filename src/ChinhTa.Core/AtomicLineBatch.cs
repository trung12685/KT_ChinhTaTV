namespace ChinhTa.Core;

// Stage the existing, verified single-Line operation inside one atomic user action.
public static class AtomicLineBatch
{
    public static void Apply(string current, string before, IReadOnlyList<Issue> plan,
        Action<Issue,string> applyStep, Action restoreOriginal, Action? verifyCompleted = null)
    {
        var backups=new List<string>();
        bool preserve=false;
        try
        {
            foreach(var item in plan.OrderBy(i=>i.LineFix!.Role switch {LineRole.Agency=>0,LineRole.Subject=>1,_=>2}))
            {
                string step=Path.Combine(Path.GetDirectoryName(before)!,Guid.NewGuid()+".step.docx");
                File.Copy(current,step);backups.Add(step);
                applyStep(item,step);
            }
            verifyCompleted?.Invoke();
        }
        catch(Exception failure)
        {
            try {restoreOriginal();}
            catch(Exception restoreFailure)
            {
                preserve=true;
                throw new AggregateException("Không khôi phục được cả nhóm Line. Bản trước lượt sửa: "+before,failure,restoreFailure);
            }
            throw;
        }
        finally
        {
            if(!preserve)
                foreach(string path in backups)
                    try{File.Delete(path);}catch(IOException){}
        }
    }
}
