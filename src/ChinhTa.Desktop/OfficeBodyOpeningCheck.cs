using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeBodyOpeningCheck
{
    private const string First="Nội dung  nội dung đã được duyệt.";
    private const string Second="Công tác tổng hợp đã được thực hiện đầy đủ.";
    private const string Third="Nội dung đã đạt yêu cầu cần được giữ nguyên.";
    private static bool Body(Issue i)=>i.Kind is FixKind.BodyFormat or FixKind.BodyParagraph or FixKind.BodyGap or FixKind.BodyLineSpacing;
    public static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"no-greeting");Directory.CreateDirectory(isolated);
        string source=Path.Combine(isolated,"no-greeting-input.docx");File.Copy(seed,source);
        using(var zip=ZipFile.Open(source,ZipArchiveMode.Update))
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
            using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value)).StartsWith("Kính gửi:",StringComparison.Ordinal)).Remove();
            foreach(var (text,first) in new[]{(First,567),(Second,720),(Third,0)})
            {
                var row=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))==text);
                var properties=row.Element(w+"pPr")!;properties.Element(w+"ind")?.Remove();properties.Element(w+"jc")?.Remove();
                properties.Add(new XElement(w+"ind",new XAttribute(w+"left",0),new XAttribute(w+"right",0),new XAttribute(w+"firstLine",first)),
                    new XElement(w+"jc",new XAttribute(w+"val",text==Third?"left":"both")));
            }
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        }
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);
        string original=test.ReadCheckText();var found=test.Check();
        var chosen=found.Where(i=>i.CanFix && Body(i) && i.Original is First or Second or Third).ToArray();
        if(chosen.Count(i=>i.Kind==FixKind.BodyFormat)!=2 || chosen.Count(i=>i.Kind==FixKind.BodyParagraph)!=1 ||
            chosen.Count(i=>i.Kind==FixKind.BodyGap)!=1 || chosen.Count(i=>i.Kind==FixKind.BodyLineSpacing)!=2)
            throw new InvalidOperationException("Mẫu không Kính gửi chưa nhận đúng bốn nhóm thân bài hoặc làm mất mức thụt đúng của hai đoạn mở đầu.");
        var before=new[]{First,Second,Third}.ToDictionary(t=>t,test.ReadCheckCurrentStyles);
        Issue[] Selected()
        {
            var current=test.Check();var styles=current.Where(i=>i.CanFix && Body(i) && i.Original is First or Second or Third).ToArray();
            var text=current.Where(i=>i.CanFix && !i.IsStyleOnly && i.Format is null && styles.Any(s=>i.Start>=s.Start && (long)i.Start+i.Length<=(long)s.Start+s.Length)).ToArray();
            return styles.Concat(text).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        var selected=Selected();bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || before.Any(p=>!p.Value.Matches(test.ReadCheckCurrentStyles(p.Key))))
            throw new InvalidOperationException("Rollback thân bài không Kính gửi chưa khôi phục chữ/cỡ/căn/spacing.");
        selected=Selected();var expected=new Dictionary<string,CharacterStyleEvidence>();
        foreach(var text in new[]{First,Second,Third})
        {
            var styles=selected.Where(i=>i.IsStyleOnly && i.Original==text).ToArray();var first=styles[0];var result=before[text];
            foreach(var item in styles)result=item.CharacterFormat is { } font?result.WithFormat(font):item.ParagraphFix is { } paragraph?
                result.WithParagraph(paragraph,before[text].Paragraph):result.WithSpacing(item.SpacingFix!,before[text].Paragraph);
            var edits=selected.Where(i=>!i.IsStyleOnly && i.Format is null && i.LineFix is null && i.Start>=first.Start && (long)i.Start+i.Length<=(long)first.Start+first.Length).ToArray();
            expected[text]=result.Project(edits,i=>text[..(i.Start-first.Start)]);
        }
        test.FixSelection(selected);test.Check();string expectedText=original.Replace(First,expected[First].Text,StringComparison.Ordinal);
        if(test.ReadCheckText()!=expectedText || test.CheckUndoCount!=1 || expected.Any(p=>!p.Value.Matches(test.ReadCheckCurrentStyles(p.Value.Text))) ||
            test.Check().Any(i=>i.CanFix && Body(i) && expected.Values.Any(v=>v.Text==i.Original)))
            throw new InvalidOperationException("Sửa chung thân bài không Kính gửi chưa giữ glyph/kiểu/spacing hoặc còn lỗi đã sửa.");
        string saved=Path.Combine(isolated,"no-greeting-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || before.Any(p=>!p.Value.Matches(test.ReadCheckCurrentStyles(p.Key))))throw new InvalidOperationException("Một Undo chưa khôi phục bản không Kính gửi.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=expectedText || expected.Any(p=>!p.Value.Matches(test.ReadCheckCurrentStyles(p.Value.Text))) ||
            !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Lưu/mở thân bài không Kính gửi chưa đúng hoặc nguồn đã đổi.");
        pass("Không Kính gửi: hai đoạn mở đầu có proof; sửa chung bốn nhóm/chữ/ba Line, giữ1cm/1,27cm/cỡ/gạch chân, rollback/một Undo/lưu-mở/nguồn");
        OfficeBodyEmphasisCheck.Run(test,source,isolated,pass);
    }
}
