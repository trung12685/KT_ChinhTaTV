using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeBodyEmphasisLayoutCheck
{
    private const string Old="Nội dung đã đạt yêu cầu cần được giữ nguyên.";
    private const string Text="Nội dung  kế hoạch kế hoạch đã được duyệt.";
    private const string Next="Đoạn văn tiếp theo cần được giữ nguyên định dạng.";
    private static bool Body(Issue i)=>i.Kind is FixKind.BodyFormat or FixKind.BodyParagraph or FixKind.BodyGap or FixKind.BodyLineSpacing;
    public static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"body-emphasis-layout");Directory.CreateDirectory(isolated);
        string source=Path.Combine(isolated,"layout-input.docx");File.Copy(seed,source);
        using(var zip=ZipFile.Open(source,ZipArchiveMode.Update))
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
            using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XElement Run(string value,int size,bool bold,bool italic)=>new(w+"r",new XElement(w+"rPr",
                new XElement(w+"rFonts",new XAttribute(w+"ascii","Arial"),new XAttribute(w+"hAnsi","Arial")),new XElement(w+"sz",new XAttribute(w+"val",size*2)),
                new XElement(w+"b",new XAttribute(w+"val",bold?1:0)),new XElement(w+"i",new XAttribute(w+"val",italic?1:0)),
                new XElement(w+"u",new XAttribute(w+"val","single")),new XElement(w+"color",new XAttribute(w+"val","FF0000"))),
                new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
            void Layout(XElement row,bool correct)
            {
                var properties=row.Element(w+"pPr")!;
                foreach(string name in new[]{"ind","jc","spacing","contextualSpacing","keepNext","pageBreakBefore"})properties.Element(w+name)?.Remove();
                properties.Add(new XElement(w+"ind",new XAttribute(w+"left",0),new XAttribute(w+"right",0),new XAttribute(w+"firstLine",correct?720:0)),
                    new XElement(w+"jc",new XAttribute(w+"val",correct?"both":"left")),new XElement(w+"spacing",new XAttribute(w+"before",0),
                        new XAttribute(w+"after",correct?120:0),new XAttribute(w+"line",correct?360:480),new XAttribute(w+"lineRule","auto"),
                        new XAttribute(w+"beforeAutospacing",0),new XAttribute(w+"afterAutospacing",0)));
                foreach(string name in new[]{"contextualSpacing","keepNext","pageBreakBefore"})properties.Add(new XElement(w+name,new XAttribute(w+"val",0)));
            }
            var row=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))==Old);
            var next=new XElement(row);next.Elements(w+"r").Remove();next.Add(Run(Next,14,false,false));Layout(next,true);row.AddAfterSelf(next);
            row.Elements(w+"r").Remove();row.Add(Run("Nội dung  ",12,false,false),Run("kế hoạch ",14,false,true),Run("kế hoạch đã được duyệt.",13,true,true));Layout(row,false);
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        }
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);test.Check();
        string original=test.ReadCheckText();var before=test.ReadCheckCurrentStyles(Text);var neighbor=test.ReadCheckCurrentStyles(Next);
        foreach(var kind in new[]{FixKind.BodyParagraph,FixKind.BodyGap,FixKind.BodyLineSpacing})
        {
            var item=test.Check().Single(i=>i.CanFix && i.Kind==kind && i.Original==Text);
            var expected=item.ParagraphFix is { } layout?before.WithParagraph(layout):before.WithSpacing(item.SpacingFix!);
            test.Fix(item);test.Check();
            if(test.ReadCheckText()!=original || test.CheckUndoCount!=1 || !expected.Matches(test.ReadCheckCurrentStyles(Text)) ||
                !neighbor.Matches(test.ReadCheckCurrentStyles(Next)) || test.Check().Any(i=>i.Kind==kind && i.Original==Text) ||
                !test.Check().Any(i=>i.Kind==FixKind.BodyFormat && i.Original==Text))
                throw new InvalidOperationException("Sửa riêng căn/spacing ở đoạn nhấn mạnh làm đổi kiểu/cỡ/phông/chữ chưa chọn hoặc còn lỗi đã sửa.");
            test.Undo();test.Check();if(!before.Matches(test.ReadCheckCurrentStyles(Text)))throw new InvalidOperationException("Undo căn/spacing nhấn mạnh chưa giữ nguyên kiểu.");
        }
        Issue[] Selected()
        {
            var current=test.Check();var styles=current.Where(i=>i.CanFix && Body(i) && i.Original==Text).ToArray();
            if(styles.Length!=4)throw new InvalidOperationException("Mẫu nhấn mạnh chưa có đủ bốn nhóm thể thức độc lập.");
            var first=styles[0];return styles.Concat(current.Where(i=>i.CanFix && !i.IsStyleOnly && i.Format is null &&
                i.Start>=first.Start && (long)i.Start+i.Length<=(long)first.Start+first.Length)).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        var selected=Selected();bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !before.Matches(test.ReadCheckCurrentStyles(Text)) ||
            !neighbor.Matches(test.ReadCheckCurrentStyles(Next)))throw new InvalidOperationException("Rollback thể thức/chữ/Line nhấn mạnh chưa khôi phục đủ lượt.");
        selected=Selected();var result=before;var anchor=selected.First(i=>Body(i));
        foreach(var item in selected.Where(i=>i.IsStyleOnly))result=item.CharacterFormat is { } font?result.WithFormat(font):item.ParagraphFix is { } layout?
            result.WithParagraph(layout,before.Paragraph):result.WithSpacing(item.SpacingFix!,before.Paragraph);
        result=result.Project(selected.Where(i=>!i.IsStyleOnly && i.Format is null && i.LineFix is null).ToArray(),i=>Text[..(i.Start-anchor.Start)]);
        test.FixSelection(selected);test.Check();string expectedText=original.Replace(Text,result.Text,StringComparison.Ordinal);
        if(test.ReadCheckText()!=expectedText || test.CheckUndoCount!=1 || !result.Matches(test.ReadCheckCurrentStyles(result.Text)) ||
            !neighbor.Matches(test.ReadCheckCurrentStyles(Next)) || test.Check().Any(i=>Body(i) && i.Original==result.Text))
            throw new InvalidOperationException("Sửa chung bốn nhóm/chữ/Line nhấn mạnh chưa giữ kiểu/cỡ/đoạn kế hoặc còn mục đã sửa.");
        string saved=Path.Combine(isolated,"layout-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !before.Matches(test.ReadCheckCurrentStyles(Text)))throw new InvalidOperationException("Một Undo chưa về nguyên kiểu/căn/spacing.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=expectedText || !result.Matches(test.ReadCheckCurrentStyles(result.Text)) || !neighbor.Matches(test.ReadCheckCurrentStyles(Next)) ||
            !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Lưu/mở thể thức nhấn mạnh chưa đúng hoặc nguồn bị đổi.");
        pass("Đoạn có từ nhấn mạnh: căn/thụt/khoảng đoạn/giãn dòng riêng và chung phông/chữ/ba Line; giữ từng kiểu/cỡ, rollback/một Undo/lưu-mở/nguồn");
    }
}
