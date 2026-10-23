using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class DocumentTypeCatalogDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var view=new DocumentTypeCatalogWindow();
        try
        {
            view.Show();view.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
            assert(view.CatalogGrid.Items.Count==31 && view.CatalogGrid.IsReadOnly && !view.CatalogGrid.CanUserAddRows && !view.CatalogGrid.CanUserDeleteRows,
                "The actual Word-independent reference window displays 31 immutable catalog entries");
            assert(view.CatalogGrid.Columns.Count==3 && ((DataGridTextColumn)view.CatalogGrid.Columns[1]).Binding is System.Windows.Data.Binding {Path.Path:"Abbreviation"},
                "The reference grid binds exact abbreviations rather than changing their case");
            var custom=(DocumentTypeEntry)view.CatalogGrid.Items[30];
            assert(custom.Source==DocumentTypeSource.UserSupplement && custom.Name=="Báo cáo đề xuất" && custom.Abbreviation=="BCĐX" && custom.SourceLabel=="Người dùng bổ sung",
                "BCĐX is visibly attributed to the user rather than the decree");
            view.CatalogGrid.ScrollIntoView(custom);view.UpdateLayout();view.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
            var picture=new RenderTargetBitmap((int)view.ActualWidth,(int)view.ActualHeight,96,96,PixelFormats.Pbgra32);picture.Render(view);
            string folder=Path.Combine(Environment.CurrentDirectory,"artifacts");Directory.CreateDirectory(folder);
            var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(picture));using(var output=File.Create(Path.Combine(folder,"document-types-preview.png")))png.Save(output);
            assert(new FileInfo(Path.Combine(folder,"document-types-preview.png")).Length>1000,
                "A real WPF catalog preview is captured without Word or private documents");
        }
        finally{view.Close();}
    }
}
