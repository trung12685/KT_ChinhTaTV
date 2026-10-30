using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeInlineWordCheck
{
    internal static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        _=modern;string source=OfficeManualTextCheck.Create(folder);byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Open(source);test.Check();
        string original=test.ReadCheckText();int at=original.IndexOf("này",StringComparison.Ordinal);
        if(at<0)throw new InvalidOperationException("Thiếu từ độc lập để thử sửa tại chữ.");
        var probe=new Issue(at,3,"này","này","Từ người dùng bấm, không phải lỗi tự động.",false,CanFix:false);
        var anchors=new Dictionary<Issue,PreviewAnchor>();string preview=test.ExportPreview([probe],anchors);
        try
        {
            using var xps=new XpsDocument(preview,FileAccess.Read);var document=xps.GetFixedDocumentSequence();var highlighter=new PreviewHighlighter();
            if(!anchors.TryGetValue(probe,out var anchor))throw new InvalidOperationException("Chưa có mốc từ mẫu.");
            var boxes=highlighter.MeasureReference(document,anchor);if(boxes.Count!=1)throw new InvalidOperationException("Chưa đo trọn từ mẫu trên XPS.");
            var box=boxes[0];var hit=highlighter.HitWord(document,anchor.Page,new Point(box.Left+box.Width/2,box.Top+box.Height/2));
            if(hit?.Word!="này")throw new InvalidOperationException("Bấm tại từ XPS chưa trả đúng từ mẫu.");
            var draft=test.PrepareInlineWord(hit.Anchor,preview);
            bool failed=false;try{test.ApplyInlineFailureProbe(draft,"mới");}catch(OfficeTextProbeException){failed=true;}
            if(!failed || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed)throw new InvalidOperationException("Lỗi sau lưu tại chữ chưa phục hồi trọn lượt.");
            pass("Bấm đúng từ trên nét XPS thật, ghép native/XML và phục hồi khi cố ý lỗi sau lưu; chưa tạo Undo giả");
        }
        finally{File.Delete(preview);}
        // Export again after rollback/reconnect; the old preview must not be reused.
        preview=test.ExportPreview([probe],anchors);
        try
        {
            using var xps=new XpsDocument(preview,FileAccess.Read);var document=xps.GetFixedDocumentSequence();var highlighter=new PreviewHighlighter();
            var box=highlighter.MeasureReference(document,anchors[probe]).Single();var hit=highlighter.HitWord(document,anchors[probe].Page,new Point(box.Left+box.Width/2,box.Top+box.Height/2))??throw new InvalidOperationException("Chưa tìm lại được từ.");
            var draft=test.PrepareInlineWord(hit.Anchor,preview);test.ApplyInlineWord(draft,"mới");
            string expected=original.Remove(at,3).Insert(at,"mới");
            if(test.ReadCheckText()!=expected || test.CheckUndoCount!=1 || !test.Changed)throw new InvalidOperationException("Lượt sửa từ chưa giữ phần ngoài chọn hoặc thiếu Undo.");
            bool stale=false;try{test.ApplyInlineWord(draft,"khác");}catch(InvalidOperationException){stale=true;}
            if(!stale || test.ReadCheckText()!=expected || test.CheckUndoCount!=1)throw new InvalidOperationException("Bản nháp cũ được ghi lại ngoài ý muốn.");
            string saved=Path.Combine(folder,"inline-word-saved.docx");test.SaveCopy(saved);test.Undo();
            if(test.ReadCheckText()!=original)throw new InvalidOperationException("Undo chưa phục hồi từ gốc.");
            test.Open(saved);test.Check();
            if(test.ReadCheckText()!=expected || !SHA256.HashData(File.ReadAllBytes(source)).SequenceEqual(hash))throw new InvalidOperationException("Lưu/mở hoặc nguồn của lượt sửa từ chưa được giữ.");
            pass("Sửa đúng từ không cần CanFix, chặn nháp cũ, một Undo, lưu/mở và hash nguồn bất biến");
        }
        finally{File.Delete(preview);}
    }
}
