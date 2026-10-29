using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// An independent synthetic Word scenario. Never uses a user's document or
// treats the existence/compilation of this scenario as a real Office PASS.
internal static class OfficeTechnicalTextCheck
{
    internal const string Prose="Nội dung  kế hoạch kế hoạch  ,báo cáo đã xong.";
    internal const string FixedProse="Nội dung kế hoạch, báo cáo đã xong.";
    internal static IReadOnlyList<string> TechnicalRows {get;}=Array.AsReadOnly(new[]
    {
        @"Tệp C:\Báo  cáo\kế hoạch kế hoạch\con đo.docx",
        "Địa chỉ https://example.invalid/tài  liệu/kế hoạch kế hoạch?mẫu=con đo",
        "Tên tệp \"Báo  cáo kế hoạch kế hoạch con đo.docx\" cần giữ nguyên."
    });
    internal static IReadOnlyList<string> FormRows {get;}=Array.AsReadOnly(new[]
    {
        "Tích chọn [        ] nếu đồng ý.",
        "Tích chọn [    X    ] nếu đồng ý.",
        "Ngày sinh:    /    /2026",
        "Ngày sinh:    /    /…",
        "Số:    ...    /BC-ABC",
        "Ngày 03 tháng        năm 2026",
        "So\u0302\u0301:        /BC-ABC"
    });
    internal static IReadOnlyList<string> ParagraphEndRows {get;}=Array.AsReadOnly(new[]
    {
        "Đã duyệt “kế hoạch”.   ",
        "Theo mẫu (nội dung);   "
    });
    internal const string ValidTechnical=@"Tệp C:\Mau\bao-cao.docx";
    private static bool Chosen(Issue issue)=>issue.CanFix && issue.Kind is FixKind.Text or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.ParagraphEndSpace;
    internal static string ExpectedText(string original)
    {
        string expected=original.Replace(Prose,FixedProse,StringComparison.Ordinal);
        foreach(string row in ParagraphEndRows)expected=expected.Replace(row+"\r",row.TrimEnd(' ')+"\r",StringComparison.Ordinal);
        return expected;
    }

