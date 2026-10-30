using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeManualTextCheck
{
    internal const string Text="Người dùng tự sửa nội dung trong bản thử này.";
    internal const string Edited="Người dùng chủ động sửa nội dung trong bản thử này.";
    internal static string Create(string folder)
    {
        XNamespace p="http://schemas.microsoft.com/office/2006/xmlPackage",w="http://schemas.openxmlformats.org/wordprocessingml/2006/main",r="http://schemas.openxmlformats.org/package/2006/relationships";
        XElement Part(string name,string type,XElement root)=>new(p+"part",new XAttribute(p+"name","/"+name),new XAttribute(p+"contentType",type),new XElement(p+"xmlData",root));
        XElement Paragraph(string text)=>new(w+"p",new XElement(w+"pPr",new XElement(w+"spacing",new XAttribute(w+"after",120))),new XElement(w+"r",
            new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),new XElement(w+"b"),new XElement(w+"sz",new XAttribute(w+"val",26))),new XElement(w+"t",text)));
        string flat=new XElement(p+"package",Part("_rels/.rels","application/vnd.openxmlformats-package.relationships+xml",new XElement(r+"Relationships",new XElement(r+"Relationship",new XAttribute("Id","main"),new XAttribute("Type","http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),new XAttribute("Target","word/document.xml")))),
            Part("word/document.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",new XElement(w+"document",new XElement(w+"body",Paragraph(Text),Paragraph("Đoạn này phải giữ nguyên chữ và định dạng."),new XElement(w+"sectPr"))))).ToString(SaveOptions.DisableFormatting);
        string file=Path.Combine(folder,"manual-input.docx");IsolatedWordPackage.FromFlat(flat).WriteNew(file);return file;
    }
    internal static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"manual-text");Directory.CreateDirectory(isolated);string source=Create(isolated);var hash=SHA256.HashData(File.ReadAllBytes(source));
        test.Open(source);test.Check();string original=test.ReadCheckText();var beforeStyles=test.ReadCheckCurrentStyles(Text);
        ManualTextTarget Prepare()
        {
            var p=test.ReadCheckCurrentParagraph(Text);return test.PrepareManualText(new(p.Start,Text.Length,Text,Text,"Nội dung do người dùng chọn sửa.",false,CanFix:false));
        }
        var target=Prepare();bool restored=false;
        try{test.ApplyManualFailureProbe(target,Edited);}catch(OfficeTextProbeException){restored=true;}
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed)throw new InvalidOperationException("Sửa thủ công lỗi muộn chưa phục hồi nguồn hoặc làm phát sinh Undo.");
        target=Prepare();test.ApplyManualText(target,Edited);test.Check();
        if(test.ReadCheckText()!=original.Replace(Text,Edited,StringComparison.Ordinal) || test.CheckUndoCount!=1 || !test.Changed)throw new InvalidOperationException("Sửa nội dung do người dùng nhập chưa đúng phạm vi hoặc chưa có một Undo.");
        var actualStyles=test.ReadCheckCurrentStyles(Edited);
        if(actualStyles.Styles.Where(s=>s is not null).Any(s=>!beforeStyles.Styles.First(v=>v is not null)!.Matches(s!)))throw new InvalidOperationException("Sửa thủ công không giữ kiểu chữ của đoạn mẫu.");
        string saved=Path.Combine(isolated,"manual-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || test.CheckUndoCount!=0 || !beforeStyles.Matches(test.ReadCheckCurrentStyles(Text)))throw new InvalidOperationException("Một Undo chưa phục hồi cả lượt thủ công.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=original.Replace(Text,Edited,StringComparison.Ordinal) || !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Lưu/mở lượt thủ công chưa giữ nội dung hoặc nguồn đầu vào.");
        pass("Sửa chữ do người dùng nhập: giữ đoạn khác/kiểu, lỗi sau lưu phục hồi, một Undo, lưu/mở và nguồn bất biến");
    }
}
