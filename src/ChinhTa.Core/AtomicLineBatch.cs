namespace ChinhTa.Core;

public sealed record LineBatchReview(IReadOnlyList<Issue> Selection,IReadOnlyList<Issue> Repairs);

// Stage the existing, verified single-Line operation inside one atomic user action.
public static class AtomicLineBatch
{
    public const string CleanupDataKey="ChinhTa.LineBatchCleanup";
    public static IReadOnlyList<CheckpointCleanup> Apply(string current, string before, IReadOnlyList<Issue> plan,
        Action<Issue,string> applyStep, Action restoreOriginal, Action? verifyCompleted = null,
        Func<LineBatchReview>? remainingIssues = null,Action<string>? deleteBackup=null)
    {
        var backups=new List<string>();
        var cleanup=new List<CheckpointCleanup>();
        bool preserve=false;
        Exception? operationFailure=null;
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
                var review=remainingIssues();
                var scope=review.Selection;
                // Current CPs may differ after Word saves. Role and exact text must still
                // describe precisely the original selection; the caller verifies the story/layout.
                if(scope.Count!=plan.Count || scope.Any(i=>i.LineFix is null ||
                    !plan.Any(p=>p.Original==i.Original && p.LineFix?.Role==i.LineFix.Role)) ||
                    scope.Select(i=>i.LineFix!.Role).Distinct().Count()!=scope.Count)
                    throw new InvalidOperationException("Mốc hiện tại không khớp nhóm Line đã chọn; phải khôi phục cả lượt sửa.");
                var repairs=review.Repairs;
                if(repairs.Any(i=>!i.CanFix || i.LineFix?.ShapeId is null ||
                    !scope.Any(p=>p.Start==i.Start && p.Original==i.Original && p.LineFix?.Role==i.LineFix.Role)) ||
                    repairs.Select(i=>i.LineFix!.Role).Distinct().Count()!=repairs.Count)
                    throw new InvalidOperationException("Không ghép chắc chắn cảnh báo còn lại với các Line vừa chọn; phải khôi phục cả lượt sửa.");
                // One measured settling pass inside the same transaction; never retry indefinitely.
                ApplyPass(repairs);
            }
            verifyCompleted?.Invoke();
        }
        catch(Exception failure)
        {
            operationFailure=failure;
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
                    try{if(deleteBackup is null)File.Delete(path);else deleteBackup(path);}
                    catch(Exception error) when(error is IOException or UnauthorizedAccessException)
                    {cleanup.Add(new(path,error));}
            if(operationFailure is not null && cleanup.Count>0)
                operationFailure.Data[CleanupDataKey]=Array.AsReadOnly(cleanup.ToArray());
        }
        return Array.AsReadOnly(cleanup.ToArray());
    }
}
