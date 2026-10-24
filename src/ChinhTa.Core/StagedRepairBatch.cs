namespace ChinhTa.Core;

public enum RepairStage { Text, Center, Line }
public sealed record DeferredRepair(Issue Requested,string ExpectedText);
public sealed record RepairBatchResult(int Selected,int Applied,int AlreadySatisfied);

// One user selection; later stages receive newly measured proposals, never old CPs/coordinates.
public static class StagedRepairBatch
{
    public static RepairStage Stage(Issue issue)=>issue.LineFix is not null?RepairStage.Line:
        issue.CenterFix is not null?RepairStage.Center:RepairStage.Text;
    public static IReadOnlyList<Issue> Prepare(IEnumerable<Issue> selection)
    {
        var items=selection.Distinct().ToArray();
        RepairDependencies.Validate(items);
        foreach(var group in items.GroupBy(Stage))BatchPlan.Create(group);
        var lines=items.Where(i=>Stage(i)==RepairStage.Line).ToArray();
        var centers=items.Where(i=>Stage(i)==RepairStage.Center).ToArray();
        if(lines.Any(i=>i.Kind!=FixKind.ShapeLine || !Enum.IsDefined(i.LineFix!.Role)) ||
            lines.Select(i=>i.LineFix!.Role).Distinct().Count()!=lines.Length ||
            centers.Select(i=>i.CenterFix!.Role).Distinct().Count()!=centers.Length)
            throw new InvalidOperationException("Mỗi vai trò đường kẻ/căn giữa chỉ được chọn một lần. Hãy quét lại.");
        return items;
    }
    public static string ExpectedReference(Issue target,IReadOnlyList<Issue> text,string story,Func<Issue,string> readPrefix)
    {
        string prefix=readPrefix(target);
        WordPreviewAnchor.Create(1,story,prefix,target.Original);
        var overlapping=text.Where(i=>i.CharacterFormat is null && i.Original!=i.Replacement &&
            i.Start<(long)target.Start+target.Length && target.Start<(long)i.Start+i.Length).ToArray();
        if(overlapping.Any(i=>i.Start<target.Start || (long)i.Start+i.Length>(long)target.Start+target.Length))
            throw new InvalidOperationException("Vùng sửa chữ cắt qua mốc nhóm sau; hãy sửa riêng rồi quét lại.");
        return AtomicTextBatch.ExpectedStory(overlapping,target.Original,item=>
        {
            string current=readPrefix(item);
            if(!current.StartsWith(prefix,StringComparison.Ordinal))
                throw new InvalidOperationException("Không đối chiếu được chữ trong mốc nhóm sau.");
            return current[prefix.Length..];
        });
    }
    private static Issue ValidateResolved(DeferredRepair request,Issue current)
    {
        var old=request.Requested;
        if(!current.CanFix || current.Prerequisites is {Count:>0} || current.Original!=request.ExpectedText || current.Kind!=old.Kind ||
            Stage(current)!=Stage(old) || old.LineFix?.Role!=current.LineFix?.Role ||
            old.CenterFix?.Role!=current.CenterFix?.Role)
            throw new InvalidOperationException("Mốc đo lại chưa khớp đúng mục đã chọn; phải khôi phục cả lượt.");
        return current;
    }
    // A null resolution is allowed only when the caller has positively proved that
    // the same identified target already passes its current geometry checks.
    public static RepairBatchResult Apply(IReadOnlyList<DeferredRepair> requests,
        Action<RepairStage> refresh,Func<DeferredRepair,Issue?> resolve,
        Action<IReadOnlyList<Issue>> apply,Action verifyCompleted,Action commit,Action restore)
    {
        var selected=Prepare(requests.Select(r=>r.Requested));
        if(selected.Count!=requests.Count)throw new InvalidOperationException("Lựa chọn lặp lại; hãy quét lại.");
        int applied=0,satisfied=0;
        try
        {
            foreach(var stage in Enum.GetValues<RepairStage>())
            {
                var scope=requests.Where(r=>Stage(r.Requested)==stage).ToArray();
                if(scope.Length==0)continue;
                IReadOnlyList<Issue> plan;
                if(stage==RepairStage.Text)plan=BatchPlan.Create(scope.Select(r=>r.Requested));
                else
                {
                    refresh(stage);
                    var measured=new List<Issue>();
                    foreach(var request in scope)
                    {
                        var current=resolve(request);
                        if(current is null)satisfied++;
                        else measured.Add(ValidateResolved(request,current));
                    }
                    plan=BatchPlan.Create(measured);
                }
                if(plan.Count>0){apply(plan);applied+=plan.Count;}
            }
            verifyCompleted();
            commit(); // Only this completion publishes the single user Undo checkpoint.
            return new(selected.Count,applied,satisfied);
        }
        catch(Exception failure)
        {
            try{restore();}
            catch(Exception restoreFailure)
            {throw new AggregateException("Chưa phục hồi được lượt sửa nhiều nhóm; giữ checkpoint để khôi phục.",failure,restoreFailure);}
            throw;
        }
    }
}
