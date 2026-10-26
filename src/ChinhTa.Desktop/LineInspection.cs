using System.Windows.Documents;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
public record WordLineShape(int Id, int AnchorStart, int Page, LineGeometry? Geometry = null, bool AppOwned = false);

public static class LineInspection
{
    private const string AmbiguousStroke="Có nhiều nét chồng hoặc quá gần vị trí đường kẻ này. Chưa xác định được nét duy nhất; cần kiểm tra thủ công trước khi đặt lại đường kẻ.";
    public static IReadOnlyList<(Issue Issue, PreviewAnchor Anchor)> Inspect(FixedDocumentSequence document,
        PreviewHighlighter preview, IReadOnlyList<LineTarget> targets, IReadOnlyList<WordLineShape> shapes,
        IReadOnlyDictionary<Issue, PreviewAnchor> anchors, IReadOnlyList<Issue> otherIssues, LinePolicy policy, bool hasGroupedShapes, IReadOnlyList<Issue>? verifiedEdits = null)
    {
        var result = new List<(Issue, PreviewAnchor)>();
        var strokePages=new Dictionary<int,HorizontalStrokeEvidence>();
        foreach (var target in targets)
        {
            var prerequisites=otherIssues.Where(i=>i.CanFix && StagedRepairBatch.Stage(i)==RepairStage.Text &&
                i.Start<(long)target.Start+target.Text.Length && (long)i.Start+i.Length>target.RegionStart).Distinct().ToArray();
            bool hasAnchor = anchors.TryGetValue(target.Probe, out var anchor);
            anchor ??= new PreviewAnchor(1, target.Text, 0, target.Text.Length);
            string citation = ShapeLineRules.Citation(target) +
                (target.Role == LineRole.Motto ? " Độ dài bằng dòng tiêu ngữ (dung sai đo 5% và 1 pt)." : " Độ dài theo NĐ30.") +
                " Khoảng cách và dung sai đo/tâm theo cấu hình người dùng.";
            Issue Notice(string message, ShapeLineFix? fix = null) => target.Probe with
            { Message = message, Citation = citation, CanFix = fix is not null, LineFix = fix,
                Prerequisites=fix is not null && prerequisites.Length>0?Array.AsReadOnly(prerequisites):null };
            var reference=hasAnchor ? ReferenceMeasurement.Read(document,preview,anchor) : null;
            if(reference is null || !target.Centered)
            {
                result.Add((Notice("Chưa đo/nhận diện chắc chắn dòng tham chiếu hoặc dòng chưa căn giữa vùng đặt. Kiểm tra thủ công trước khi thêm Shape Line."), anchor));
                continue;
            }
            if(!strokePages.TryGetValue(anchor.Page,out var strokeEvidence))
                strokePages[anchor.Page]=strokeEvidence=preview.ReadHorizontalLines(document,anchor.Page);
            if(strokeEvidence.HasUnverifiedPaths)
            {
                result.Add((Notice("Có nét bị cắt một phần hoặc chưa xác minh được cách hiển thị trên trang. Cần kiểm tra thủ công trước khi tự thêm hoặc đặt lại đường kẻ."),anchor));
                continue;
            }
            // Recheck successful edits against the new XPS and the current reference. Never hide by position alone.
            var verified=verifiedEdits?.SingleOrDefault(i=>i.Start==target.Start && i.Original==target.Text && i.LineFix?.Role==target.Role);
            if(verified?.LineFix is ShapeLineFix accepted)
            {
                var expected=new LineGeometry(accepted.Left,accepted.Top,accepted.Width);
                var rendered=strokeEvidence.Lines;
                if(RenderedLineVerifier.MatchCount(expected,rendered)>1)
                {
                    result.Add((Notice(AmbiguousStroke),anchor));continue;
                }
                var actual=RenderedLineVerifier.Find(expected,rendered);
                if(actual is not null && ShapeLineRules.Evaluate(target,reference,actual,accepted.ShapeId,policy).Errors.Length==0)
                {
                    if(prerequisites.Length>0)
                    {
                        var current=shapes.Where(s=>s.Page==anchor.Page && s.AnchorStart>=target.RegionStart && s.AnchorStart<target.RegionEnd).ToArray();
                        bool owned=current.Length==1 && current[0].AppOwned && current[0].Id==accepted.ShapeId;
                        result.Add((Notice("Nét hiện tại đã đạt; cần đo lại sau các lỗi chữ/phông liên quan trước khi kết luận lượt sửa.",owned?accepted:null)
                            with {Title="Đo lại đường kẻ dưới "+target.Label+" sau sửa chữ/phông"},anchor));
                    }
                    continue;
                }
            }
            bool IsInRegion(WordLineShape shape, LineTarget region) => shape.AnchorStart >= region.RegionStart && shape.AnchorStart < region.RegionEnd;
            var pageShapes = shapes.Where(s => s.Page == anchor.Page).ToArray();
            var own = pageShapes.Where(s => IsInRegion(s, target)).ToArray();
            bool allAssigned = !hasGroupedShapes && pageShapes.All(s => targets.Count(t => IsInRegion(s,t)) == 1);
            bool missing=own.Length==0 && allAssigned;
            ShapeLineFix? fix;
            string[] errors;
            if (own.Length == 0 && allAssigned)
            {
                (fix, errors) = ShapeLineRules.Evaluate(target, reference, null, null, policy);
                if(RenderedLineVerifier.MatchCount(new(fix.Left,fix.Top,fix.Width),strokeEvidence.Lines)>0)
                {
                    result.Add((Notice("Đã có nét tại vị trí đề xuất nhưng chưa ghép được với Shape Line của vùng này. Kiểm tra thủ công; chưa tự thêm đường kẻ để tránh chồng nét."),anchor));
                    continue;
                }
            }
            else if (own.Length == 1 && own[0].AppOwned && own[0].Geometry is LineGeometry measured)
            {
                var rendered = strokeEvidence.Lines;
                if(RenderedLineVerifier.MatchCount(measured,rendered)>1)
                {
                    result.Add((Notice(AmbiguousStroke),anchor));
                    continue;
                }
                var actual = RenderedLineVerifier.Find(measured, rendered);
                if (actual is null)
                {
                    var correction = ShapeLineRules.Evaluate(target, reference, null, own[0].Id, policy).Fix;
                    result.Add((Notice("Shape đã tồn tại nhưng nét trên trang chưa khớp tọa độ khai báo hoặc chưa hiển thị. Cần đặt lại vị trí theo dòng mốc; chưa coi là đúng thể thức. "+
                        (prerequisites.Length>0?"Chọn kèm các lỗi liên quan; sẽ đo lại sau khi sửa chữ/phông.":""),correction) with {Title="Đường kẻ dưới "+target.Label+" chưa hiển thị đúng"}, anchor));
                    continue;
                }
                (fix, errors) = ShapeLineRules.Evaluate(target, reference, actual, own[0].Id, policy);
            }
            else
            {
                var rendered = strokeEvidence.Lines;
                var near = rendered.Where(l => l.Y >= reference.Bottom - 2 && l.Y <= reference.Bottom + 80 &&
                    Math.Abs(l.Left + l.Width / 2 - (reference.Left + reference.Width / 2)) < Math.Max(40, reference.Width / 2)).ToArray();
                if (hasGroupedShapes || own.Length != 1 || rendered.Count != pageShapes.Length || near.Length != 1)
                {
                    result.Add((Notice("Có đường/đối tượng nhưng chưa ghép chắc chắn Shape Line với vùng này (có thể nhóm shape, neo xa, border hoặc đường chồng). Kiểm tra thủ công; không tự tạo thêm để tránh trùng."), anchor));
                    continue;
                }
                (fix, errors) = ShapeLineRules.Evaluate(target, reference, near[0], own[0].Id, policy);
            }
            if (errors.Length == 0)
            {
                if(prerequisites.Length>0 && own.Length==1 && own[0].AppOwned)
                    result.Add((Notice("Nét hiện tại đã đạt; sẽ đo lại sau các lỗi chữ/phông liên quan và chỉ chỉnh nếu số đo mới yêu cầu.",fix)
                        with {Title="Đo lại đường kẻ dưới "+target.Label+" sau sửa chữ/phông"},anchor));
                continue;
            }
            bool foreign = own.Length == 1 && !own[0].AppOwned;
            string message = string.Join(" ", errors) + $" Dòng cuối rộng {reference.Width:F1} pt; " +
                $"đề xuất dài {fix.Width:F1} pt, cách dưới {fix.Top - reference.Bottom:F1} pt. " +
                (foreign ? "Line có sẵn không có dấu nhận diện của ứng dụng; cần chỉnh thủ công trong Word." :
                    prerequisites.Length>0 ? "Chọn kèm các lỗi liên quan; sẽ sửa chữ/phông, đo lại rồi kẻ trong cùng lượt." : "Có thể tạo/đặt lại Line mà không thay nội dung chữ.");
            result.Add((Notice(message, foreign ? null : fix) with
                {Title=(missing?"Thiếu đường kẻ dưới ":"Đường kẻ dưới ")+target.Label+(missing?"":" chưa đúng")}, anchor));
        }
        return result;
    }
}

