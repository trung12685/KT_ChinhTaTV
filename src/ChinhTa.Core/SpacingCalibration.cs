namespace ChinhTa.Core;

public enum SpacingCalibrationStatus { Unproven, BelowSingle, WithinRange, AboveOneAndHalf }
public enum SpacingGapCalibrationStatus { Unproven, BelowSixPoints, AtLeastSixPoints }
public sealed record SpacingGlyphSpan(int Start,int EndExclusive,int Page);
public sealed record SpacingCalibrationResult(SpacingCalibrationStatus Status,string Reason,
    IReadOnlyList<double> SourceStepsPt,IReadOnlyList<double> SingleStepsPt,IReadOnlyList<double> OneAndHalfStepsPt);
public sealed record SpacingGapCalibrationResult(SpacingGapCalibrationStatus Status,string Reason,double? SourceEffectPt,double? SixPointReferenceEffectPt);

// Read-only diagnostics on complete rendered trials. These results do not grant
// C28/C29 write permission. Font sizes and ink rectangles are intentionally absent.
public static class SpacingCalibration
{
    private const double CoordinateTolerance=.01; // XPS units (1/96 inch).
    private const double PointTolerance=.05;
    private const double PointsPerXpsUnit=.75;
    private static bool Same(double a,double b)=>Math.Abs(a-b)<=CoordinateTolerance;
    private static InvalidOperationException Unknown(string reason)=>new(reason);
    private sealed record Line(int Start,int EndExclusive,double Baseline);

