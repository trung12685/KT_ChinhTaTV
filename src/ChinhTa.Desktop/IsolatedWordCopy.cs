using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal sealed record IsolatedCopyResult(int Parts,int Pages,int GlyphRuns);
internal sealed record SpacingDiagnosticTarget(string Text,string? NextText);
internal sealed record IsolatedSpacingCalibrationReport(SpacingCalibrationResult Line,SpacingGapCalibrationResult? Gap,int Parts,int Pages,int GlyphRuns);

// All packages open read-only in a newly owned Word application. Trial changes
// happen in full Flat OPC copies, never through the live user document or Undo.
internal static class IsolatedWordCopy
{
    internal static IsolatedCopyResult Verify(IsolatedWordPackage package,IsolatedSpacingPreview source)=>
        Run(package,source,(baseline,_)=>new IsolatedCopyResult(package.PartNames.Count,baseline.Pages,baseline.GlyphRuns));

    internal static IsolatedSpacingCalibrationReport Calibrate(string flat,IsolatedSpacingPreview source,SpacingDiagnosticTarget target)
    {
        var anchor=IsolatedSpacingTrial.Find(flat,target.Text);
        var single=IsolatedSpacingTrial.Create(flat,anchor,IsolatedSpacingTrialKind.Single);
        var half=IsolatedSpacingTrial.Create(flat,anchor,IsolatedSpacingTrialKind.OneAndHalf);
        IsolatedSpacingTrialResult? gap=null,gapReference=null;
        SpacingGapCalibrationResult? unavailableGap=null;
        if(target.NextText is not null)
        {
            try
            {
                var next=IsolatedSpacingTrial.Find(flat,target.NextText);
                gap=IsolatedSpacingTrial.Create(flat,anchor,IsolatedSpacingTrialKind.ZeroParagraphGap,next);
                gapReference=IsolatedSpacingTrial.Create(flat,anchor,IsolatedSpacingTrialKind.SixPointParagraphGap,next);
            }
            catch(InvalidOperationException)
            {unavailableGap=new(SpacingGapCalibrationStatus.Unproven,"Cặp đoạn chưa có nguồn/context đủ chắc chắn; giữ phép đo dòng độc lập.",null,null);}
        }
        string visible=target.Text.Replace("\v","",StringComparison.Ordinal);
        string? nextVisible=target.NextText?.Replace("\v","",StringComparison.Ordinal);
        return Run<IsolatedSpacingCalibrationReport>(IsolatedWordPackage.FromFlat(flat),source,(baseline,capture)=>
        {
            var lower=capture(IsolatedWordPackage.FromFlat(single.FlatXml));source.RequireSpacingTrial(lower,[visible]);
            var upper=capture(IsolatedWordPackage.FromFlat(half.FlatXml));source.RequireSpacingTrial(upper,[visible]);
            var lines=SpacingCalibration.MeasureLines(source.Layout,baseline.Layout,lower.Layout,upper.Layout,visible);
            SpacingGapCalibrationResult? gapResult=unavailableGap;
            if(gap is not null && gapReference is not null && nextVisible is not null)
            {
                try
                {
                    var zero=capture(IsolatedWordPackage.FromFlat(gap.FlatXml));source.RequireSpacingTrial(zero,[visible,nextVisible]);
                    var six=capture(IsolatedWordPackage.FromFlat(gapReference.FlatXml));source.RequireSpacingTrial(six,[visible,nextVisible]);
                    gapResult=SpacingCalibration.MeasureGap(source.Layout,baseline.Layout,zero.Layout,six.Layout,visible,nextVisible);
                }
                catch(InvalidOperationException)
                {gapResult=new(SpacingGapCalibrationStatus.Unproven,"Trial khoảng đoạn chưa đủ bằng chứng; không suy từ phép đo dòng.",null,null);}
            }
            return new(lines,gapResult,IsolatedWordPackage.FromFlat(flat).PartNames.Count,baseline.Pages,baseline.GlyphRuns);
        });
    }

