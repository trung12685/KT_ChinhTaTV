using System.Windows.Forms;
using ChinhTa.WordProtocol;
namespace ChinhTa.WordAddin;
internal sealed class LiveReviewEpoch
{
    internal string Key {get;private set;}="";
    internal long Version {get;private set;}
    internal bool Observe(string key){if(Key==key)return false;Key=key;Version++;return true;}
    internal void Clear(){Key="";Version++;}
    internal bool Accept(long version,string key)=>Version==version && Key==key;
}
internal sealed class LiveReviewSession:IDisposable
{
    private readonly object application;
    private readonly WorkerClient worker=new WorkerClient();
    private readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer{Interval=300};
    private readonly UnderlineOverlay overlay=new UnderlineOverlay();
    private readonly LiveReviewEpoch epoch=new LiveReviewEpoch();
    private readonly Control dispatcher=new Control();
    private WordActiveParagraph? pendingParagraph;
    private object? pendingWindow;
    private long pendingVersion;
    private string pendingKey="";
    private IntPtr pendingHandle;
    private string lastFailure="";
    private DateTime changed=DateTime.UtcNow;
    private List<FindingDto> findings=new List<FindingDto>();
    private bool busy,disposed,enabled=true,nativeAttempted;
    private IntPtr owner;
    private long completedVersion=-1;
    internal event Action<IReadOnlyList<FindingDto>,string>? Changed;
    internal bool Enabled {get=>enabled;set{enabled=value;Invalidate();}}
    internal LiveReviewSession(object app){application=app;dispatcher.CreateControl();_ = dispatcher.Handle;timer.Tick+=Tick;timer.Start();}
    internal void Invalidate(){epoch.Clear();findings.Clear();overlay.Clear();Changed?.Invoke(findings,enabled?"Gạch lỗi khi gõ: đang chờ đoạn đang soạn.":"Đã tắt gạch lỗi khi gõ.");}
    private void Tick(object? sender,EventArgs args)
    {
        if(disposed)return;
        if(!enabled){overlay.Clear();return;}
        object? window=null;WordActiveParagraph? current=null;
        try
        {
            dynamic app=application;window=app.ActiveWindow;dynamic w=window;
            IntPtr handle=new IntPtr((int)w.Hwnd);
            if(!NativeView.CanRender(handle)){overlay.Clear();return;}
            if(owner!=handle){owner=handle;Invalidate();}
            current=WordActiveParagraph.Capture(window);
            if(epoch.Observe(current.Key)){changed=DateTime.UtcNow;findings.Clear();overlay.Clear();Changed?.Invoke(findings,"Đang soạn; sẽ kiểm sau khi bạn ngừng gõ một lúc.");}
            if(!nativeAttempted)
            {
                nativeAttempted=true;
                try{bool installed=WordNativeProofing.Ensure(application);Changed?.Invoke(findings,installed?"Vietnamese.dic đã đăng ký; Word có bộ kiểm tiếng Việt. Gạch riêng của Add-in đang bật.":"Đã nạp Vietnamese.dic. Word chưa có bộ kiểm tiếng Việt; Add-in vẫn gạch lỗi riêng.");}
                catch(Exception error){HostLog.Error("NativeProofing.Setup",error);Changed?.Invoke(findings,error.Message);}
            }
            if(findings.Count>0)
            {
                var visible=findings.Where(f=>(DateTime.UtcNow-changed).TotalMilliseconds>=1200 || current.CaretOffset<=f.Offset || current.CaretOffset>f.Offset+f.Length);
                Draw(window,current,visible,handle);return;
            }
            if(busy || completedVersion==epoch.Version || (DateTime.UtcNow-changed).TotalMilliseconds<650)return;
            busy=true;pendingVersion=epoch.Version;pendingKey=current.Key;pendingHandle=handle;
            var request=new BridgeRequest{Operation="scan",ReviewMode="deep",StrictSpelling=true,DocumentId=current.DocumentId,SnapshotHash=BridgeContract.Hash(current.Paragraph.Text),Paragraphs=new List<ParagraphDto>{current.Paragraph}};
            pendingParagraph=current;pendingWindow=window;current=null;window=null;
            _=SendReview(request);
        }
        catch(Exception error){Failure(error);}
        finally{current?.Dispose();ComLifetime.Release(window);}
    }
    private async Task SendReview(BridgeRequest request)
    {
        BridgeResponse? response=null;Exception? failure=null;
        try{response=await worker.Send(request).ConfigureAwait(false);}catch(Exception error){failure=error;}
        // Word is an STA host, not a WinForms application. Always dispatch the
        // completion explicitly; never rely on its SynchronizationContext.
        try{if(!disposed)dispatcher.BeginInvoke(new Action(()=>Complete(response,failure)));}
        catch(InvalidOperationException){} // UI shutdown already released pending COM refs.
    }
    private void Complete(BridgeResponse? response,Exception? failure)
    {
        var captured=pendingParagraph;var window=pendingWindow;pendingParagraph=null;pendingWindow=null;busy=false;
        try
        {
            if(disposed || !enabled || captured is null || window is null || !epoch.Accept(pendingVersion,pendingKey))return;
            if(failure is not null)throw failure;
            if(response is null)throw new InvalidOperationException("Bộ kiểm chưa có kết quả.");
            using(var now=WordActiveParagraph.Capture(window))
            {
                if(now.Key!=pendingKey){Invalidate();return;}
                findings=response.Findings;completedVersion=pendingVersion;lastFailure="";
                Changed?.Invoke(findings,findings.Count+" gợi ý trong đoạn đang soạn. Đỏ: chính tả; xanh: gợi ý dùng từ/dấu câu. Tự sửa trong Word.");
                Draw(window,now,findings,pendingHandle);
            }
        }
        catch(Exception error){Failure(error);}
        finally{captured?.Dispose();ComLifetime.Release(window);}
    }
    private void Failure(Exception error)
    {
        if(disposed)return;overlay.Clear();epoch.Clear();findings.Clear();
        string key=error.GetType().FullName+":"+error.HResult;
        if(lastFailure==key)return;lastFailure=key;
        if(error is InvalidOperationException)Changed?.Invoke(findings,error.Message);else HostLog.Error("LiveReview",error);
    }
    private void Draw(object window,WordActiveParagraph snapshot,IEnumerable<FindingDto> values,IntPtr handle)
    {
        var marks=snapshot.Measure(window,values);
        using(var verified=WordActiveParagraph.Capture(window))
            if(verified.Key==snapshot.Key && enabled && !disposed)overlay.Present(handle,marks);else Invalidate();
    }
    internal void ShowFormatHint(FindingDto finding)
    {
        if(!enabled)return;object? window=null;
        try
        {
            dynamic app=application;window=app.ActiveWindow;
            using(var now=WordActiveParagraph.Capture(window))
            {
                int length=Math.Min(64,finding.Original.Length),offset=now.CaretOffset;
                if(length<=0 || offset>now.Paragraph.Text.Length-length || now.Paragraph.Text.Substring(offset,length)!=finding.Original.Substring(0,length))return;
                epoch.Clear();epoch.Observe(now.Key);findings=new List<FindingDto>{new(){ParagraphIndex=1,Offset=offset,Length=length,Original=finding.Original.Substring(0,length),InTable=now.Paragraph.InTable,Category="Format",Message=finding.Message}};
                completedVersion=epoch.Version;
            }
        }
        finally{ComLifetime.Release(window);}
    }
    internal void Select(FindingDto finding)
    {
        object? window=null,target=null;
        try
        {
            dynamic app=application;window=app.ActiveWindow;
            using(var now=WordActiveParagraph.Capture(window))
            {
                if(now.Key!=epoch.Key || !findings.Contains(finding))throw new InvalidOperationException("Đoạn đã đổi; chờ kiểm lại trước khi chọn gợi ý.");
                dynamic w=window;object document=w.Document;
                try
                {
                    dynamic d=document;
                    string Read(int start,int end){object range=d.Range(start,end);try{dynamic r=range;return (string)r.Text;}finally{ComLifetime.Release(range);}}
                    var proven=NativeRangeProof.Resolve(now.Paragraph,finding,Read);target=d.Range(proven.Start,proven.End);dynamic r=target;r.Select();w.Activate();
                }
                finally{ComLifetime.Release(document);}
            }
        }
        finally{ComLifetime.Release(target);ComLifetime.Release(window);}
    }
    public void Dispose(){if(disposed)return;disposed=true;timer.Stop();timer.Dispose();epoch.Clear();pendingParagraph?.Dispose();pendingParagraph=null;ComLifetime.Release(pendingWindow);pendingWindow=null;dispatcher.Dispose();overlay.Dispose();worker.Dispose();}
}
