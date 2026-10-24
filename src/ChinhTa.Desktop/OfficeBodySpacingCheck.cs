using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeBodySpacingCheck
{
    private const string SourceFirst="Nội dung  kế hoạch kế hoạch đã được duyệt.";
    private const string First="Nội dung  nội dung đã được duyệt.";
    private const string Second="Công tác tổng hợp đã được thực hiện đầy đủ.";
    private const string Correct="Nội dung đã đạt yêu cầu cần được giữ nguyên.";
    public static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"body-spacing");Directory.CreateDirectory(isolated);
        string source=Path.Combine(isolated,"spacing-input.docx");File.Copy(seed,source);
        using(var zip=ZipFile.Open(source,ZipArchiveMode.Update))
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
            using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var shortRow=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))==SourceFirst);
            var oldRuns=shortRow.Elements(w+"r").ToArray();
            XElement Run(string value,XElement old)=>new(w+"r",old.Element(w+"rPr") is { } style?new XElement(style):null,
                new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
            shortRow.Elements(w+"r").Remove();shortRow.Add(Run(First[..10],oldRuns[0]),Run(First[10..],oldRuns[^1]));
            foreach(string text in new[]{First,Second,Correct})
            {
                var row=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))==text);
                var properties=row.Element(w+"pPr")!;
                foreach(string name in new[]{"spacing","contextualSpacing","keepNext","pageBreakBefore"})properties.Element(w+name)?.Remove();
                properties.Add(new XElement(w+"spacing",new XAttribute(w+"before",text==Correct?120:0),new XAttribute(w+"after",text==Correct?120:0),
                    new XAttribute(w+"line",text==Correct?360:480),new XAttribute(w+"lineRule","auto"),new XAttribute(w+"beforeAutospacing",0),new XAttribute(w+"afterAutospacing",0)));
                foreach(string name in new[]{"contextualSpacing","keepNext","pageBreakBefore"})properties.Add(new XElement(w+name,new XAttribute(w+"val",0)));
            }
            foreach(var grid in xml.Descendants(w+"docGrid"))grid.SetAttributeValue(w+"type","default");
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        }
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);
        string original=test.ReadCheckText();var found=test.Check();
        var gap=found.Single(i=>i.CanFix && i.Kind==FixKind.BodyGap && i.Original==First);
        if(found.Count(i=>i.CanFix && i.Kind==FixKind.BodyLineSpacing && i.Original is First or Second)!=2 ||
            found.Any(i=>i.Kind is FixKind.BodyGap or FixKind.BodyLineSpacing && i.Original==Correct))
            throw new InvalidOperationException("Mẫu khoảng đoạn/giãn dòng chưa phân biệt sai thật với1,5 dòng/khoảng6pt đúng.");
        var sourceStyle=test.ReadCheckCurrentStyles(First);var neighborStyle=test.ReadCheckCurrentStyles(Second);var correctStyle=test.ReadCheckCurrentStyles(Correct);
        var gapStyle=sourceStyle.WithSpacing(gap.SpacingFix!);test.Fix(gap);test.Check();
        if(test.ReadCheckText()!=original || test.CheckUndoCount!=1 || !gapStyle.Matches(test.ReadCheckCurrentStyles(First)) ||
            !neighborStyle.Matches(test.ReadCheckCurrentStyles(Second)) || test.Check().Any(i=>i.Kind==FixKind.BodyGap && i.Original==First) ||
            !test.Check().Any(i=>i.Kind==FixKind.BodyLineSpacing && i.Original==First))
            throw new InvalidOperationException("Sửa riêng khoảng đoạn chưa giữ giãn dòng/phông/chữ/đoạn kế hoặc còn lỗi đã sửa.");
        test.Undo();test.Check();
        var line=test.Check().Single(i=>i.CanFix && i.Kind==FixKind.BodyLineSpacing && i.Original==First);
        var lineStyle=sourceStyle.WithSpacing(line.SpacingFix!);test.Fix(line);test.Check();
        if(!lineStyle.Matches(test.ReadCheckCurrentStyles(First)) || !test.Check().Any(i=>i.Kind==FixKind.BodyGap && i.Original==First))
            throw new InvalidOperationException("Sửa riêng giãn dòng làm đổi khoảng đoạn/phông chưa chọn.");
        test.Undo();test.Check();
        Issue[] Selected()
        {
            var current=test.Check();var styles=current.Where(i=>i.CanFix && i.IsStyleOnly && i.Original is First or Second).ToArray();
            var text=current.Where(i=>i.CanFix && !i.IsStyleOnly && i.Format is null && styles.Any(s=>i.Start>=s.Start && (long)i.Start+i.Length<=(long)s.Start+s.Length)).ToArray();
            return styles.Concat(text).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        var selected=Selected();bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed ||
            !sourceStyle.Matches(test.ReadCheckCurrentStyles(First)) || !neighborStyle.Matches(test.ReadCheckCurrentStyles(Second)))
            throw new InvalidOperationException("Rollback khoảng đoạn/giãn dòng/chữ/phông/Line chưa khôi phục cả lượt.");
        selected=Selected();var first=selected.Single(i=>i.Kind==FixKind.BodyGap && i.Original==First);
        var firstStyles=selected.Where(i=>i.IsStyleOnly && i.Original==First).ToArray();
        var firstText=selected.Where(i=>!i.IsStyleOnly && i.Format is null && i.LineFix is null && i.Start>=first.Start && (long)i.Start+i.Length<=(long)first.Start+first.Length).ToArray();
        var expected=sourceStyle;
        foreach(var item in firstStyles)expected=item.CharacterFormat is { } font?expected.WithFormat(font):item.ParagraphFix is { } layout?
            expected.WithParagraph(layout,sourceStyle.Paragraph):expected.WithSpacing(item.SpacingFix!,sourceStyle.Paragraph);
        expected=expected.Project(firstText,i=>First[..(i.Start-first.Start)]);
        test.FixSelection(selected);test.Check();string expectedText=original.Replace(First,expected.Text,StringComparison.Ordinal);
        if(test.ReadCheckText()!=expectedText || test.CheckUndoCount!=1 || !expected.Matches(test.ReadCheckCurrentStyles(expected.Text)) ||
            !correctStyle.Matches(test.ReadCheckCurrentStyles(Correct)) || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.BodyGap or FixKind.BodyLineSpacing && i.Original==expected.Text))
            throw new InvalidOperationException("Sửa chung khoảng đoạn/giãn dòng/chữ/phông/căn/Line chưa giữ phần đúng hoặc còn lỗi đã sửa.");
        string saved=Path.Combine(isolated,"spacing-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !sourceStyle.Matches(test.ReadCheckCurrentStyles(First)))throw new InvalidOperationException("Một Undo chưa về nguyên khoảng đoạn/giãn dòng.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=expectedText || !expected.Matches(test.ReadCheckCurrentStyles(expected.Text)) || !correctStyle.Matches(test.ReadCheckCurrentStyles(Correct)) ||
            !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Lưu/mở khoảng đoạn/giãn dòng chưa đúng hoặc nguồn bị đổi.");
        pass("Khoảng đoạn/giãn dòng: chọn riêng/chung chữ/phông/căn/ba Line, giữ cỡ/gạch chân/phần chưa chọn, rollback/một Undo/lưu-mở/nguồn");
    }
}
