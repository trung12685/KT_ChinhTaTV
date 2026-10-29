using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ChinhTa.Core;
using Microsoft.Win32;

namespace ChinhTa.Desktop;

internal sealed class DocumentCoverageWindow:Window
{
    internal sealed record DisplayRow(string Id,string Name,string State,string Counts,string Findings,string Detail);
    internal DataGrid Table {get;}
    internal string ReportText {get;}
    internal Button ExportButton {get;}
    internal TextBlock ExportStatus {get;}=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,8)};
    private readonly Action<string,string>? export;
    public DocumentCoverageWindow(DocumentCoverageSnapshot snapshot,Action<string,string>? export=null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);this.export=export;
        Title="Phạm vi kiểm tra tài liệu";Width=1080;Height=720;MinWidth=760;MinHeight=480;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        ReportText=snapshot.ToText();
        var root=new DockPanel{Margin=new Thickness(16)};
        var summary=new TextBlock{Text=snapshot.Summary+"\n"+DocumentCoverageSnapshot.Legend,
            TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)};
        DockPanel.SetDock(summary,Dock.Top);root.Children.Add(summary);
        var bottom=new StackPanel{Margin=new Thickness(0,12,0,0)};bottom.Children.Add(ExportStatus);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
        ExportButton=new Button{Content="Lưu báo cáo TXT…",Padding=new Thickness(12,6,12,6),Margin=new Thickness(0,0,8,0),IsEnabled=export is not null};
        ExportButton.Click+=(_,_)=>
        {
            var dialog=new SaveFileDialog{Filter="Báo cáo tiếng Việt (*.txt)|*.txt",DefaultExt=".txt",AddExtension=true,
                FileName="ChinhTaViet-pham-vi-kiem-tra.txt",OverwritePrompt=true};
            if(dialog.ShowDialog(this)!=true)return;
            if(ExportTo(dialog.FileName) is Exception error)
                MessageBox.Show(this,"Chưa lưu được báo cáo. "+error.Message,"Lưu báo cáo",MessageBoxButton.OK,MessageBoxImage.Warning);
        };
        var copy=new Button{Content="Sao chép báo cáo",Padding=new Thickness(12,6,12,6)};
        copy.Click+=(_,_)=>
        {
            try{Clipboard.SetText(ReportText);copy.Content="Đã sao chép báo cáo";}
            catch(ExternalException){MessageBox.Show(this,"Chưa sao chép được. Hãy thử lại.",Title,MessageBoxButton.OK,MessageBoxImage.Information);}
        };
        var close=new Button{Content="Đóng",IsCancel=true,Padding=new Thickness(12,6,12,6),Margin=new Thickness(8,0,0,0)};
        close.Click+=(_,_)=>Close();buttons.Children.Add(ExportButton);buttons.Children.Add(copy);buttons.Children.Add(close);
        bottom.Children.Add(buttons);DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
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
    internal Exception? ExportTo(string destination)
    {
        try
        {
            if(export is null)throw new InvalidOperationException("Chưa có nơi lưu báo cáo cho cửa sổ này.");
            export(destination,ReportText);
            ExportStatus.Text="Đã lưu báo cáo: "+destination;
            return null;
        }
        catch(Exception error)
        {
            Diagnostics.Write("UI.CoverageReport.Export",error);
            ExportStatus.Text="Chưa lưu được báo cáo; bạn có thể chọn nơi lưu khác và thử lại.";
            return error;
        }
    }
}
