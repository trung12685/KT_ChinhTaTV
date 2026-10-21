using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
public record OfficeCheckResult(bool Passed, string Stage, string Folder);

// Runs on the WPF STA thread using a separate Word session and a synthetic document only.
public static class OfficeIntegrationCheck
{
    public static OfficeCheckResult Run()
    {
        string folder = Path.Combine(Path.GetDirectoryName(Diagnostics.LogPath)!, "OfficeCheck-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string report = Path.Combine(folder,"office-check.txt");
        string stage = "Tìm mẫu", office = "";
        void Pass(string message) => File.AppendAllText(report,"PASS " + message + Environment.NewLine);
        File.WriteAllText(report,$"ChinhTaViet {typeof(OfficeIntegrationCheck).Assembly.GetName().Version}\n{DateTimeOffset.Now:O}\n");
        try
        {
            string sample = Path.Combine(AppContext.BaseDirectory,"samples","BaDuongKeThieu.docx");
            if (!File.Exists(sample)) throw new FileNotFoundException("Thiếu mẫu đi kèm bản giải nén");
            using var test = new WordSession {Profile=DocumentProfile.State, LineCheckArtifactFolder=folder};
            stage="Mở Word và mẫu";
            test.Open(sample);
            office=test.OfficeDescription;
            File.AppendAllText(report,office+Environment.NewLine);
            Pass(stage);
            stage="Nhận diện ba mốc";
            test.Check();
            File.AppendAllText(report,$"Targets={test.LineTargets.Count}; Shapes={test.LineShapes.Count}\n");
            if(test.LineTargets.Count!=3 || test.LineShapes.Count!=0)throw new InvalidOperationException("Không nhận diện được đúng mẫu ba mốc chưa có Line");
            Pass(stage);
            // Fixed coordinates exercise the write/open/save pipeline, not legal layout measurement.
            var plan=test.LineTargets.Select(t=>t.Probe with {CanFix=true,
                LineFix=ShapeLineRules.Evaluate(t,new(100,100+(int)t.Role*45,210),null,null,LinePolicy.Decree30).Fix}).ToArray();
            stage="Ghi nhóm ba Line từ checkpoint không bị Word khóa";
            test.FixLineProbe(plan);
            Pass(stage);
            stage="Word nhận ba Line sau mở lại";
            test.Check();
            if(test.LineShapes.Count!=3 || test.LineShapes.Any(s=>!s.AppOwned))throw new InvalidOperationException("Số Line không đúng");
            Pass(stage);
            stage="Xuất bản xem trước XPS";
            string xps=test.ExportPreview([],new Dictionary<Issue,PreviewAnchor>());
            File.Copy(xps,Path.Combine(folder,"line-preview.xps"));
            Pass(stage);
            stage="Lưu bản sao DOCX";
            string saved=Path.Combine(folder,"three-lines.docx");
            test.SaveCopy(saved);
            Pass(stage);
            stage="Hoàn tác nhóm Line";
            test.Undo();test.Check();
            if(test.LineShapes.Count!=0)throw new InvalidOperationException("Hoàn tác chưa về mẫu ban đầu");
            Pass(stage);
            stage="Mở lại bản đã lưu";
            test.Open(saved);test.Check();
            if(test.LineShapes.Count!=3 || test.LineShapes.Any(s=>!s.AppOwned))throw new InvalidOperationException("Line không giữ được sau lưu/mở");
            test.VerifyLineResult(plan);
            Pass(stage);
            stage="Đo và sửa Line trên mẫu chế độ Word 2013";
            string modern=CreateModernSample(sample,folder);
            test.Open(modern);
            var modernIssues=test.Check();
            var measured=InspectLines(test,modernIssues);
            if(measured.Length!=3 || measured.Any(i=>!i.CanFix || i.LineFix is null))
                throw new InvalidOperationException("Chưa đo được ba Line trên mẫu tương thích hiện đại");
            // Reproduce sequential creation: the two existing Lines must survive the missing-Motto case.
            foreach(var role in new[]{LineRole.Agency,LineRole.Subject,LineRole.Motto})
            {
                stage="Đo và sửa mẫu Word 2013: " + role;
                test.FixMany([measured.Single(i=>i.LineFix!.Role==role)]);
                test.Check();
                Pass(stage);
            }
            test.VerifyLineResult(test.VerifiedLineEdits,verifyReference:true);
            stage="Lưu/mở lại và kiểm nét đã đo trên mẫu Word 2013";
            var acceptedModern=test.VerifiedLineEdits;
            string modernSaved=Path.Combine(folder,"modern-three-lines.docx");
            test.SaveCopy(modernSaved);
            string modernPreview=test.ExportPreview([],new Dictionary<Issue,PreviewAnchor>());
            File.Copy(modernPreview,Path.Combine(folder,"modern-line-preview.xps"));
            for(int step=0;step<3;step++)test.Undo();
            test.Check();
            if(test.LineShapes.Count!=0)throw new InvalidOperationException("Hoàn tác mẫu hiện đại chưa phục hồi");
            test.Open(modernSaved);test.Check();test.VerifyLineResult(acceptedModern,verifyReference:true);
            Pass(stage);
            stage="Sửa cả ba Line trong một lượt trên mẫu Word 2013";
            test.Open(modern);test.Check();
            test.FixMany(measured);
            var batchIssues=test.Check();
            test.VerifyLineResult(test.VerifiedLineEdits,verifyReference:true);
            if(InspectLines(test,batchIssues).Length!=0)
                throw new InvalidOperationException("Còn cảnh báo Line sau khi sửa và quét lại cả nhóm");
            Pass(stage);
            stage="Quét lại lần nữa và lưu nhóm ba Line không cần sửa lần hai";
            if(InspectLines(test,test.Check()).Length!=0)
                throw new InvalidOperationException("Cảnh báo Line xuất hiện lại ở lần quét kế tiếp");
            string groupedSaved=Path.Combine(folder,"grouped-three-lines.docx");
            test.SaveCopy(groupedSaved);
            if(InspectLines(test,test.Check()).Length!=0)
                throw new InvalidOperationException("Còn cảnh báo Line sau khi lưu nhóm");
            Pass(stage);
            stage="Hoàn tác một lần cho cả nhóm ba Line";
            test.Undo();test.Check();
            if(test.LineShapes.Count!=0)throw new InvalidOperationException("Hoàn tác một lần chưa bỏ cả nhóm ba Line");
            Pass(stage);
            stage="Mở lại nhóm ba Line đã lưu, kiểm không dựa vào lịch sử sửa";
            test.Open(groupedSaved);
            if(InspectLines(test,test.Check()).Length!=0)
                throw new InvalidOperationException("Còn cảnh báo sau khi mở lại nhóm ba Line");
            Pass(stage);
            stage="Đặt lại cùng lúc hai Line đã có, giữ bù tọa độ";
            test.Open(modern);test.Check();
            var shifted=measured.Select(i=>i.LineFix!.Role==LineRole.Agency?i:i with {LineFix=i.LineFix! with {Left=i.LineFix!.Left-6}}).ToArray();
            test.FixLineProbe(shifted);
            var existingIssues=test.Check();
            var repair=InspectLines(test,existingIssues);
            if(repair.Length!=2 || repair.Any(i=>!i.CanFix || i.LineFix is null))
                throw new InvalidOperationException("Không nhận đúng hai nét lệch tâm đã có trong mẫu");
            test.FixMany(repair);
            if(InspectLines(test,test.Check()).Length!=0)
                throw new InvalidOperationException("Còn cảnh báo sau khi đặt lại hai nét đã có");
            Pass(stage);
            stage="Lưu/mở lại sau khi sửa hai Line đã có";
            string updated=Path.Combine(folder,"updated-existing-lines.docx");
            test.SaveCopy(updated);test.Open(updated);
            if(InspectLines(test,test.Check()).Length!=0)
                throw new InvalidOperationException("Còn cảnh báo sau lưu/mở hai nét đặt lại");
            Pass(stage);
            File.AppendAllText(report,"RESULT PASS\nKiểm thao tác Word/Line và vị trí nét theo số đo XPS trên hai mẫu; không xác nhận mọi tài liệu.\n");
            return new(true,stage,folder);
        }
        catch(Exception error)
        {
            Diagnostics.Write("OfficeCheck.Failed",error);
            File.AppendAllText(report,$"FAIL {stage}\n{office}\n");
            Exception? cause = error;
            for (int depth=0; cause is not null && depth<8; depth++,cause=cause.InnerException)
            {
                File.AppendAllText(report,$"Cause{depth}: {cause.GetType().Name} HRESULT=0x{cause.HResult:X8}\n{cause.StackTrace}\n");
                if (cause.Data["ChinhTa.LineVerification"] is string details)
                    File.AppendAllText(report, details + Environment.NewLine);
            }
            File.AppendAllText(report,"RESULT FAIL\n");
            return new(false,stage,folder);
        }
    }
    private static Issue[] InspectLines(WordSession test,IReadOnlyList<Issue> otherIssues)
    {
        var anchors=new Dictionary<Issue,PreviewAnchor>();
        string path=test.ExportPreview(test.LineTargets.Select(t=>t.Probe).ToArray(),anchors);
        using var input=new XpsDocument(path,FileAccess.Read);
        return LineInspection.Inspect(input.GetFixedDocumentSequence(),new PreviewHighlighter(),
            test.LineTargets,test.LineShapes,anchors,otherIssues,LinePolicy.Decree30,test.HasGroupedShapes,test.VerifiedLineEdits)
            .Select(pair=>pair.Issue).ToArray();
    }
    private static string CreateModernSample(string sample,string folder)
    {
        string path=Path.Combine(folder,"modern-input.docx");File.Copy(sample,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XNamespace r="http://schemas.openxmlformats.org/package/2006/relationships";
        XNamespace ct="http://schemas.openxmlformats.org/package/2006/content-types";
        XDocument Read(string name,XElement fallback)
        {
            using var stream=zip.GetEntry(name)?.Open();
            return stream is null?new XDocument(fallback):XDocument.Load(stream);
        }
        void Write(string name,XDocument xml)
        {
            zip.GetEntry(name)?.Delete();using var stream=zip.CreateEntry(name).Open();xml.Save(stream);
        }
        Write("word/settings.xml",new XDocument(new XElement(w+"settings",new XElement(w+"compat",
            new XElement(w+"compatSetting",new XAttribute(w+"name","compatibilityMode"),
                new XAttribute(w+"uri","http://schemas.microsoft.com/office/word"),new XAttribute(w+"val",15))))));
        var rels=Read("word/_rels/document.xml.rels",new XElement(r+"Relationships"));
        rels.Root!.Add(new XElement(r+"Relationship",new XAttribute("Id","rIdChinhTaSettings"),
            new XAttribute("Type","http://schemas.openxmlformats.org/officeDocument/2006/relationships/settings"),new XAttribute("Target","settings.xml")));
        Write("word/_rels/document.xml.rels",rels);
        var types=Read("[Content_Types].xml",new XElement(ct+"Types"));
        types.Root!.Add(new XElement(ct+"Override",new XAttribute("PartName","/word/settings.xml"),
            new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml")));
        Write("[Content_Types].xml",types);
        return path;
    }

}

