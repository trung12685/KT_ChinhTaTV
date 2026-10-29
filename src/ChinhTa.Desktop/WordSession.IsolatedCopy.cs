using System.IO;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    private string? spacingReferencePath,spacingReferenceFlat;
    private void PrepareCheckSpacingReference()
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ xuất mẫu trong phiên kiểm Word riêng.");
        string before=ReadPageBodyXml();string path=ExportPreview([],new Dictionary<Issue,PreviewAnchor>());
        // Synthetic test only: a Word-export side effect must remain a FAIL.
        if(before!=ReadPageBodyXml())throw new InvalidOperationException("Word đã đổi mẫu khi xuất bản xem trước; chưa có nguồn trung thực.");
        // ExportPreview's normal caller owns this file. The check keeps it in its
        // session folder, which WordSession.Dispose cleans on a safe shutdown.
        if(path!=spacingReferencePath)throw new InvalidOperationException("Chưa có mốc bản xem trước của mẫu.");
    }
    internal IsolatedCopyResult VerifyCheckIsolatedCopy()
    {PrepareCheckSpacingReference();return WithSpacingReference((flat,source)=>IsolatedWordCopy.Verify(IsolatedWordPackage.FromFlat(flat),source));}

    internal IsolatedSpacingCalibrationReport VerifyCheckSpacingCalibration(SpacingDiagnosticTarget target)
    {PrepareCheckSpacingReference();return MeasureSpacing(target);}

    internal IReadOnlyList<SpacingDiagnosticTarget> SpacingDiagnosticTargets()
    {
        bool Eligible(Paragraph row)=>!string.IsNullOrWhiteSpace(row.Text) && !row.InTable && row.Outline?.Scope==OutlineScope.PlainBody && BodySpacingRules.Ordinary(row.ParagraphStyle) &&
            row.Outline.SpacingContext is {Supported:true,Contextual:false,Grid:false,PageBreakBefore:false,KeepNext:false};
        string Text(Paragraph row)=>row.Text.TrimEnd('\r','\a','\n');
        return lastBodyParagraphs.Select((row,i)=>(row,i)).Where(p=>Eligible(p.row)).Select(p=>new SpacingDiagnosticTarget(Text(p.row),
            p.i+1<lastBodyParagraphs.Count && Eligible(lastBodyParagraphs[p.i+1])?Text(lastBodyParagraphs[p.i+1]):null)).ToArray();
    }
    internal IsolatedSpacingCalibrationReport MeasureSpacing(SpacingDiagnosticTarget target)=>
        WithSpacingReference((flat,source)=>IsolatedWordCopy.Calibrate(flat,source,target));

    private T WithSpacingReference<T>(Func<string,IsolatedSpacingPreview,T> read)
    {
        if(document is null || spacingReferencePath is null || spacingReferenceFlat is null || !File.Exists(spacingReferencePath))
            throw new InvalidOperationException("Cần mở và quét tài liệu để có bản xem trước hiện tại trước khi đo.");
        string flat=ReadPageBodyXml();
        if(flat!=spacingReferenceFlat)throw new InvalidOperationException("Tài liệu đã đổi sau bản xem trước; hãy quét lại trước khi đo.");
        string reference=Path.Combine(Path.GetTempPath(),"ChinhTa-SpacingReference-"+Guid.NewGuid().ToString("N")+".xps");
        Exception? failure=null;T result=default!;bool completed=false;
        try
        {
            // Use a private XPS copy: the UI may already own an open PackageStore
            // entry for the displayed preview. Never close or overwrite it.
            File.Copy(spacingReferencePath,reference,false);IsolatedSpacingPreview source;
            using(var xps=new XpsDocument(reference,FileAccess.Read))source=IsolatedSpacingPreview.Capture(xps.GetFixedDocumentSequence());
            result=read(flat,source);completed=true;
        }
        catch(Exception error){failure=error;}
        finally
        {
            try
            {
                if(flat!=ReadPageBodyXml())
                {spacingReferencePath=spacingReferenceFlat=null;throw new InvalidOperationException("Nguồn Word đã đổi khi đo; kết quả cũ không còn được dùng.");}
            }
            catch(Exception error){failure=failure is null?error:new AggregateException(failure,error);}
            try{if(File.Exists(reference))File.Delete(reference);}
            catch(Exception error){failure=failure is null?error:new AggregateException(failure,error);}
        }
        if(failure is not null)throw new InvalidOperationException("Chưa đủ bằng chứng đo giãn dòng/khoảng đoạn; tài liệu không được tự sửa.",failure);
        if(!completed)throw new InvalidOperationException("Chưa có kết quả đo.");
        return result;
    }
}
