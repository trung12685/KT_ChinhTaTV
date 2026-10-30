using System.IO;
using System.Windows;
using System.Windows.Documents;
using ChinhTa.Core;
using Microsoft.Win32;

namespace ChinhTa.Desktop;
public partial class MainWindow
{
    private static string SpellingCache=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ChinhTaViet","Spelling","local-import.zip");
    private void ShowSpellingSource()
    {
        var source=session.SpellingModel.Source;
        SpellingInfo.Text="Chính tả thử nghiệm: "+source.Name+(source.Starter?". Chỉ vài nhóm lỗi mẫu; nạp ZIP để dùng từ điển/ngữ cảnh của bạn.":$". {source.Headwords:N0} mục từ; {source.Bigrams:N0} bigram; {source.Trigrams:N0} trigram. Mã nguồn dữ liệu: {source.Fingerprint[..12]}.")+" Gợi ý màu vàng, duyệt từng chỗ; chưa kiểm chính tả toàn diện, chưa kiểm vùng trong bảng/đối tượng.";
    }
    private void LoadSpellingCache()
    {
        try{if(File.Exists(SpellingCache)){using var stream=File.OpenRead(SpellingCache);session.SpellingModel=VietnameseSpellingImport.LoadZip(stream);}}
        catch(Exception error){Diagnostics.Write("Spelling.LoadLocal",error);session.SpellingModel=VietnameseSpellingModel.Starter;Status.Text="Chưa nạp được dữ liệu chính tả đã lưu. Đang dùng bộ mẫu nhỏ; bạn có thể nạp lại ZIP.";}
        ShowSpellingSource();
    }
    private async void ImportSpellingClick(object sender,RoutedEventArgs e)
    {
        var dialog=new OpenFileDialog{Title="Chọn 2026-10-24.zip hoặc ZIP cùng cấu trúc từ điển/mô hình",Filter="ZIP ngôn ngữ (*.zip)|*.zip",CheckFileExists=true};
        if(dialog.ShowDialog(this)!=true || !TryCloseInline())return;
        ImportSpellingMenu.IsEnabled=false;Status.Text="Đang kiểm dữ liệu chính tả trong ZIP…";
        try
        {
            string path=dialog.FileName,cache=SpellingCache;
            var model=await Task.Run(()=>{using var stream=File.OpenRead(path);return VietnameseSpellingImport.ImportAndStore(stream,cache);});
            // A user may open an inline draft while the import runs. Preserve it
            // and let the next explicit scan adopt the newly loaded resources.
            session.SpellingModel=model;ShowSpellingSource();
            if(session.IsOpen && inlineEditor is null)Run(Scan);
            Status.Text="Đã nạp dữ liệu chính tả trên máy. "+(inlineEditor is null?"Quét lại để xem gợi ý mới.":"Giữ nguyên ô đang sửa; đóng ô rồi Quét lại để xem gợi ý mới.");
        }
        catch(Exception error){Diagnostics.Write("Spelling.Import",error);MessageBox.Show(this,"Chưa nạp được dữ liệu; giữ bộ đang dùng.\n"+error.Message,"Nạp chính tả",MessageBoxButton.OK,MessageBoxImage.Warning);}
        finally{ImportSpellingMenu.IsEnabled=true;}
    }
    private void ResetSpellingClick(object sender,RoutedEventArgs e)
    {
        if(!ImportSpellingMenu.IsEnabled || !TryCloseInline())return;
        Run(()=>{if(File.Exists(SpellingCache))File.Delete(SpellingCache);session.SpellingModel=VietnameseSpellingModel.Starter;ShowSpellingSource();if(session.IsOpen)Scan();});
    }
    private void SpellingEdit(Issue issue)
    {
        if(!TryCloseInline() || Preview.Document is not FixedDocumentSequence sequence || xpsPath is null)return;
        if(!anchors.TryGetValue(issue,out var anchor)){Status.Text="Chưa ghép được cảnh báo với chữ trên trang; hãy Quét lại hoặc sửa trong Word.";return;}
        var hit=highlighter.SelectedPhrase(sequence,anchor,issue.Original);
        if(hit is null){Status.Text="Chưa xác nhận vị trí từ/cụm trên trang; hãy Quét lại hoặc sửa trong Word.";return;}
        Run(()=>{var draft=session.PrepareSpellingEdit(issue,xpsPath);OpenInlineEditor(hit,draft,issue.SpellingSuggestions);});
    }
}
