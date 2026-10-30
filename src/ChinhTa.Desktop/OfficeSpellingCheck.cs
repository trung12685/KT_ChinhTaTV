using System.IO;
using System.Security.Cryptography;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeSpellingCheck
{
    internal static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        _=modern;const string text="Tương lai sáng lạng. Cần giải quyết dứt điễm vụ việc.";
        string source=OfficeManualTextCheck.Create(folder,text);byte[] hash=SHA256.HashData(File.ReadAllBytes(source));
        var previous=test.SpellingModel;test.SpellingModel=VietnameseSpellingModel.Starter;
        try
        {
            test.Open(source);var issues=test.Check();var issue=issues.Single(i=>i.Kind==FixKind.SpellingReview && i.Original=="sáng lạng");
            if(issue.CanFix || issue.SpellingSuggestions?.Single()!="xán lạn")throw new InvalidOperationException("Chính tả chưa tách thành gợi ý duyệt tay.");
            string original=test.ReadCheckText();var styles=test.ReadCheckCurrentStyles(text);var anchors=new Dictionary<Issue,PreviewAnchor>();string preview=test.ExportPreview([issue],anchors);
            try
            {
                using var xps=new XpsDocument(preview,FileAccess.Read);var document=xps.GetFixedDocumentSequence();
                if(!anchors.TryGetValue(issue,out var anchor) || new PreviewHighlighter().SelectedPhrase(document,anchor,issue.Original) is null)throw new InvalidOperationException("Chưa đo đúng cụm trên XPS.");
                var draft=test.PrepareSpellingEdit(issue,preview);test.ApplyInlineWord(draft,"xán lạn");
                string expected=original.Remove(issue.Start,issue.Length).Insert(issue.Start,"xán lạn");
                if(test.ReadCheckText()!=expected || test.CheckUndoCount!=1)throw new InvalidOperationException("Ghi cụm đã duyệt chưa giữ chữ ngoài chọn hoặc Undo.");
                var after=test.Check();if(after.Any(i=>i.Kind==FixKind.SpellingReview && i.Original=="sáng lạng") || !after.Any(i=>i.Kind==FixKind.SpellingReview && i.Original=="điễm"))throw new InvalidOperationException("Quét sau sửa chưa giữ cảnh báo chưa sửa.");
                var newStyles=test.ReadCheckCurrentStyles(text.Replace("sáng lạng","xán lạn",StringComparison.Ordinal));
                if(!styles.Complete || !newStyles.Complete || newStyles.Styles.Where(s=>s is not null).Any(s=>!styles.Styles.First(v=>v is not null)!.Matches(s!)))throw new InvalidOperationException("Kiểu chữ đổi sau sửa cụm.");
                string saved=Path.Combine(folder,"spelling-saved.docx");test.SaveCopy(saved);test.Undo();if(test.ReadCheckText()!=original)throw new InvalidOperationException("Undo chưa trả chữ gốc.");
                test.Open(saved);if(test.ReadCheckText()!=expected || !SHA256.HashData(File.ReadAllBytes(source)).SequenceEqual(hash))throw new InvalidOperationException("Lưu/mở hoặc nguồn đã đổi ngoài lượt sửa.");
                pass("Gợi ý chính tả là thủ công; đúng cụm XPS/native/XML, giữ kiểu chữ, còn cảnh báo chưa sửa, một Undo và lưu/mở/hash nguồn");
            }
            finally{File.Delete(preview);}
        }
        finally{test.SpellingModel=previous;}
    }
}
