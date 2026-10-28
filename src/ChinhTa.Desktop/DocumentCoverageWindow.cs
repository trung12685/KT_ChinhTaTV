using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal sealed class DocumentCoverageWindow:Window
{
    internal sealed record DisplayRow(string Id,string Name,string State,string Counts,string Findings,string Detail);
    internal DataGrid Table {get;}
    internal string ReportText {get;}
    public DocumentCoverageWindow(DocumentCoverageSnapshot snapshot)
    {
        Title="Phạm vi kiểm tra tài liệu";Width=1080;Height=720;MinWidth=760;MinHeight=480;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        ReportText=snapshot.ToText();
        var root=new DockPanel{Margin=new Thickness(16)};
        var summary=new TextBlock{Text=snapshot.Summary+"\n"+DocumentCoverageSnapshot.Legend,
            TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)};
        DockPanel.SetDock(summary,Dock.Top);root.Children.Add(summary);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,12,0,0)};
        var copy=new Button{Content="Sao chép báo cáo",Padding=new Thickness(12,6,12,6)};
        copy.Click+=(_,_)=>
        {
            try{Clipboard.SetText(ReportText);copy.Content="Đã sao chép báo cáo";}
            catch(ExternalException){MessageBox.Show(this,"Chưa sao chép được. Hãy thử lại.",Title,MessageBoxButton.OK,MessageBoxImage.Information);}
        };
        var close=new Button{Content="Đóng",IsCancel=true,Padding=new Thickness(12,6,12,6),Margin=new Thickness(8,0,0,0)};
        close.Click+=(_,_)=>Close();buttons.Children.Add(copy);buttons.Children.Add(close);
        DockPanel.SetDock(buttons,Dock.Bottom);root.Children.Add(buttons);
        Table=new DataGrid{IsReadOnly=true,AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,
            CanUserReorderColumns=false,HeadersVisibility=DataGridHeadersVisibility.Column,SelectionMode=DataGridSelectionMode.Single,
            ItemsSource=snapshot.Rows.Select(r=>new DisplayRow(r.Id,r.Name,DocumentCoverageSnapshot.Label(r.State),
                $"{r.ObservedCount}/{r.RecognizedCount}",$"{r.RepairableFindings} đề xuất; {r.ManualFindings} thủ công",r.Reason+"\n"+r.Scope)).ToArray()};
        void Column(string title,string property,double width)
        {
            var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.TextWrappingProperty,TextWrapping.Wrap));
            style.Setters.Add(new Setter(FrameworkElement.MarginProperty,new Thickness(4)));
            Table.Columns.Add(new DataGridTextColumn{Header=title,Binding=new Binding(property),Width=new DataGridLength(width),ElementStyle=style});
        }
        Column("Mã",nameof(DisplayRow.Id),48);Column("Nhóm kiểm",nameof(DisplayRow.Name),175);
        Column("Phạm vi đã quan sát",nameof(DisplayRow.State),175);Column("Đã quan sát/nhận diện",nameof(DisplayRow.Counts),105);
        Column("Cảnh báo",nameof(DisplayRow.Findings),135);Column("Ghi nhận và giới hạn",nameof(DisplayRow.Detail),355);
        root.Children.Add(Table);Content=root;
    }
}