    // The match must consume complete glyph runs and complete first/last lines.
    // No truncation, Unicode normalization, substring or first-match fallback.
    public static SpacingGlyphSpan ResolveParagraphSpan(IsolatedLayoutProof snapshot,string visibleText)
    {
        if(string.IsNullOrWhiteSpace(visibleText) || visibleText.Any(char.IsControl))
            throw Unknown("Chưa có nguyên văn hiển thị đủ rõ để ghép đoạn hiệu chuẩn.");
        var glyphs=snapshot.GlyphEvidence;var matches=new List<SpacingGlyphSpan>();
        for(int start=0;start<glyphs.Count;start++)
        {
            int consumed=0;
            for(int end=start;end<glyphs.Count && glyphs[end].Page==glyphs[start].Page;end++)
            {
                string text=glyphs[end].Text;
                if(text.Length>visibleText.Length-consumed || !visibleText.AsSpan(consumed,text.Length).SequenceEqual(text))break;
                consumed+=text.Length;
                if(consumed!=visibleText.Length)continue;
                bool startsWholeLine=start==0 || glyphs[start-1].Page!=glyphs[start].Page || !Same(glyphs[start-1].Baseline,glyphs[start].Baseline);
                bool endsWholeLine=end+1==glyphs.Count || glyphs[end+1].Page!=glyphs[end].Page || !Same(glyphs[end+1].Baseline,glyphs[end].Baseline);
                if(startsWholeLine && endsWholeLine)matches.Add(new(start,end+1,glyphs[start].Page));
                break;
            }
        }
        if(matches.Count!=1)throw Unknown("Đoạn hiệu chuẩn thiếu, trùng hoặc không ghép được trọn glyph/dòng trên cùng trang.");
        _=Lines(snapshot,matches[0]);return matches[0];
    }
    private static IReadOnlyList<Line> Lines(IsolatedLayoutProof snapshot,SpacingGlyphSpan span)
    {
        var glyphs=snapshot.GlyphEvidence;var result=new List<Line>();int start=span.Start;
        for(int index=span.Start+1;index<=span.EndExclusive;index++)
        {
            if(index<span.EndExclusive && Same(glyphs[index].Baseline,glyphs[start].Baseline))
            {
                if(glyphs[index].X<=glyphs[index-1].X+CoordinateTolerance)
                    throw Unknown("Glyph cùng dòng chưa có thứ tự ngang chắc chắn để hiệu chuẩn.");
                continue;
            }
            result.Add(new(start,index,glyphs[start].Baseline));
            if(index<span.EndExclusive && glyphs[index].Baseline<=glyphs[start].Baseline+CoordinateTolerance)
                throw Unknown("Vector baseline chưa tăng đơn điệu trong đoạn hiệu chuẩn.");
            start=index;
        }
        return result;
    }
    private static void RequireOrderedBoundary(IsolatedLayoutProof snapshot,SpacingGlyphSpan span)
    {
        var glyphs=snapshot.GlyphEvidence;
        if(span.Start>0 && glyphs[span.Start-1].Page==span.Page &&
            glyphs[span.Start].Baseline<=glyphs[span.Start-1].Baseline+CoordinateTolerance ||
            span.EndExclusive<glyphs.Count && glyphs[span.EndExclusive].Page==span.Page &&
            glyphs[span.EndExclusive].Baseline<=glyphs[span.EndExclusive-1].Baseline+CoordinateTolerance)
            throw Unknown("Đoạn hiệu chuẩn vượt ranh giới baseline của nội dung liền trước/sau.");
    }
    public static void RequireTrial(IsolatedLayoutProof source,IsolatedLayoutProof trial,IReadOnlyList<string> visibleParagraphTexts)
    {
        if(visibleParagraphTexts.Count is <1 or >2)throw Unknown("Phạm vi hiệu chuẩn chỉ hỗ trợ một đoạn hoặc một cặp liền nhau.");
        if(source.PageCount!=trial.PageCount || source.GlyphRuns!=trial.GlyphRuns ||
            source.PageSizes.Where((p,i)=>!Same(p.Width,trial.PageSizes[i].Width) || !Same(p.Height,trial.PageSizes[i].Height)).Any())
            throw Unknown("Hiệu chuẩn làm đổi số trang, khổ trang hoặc số glyph; chưa chứng minh không reflow.");
        var spans=visibleParagraphTexts.Select(text=>ResolveParagraphSpan(source,text)).ToArray();
        for(int i=0;i<spans.Length;i++)
        {
            var current=ResolveParagraphSpan(trial,visibleParagraphTexts[i]);
            if(current!=spans[i] || i>0 && (spans[i-1].EndExclusive!=spans[i].Start || spans[i-1].Page!=spans[i].Page))
                throw Unknown("Phạm vi hiệu chuẩn đã đổi hoặc hai đoạn không liền nhau trên cùng trang.");
            RequireOrderedBoundary(source,spans[i]);RequireOrderedBoundary(trial,current);
            var beforeLines=Lines(source,spans[i]);var afterLines=Lines(trial,current);
            if(beforeLines.Count!=afterLines.Count || beforeLines.Where((line,n)=>line.Start!=afterLines[n].Start || line.EndExclusive!=afterLines[n].EndExclusive).Any())
                throw Unknown("Hiệu chuẩn đổi ngắt dòng hoặc cách ghép glyph vào baseline.");
        }
        int first=spans[0].Start,last=spans[^1].EndExclusive,page=spans[0].Page;
        double? suffixShift=null;
        for(int i=0;i<source.GlyphRuns;i++)
        {
            var a=source.GlyphEvidence[i];var b=trial.GlyphEvidence[i];
            var bounds=source.PageSizes[a.Page-1];
            if(a.X<0 || a.X>bounds.Width || b.X<0 || b.X>bounds.Width ||
                a.Baseline<0 || a.Baseline>bounds.Height || b.Baseline<0 || b.Baseline>bounds.Height)
                throw Unknown("Baseline hoặc tọa độ glyph nằm ngoài trang; chưa đủ bằng chứng hiệu chuẩn.");
            if(a.Page!=b.Page || a.Text!=b.Text || a.Shape!=b.Shape || !Same(a.X,b.X))
                throw Unknown("Hiệu chuẩn đổi chữ, phông, shaping hoặc vị trí ngang của glyph.");
            double shift=b.Baseline-a.Baseline;
            if(i>=first && i<last)continue;
            if(i<first || a.Page!=page)
            {
                if(!Same(shift,0))throw Unknown("Hiệu chuẩn làm dịch phần trước hoặc trang chưa chọn.");
            }
            else
            {
                suffixShift??=shift;
                if(!Same(shift,suffixShift.Value))throw Unknown("Phần sau đoạn hiệu chuẩn chưa giữ được cùng một dịch chuyển dọc.");
            }
        }
    }
    private static IReadOnlyList<double> Steps(IReadOnlyList<Line> lines)=>Array.AsReadOnly(lines.Skip(1)
        .Select((line,i)=>(line.Baseline-lines[i].Baseline)*PointsPerXpsUnit).ToArray());

