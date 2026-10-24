using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeFormatSystemCheck
{
    private static bool Component(Issue i)=>i.Kind is FixKind.AgencyFormat or FixKind.AgencyText or FixKind.RecipientFormat or
        FixKind.RecipientText or FixKind.DocumentHeadingFormat or FixKind.DocumentTypeText;
    private static Issue Title(WordSession test)=>new(test.LineTargets.Single(t=>t.Role==LineRole.Subject).RegionStart,7,"BÁO CÁO","BÁO CÁO","",false);
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"format-system");Directory.CreateDirectory(isolated);
        string source=OfficeComponentCheck.Create(modern,isolated,false),good=Create(source,isolated,false),bad=Create(source,isolated,true);
        byte[] digest=SHA256.HashData(File.ReadAllBytes(bad));test.Profile=DocumentProfile.State;test.Open(good);
        if(test.Check().Any(Component))throw new InvalidOperationException("Mẫu đúng có cỡ hợp lệ trộn/ngắt dòng/dấu cách khác kiểu còn bị báo sai.");
        var title=Title(test);var titleSizes=test.ReadCheckSizes(title);var decoration=test.ReadCheckDecorations(title with {Length=3,Original="BÁO",Replacement="BÁO"});
        if(!titleSizes.Sizes.Contains(13) || !titleSizes.Sizes.Contains(14) || decoration.Underline!=1)
            throw new InvalidOperationException("Mẫu hệ thống thiếu hai cỡ hợp lệ hoặc kiểu cần giữ.");
        string originalGood=test.ReadCheckText();var lines=OfficeComponentCheck.ReadLines(test);
        if(lines.Length!=3)throw new InvalidOperationException("Mẫu hệ thống chưa nhận đủ ba Line từ nét XPS.");
        test.FixSelection(lines);test.Check();
        if(test.Check().Any(Component) || test.ReadCheckText()!=originalGood || test.CheckUndoCount!=1 || !titleSizes.Matches(test.ReadCheckSizes(Title(test))))
            throw new InvalidOperationException("Sau ba Line, mẫu đúng có cảnh báo giả hoặc cỡ/chữ bị đổi.");
        string goodSaved=Path.Combine(isolated,"good-three-lines.docx");test.SaveCopy(goodSaved);test.Undo();
        if(test.ReadCheckText()!=originalGood)throw new InvalidOperationException("Undo mẫu đúng chưa phục hồi chữ.");
        test.Open(goodSaved);test.Check();
        if(test.Check().Any(Component) || !titleSizes.Matches(test.ReadCheckSizes(Title(test))))
            throw new InvalidOperationException("Mở lại ba Line sinh cảnh báo giả hoặc đổi cỡ hợp lệ.");
        pass("Định dạng hệ thống: hai cỡ hợp lệ/màu đen/ngắt dòng/dấu cách khác kiểu vẫn đúng trước-sau ba Line, lưu/mở và Undo");
        test.Open(bad);string original=test.ReadCheckText();var found=test.Check();
        var formats=found.Where(i=>i.CanFix && Component(i)).ToArray();
        if(formats.Count(i=>i.CharacterFormat?.PreserveSize==true)<3 || found.Any(i=>Component(i) && !i.CanFix))
            throw new InvalidOperationException("Sai thật chưa có đủ đề xuất giữ cỡ hoặc vẫn bị giữ thủ công do aggregate trộn.");
        var agency=formats.Single(i=>i.Kind==FixKind.AgencyFormat);var agencySizes=test.ReadCheckSizes(agency);
        var expectedAgency=agencySizes.Project(formats.Where(i=>i.Kind==FixKind.AgencyText).ToArray(),i=>agency.Original[..(i.Start-agency.Start)]);
        var badTitle=Title(test);var beforeTitle=test.ReadCheckSizes(badTitle);
        test.Fix(agency);
        if(!test.Check().Any(i=>i.CanFix && i.Kind==FixKind.DocumentHeadingFormat) ||
            !agencySizes.Matches(test.ReadCheckSizes(agency)) || test.ReadCheckText()!=original)
            throw new InvalidOperationException("Sửa riêng phông cơ quan làm mất lỗi chưa chọn hoặc đổi cỡ/chữ.");
        test.Undo();if(test.ReadCheckText()!=original)throw new InvalidOperationException("Undo phông riêng chưa phục hồi chữ.");
        Issue[] SelectLines()
        {
            var current=test.Check();var intents=OfficeComponentCheck.ReadLines(test);
            var model=new IssueReviewModel(current.Concat(intents));
            foreach(var choice in model.Choices.Where(c=>c.Issue.LineFix is not null))choice.IsChecked=true;
            var chosen=model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue).ToArray();
            if(chosen.Count(i=>i.LineFix is not null)!=3 || chosen.Count(i=>i.CharacterFormat?.PreserveSize==true)<3)
                throw new InvalidOperationException("Chọn ba đường kẻ chưa tích kèm đủ lỗi phông cần sửa trước.");
            return chosen;
        }
        var selected=SelectLines();
        bool rolledBack=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){rolledBack=true;}
        if(!rolledBack || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !agencySizes.Matches(test.ReadCheckSizes(agency)))
            throw new InvalidOperationException("Lỗi sau Line chưa phục hồi cả nhóm phông/chữ/cỡ.");
        selected=SelectLines();
        if(selected.Count(i=>i.LineFix is not null)!=3)throw new InvalidOperationException("Lượt chung thiếu ba Line đo thực tế.");
        test.FixSelection(selected);test.Check();
        string expected=original.Replace("đơn vị ban hành","ĐƠN VỊ BAN HÀNH",StringComparison.Ordinal);
        var freshAgency=test.LineTargets.Single(t=>t.Role==LineRole.Agency).Probe;
        if(test.ReadCheckText()!=expected || test.Check().Any(Component) || test.CheckUndoCount!=1 ||
            !beforeTitle.Matches(test.ReadCheckSizes(Title(test))) || !expectedAgency.Matches(test.ReadCheckSizes(freshAgency)) ||
            test.ReadCheckDecorations(Title(test) with {Length=3,Original="BÁO",Replacement="BÁO"})!=decoration)
            throw new InvalidOperationException("Sửa chung chưa giữ từng cỡ hợp lệ/kiểu hoặc còn lỗi phông sau ba Line.");
        string saved=Path.Combine(isolated,"format-system-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original || !agencySizes.Matches(test.ReadCheckSizes(agency)))throw new InvalidOperationException("Một Undo chưa phục hồi phông/chữ/cỡ cả lượt.");
        test.Open(saved);test.Check();
        if(test.Check().Any(Component) || test.ReadCheckText()!=expected || !beforeTitle.Matches(test.ReadCheckSizes(Title(test))) ||
            !SHA256.HashData(File.ReadAllBytes(bad)).SequenceEqual(digest))throw new InvalidOperationException("Lưu/mở hệ thống chưa đúng hoặc nguồn bị đổi.");
        pass("Định dạng hệ thống: chọn ba Line tích kèm đúng phông/chữ, sửa riêng/chung giữ từng cỡ/gạch chân, rollback cả lượt, một Undo/lưu-mở/nguồn");
    }
    private static string Create(string source,string folder,bool bad)
    {
        string path=Path.Combine(folder,bad?"format-system-bad.docx":"format-system-good.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
        using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,float size,bool bold=false,bool italic=false,bool wrong=false,bool underline=false)=>new(w+"r",
            new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii",wrong?"Arial":"Times New Roman"),new XAttribute(w+"hAnsi",wrong?"Arial":"Times New Roman")),
                new XElement(w+"sz",new XAttribute(w+"val",size*2)),new XElement(w+"b",new XAttribute(w+"val",bold?1:0)),
                new XElement(w+"i",new XAttribute(w+"val",italic?1:0)),new XElement(w+"color",new XAttribute(w+"val",wrong?"FF0000":"000000")),
                new XElement(w+"u",new XAttribute(w+"val",underline?"single":"none"))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        XElement Row(string text)=>xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))==text);
        void Replace(string text,params XElement[] runs){var p=Row(text);p.Elements(w+"r").Remove();p.Add(runs);}
        Replace("CƠ QUAN CHỦ QUẢN",Run("CƠ",12),Run(" ",8,wrong:true),Run("QUAN CHỦ QUẢN",13));
        Replace("ĐƠN VỊ BAN HÀNH",Run(bad?"đ":"Đ",12,true,wrong:bad),Run(bad?"ơn":"ƠN",13,true,wrong:bad),Run(" ",8,wrong:true),Run(bad?"vị ban hành":"VỊ BAN HÀNH",13,true));
        Replace("BÁO CÁO",Run("BÁO",13,true,underline:true),Run(" ",8,wrong:true),Run("CÁO",14,true,wrong:bad));
        var soft=Run("",8,wrong:true);soft.Element(w+"t")!.ReplaceWith(new XElement(w+"br"));
        Replace("Mang lại bình ",Run("Mang lại",13,true,italic:bad),Run(" ",8,wrong:true),soft,Run("bình yên",14,true));
        Replace("Số: 01/BC-ĐV",Run("Số:",13,true),Run(" 01/BC-ĐV",13));
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
