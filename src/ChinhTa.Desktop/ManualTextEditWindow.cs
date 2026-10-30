using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal sealed record ManualApplyResult(bool Applied,bool Reviewed,string? Error);
internal sealed class ManualTextEditWindow:Window
{
    internal TextBox Editor {get;}
    internal TextBlock Feedback {get;}
    internal Button ApplyButton {get;}
    private readonly ManualTextTarget target;
    private readonly Func<string,ManualApplyResult> apply;
    private bool applied,busy,blocked;
    internal ManualTextEditWindow(ManualTextTarget target,Func<string,ManualApplyResult> apply,int selectionStart=0,int selectionLength=0)
    {
        this.target=target;this.apply=apply;Title="Sửa thủ công — nội dung do bạn nhập";Width=680;Height=410;MinWidth=520;MinHeight=330;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel{Margin=new Thickness(16)};
        var note=new TextBlock{Text="Bạn sửa chữ trong đoạn bên dưới rồi bấm Áp dụng. Bản thử giữ kiểu chữ của vùng chứa chỗ sửa; hãy sửa từng từ/vùng trong một lần. Không sửa bảng, phông hoặc lề trong ô này.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)};
        DockPanel.SetDock(note,Dock.Top);panel.Children.Add(note);
        Feedback=new TextBlock{Text="Chưa ghi thay đổi vào Word.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)};
        var bottom=new StackPanel();DockPanel.SetDock(bottom,Dock.Bottom);bottom.Children.Add(new ScrollViewer{MaxHeight=110,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Content=Feedback});
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
        ApplyButton=new Button{Content="Áp dụng nội dung tôi sửa",Padding=new Thickness(12,6,12,6),IsEnabled=false};
        var reset=new Button{Content="Trả lại bản gốc",Margin=new Thickness(8,0,8,0),Padding=new Thickness(10,6,10,6)};
        var close=new Button{Content="Đóng",Padding=new Thickness(12,6,12,6),IsCancel=true};
        buttons.Children.Add(ApplyButton);buttons.Children.Add(reset);buttons.Children.Add(close);bottom.Children.Add(buttons);panel.Children.Add(bottom);
        Editor=new TextBox{Text=target.Original,AcceptsReturn=false,AcceptsTab=false,TextWrapping=TextWrapping.Wrap,
            VerticalScrollBarVisibility=ScrollBarVisibility.Auto,FontSize=16,Padding=new Thickness(8)};
        panel.Children.Add(Editor);Content=panel;
        Loaded+=(_,_)=>{Editor.Focus();if(selectionStart>=0 && selectionLength>=0 && (long)selectionStart+selectionLength<=Editor.Text.Length)Editor.Select(selectionStart,selectionLength);};
        Editor.TextChanged+=(_,_)=>ApplyButton.IsEnabled=!blocked && !busy && Editor.Text!=target.Original;
        reset.Click+=(_,_)=>{Editor.Text=target.Original;if(!blocked)Feedback.Text="Đã trả lại bản gốc trong ô sửa; chưa ghi vào Word.";};
        close.Click+=(_,_)=>Close();ApplyButton.Click+=(_,_)=>Apply();Closing+=CheckClosing;
    }
    internal void Apply()
    {
        if(busy || blocked || applied)return;
        try{_=target.Edit(Editor.Text);}catch(InvalidOperationException error){Feedback.Text=error.Message;return;}
        busy=true;ApplyButton.IsEnabled=false;Editor.IsReadOnly=true;
        try
        {
            var result=apply(Editor.Text);
            if(result.Applied)
            {
                applied=true;Feedback.Text=result.Reviewed?"Đã áp dụng và quét lại. Hoàn tác một lần để trở về.":"Đã áp dụng nhưng chưa quét lại được. Không áp dụng lần nữa.\n"+(result.Error??"Hãy đóng ô sửa và Quét lại.");
                if(result.Reviewed && IsVisible)DialogResult=true;
            }
            else{blocked=true;Feedback.Text=(result.Error??"Chưa áp dụng được.")+"\nBản nháp còn trong ô. Sao chép nếu cần giữ, rồi đóng và Quét lại trước khi thử tiếp.";}
        }
        catch(Exception error)
        {
            blocked=true;Diagnostics.Write("UI.ManualEdit.Callback",error);
            Feedback.Text="Chưa xác nhận được kết quả thao tác. Không áp dụng lại bản nháp này; giữ bản làm việc và nhật ký, đóng ô sửa rồi kiểm tra tài liệu.\n"+error.Message;
        }
        finally{busy=false;Editor.IsReadOnly=applied;ApplyButton.IsEnabled=false;}
    }
    private void CheckClosing(object? sender,CancelEventArgs args)
    {
        if(busy){args.Cancel=true;return;}
        if(!applied && Editor.Text!=target.Original && MessageBox.Show(this,"Bản nháp chưa được áp dụng. Đóng và bỏ bản nháp?","Sửa thủ công",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)!=MessageBoxResult.Yes)args.Cancel=true;
    }
}
