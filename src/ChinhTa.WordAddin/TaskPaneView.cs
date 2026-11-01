using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ChinhTa.WordProtocol;
namespace ChinhTa.WordAddin;
[ComVisible(true),Guid(Ids.PaneGuid),ProgId(Ids.PaneProgId),ClassInterface(ClassInterfaceType.AutoDispatch)]
public sealed class TaskPaneView:UserControl
{
    private readonly WorkerClient worker=new WorkerClient();
    private object? application,window;
    private WordSnapshot? snapshot;
    private LiveReviewSession? live;
    private readonly ListBox liveFindings=new ListBox{Dock=DockStyle.Top,Height=100,HorizontalScrollbar=true};
    private readonly Label liveStatus=new Label{Dock=DockStyle.Top,Height=66,Text="Gạch khi gõ đang bật: đỏ — chính tả; xanh — gợi ý; vàng — thể thức khi chọn mục. Mở bảng để xem chi tiết."};
    private readonly ComboBox profile=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=155};
    private readonly ListBox findings=new ListBox{Dock=DockStyle.Fill,HorizontalScrollbar=true};
    private readonly TextBox details=new TextBox{Dock=DockStyle.Bottom,Height=110,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical};
    private readonly TextBox term=new TextBox{Dock=DockStyle.Top};
    private readonly ListBox terms=new ListBox{Dock=DockStyle.Fill};
    private readonly TextBox status=new TextBox{Dock=DockStyle.Bottom,Height=125,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Text="Bấm Quét để kiểm nội dung chính và ô bảng. Bấm Chọn chữ, rồi sửa bằng bàn phím trong Word. Mọi gợi ý đều cần bạn duyệt."};
    private readonly FlowLayoutPanel actions=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true};
    private bool busy;
    public TaskPaneView()
    {
        Dock=DockStyle.Fill;Font=new Font("Segoe UI",9);Padding=new Padding(8);
        var tabs=new TabControl{Dock=DockStyle.Fill};var review=new TabPage("Kiểm tra");var dictionary=new TabPage("Từ cá nhân");tabs.TabPages.Add(review);tabs.TabPages.Add(dictionary);
        review.Controls.Add(findings);review.Controls.Add(details);review.Controls.Add(liveFindings);review.Controls.Add(liveStatus);review.Controls.Add(actions);
        AddButton(actions,"Chính tả",()=>Scan("spelling"));AddButton(actions,"Kiểm sâu",()=>Scan("deep"));AddButton(actions,"Thể thức",()=>Scan("format"));AddButton(actions,"Chọn chữ",SelectFinding);
        profile.Items.AddRange(new object[]{"Nhà nước — NĐ30","Văn bản Đảng","Cá nhân"});profile.SelectedIndex=0;actions.Controls.Add(profile);
        liveFindings.SelectedIndexChanged+=(_,_)=>{if(liveFindings.SelectedItem is FindingDto f)details.Text=f.Original+"\r\n"+f.Message+"\r\nGợi ý: "+string.Join("; ",f.Suggestions);};
        liveFindings.DoubleClick+=(_,_)=>{try{if(liveFindings.SelectedItem is FindingDto f)live?.Select(f);}catch(Exception error){Fail("SelectLive",error);}};
        findings.SelectedIndexChanged+=(_,_)=>{if(findings.SelectedItem is FindingDto f)details.Text="Đoạn "+f.ParagraphIndex+(f.InTable?" trong bảng":"")+": "+f.Original+"\r\n"+f.Message+"\r\nGợi ý: "+string.Join("; ",f.Suggestions)+"\r\nBạn sửa trực tiếp trong Word; bảng này không ghi thay chữ.";};
        findings.DoubleClick+=(_,_)=>SelectFinding();
        var dictionaryActions=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true};
        dictionary.Controls.Add(terms);dictionary.Controls.Add(dictionaryActions);dictionary.Controls.Add(term);
        AddButton(dictionaryActions,"Lấy chữ đang chọn",ReadSelection);AddButton(dictionaryActions,"Thêm",()=>ChangeTerm("add"));AddButton(dictionaryActions,"Xóa mục chọn",()=>ChangeTerm("remove"));AddButton(dictionaryActions,"Tải danh sách",LoadTerms);AddButton(dictionaryActions,"Đồng bộ từ đơn với Word",SyncWord);
        Controls.Add(tabs);Controls.Add(status);
    }
    private void AddButton(FlowLayoutPanel panel,string caption,Action click)
    {var button=new Button{Text=caption,AutoSize=true};button.Click+=(_,_)=>{if(!busy)click();};panel.Controls.Add(button);}
    internal void Bind(object app,object ownerWindow,LiveReviewSession? session=null){application=app;window=ownerWindow;live=session;if(live is not null)live.Changed+=LiveChanged;}
    private void LiveChanged(IReadOnlyList<FindingDto> values,string message)
    {
        if(IsDisposed)return;
        try{RequireWindow();liveFindings.DataSource=values.ToArray();liveStatus.Text=message;}
        catch(InvalidOperationException){liveFindings.DataSource=null;liveStatus.Text="Chờ bạn quay lại cửa sổ Word này.";}
    }
    private void RequireWindow()
    {
        if(application is null || window is null)throw new InvalidOperationException("Bảng chưa gắn với cửa sổ Word.");
        dynamic app=application;object active=app.ActiveWindow;
        try{if(!ComLifetime.Same(active,window))throw new InvalidOperationException("Hãy quay về đúng cửa sổ Word có bảng này.");}
        finally{ComLifetime.Release(active);}
    }
    private async void Scan(string mode)
    {
        if(busy)return;busy=true;WordSnapshot? captured=null;
        try
        {
            ClearResults();status.Text="Đang đọc và kiểm tra văn bản. Bạn vẫn có thể gõ trong Word; hãy quét lại nếu tài liệu thay đổi.";
            RequireWindow();captured=WordSnapshot.Capture(window!,mode=="format");
            var response=await worker.Send(new(){Operation="scan",ReviewMode=mode,StrictSpelling=true,Profile=profile.SelectedIndex==1?"Party":profile.SelectedIndex==2?"Personal":"State",Sections=captured.Sections,DocumentId=captured.DocumentId,SnapshotHash=captured.Hash,Paragraphs=captured.Paragraphs.ToList()});
            if(IsDisposed)return;RequireWindow();captured.RequireCurrent(window!);
            snapshot?.Dispose();snapshot=captured;captured=null;findings.DataSource=response.Findings;details.Clear();
            status.Text=response.Source+"\r\n"+response.Findings.Count+" gợi ý. Đã kiểm "+response.ScannedParagraphs+"/"+response.TotalParagraphs+" đoạn ("+response.ScannedTableParagraphs+" trong bảng); bỏ qua "+response.SkippedParagraphs+" đoạn. "+
                (response.Truncated?"Dừng ở 1.000 mục; chưa kiểm xong tài liệu. ":"")+response.Notice;
            if(mode=="format")status.Text+="\r\nĐịnh dạng quan sát được: "+response.ObservedFormatParagraphs+"/"+response.TotalParagraphs+" đoạn; "+response.ObservedSections+" section có số đo.";
        }
        catch(Exception error){Fail("Scan",error);}
        finally{captured?.Dispose();busy=false;}
    }
    private void SelectFinding()
    {
        try{RequireWindow();if(snapshot is null || findings.SelectedItem is not FindingDto finding)return;snapshot.Select(window!,finding);if(finding.Category=="Format")live?.ShowFormatHint(finding);status.Text="Đã chọn đúng chữ. Bạn gõ sửa trong Word, rồi kiểm lại. Hoàn tác và lưu bằng Word.";}
        catch(Exception error){Fail("Select",error);}
    }
    private void ReadSelection()
    {
        object? selection=null,range=null;
        try{RequireWindow();dynamic app=application!;selection=app.Selection;dynamic s=selection;range=s.Range;dynamic r=range;term.Text=((string)r.Text).Trim();}
        catch(Exception error){Fail("ReadSelection",error);}
        finally{ComLifetime.Release(range);ComLifetime.Release(selection);}
    }
    private async void ChangeTerm(string operation)
    {
        if(busy)return;busy=true;
        try
        {
            string value=operation=="remove"?terms.SelectedItem as string??"":term.Text;
            if(value.Length==0)throw new InvalidOperationException("Nhập từ cần thêm hoặc chọn mục cần xóa.");
            var response=await worker.Send(new(){Operation=operation,Term=value});if(IsDisposed)return;
            terms.DataSource=response.Terms;ClearResults();live?.Invalidate();
            status.Text="Đã lưu từ điển cá nhân, bản "+response.DictionaryRevision+". Quét lại để cập nhật. Thêm từ không bỏ qua lỗi dùng từ theo ngữ cảnh. Chọn Đồng bộ nếu muốn Word nhận từ đơn.";
            try
            {
                var mirror=await worker.Send(new(){Operation="mirror"});if(IsDisposed)return;RequireWindow();
                WordNativeProofing.Register(application!,mirror.MirrorPath,false);status.Text="Đã lưu từ cá nhân, kiểm lại gạch riêng và đồng bộ từ đơn với Word. Cụm sai chính tả đã đối chiếu vẫn được báo. Word có thể cần mở lại để nạp tệp.";
            }
            catch(Exception error){HostLog.Error("AutoMirror",error);if(!IsDisposed)status.Text="Từ cá nhân đã lưu và áp dụng cho Add-in; Word chưa đồng bộ: "+error.Message;}
        }
        catch(Exception error){Fail("Dictionary",error);}
        finally{busy=false;}
    }
    private async void LoadTerms()
    {
        if(busy)return;busy=true;
        try{var response=await worker.Send(new(){Operation="terms"});if(!IsDisposed){terms.DataSource=response.Terms;status.Text=response.Terms.Length+" mục cá nhân; được giữ khi đóng Word và nâng cấp Add-in.";}}
        catch(Exception error){Fail("Terms",error);}finally{busy=false;}
    }
    private async void SyncWord()
    {
        if(busy)return;busy=true;
        try
        {
            RequireWindow();var response=await worker.Send(new(){Operation="mirror"});if(IsDisposed)return;RequireWindow();
            WordNativeProofing.Register(application!,response.MirrorPath,false);
            status.Text="Đã đăng ký từ cá nhân với Word, giữ từ điển/default hiện có. Word có thể cần mở lại để cập nhật gạch đỏ. "+response.Notice;
        }
        catch(Exception error){Fail("SyncWord",error);status.Text="Từ cá nhân vẫn được giữ; chưa xác nhận đồng bộ Word. "+error.Message;}
        finally{busy=false;}
    }
    private void Fail(string operation,Exception error){HostLog.Error(operation,error);if(!IsDisposed)status.Text=error.Message;}
    private void ClearResults(){findings.DataSource=null;details.Clear();snapshot?.Dispose();snapshot=null;}
    protected override void Dispose(bool disposing)
    {
        if(disposing){if(live is not null)live.Changed-=LiveChanged;live=null;worker.Dispose();snapshot?.Dispose();snapshot=null;ComLifetime.Release(window);window=null;application=null;}
        base.Dispose(disposing);
    }
}
