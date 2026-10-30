using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal sealed class InlineWordEditor
{
    private readonly Popup popup=new(){StaysOpen=true,AllowsTransparency=true,Placement=PlacementMode.Relative};
    private readonly Window owner;
    private readonly InlineWordDraft draft;
    private readonly Func<string,ManualApplyResult> apply;
    private readonly Action closed;
    private readonly PreviewWordHit hit;
    private bool busy,blocked,applied,composing,closedOnce,detached;
    private Point? lastPosition;
    internal TextBox Editor {get;}
    internal IReadOnlyList<Button> SuggestionButtons {get;}
    internal TextBlock Feedback {get;}
    internal Button ApplyButton {get;}
    internal bool IsOpen=>popup.IsOpen;
    internal bool Dirty=>!applied && Editor.Text!=draft.Original;
    internal InlineWordEditor(Window owner,PreviewWordHit hit,InlineWordDraft draft,Func<string,ManualApplyResult> apply,Action closed,IReadOnlyList<string>? suggestions=null)
    {
        this.owner=owner;this.hit=hit;this.draft=draft;this.apply=apply;this.closed=closed;
        var body=new StackPanel{Width=Math.Clamp(hit.Bounds.Width+90,220,420)};
        Editor=new TextBox{Text=draft.Original,FontSize=18,Padding=new Thickness(5),AcceptsReturn=false,AcceptsTab=false,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MaxHeight=100};
        Feedback=new TextBlock{Text="Sửa chữ tại đây • Enter ghi • Esc hủy. Chưa ghi vào Word.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,5,0,5)};
        body.Children.Add(Editor);body.Children.Add(new ScrollViewer{MaxHeight=130,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Content=Feedback});
        var proposals=new WrapPanel();var suggestionButtons=new List<Button>();
        foreach(string suggestion in (suggestions??[]).Distinct(StringComparer.Ordinal).Take(3))
        {
            var button=new Button{Content=suggestion,Margin=new Thickness(0,0,5,5),Padding=new Thickness(5),ToolTip="Điền vào ô sửa; chưa ghi vào Word."};
            button.Click+=(_,_)=>{if(busy || blocked || applied)return;Editor.Text=suggestion;Editor.Focus();Editor.SelectAll();};
            proposals.Children.Add(button);suggestionButtons.Add(button);
        }
        SuggestionButtons=suggestionButtons.AsReadOnly();body.Children.Add(proposals);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
        ApplyButton=new Button{Content="Ghi chữ tôi sửa",IsEnabled=false,Padding=new Thickness(8,4,8,4)};
        var cancel=new Button{Content="Hủy / Đóng",Margin=new Thickness(8,0,0,0),Padding=new Thickness(8,4,8,4)};
        buttons.Children.Add(ApplyButton);buttons.Children.Add(cancel);body.Children.Add(buttons);
        popup.Child=new Border{Background=Brushes.White,BorderBrush=Brushes.DarkGoldenrod,BorderThickness=new Thickness(1.5),Padding=new Thickness(8),Child=body};
        popup.PlacementTarget=hit.Page;popup.HorizontalOffset=hit.Bounds.Left;popup.VerticalOffset=hit.Bounds.Top;
        Editor.TextChanged+=(_,_)=>ApplyButton.IsEnabled=!busy && !blocked && Dirty;
        TextCompositionManager.AddPreviewTextInputStartHandler(Editor,(_,_)=>composing=true);
        Editor.PreviewTextInput+=(_,_)=>composing=false;
        Editor.PreviewKeyDown+=(_,e)=>
        {
            if(composing || e.Key==Key.ImeProcessed || Keyboard.Modifiers!=ModifierKeys.None)return;
            if(e.Key==Key.Enter){e.Handled=true;Apply();}
            else if(e.Key==Key.Escape){e.Handled=true;TryClose(ConfirmDiscard);}
        };
        ApplyButton.Click+=(_,_)=>Apply();cancel.Click+=(_,_)=>TryClose(ConfirmDiscard);
        popup.Opened+=(_,_)=>{Editor.Focus();Keyboard.Focus(Editor);Editor.SelectAll();};
        popup.Closed+=(_,_)=>NotifyClosed();
    }
    private bool ConfirmDiscard()=>MessageBox.Show(owner,"Chữ đang nhập chưa được ghi. Đóng và bỏ bản nháp?","Sửa tại chữ",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes;
    internal void Open(){popup.IsOpen=true;Editor.Focus();Keyboard.Focus(Editor);Editor.SelectAll();}
    internal bool TryClose(Func<bool> confirmDiscard)
    {
        if(busy || Dirty && !confirmDiscard())return false;
        ForceClose();return true;
    }
    internal void ForceClose(){popup.IsOpen=false;NotifyClosed();}
    private void NotifyClosed(){if(closedOnce)return;closedOnce=true;closed();}
    internal void Focus()=>Editor.Focus();
    internal void DetachAnchor()
    {
        if(!IsOpen || detached)return;detached=true;popup.PlacementTarget=owner;popup.Placement=PlacementMode.Center;popup.HorizontalOffset=popup.VerticalOffset=0;
    }
    internal void Reposition()
    {
        if(!IsOpen || detached)return;
        try
        {
            var current=hit.Page.TranslatePoint(new Point(),owner);
            if(lastPosition==current)return;lastPosition=current;
            popup.HorizontalOffset=hit.Bounds.Left+.01;popup.HorizontalOffset=hit.Bounds.Left;
        }
        catch(InvalidOperationException){DetachAnchor();}
    }
    internal void Apply()
    {
        if(busy || blocked || applied)return;
        if(composing){Feedback.Text="Hãy hoàn tất gõ tiếng Việt trước khi ghi.";return;}
        try{_=draft.Change(Editor.Text);}catch(InvalidOperationException error){Feedback.Text=error.Message;return;}
        busy=true;Editor.IsReadOnly=true;ApplyButton.IsEnabled=false;
        try
        {
            var result=apply(Editor.Text);applied=result.Applied;
            if(applied && result.Reviewed){ForceClose();return;}
            blocked=true;DetachAnchor();
            Feedback.Text=(applied?"Đã ghi nhưng chưa quét lại được. Không ghi lần nữa.\n":"Chưa xác nhận ghi; bản nháp được giữ, không tự gửi lại.\n")+
                (result.Error??"Hãy đóng ô sửa và Quét lại.");
        }
        catch(Exception error)
        {
            blocked=true;DetachAnchor();Diagnostics.Write("UI.Inline.Callback",error);
            Feedback.Text="Chưa xác nhận kết quả. Bản nháp còn ở đây để sao chép; không ghi lại. Giữ bản làm việc và Quét lại.\n"+error.Message;
        }
        finally{busy=false;Editor.IsReadOnly=applied;ApplyButton.IsEnabled=false;}
    }
}
