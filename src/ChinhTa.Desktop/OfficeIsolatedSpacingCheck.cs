using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Diagnostic baseline only: no spacing writer or automatic repair is enabled.
internal static class OfficeIsolatedSpacingCheck
{
    internal const string First="Nội dung kế hoạch cần được giữ nguyên khi đối chiếu bản sao cô lập.";
    internal const string Second="Đoạn tiếp theo giữ nguyên phông chữ và khoảng trước sau của tài liệu.";
    private static string ParagraphText(string text)=>text+"\vDòng thứ hai dùng cùng phông chữ.\vDòng thứ ba giữ nguyên baseline.";
    internal static void RunCalibration(WordSession test,string seed,string folder,Action<string> pass)
    {
        foreach(string rule in new[]{"exact","atLeast"})
        {
            string source=Create(seed,folder,rule);byte[] digest=OfficeFileEvidence.Digest(source);test.Open(source);
            string story=test.ReadCheckText();int undo=test.CheckUndoCount;bool changed=test.Changed;
            var target=new SpacingDiagnosticTarget(ParagraphText(First),ParagraphText(Second));
            var result=test.VerifyCheckSpacingCalibration(target);var repeated=test.MeasureSpacing(target);
            var independent=test.MeasureSpacing(target with {NextText="Đoạn không có trong mẫu kiểm cô lập."});
            if(result.Line.Status==SpacingCalibrationStatus.Unproven || result.Gap is not {Status:not SpacingGapCalibrationStatus.Unproven} ||
                repeated.Line.Status!=result.Line.Status || repeated.Gap?.Status!=result.Gap.Status ||
                !repeated.Line.SourceStepsPt.SequenceEqual(result.Line.SourceStepsPt) || !repeated.Line.SingleStepsPt.SequenceEqual(result.Line.SingleStepsPt) ||
                !repeated.Line.OneAndHalfStepsPt.SequenceEqual(result.Line.OneAndHalfStepsPt) ||
                repeated.Gap.SourceEffectPt!=result.Gap.SourceEffectPt || repeated.Gap.SixPointReferenceEffectPt!=result.Gap.SixPointReferenceEffectPt ||
                independent.Line.Status!=result.Line.Status || independent.Gap?.Status!=SpacingGapCalibrationStatus.Unproven ||
                !independent.Line.SourceStepsPt.SequenceEqual(result.Line.SourceStepsPt) ||
                test.ReadCheckText()!=story || test.CheckUndoCount!=undo || test.Changed!=changed || !digest.SequenceEqual(OfficeFileEvidence.Digest(source)))
                throw new InvalidOperationException("Hiệu chuẩn bản sao chưa chứng minh được vector dòng/khoảng đoạn hoặc chưa giữ nguồn/Undo khi đo lặp.");
            pass($"Hiệu chuẩn {rule}: Single/1,5 và hai trial khoảng đoạn; hai lượt cùng kết quả, giữ nguyên nguồn/Undo; chỉ chẩn đoán, chưa sửa.");
        }
    }
    internal static string Create(string seed,string folder,string rule)
    {
        if(rule is not ("exact" or "atLeast"))throw new ArgumentException("Chế độ mẫu chưa hỗ trợ.",nameof(rule));
        string path=Path.Combine(folder,"isolated-spacing-"+rule+".docx");File.Copy(seed,path,false);
        using(var zip=ZipFile.Open(path,ZipArchiveMode.Update))
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
            using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var body=xml.Root!.Element(w+"body")!;
            // Explicit soft breaks guarantee multiple baselines independently
            // of printer line wrapping, without treating Word CP as string index.
            XElement Row(string text)=>new(w+"p",new XElement(w+"pPr",
                new XElement(w+"spacing",new XAttribute(w+"before",60),new XAttribute(w+"after",60),new XAttribute(w+"line",rule=="exact"?360:400),new XAttribute(w+"lineRule",rule)),
                new XElement(w+"jc",new XAttribute(w+"val","left"))),
                new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),new XElement(w+"sz",new XAttribute(w+"val",26))),
                    new XElement(w+"t",text),new XElement(w+"br"),new XElement(w+"t","Dòng thứ hai dùng cùng phông chữ."),new XElement(w+"br"),new XElement(w+"t","Dòng thứ ba giữ nguyên baseline.")));
            var section=body.Elements(w+"sectPr").Single();section.AddBeforeSelf(Row(First),Row(Second));
            entry.Delete();using var output=zip.CreateEntry("word/document.xml").Open();xml.Save(output);
        }
        OfficeFocusedCheck.OrderFixtureProperties(path);return path;
    }
    internal static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        foreach(string rule in new[]{"exact","atLeast"})
        {
            string source=Create(seed,folder,rule);byte[] digest=OfficeFileEvidence.Digest(source);test.Open(source);
            string story=test.ReadCheckText();int undo=test.CheckUndoCount;bool changed=test.Changed;
            var proof=test.VerifyCheckIsolatedCopy();
            // Repeat from the unchanged source to catch leftover instances/files
            // and accidental dependence on state from the first diagnostic.
            var repeated=test.VerifyCheckIsolatedCopy();
            if(proof.Pages<1 || proof.GlyphRuns<6 || proof!=repeated || test.ReadCheckText()!=story || test.CheckUndoCount!=undo || test.Changed!=changed ||
                !digest.SequenceEqual(OfficeFileEvidence.Digest(source)))
                throw new InvalidOperationException("Đối chiếu lặp bản sao chưa giữ nguyên nguồn/Undo/trạng thái hoặc thiếu nhiều dòng XPS.");
            pass($"Bản sao cô lập {rule}: {proof.Parts} part, {proof.Pages} trang, {proof.GlyphRuns} glyph run; khớp nguồn XPS hai lần, giữ nguồn/Undo; chưa hiệu chuẩn hoặc sửa spacing.");
        }
    }
}
