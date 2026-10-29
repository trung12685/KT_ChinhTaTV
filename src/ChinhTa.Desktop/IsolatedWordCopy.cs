using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal sealed record IsolatedCopyResult(int Parts,int Pages,int GlyphRuns);

// Never attaches to an existing Word application. The clone is disposable and
// opened read-only; no save, checkpoint or user-document mutation is performed.
internal static class IsolatedWordCopy
{
    internal static IsolatedCopyResult Verify(IsolatedWordPackage package,IsolatedSpacingPreview source)
    {
        package.RequirePassiveCalibration();
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-Isolated-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        dynamic? app=null,copy=null;Exception? failure=null;IsolatedCopyResult? result=null;Action? restoreOptions=null;
        void Release(object? value){if(value is not null && Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
        try
        {
            string path=Path.Combine(folder,"copy.docx"),preview=Path.Combine(folder,"copy.xps");
            package.WriteNew(path);
            app=Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application")??throw new InvalidOperationException("Cần Microsoft Word desktop."));
            app!.Visible=false;app.DisplayAlerts=0;app.ScreenUpdating=false;app.AutomationSecurity=3;
            dynamic options=app.Options;
            try
            {
                bool links=(bool)options.UpdateLinksAtOpen,fields=(bool)options.UpdateFieldsAtPrint,
                    printLinks=(bool)options.UpdateLinksAtPrint,normalPrompt=(bool)options.SaveNormalPrompt;
                restoreOptions=()=>
                {
                    dynamic previous=app.Options;
                    try{previous.UpdateLinksAtOpen=links;previous.UpdateFieldsAtPrint=fields;previous.UpdateLinksAtPrint=printLinks;previous.SaveNormalPrompt=normalPrompt;}
                    finally{Release(previous);}
                };
                options.UpdateLinksAtOpen=false;options.UpdateFieldsAtPrint=false;options.UpdateLinksAtPrint=false;options.SaveNormalPrompt=false;
            }
            finally{Release(options);}
            dynamic documents=app.Documents;
            try{copy=documents.Open(path,ConfirmConversions:false,ReadOnly:true,AddToRecentFiles:false,Visible:false,OpenAndRepair:false,NoEncodingDialog:true);}
            finally{Release(documents);}
            if(!(bool)copy!.ReadOnly || (bool)copy.TrackRevisions || (int)copy.ProtectionType!=-1)
                throw new InvalidOperationException("Bản sao cô lập không ở trạng thái chỉ đọc được hỗ trợ.");
            copy.Repaginate();copy.ExportAsFixedFormat(preview,18,OpenAfterExport:false);
            using(var xps=new XpsDocument(preview,FileAccess.Read))
            {
                var observed=IsolatedSpacingPreview.Capture(xps.GetFixedDocumentSequence());source.RequireSame(observed);
                result=new(package.PartNames.Count,observed.Pages,observed.GlyphRuns);
            }
            package.VerifyFile(path); // Word must not have rewritten the candidate.
        }
        catch(Exception error){failure=error;}
        finally
        {
            bool closed=true;
            void Cleanup(Action action)
            {
                try{action();}
                catch(Exception error){closed=false;failure=failure is null?error:new AggregateException(failure,error);}
            }
            if(copy is not null){Cleanup(()=>copy.Close(0));Cleanup(()=>Release(copy));}
            if(restoreOptions is not null)Cleanup(restoreOptions);
            if(app is not null){Cleanup(()=>app.Quit(0));Cleanup(()=>Release(app));}
            // A failed Close/Quit keeps the temporary copy for recovery, without
            // killing Word processes or hiding cleanup errors behind a PASS.
            if(closed)Cleanup(()=>Directory.Delete(folder,true));
        }
        if(failure is not null)throw new InvalidOperationException("Chưa xác minh được bản sao Word cô lập; không hiệu chuẩn spacing. Thư mục tạm (nếu còn): "+folder,failure);
        return result??throw new InvalidOperationException("Chưa có kết quả đối chiếu bản sao cô lập.");
    }
}
