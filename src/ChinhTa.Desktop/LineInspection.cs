using System.Windows.Documents;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
public record WordLineShape(int Id, int AnchorStart, int Page, LineGeometry? Geometry = null, bool AppOwned = false);

public static class LineInspection
{
    public static IReadOnlyList<(Issue Issue, PreviewAnchor Anchor)> Inspect(FixedDocumentSequence document,
        PreviewHighlighter preview, IReadOnlyList<LineTarget> targets, IReadOnlyList<WordLineShape> shapes,
        IReadOnlyDictionary<Issue, PreviewAnchor> anchors, IReadOnlyList<Issue> otherIssues, LinePolicy policy, bool hasGroupedShapes, IReadOnlyList<Issue>? verifiedEdits = null)
    {
        var result = new List<(Issue, PreviewAnchor)>();
        foreach (var target in targets)
        {
            bool hasAnchor = anchors.TryGetValue(target.Probe, out var anchor);
            anchor ??= new PreviewAnchor(1, target.Text, 0, target.Text.Length);
            string citation = ShapeLineRules.Citation(target) +
                (target.Role == LineRole.Motto ? " Độ dài bằng dòng tiêu ngữ (dung sai đo 5% và 1 pt)." : " Độ dài theo NĐ30.") +
                " Khoảng cách và dung sai đo/tâm theo cấu hình người dùng.";
            Issue Notice(string message, ShapeLineFix? fix = null) => target.Probe with
            { Message = message, Citation = citation, CanFix = fix is not null, LineFix = fix };
            var reference=hasAnchor ? ReferenceMeasurement.Read(document,preview,anchor) : null;
            if(reference is null || !target.Centered)
            {
                result.Add((Notice("Chưa đo/nhận diện chắc chắn dòng tham chiếu hoặc dòng chưa căn giữa vùng đặt. Kiểm tra thủ công trước khi thêm Shape Line."), anchor));
                continue;
            }
            // Recheck successful edits against the new XPS and the current reference. Never hide by position alone.
            var verified=verifiedEdits?.SingleOrDefault(i=>i.Start==target.Start && i.Original==target.Text && i.LineFix?.Role==target.Role);
            if(verified?.LineFix is ShapeLineFix accepted)
            {
                var actual=RenderedLineVerifier.Find(new(accepted.Left,accepted.Top,accepted.Width),preview.HorizontalLines(document,anchor.Page));
                if(actual is not null && ShapeLineRules.Evaluate(target,reference,actual,accepted.ShapeId,policy).Errors.Length==0)
                    continue;
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
            }
            else if (own.Length == 1 && own[0].AppOwned && own[0].Geometry is LineGeometry measured)
            {
                var actual = RenderedLineVerifier.Find(measured, preview.HorizontalLines(document, anchor.Page));
                if (actual is null)
                {
                    bool overlapping = otherIssues.Any(i => i.CanFix && i.Start < target.Start + target.Text.Length && (long)i.Start + i.Length > target.Start);
                    var correction = ShapeLineRules.Evaluate(target, reference, null, own[0].Id, policy).Fix;
                    result.Add((Notice("Shape đã tồn tại nhưng nét trên trang chưa khớp tọa độ khai báo hoặc chưa hiển thị. Cần đặt lại vị trí theo dòng mốc; chưa coi là đúng thể thức.",
                        overlapping ? null : correction) with {Title="Đường kẻ dưới "+target.Label+" chưa hiển thị đúng"}, anchor));
                    continue;
                }
                (fix, errors) = ShapeLineRules.Evaluate(target, reference, actual, own[0].Id, policy);
            }
            else
            {
                var rendered = preview.HorizontalLines(document, anchor.Page);
                var near = rendered.Where(l => l.Y >= reference.Bottom - 2 && l.Y <= reference.Bottom + 80 &&
                    Math.Abs(l.Left + l.Width / 2 - (reference.Left + reference.Width / 2)) < Math.Max(40, reference.Width / 2)).ToArray();
                if (hasGroupedShapes || own.Length != 1 || rendered.Count != pageShapes.Length || near.Length != 1)
                {
                    result.Add((Notice("Có đường/đối tượng nhưng chưa ghép chắc chắn Shape Line với vùng này (có thể nhóm shape, neo xa, border hoặc đường chồng). Kiểm tra thủ công; không tự tạo thêm để tránh trùng."), anchor));
                    continue;
                }
                (fix, errors) = ShapeLineRules.Evaluate(target, reference, near[0], own[0].Id, policy);
            }
            if (errors.Length == 0) continue;
            bool foreign = own.Length == 1 && !own[0].AppOwned;
            bool blocked = foreign || otherIssues.Any(i => i.CanFix && i.Start < target.Start + target.Text.Length && (long)i.Start + i.Length > target.Start);
            string message = string.Join(" ", errors) + $" Dòng cuối rộng {reference.Width:F1} pt; " +
                $"đề xuất dài {fix.Width:F1} pt, cách dưới {fix.Top - reference.Bottom:F1} pt. " +
                (foreign ? "Line có sẵn không có dấu nhận diện của ứng dụng; cần chỉnh thủ công trong Word." :
                    blocked ? "Sửa nội dung/định dạng trong vùng trước rồi quét lại để đo đúng." : "Có thể tạo/đặt lại Line mà không thay nội dung chữ.");
            result.Add((Notice(message, blocked ? null : fix) with
                {Title=(missing?"Thiếu đường kẻ dưới ":"Đường kẻ dưới ")+target.Label+(missing?"":" chưa đúng")}, anchor));
        }
        return result;
    }
}

