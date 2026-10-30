using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

public static class OfficeFocusedChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var cases=OfficeFocusedCheck.Cases;
        assert(OfficeFocusedCheck.CodeIdentity().Contains(typeof(WordSession).Module.ModuleVersionId.ToString(),StringComparison.Ordinal) &&
            OfficeFocusedCheck.CodeIdentity().Contains(typeof(Checker).Module.ModuleVersionId.ToString(),StringComparison.Ordinal),
            "The synthetic Word report identifies actual Desktop/Core modules even when successive previews share a development version");
        assert(cases.Count==16 && cases.Select(c=>c.Id).Distinct().Count()==cases.Count && cases.Any(c=>c.Id=="isolated-spacing") && cases.Any(c=>c.Id=="spacing-calibration") && cases.Any(c=>c.Id=="technical-text") && cases.Any(c=>c.Id=="outline-references") &&
            cases.Any(c=>c.Id=="retained-marks") && cases.Any(c=>c.Id=="annotated-text") &&
            cases.Any(c=>c.Id=="body-spacing") && cases.Any(c=>c.Id=="handwriting") && cases.Any(c=>c.Id=="footer-page-number") && cases.Any(c=>c.Id=="empty-page-layout"),
            "Focused Word checks expose independent reference, spacing and handwriting families rather than require an earlier Subject PASS");
        var success=new OfficeSuiteReport(cases.Select(c=>new OfficeSuiteResult(c,OfficeSuiteStatus.Pass)).ToArray());
        string summary=OfficeFocusedCheck.Summary(success);
        assert(summary.Contains("ĐẠT CÁC NHÓM ĐÃ CHỌN",StringComparison.Ordinal) && summary.Contains(OfficeFocusedCheck.Scope,StringComparison.Ordinal) &&
            summary.Contains("không xác nhận bài Word toàn bộ",StringComparison.Ordinal),
            "A focused-family PASS explicitly cannot stand for full Word integration or reader comparison");
        var mixed=new OfficeSuiteReport([new(cases[0],OfficeSuiteStatus.Pass),new(cases[1],OfficeSuiteStatus.Fail,new IOException("synthetic")),new(cases[2],OfficeSuiteStatus.NotRun)]);
        summary=OfficeFocusedCheck.Summary(mixed);
        assert(!mixed.Passed && summary.Contains("1 đạt; 1 lỗi; 1 chưa chạy.",StringComparison.Ordinal) &&
            summary.Contains("PASS — "+cases[0].Name,StringComparison.Ordinal) && summary.Contains("FAIL — "+cases[1].Name,StringComparison.Ordinal) &&
            summary.Contains("NOT_RUN — "+cases[2].Name,StringComparison.Ordinal),
            "The focused Word summary retains every selected family's actual PASS, FAIL and NOT_RUN result");
        bool emptyRejected=false;try{OfficeFocusedCheck.RequireCompleted(0);}catch(InvalidOperationException){emptyRejected=true;}
        assert(emptyRejected,"A focused Office callback that confirms no real assertion cannot silently earn PASS");
        foreach(var invalid in new IReadOnlyList<OfficeSuiteCase>[]
            {[],[new("unknown-reference-check","Unknown")],[cases[^1],cases[^1]]})
        {
            bool rejected=false;
            try{OfficeFocusedCheck.Run(invalid);}catch(ArgumentException){rejected=true;}
            assert(rejected,"Empty, unknown and duplicated focused selection fails before creating any Word session, including the independent reference suite");
        }
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-focused-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            using(var session=OfficeFocusedCheck.NewSession(folder))
                assert(!session.UseOptimizedReading && session.Profile==DocumentProfile.State && session.LineCheckArtifactFolder==folder && !session.IsOpen,
                    "Each focused-family session starts isolated and always uses the legacy reader regardless of the main window mode");
            string sample=Path.Combine("samples","BaDuongKeThieu.docx");byte[] digest=SHA256.HashData(File.ReadAllBytes(sample));
            string modern=OfficeFocusedCheck.CreateSeed(sample,folder);
            using(var package=WordprocessingDocument.Open(modern,false))
            {
                var errors=new OpenXmlValidator().Validate(package).ToArray();
                assert(errors.Length==0,"The shared focused Word seed remains a schema-valid modern-compatibility DOCX without starting Word: "+
                    string.Join(" | ",errors.Take(8).Select(e=>e.Id+": "+e.Description)));
            }
            string seed=OfficeFocusedCheck.CreateBodySpacingSource(modern,folder);
            using(var package=WordprocessingDocument.Open(seed,false))
            {
                var errors=new OpenXmlValidator().Validate(package).ToArray();
                assert(errors.Length==0,"Constructing the independent spacing source does not require any font, Line or Word runtime test to pass: "+
                    string.Join(" | ",errors.Take(8).Select(e=>e.Id+": "+e.Description)));
            }
            using(var zip=ZipFile.OpenRead(seed))
            using(var input=zip.GetEntry("word/document.xml")!.Open())
            {
                var xml=XDocument.Load(input);XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                var texts=xml.Descendants(w+"p").Select(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))).ToArray();
                assert(new[]{"Nội dung  kế hoạch kế hoạch đã được duyệt.","Công tác tổng hợp đã được thực hiện đầy đủ.","Nội dung đã đạt yêu cầu cần được giữ nguyên."}.All(texts.Contains),
                    "The extracted body source retains every paragraph required by the existing direct spacing scenario");
            }
            foreach(var scenario in new[]{"footer-page-number","footer-page-number-long","empty-page-layout"})
            {
                string isolated=Path.Combine(folder,scenario);Directory.CreateDirectory(isolated);
                string fixture=scenario.StartsWith("footer-page-number",StringComparison.Ordinal)?OfficeFooterPageMigrationCheck.Create(modern,isolated,scenario.EndsWith("-long",StringComparison.Ordinal)):OfficeEmptyHeaderFooterCheck.Create(modern,isolated);
                using var package=WordprocessingDocument.Open(fixture,false);
                var errors=new OpenXmlValidator().Validate(package).ToArray();
                assert(errors.Length==0,"The independent "+scenario+" Office fixture is schema-valid without requiring Subject or Word runtime: "+
                    string.Join(" | ",errors.Take(8).Select(e=>e.Id+": "+e.Description)));
                assert(SHA256.HashData(File.ReadAllBytes(sample)).SequenceEqual(digest),
                    "Preparing "+scenario+" keeps the bundled source bytes unchanged");
            }
            assert(digest.SequenceEqual(SHA256.HashData(File.ReadAllBytes(sample))) && Path.GetFullPath(seed)!=Path.GetFullPath(sample),
                "Focused Word fixture preparation keeps the bundled source bytes unchanged and uses its own copy");
        }
        finally{Directory.Delete(folder,true);}
    }
}
