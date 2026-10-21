namespace ChinhTa.Core;

// Stage the existing, verified single-Line operation inside one atomic user action.
public static class AtomicLineBatch
{
    public static void Apply(string current, string before, IReadOnlyList<Issue> plan,
        Action<Issue,string> applyStep, Action restoreOriginal, Action? verifyCompleted = null,
        Func<IReadOnlyList<Issue>>? remainingIssues = null)
    {
        var backups=new List<string>();
        bool preserve=false;
        try
        {
            void ApplyPass(IReadOnlyList<Issue> items)
            {
                foreach(var item in items.OrderBy(i=>i.LineFix!.Role switch {LineRole.Agency=>0,LineRole.Subject=>1,_=>2}))
                {
                    string step=Path.Combine(Path.GetDirectoryName(before)!,Guid.NewGuid()+".step.docx");
                    File.Copy(current,step);backups.Add(step);
                    applyStep(item,step);
                }
            }
            ApplyPass(plan);
            if(remainingIssues is not null)
            {
                var repairs=remainingIssues();
                if(repairs.Any(i=>!i.CanFix || i.LineFix?.ShapeId is null ||
                    !plan.Any(p=>p.Start==i.Start && p.Original==i.Original && p.LineFix?.Role==i.LineFix.Role)) ||
                    repairs.Select(i=>i.LineFix!.Role).Distinct().Count()!=repairs.Count)
                    throw new InvalidOperationException("Không ghép chắc chắn cảnh báo còn lại với các Line vừa chọn; phải khôi phục cả lượt sửa.");
                // One measured settling pass inside the same transaction; never retry indefinitely.
                ApplyPass(repairs);
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
