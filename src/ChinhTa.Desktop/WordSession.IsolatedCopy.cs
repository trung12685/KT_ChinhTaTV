using System.IO;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    // Explicit diagnostic only. Do not run automatically during Check/Fix or
    // infer write permission for C28/C29 from a successful copy comparison.
    internal IsolatedCopyResult VerifyCheckIsolatedCopy()
    {
        if(document is null)throw new InvalidOperationException("Chưa mở tài liệu để đối chiếu bản sao.");
        string flat=ReadPageBodyXml();var package=IsolatedWordPackage.FromFlat(flat);package.RequirePassiveCalibration();
        string? preview=null;Exception? failure=null;IsolatedCopyResult? result=null;
        try
        {
            preview=ExportPreviewCore([],new Dictionary<Issue,PreviewAnchor>());
            IsolatedSpacingPreview source;
            using(var xps=new XpsDocument(preview,FileAccess.Read))source=IsolatedSpacingPreview.Capture(xps.GetFixedDocumentSequence());
            result=IsolatedWordCopy.Verify(package,source);
        }
        catch(Exception error){failure=error;}
        finally
        {
            try
            {
                // Includes unsaved content, all stories, binaries and relations.
                // A read/export that changed fields is not a faithful baseline.
                if(flat!=ReadPageBodyXml())throw new InvalidOperationException("Nguồn Word đã đổi trong lúc đối chiếu bản sao; không dùng kết quả hiệu chuẩn.");
            }
            catch(Exception error){failure=failure is null?error:new AggregateException(failure,error);}
            try{if(preview is not null && File.Exists(preview))File.Delete(preview);}
            catch(Exception error){failure=failure is null?error:new AggregateException(failure,error);}
        }
        if(failure is not null)throw new InvalidOperationException("Đối chiếu bản sao chưa đạt; chưa mở quyền sửa Exactly/AtLeast/khoảng đoạn gộp.",failure);
        return result??throw new InvalidOperationException("Không có kết quả đối chiếu bản sao.");
    }
}
