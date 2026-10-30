using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Read-only reference, independent of Word and the document's issue selection.
public sealed class DocumentTypeCatalogWindow:Window
{
    public DataGrid CatalogGrid{get;}=new(){IsReadOnly=true,AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,
        CanUserReorderColumns=false,RowHeight=26,HeadersVisibility=DataGridHeadersVisibility.Column,GridLinesVisibility=DataGridGridLinesVisibility.Horizontal};
    public DocumentTypeCatalogWindow()
    {
        Title="Danh mục tên loại văn bản";Width=970;Height=760;MinWidth=720;MinHeight=460;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new DockPanel{Margin=new(18)};Content=root;Background=new SolidColorBrush(Color.FromRgb(241,245,249));
        var heading=new TextBlock{Text="TÊN LOẠI VÀ CHỮ VIẾT TẮT",FontWeight=FontWeights.Bold,FontSize=20,Margin=new(0,0,0,10)};
        DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
        var note=new TextBlock{Text="NĐ30, Phụ lục III, phần I, trang 40: 27 tên loại hành chính và 3 loại bản sao. Bổ sung theo yêu cầu của bạn: Báo cáo đề xuất — BCĐX.",
            TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,10)};DockPanel.SetDock(note,Dock.Top);root.Children.Add(note);
        var footer=new TextBlock{Text="Giữ đúng QyĐ, CTr, TTr, TrS. BCĐX do người dùng bổ sung. Công văn không có dòng tên loại: trích yếu V/v cỡ 12–13 dưới số/ký hiệu, không kẻ trích yếu. Tên loại/trích yếu của văn bản có tên loại cỡ 13–14 đậm, căn giữa và có nét dưới trích yếu. Bảng chỉ để tra cứu; không đổi mã cơ quan hoặc thể thức bản sao.",
            TextWrapping=TextWrapping.Wrap,Margin=new(0,10,0,0),Foreground=Brushes.DimGray};DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        CatalogGrid.Columns.Add(new DataGridTextColumn{Header="Tên loại",Binding=new Binding(nameof(DocumentTypeEntry.Name)),Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
        CatalogGrid.Columns.Add(new DataGridTextColumn{Header="Viết tắt",Binding=new Binding(nameof(DocumentTypeEntry.Abbreviation)),Width=90});
        CatalogGrid.Columns.Add(new DataGridTextColumn{Header="Nguồn / phạm vi",Binding=new Binding(nameof(DocumentTypeEntry.SourceLabel)),Width=260});
        var row=new Style(typeof(DataGridRow));
        var custom=new DataTrigger{Binding=new Binding(nameof(DocumentTypeEntry.Source)),Value=DocumentTypeSource.UserSupplement};
        custom.Setters.Add(new Setter(BackgroundProperty,Brushes.LightYellow));row.Triggers.Add(custom);CatalogGrid.RowStyle=row;
        CatalogGrid.ItemsSource=DocumentTypeCatalog.Entries;root.Children.Add(CatalogGrid);
    }
}
