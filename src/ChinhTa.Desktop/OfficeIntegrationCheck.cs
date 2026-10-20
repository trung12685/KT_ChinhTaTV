using System.IO;
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
            test.FixMany(plan);
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
            Pass(stage);
            File.AppendAllText(report,"RESULT PASS\nChỉ kiểm thao tác Word/Line; không xác minh vị trí thể thức hoặc mọi tài liệu.\n");
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
}