    public static SpacingCalibrationResult MeasureLines(IsolatedLayoutProof source,IsolatedLayoutProof unchangedClone,
        IsolatedLayoutProof single,IsolatedLayoutProof oneAndHalf,string visibleParagraphText)
    {
        try
        {
            source.RequireSame(unchangedClone);
            RequireTrial(source,single,[visibleParagraphText]);RequireTrial(source,oneAndHalf,[visibleParagraphText]);
            var span=ResolveParagraphSpan(source,visibleParagraphText);var sourceLines=Lines(source,span);
            if(sourceLines.Count<3)throw Unknown("Cần ít nhất ba baseline cùng trang để đối chiếu vector giãn dòng.");
            var observed=Steps(sourceLines);var lower=Steps(Lines(single,span));var upper=Steps(Lines(oneAndHalf,span));
            if(observed.Any(v=>!double.IsFinite(v)) || lower.Any(v=>!double.IsFinite(v)) || upper.Any(v=>!double.IsFinite(v)) ||
                lower.Where((step,i)=>step<=PointTolerance || !double.IsFinite(upper[i]-step) || upper[i]-step<=2*PointTolerance).Any())
                throw Unknown("Hai mức Single/1,5 chưa tạo được khoảng baseline tham chiếu phân biệt chắc chắn.");
            bool within=observed.Select((step,i)=>step>=lower[i]-PointTolerance && step<=upper[i]+PointTolerance).All(x=>x);
            bool below=observed.Select((step,i)=>step<lower[i]-PointTolerance).All(x=>x);
            bool above=observed.Select((step,i)=>step>upper[i]+PointTolerance).All(x=>x);
            var status=within?SpacingCalibrationStatus.WithinRange:below?SpacingCalibrationStatus.BelowSingle:
                above?SpacingCalibrationStatus.AboveOneAndHalf:SpacingCalibrationStatus.Unproven;
            string reason=status switch
            {
                SpacingCalibrationStatus.WithinRange=>"Mọi khoảng baseline nằm trong hai vector Single/1,5 đã đo; đây là kết quả chẩn đoán, chưa cấp quyền sửa.",
                SpacingCalibrationStatus.BelowSingle=>"Mọi khoảng baseline thấp hơn vector Single đã đo; chưa cấp quyền sửa.",
                SpacingCalibrationStatus.AboveOneAndHalf=>"Mọi khoảng baseline lớn hơn vector 1,5 đã đo; chưa cấp quyền sửa.",
                _=>"Các khoảng baseline không cùng một kết luận; giữ kiểm tra thủ công."
            };
            return new(status,reason,observed,lower,upper);
        }
        catch(InvalidOperationException error){return new(SpacingCalibrationStatus.Unproven,error.Message,Array.Empty<double>(),Array.Empty<double>(),Array.Empty<double>());}
    }
    public static SpacingGapCalibrationResult MeasureGap(IsolatedLayoutProof source,IsolatedLayoutProof unchangedClone,
        IsolatedLayoutProof zeroGapTrial,IsolatedLayoutProof sixPointReference,string firstVisibleParagraph,string secondVisibleParagraph)
    {
        try
        {
            source.RequireSame(unchangedClone);RequireTrial(source,zeroGapTrial,[firstVisibleParagraph,secondVisibleParagraph]);
            RequireTrial(source,sixPointReference,[firstVisibleParagraph,secondVisibleParagraph]);
            var first=ResolveParagraphSpan(source,firstVisibleParagraph);var second=ResolveParagraphSpan(source,secondVisibleParagraph);
            var a=Lines(source,first);var b=Lines(source,second);var zeroA=Lines(zeroGapTrial,first);var zeroB=Lines(zeroGapTrial,second);
            var referenceA=Lines(sixPointReference,first);var referenceB=Lines(sixPointReference,second);
            if(a.Count<2 || b.Count<2)throw Unknown("Cần ít nhất hai baseline ở mỗi đoạn để tách tác động khoảng đoạn khỏi giãn dòng.");
            bool ChangedVector(IReadOnlyList<Line> before,IReadOnlyList<Line> after)=>before.Where((line,i)=>
                !Same(line.Baseline-after[i].Baseline,before[0].Baseline-after[0].Baseline)).Any();
            if(ChangedVector(a,zeroA) || ChangedVector(b,zeroB) || ChangedVector(a,referenceA) || ChangedVector(b,referenceB))
                throw Unknown("Trial bỏ khoảng hoặc tham chiếu6pt làm đổi vector giãn dòng; chưa tách được khoảng đoạn.");
            double boundary=b[0].Baseline-a[^1].Baseline,zeroBoundary=zeroB[0].Baseline-zeroA[^1].Baseline,
                referenceBoundary=referenceB[0].Baseline-referenceA[^1].Baseline;
            if(boundary<=0 || zeroBoundary<=0 || referenceBoundary<=0)throw Unknown("Ranh giới baseline hai đoạn chưa có thứ tự dọc chắc chắn.");
            double measured=(boundary-zeroBoundary)*PointsPerXpsUnit,reference=(referenceBoundary-zeroBoundary)*PointsPerXpsUnit;
            if(measured< -PointTolerance || reference<=2*PointTolerance)
                throw Unknown("Tác động spacing hoặc tham chiếu6pt chưa phân biệt được với leading/bố cục; giữ kiểm thủ công.");
            measured=Math.Max(0,measured);
            return new(measured>=reference-PointTolerance?SpacingGapCalibrationStatus.AtLeastSixPoints:SpacingGapCalibrationStatus.BelowSixPoints,
                "Đối chiếu tác động spacing với tham chiếu6pt đã đo cùng bối cảnh; các giá trị là chênh lệch baseline so trial0, không phải tổng khoảng hiển thị và chưa cấp quyền sửa.",measured,reference);
        }
        catch(InvalidOperationException error){return new(SpacingGapCalibrationStatus.Unproven,error.Message,null,null);}
    }
}