    internal static string Create(string seed,string folder)
    {
        string path=Path.Combine(folder,"technical-text-input.docx");File.Copy(seed,path,false);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
        using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string value,bool retained=false,int size=13)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",retained?1:0)),new XElement(w+"sz",new XAttribute(w+"val",size*2)),
            new XElement(w+"u",new XAttribute(w+"val",retained?"single":"none"))),
            new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
        XElement Row(string value)=>new(w+"p",new XElement(w+"pPr",new XElement(w+"jc",new XAttribute(w+"val","left"))),Run(value));
        var body=xml.Root!.Element(w+"body")!;
        var section=body.Elements(w+"sectPr").SingleOrDefault();
        if(section is null || body.Descendants(w+"sectPr").Count()!=1)
            throw new InvalidOperationException("Mẫu kỹ thuật cần một section rõ ràng.");
        // Replace only the newly generated fixture's main body, keeping its
        // section and every other package part/relationship from the seed.
        var retainedSection=new XElement(section);
        string rest=Prose["Nội dung".Length..];int comma=rest.IndexOf(',');
        body.ReplaceNodes(new XElement(w+"p",new XElement(w+"pPr",new XElement(w+"jc",new XAttribute(w+"val","left"))),
            Run("Nội dung",true),Run(rest[..comma]),Run(",",true,14),Run(rest[(comma+1)..])),
            TechnicalRows.Select(Row),FormRows.Select(Row),ParagraphEndRows.Select(Row),Row(ValidTechnical),retainedSection);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        return path;
    }
    internal static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        string source=Create(seed,folder);byte[] digest=SHA256.HashData(File.ReadAllBytes(source));
        test.Profile=DocumentProfile.Personal;test.Open(source);string original=test.ReadCheckText();
        void VerifyKept()
        {
            string story=test.ReadCheckText();
            if(!TechnicalRows.Concat(FormRows).Append(ValidTechnical).All(row=>story.Contains(row+"\r",StringComparison.Ordinal)))
                throw new InvalidOperationException("Tên tệp, đường dẫn, địa chỉ hoặc ô trống của mẫu đã bị đổi.");
            var current=test.Check();
            if(TechnicalRows.Any(row=>current.Count(i=>!i.CanFix && i.Original==row)!=1))
                throw new InvalidOperationException("Dòng kỹ thuật chưa được giữ thành cảnh báo thủ công độc lập.");
        }
        var initial=test.Check();var selected=initial.Where(Chosen).ToArray();
        if(selected.Length!=3+ParagraphEndRows.Count || selected.Count(i=>i.Kind==FixKind.ParagraphEndSpace)!=ParagraphEndRows.Count ||
            !original.Contains(Prose+"\r",StringComparison.Ordinal) ||
            initial.Any(i=>i.CanFix && TechnicalRows.Contains(i.Original)))
            throw new InvalidOperationException("Mẫu chưa phân biệt gợi ý văn xuôi/cuối đoạn với các dòng kỹ thuật và ô trống phải giữ.");
        var spaces=selected.Single(i=>i.Kind==FixKind.Text && i.Original=="  ");
        // Anchor to the actual returned Word CP and a verified literal prefix,
        // never to an index computed from the concatenated story string.
        const string retained="Nội dung";
        var probe=new Issue(spaces.Start-retained.Length,retained.Length,retained,retained,"",false);
        var decoration=test.ReadCheckDecorations(probe);
        if(decoration.Bold!=-1 || decoration.Underline!=1)
            throw new InvalidOperationException("Mẫu thiếu chữ đậm/gạch chân cần bảo toàn.");
        var punctuation=selected.Single(i=>i.Kind==FixKind.PunctuationSpace);
        var punctuationProbe=test.CaseTrackingRegions([punctuation]).Single();
        var originalStyles=test.ReadCheckCurrentStyles(Prose);
        var singleStyles=test.ReadCheckExpectedStyles(punctuationProbe,[punctuation]);
        const string singleProse="Nội dung  kế hoạch kế hoạch, báo cáo đã xong.";
        VerifyKept();test.Fix(punctuation);
        string single=original.Replace(Prose,singleProse,StringComparison.Ordinal);
        if(test.ReadCheckText()!=single || test.CheckUndoCount!=1 || test.ReadCheckDecorations(probe)!=decoration ||
            test.Check().Any(i=>Chosen(i) && i.Kind==FixKind.PunctuationSpace) || !singleStyles.Matches(test.ReadCheckCurrentStyles(singleProse)))
            throw new InvalidOperationException("Sửa riêng dấu câu chưa giữ các phần chưa chọn hoặc kiểu chữ.");
        VerifyKept();test.Undo();test.Check();
        if(test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.ReadCheckDecorations(probe)!=decoration ||
            !originalStyles.Matches(test.ReadCheckCurrentStyles(Prose)))
            throw new InvalidOperationException("Một Undo của sửa riêng chưa về nguồn mẫu.");
        pass("Chữ trong đường dẫn/tên tệp/địa chỉ: chỉ cảnh báo; sửa riêng dấu câu văn xuôi, giữ dấu phẩy 14 pt đậm/gạch chân và thuộc tính đoạn, một Undo");

        selected=test.Check().Where(Chosen).ToArray();bool restored=false;
        try{test.FixTextFailureProbe(selected,selected.Length);}
        catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){restored=true;}
        if(!restored || test.ReadCheckText()!=original || test.Changed || test.CheckUndoCount!=0 || test.ReadCheckDecorations(probe)!=decoration ||
            !originalStyles.Matches(test.ReadCheckCurrentStyles(Prose)))
            throw new InvalidOperationException("Lỗi cuối lượt chưa phục hồi văn xuôi, dòng kỹ thuật và trạng thái Undo.");
        VerifyKept();selected=test.Check().Where(Chosen).ToArray();
        var batchStyles=test.ReadCheckExpectedStyles(test.CaseTrackingRegions(selected).Single(),selected);
        test.FixMany(selected);
        string expected=ExpectedText(original);
        if(test.ReadCheckText()!=expected || test.CheckUndoCount!=1 || test.Check().Any(Chosen) || test.ReadCheckDecorations(probe)!=decoration ||
            !batchStyles.Matches(test.ReadCheckCurrentStyles(FixedProse)))
            throw new InvalidOperationException("Sửa nhóm văn xuôi chưa đạt nguyên văn/kiểu/Undo hoặc còn lỗi đã chọn.");
        VerifyKept();string saved=Path.Combine(folder,"technical-text-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.ReadCheckDecorations(probe)!=decoration ||
            !originalStyles.Matches(test.ReadCheckCurrentStyles(Prose)))
            throw new InvalidOperationException("Một Undo chưa phục hồi cả nhóm văn xuôi trước lưu.");
        VerifyKept();test.Open(saved);test.Check();
        if(test.ReadCheckText()!=expected || test.Check().Any(Chosen) || test.ReadCheckDecorations(probe)!=decoration ||
            !batchStyles.Matches(test.ReadCheckCurrentStyles(FixedProse)) ||
            !SHA256.HashData(File.ReadAllBytes(source)).SequenceEqual(digest))
            throw new InvalidOperationException("Lưu/mở chưa giữ dòng kỹ thuật, phông/chữ hoặc nguồn mẫu bất biến.");
        VerifyKept();pass("Văn xuôi và khoảng cuối sau ngoặc cùng tài liệu có tên tệp/địa chỉ/ô trống: sửa nhóm, lỗi cuối lượt/rollback, một Undo, lưu-mở; nguồn, chuỗi kỹ thuật và ô chọn/ngày chưa điền giữ nguyên");
    }
}
