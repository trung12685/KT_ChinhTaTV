using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal static class OfficeHierarchicalOutlineCheck
{
    internal static IReadOnlyList<string> ReferenceRows {get;}=Array.AsReadOnly(new[]
    {
        "Xem mục 1.2 để đối chiếu.",
        "Xem mục 1.1 đến 1.5 để đối chiếu.",
        "Xem mục 1.1 tới 1.3 để đối chiếu.",
        "Xem mục (8), (1.4) để đối chiếu.",
        "Xem mục (1.1) – (1.5) để đối chiếu.",
        "Xem mu\u0323c\u00a0số\u00a0(8), (001.004) để đối chiếu."
    });
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"decimal-outline");Directory.CreateDirectory(isolated);
        string seed=OfficeComponentCheck.Create(modern,isolated,false),source=Create(seed,isolated);
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);
        bool Body(Issue i)=>i.CanFix && i.Kind is FixKind.OutlineOrder or FixKind.OutlineMarker or FixKind.Text or FixKind.RepeatedText;
        var found=test.Check();var orders=found.Where(i=>i.CanFix && i.Kind==FixKind.OutlineOrder).ToArray();
        if(orders.Length!=2 || orders.Any(i=>i.Original!="4" || i.Replacement!="2"))
            throw new InvalidOperationException("Mẫu số nhiều cấp chưa nhận đúng hai phần số cuối cần sửa.");
        string original=test.ReadCheckText();var first=orders[0];
        var prefix=first with {Start=first.Start-2,Length=2,Original="1.",Replacement="1."};
        var decoration=test.ReadCheckDecorations(prefix);
        test.Fix(first);test.Check();
        if(test.ReadCheckText()!=original.Replace("1.4.","1.2.",StringComparison.Ordinal) || test.ReadCheckDecorations(prefix)!=decoration ||
            test.Check().Count(i=>i.CanFix && i.Kind==FixKind.OutlineOrder)!=1)
            throw new InvalidOperationException("Sửa riêng số cuối làm đổi tiền tố/kiểu hoặc mất mục chưa chọn.");
        test.Undo();test.Check();
        if(test.ReadCheckText()!=original)throw new InvalidOperationException("Undo riêng số nhiều cấp chưa phục hồi chữ.");
        Issue[] Select()=>test.Check().Where(Body).Concat(OfficeComponentCheck.ReadLines(test).Where(i=>i.LineFix?.Role==LineRole.Subject)).ToArray();
        var selected=Select();bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed)
            throw new InvalidOperationException("Lỗi sau Line chưa phục hồi cả nhóm số nhiều cấp/dấu đơn/chữ.");
        test.FixSelection(Select());test.Check();
        string expected=original.Replace("1.4.","1.2.",StringComparison.Ordinal).Replace("2.4.","2.2.",StringComparison.Ordinal)
            .Replace("- Nội dung  kế hoạch kế hoạch","Nội dung kế hoạch",StringComparison.Ordinal);
        if(test.ReadCheckText()!=expected || test.Check().Any(Body) || test.CheckUndoCount!=1 ||
            !OfficeComponentCheck.ReadLines(test).Select(i=>i.LineFix!.Role).OrderBy(r=>r).SequenceEqual(new[]{LineRole.Agency,LineRole.Motto}))
            throw new InvalidOperationException("Nhóm số nhiều cấp/chữ/Line chưa đúng phạm vi, quét lại hoặc một Undo.");
        string saved=Path.Combine(isolated,"decimal-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original)throw new InvalidOperationException("Một Undo chưa phục hồi toàn lượt số nhiều cấp.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=expected || test.Check().Any(Body) || !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))
            throw new InvalidOperationException("Lưu/mở số nhiều cấp chưa đúng hoặc nguồn thay đổi.");
        foreach(var reference in ReferenceRows.Select((text,index)=>(text,index)))
        {
            string guarded=Create(seed,isolated,reference.text,reference.index);
            byte[] guardedHash=SHA256.HashData(File.ReadAllBytes(guarded));test.Open(guarded);
            string guardedText=test.ReadCheckText();var blocked=test.Check();
            var independent=blocked.Where(i=>i.CanFix && i.Kind==FixKind.OutlineOrder).ToArray();
            if(independent.Length!=1 || independent[0].OutlineContext?.Label!="2.4" ||
                !blocked.Any(i=>!i.CanFix && i.Kind==FixKind.OutlineOrder && i.Original.Contains("1.4.",StringComparison.Ordinal)) ||
                test.ReadCheckText()!=guardedText || test.Changed || test.CheckUndoCount!=0 ||
                !guardedHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(guarded))))
                throw new InvalidOperationException("Dẫn chiếu đơn/danh sách/khoảng chưa giữ riêng chuỗi liên quan hoặc làm đổi nguồn mẫu.");
        }
        pass("Số nhiều cấp: sửa riêng/chung số cuối, giữ tiền tố/tab/kiểu, dẫn chiếu chặn nhóm liên quan, lỗi sau Line/rollback, một Undo/lưu-mở/băm nguồn");
    }
    internal static string Create(string seed,string folder,string? reference=null,int referenceIndex=0)
    {
        string path=Path.Combine(folder,reference is null?"decimal-input.docx":$"decimal-referenced-{referenceIndex}.docx");File.Copy(seed,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
        using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var recipient=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))=="Nơi nhận:").Ancestors(w+"tbl").First();
        var greeting=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value)).StartsWith("Kính gửi:",StringComparison.Ordinal));
        if(greeting.Parent!=recipient.Parent)throw new InvalidOperationException("Mẫu kiểm số nhiều cấp chưa có ranh giới thân trực tiếp.");
        foreach(var old in greeting.ElementsAfterSelf().TakeWhile(e=>e!=recipient).ToArray())old.Remove();
        XElement Run(string text,bool decorated=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",decorated?1:0)),new XElement(w+"sz",new XAttribute(w+"val",26)),
            new XElement(w+"u",new XAttribute(w+"val",decorated?"single":"none"))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        XElement Row(string text)
        {
            var p=new XElement(w+"p",new XElement(w+"pPr",new XElement(w+"numPr",new XElement(w+"numId",new XAttribute(w+"val",0)))));
            foreach(var part in text.Split('\t').Select((text,i)=>(text,i)))
            {
                if(part.i>0)p.Add(new XElement(w+"r",new XElement(w+"tab")));
                if(part.text.StartsWith("1.4.",StringComparison.Ordinal))p.Add(Run("1.",true),Run(part.text[2..]));
                else p.Add(Run(part.text));
            }
            return p;
        }
        var rows=new[]{"1. Mục cha","\t1.1.\tNhánh đầu","\t1.4.\tNhánh sau","- Nội dung  kế hoạch kế hoạch",
            "2. Mục cha sau","\t2.1.\tNhánh đầu","\t2.4.\tNhánh sau"}.Select(Row).ToList();
        if(reference is not null)rows.Add(Row(reference));recipient.AddBeforeSelf(rows);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
