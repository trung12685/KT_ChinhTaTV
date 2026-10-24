using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal static class OfficeHeadingSizeCheck
{
    public static void Run(WordSession test,string withLines,string folder,Action<string> pass)
    {
        string source=Create(withLines,folder);byte[] hash=SHA256.HashData(File.ReadAllBytes(source));
        test.Open(source);test.Profile=DocumentProfile.State;string original=test.ReadCheckText();
        Issue[] Headings()=>test.Check().Where(i=>i.CanFix && i.Format?.RepairInvalidSizes==true).ToArray();
        Issue[] HeadingDefects()=>test.Check().Where(i=>HeadingRules.IsCandidate(i.Original,DocumentProfile.State) &&
            i.Kind is FixKind.NationalHeading or FixKind.StateMotto or FixKind.Advisory).ToArray();
        var headings=Headings();
        if(headings.Length!=2 || !headings.Any(i=>i.Kind==FixKind.NationalHeading) || !headings.Any(i=>i.Kind==FixKind.StateMotto))
            throw new InvalidOperationException("Mẫu quốc hiệu/tiêu ngữ có Line chưa có hai đề xuất sửa riêng cỡ sai.");
        FontSizeEvidence[] CurrentSizes(){test.Check();return headings.Select(test.ReadCheckCurrentSizes).ToArray();}
        bool Matches(FontSizeEvidence[] proofs)=>CurrentSizes().Select((s,i)=>proofs[i].Matches(s)).All(x=>x);
        var sizes=CurrentSizes();
        var expected=sizes.Select((s,i)=>s.CorrectInvalid(headings[i].Format!.Size,headings[i].Format!.MaximumSize!.Value)).ToArray();
        test.Fix(headings[0]);
        if(test.ReadCheckText()!=original || !expected[0].Matches(CurrentSizes()[0]) || HeadingDefects().Length!=1)
            throw new InvalidOperationException("Sửa riêng cỡ tiêu đề làm đổi chữ/cỡ đúng hoặc làm mất lỗi chưa chọn.");
        test.Undo();
        if(!sizes[0].Matches(CurrentSizes()[0]))throw new InvalidOperationException("Undo riêng chưa phục hồi cỡ tiêu đề.");
        Issue[] Select()
        {
            var current=test.Check();var lines=OfficeComponentCheck.ReadLines(test);
            var model=new IssueReviewModel(current.Concat(lines));
            var motto=model.Choices.Single(c=>c.Issue.LineFix?.Role==LineRole.Motto);motto.IsChecked=true;
            var chosen=model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue).ToArray();
            if(chosen.Count(i=>i.Format?.RepairInvalidSizes==true)!=2)
                throw new InvalidOperationException("Line tiêu ngữ chưa tích kèm đúng hai sửa cỡ trong ô tiêu đề.");
            return chosen;
        }
        bool restored=false;
        try{test.FixStagedFailureProbe(Select(),1);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed ||
            !Matches(sizes))
            throw new InvalidOperationException("Rollback sau nhóm phông chưa phục hồi cả chữ/cỡ tiêu đề.");
        test.FixSelection(Select());test.Check();
        if(test.ReadCheckText()!=original || HeadingDefects().Length!=0 || test.CheckUndoCount!=1 ||
            !Matches(expected) ||
            OfficeComponentCheck.ReadLines(test,verifyAll:true).Length!=0)
            throw new InvalidOperationException("Quốc hiệu/tiêu ngữ sau sửa nhóm chưa đạt cỡ hoặc nét thật.");
        string saved=Path.Combine(folder,"heading-size-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original || !Matches(sizes))
            throw new InvalidOperationException("Một Undo chưa phục hồi cỡ toàn lượt tiêu đề.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=original || HeadingDefects().Length!=0 || OfficeComponentCheck.ReadLines(test,verifyAll:true).Length!=0 ||
            !Matches(expected) ||
            !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))
            throw new InvalidOperationException("Lưu/mở cỡ tiêu đề chưa giữ chữ/cỡ/nét hoặc nguồn thay đổi.");
        pass("Quốc hiệu/tiêu ngữ có Line: chỉ sửa cỡ sai, giữ cỡ đúng, chọn phụ thuộc/đo lại, sửa riêng/nhóm, rollback giữa lượt, một Undo/lưu-mở/băm nguồn");
    }
    private static string Create(string source,string folder)
    {
        string path=Path.Combine(folder,"heading-size-with-lines.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
        using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,float size)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"sz",new XAttribute(w+"val",size*2)),new XElement(w+"b"),new XElement(w+"i",new XAttribute(w+"val",0)),
            new XElement(w+"color",new XAttribute(w+"val","000000"))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        foreach(string text in new[]{Checker.Heading,HeadingRules.Motto})
        {
            var p=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))==text);
            // Word may merge text and the owned drawing into the same run. Remove
            // only text nodes so every Line/relationship remains in the fixture.
            foreach(var t in p.Descendants(w+"t").ToArray())t.Remove();
            float min=text==Checker.Heading?12:13;
            p.Add(Run(text[..1],min-2),Run(text[1..^4],min),Run(text[^4..],min+1));
        }
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