    private static T Run<T>(IsolatedWordPackage package,IsolatedSpacingPreview source,
        Func<IsolatedSpacingPreview,Func<IsolatedWordPackage,IsolatedSpacingPreview>,T> measure)
    {
        package.RequirePassiveCalibration();
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-Isolated-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        dynamic? app=null;var opened=new List<object>();var restoreOptions=new List<Action>();
        Exception? failure=null;T result=default!;bool completed=false;int sequence=0;
        void Release(object? value){if(value is not null && Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
        try
        {
            app=Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application")??throw new InvalidOperationException("Cần Microsoft Word desktop."));
            app!.Visible=false;app.DisplayAlerts=0;app.ScreenUpdating=false;app.AutomationSecurity=3;
            dynamic options=app.Options;
            try
            {
                // Snapshot all before changing any; attempt each restoration even
                // if another setter fails. Never let one option hide the others.
                bool links=(bool)options.UpdateLinksAtOpen,fields=(bool)options.UpdateFieldsAtPrint,
                    printLinks=(bool)options.UpdateLinksAtPrint,normalPrompt=(bool)options.SaveNormalPrompt;
                void Restore(Action<dynamic> action)
                {dynamic previous=app.Options;try{action(previous);}finally{Release(previous);}}
                restoreOptions.Add(()=>Restore(p=>p.UpdateLinksAtOpen=links));
                restoreOptions.Add(()=>Restore(p=>p.UpdateFieldsAtPrint=fields));
                restoreOptions.Add(()=>Restore(p=>p.UpdateLinksAtPrint=printLinks));
                restoreOptions.Add(()=>Restore(p=>p.SaveNormalPrompt=normalPrompt));
                options.UpdateLinksAtOpen=false;options.UpdateFieldsAtPrint=false;options.UpdateLinksAtPrint=false;options.SaveNormalPrompt=false;
            }
            finally{Release(options);}
            IsolatedSpacingPreview Capture(IsolatedWordPackage current)
            {
                current.RequirePassiveCalibration();int number=++sequence;
                string path=Path.Combine(folder,$"copy-{number}.docx"),preview=Path.Combine(folder,$"copy-{number}.xps");current.WriteNew(path);
                dynamic documents=app.Documents;dynamic copy;
                try{copy=documents.Open(path,ConfirmConversions:false,ReadOnly:true,AddToRecentFiles:false,Visible:false,OpenAndRepair:false,NoEncodingDialog:true);opened.Add((object)copy);}
                finally{Release(documents);}
                if(!(bool)copy.ReadOnly || (bool)copy.TrackRevisions || (int)copy.ProtectionType!=-1)
                    throw new InvalidOperationException("Bản sao cô lập không ở trạng thái chỉ đọc được hỗ trợ.");
                copy.Repaginate();copy.ExportAsFixedFormat(preview,18,OpenAfterExport:false);
                IsolatedSpacingPreview observed;
                using(var xps=new XpsDocument(preview,FileAccess.Read))observed=IsolatedSpacingPreview.Capture(xps.GetFixedDocumentSequence());
                current.VerifyFile(path);return observed;
            }
            var baseline=Capture(package);source.RequireSame(baseline); // Gate before ANY trial is rendered.
            result=measure(baseline,Capture);completed=true;
        }
        catch(Exception error){failure=error;}
        finally
        {
            var cleanup=new List<Action>();
            foreach(var resource in opened.AsEnumerable().Reverse())
            {cleanup.Add(()=>{dynamic copy=resource;copy.Close(0);});cleanup.Add(()=>Release(resource));}
            cleanup.AddRange(restoreOptions);
            if(app is not null){cleanup.Add(()=>app.Quit(0));cleanup.Add(()=>Release(app));}
            failure=IsolatedSessionCleanup.Complete(failure,cleanup,()=>Directory.Delete(folder,true));
        }
        if(failure is not null)throw new InvalidOperationException("Phiên đo bản sao chưa hoàn tất; không sửa spacing. Thư mục tạm (nếu còn): "+folder,failure);
        if(!completed)throw new InvalidOperationException("Chưa có kết quả phiên đo bản sao.");
        return result;
    }
}
