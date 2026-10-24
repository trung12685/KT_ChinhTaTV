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
                var current=InspectLines(test,test.Check()).Single(i=>i.LineFix?.Role==role);
                test.FixMany([current]);
                test.Check();
                Pass(stage);
            }
            VerifyCurrentLines(test,test.VerifiedLineEdits);
            stage="Lưu/mở lại và kiểm nét đã đo trên mẫu Word 2013";
            var acceptedModern=test.VerifiedLineEdits;
            string modernSaved=Path.Combine(folder,"modern-three-lines.docx");
            test.SaveCopy(modernSaved);
            string modernPreview=test.ExportPreview([],new Dictionary<Issue,PreviewAnchor>());
            File.Copy(modernPreview,Path.Combine(folder,"modern-line-preview.xps"));
            for(int step=0;step<3;step++)test.Undo();
            test.Check();
            if(test.LineShapes.Count!=0)throw new InvalidOperationException("Hoàn tác mẫu hiện đại chưa phục hồi");
            test.Open(modernSaved);test.Check();VerifyCurrentLines(test,acceptedModern);
            Pass(stage);
            stage="Sửa cả ba Line trong một lượt trên mẫu Word 2013";
            test.Open(modern);test.Check();
            test.FixMany(measured);
            var batchIssues=test.Check();
            VerifyCurrentLines(test,test.VerifiedLineEdits);
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
            stage="Sửa riêng số văn bản và Hoàn tác trên mẫu giả";
            string headerSample=Path.Combine(AppContext.BaseDirectory,"samples","SoKyHieuNgayThang.docx");
            test.Open(headerSample);
            string originalStory=test.ReadCheckText();
            const string originalNumber="số  :  3 / BC – ABC",originalDate="Địa danh  Ngày3  Tháng1  Năm2026";
            if(!originalStory.Contains(originalNumber,StringComparison.Ordinal) || !originalStory.Contains(originalDate,StringComparison.Ordinal))
                throw new InvalidOperationException("Mẫu số/ngày không còn đúng nội dung kiểm thử");
            string expectedHeader=originalStory.Replace(originalNumber,"Số: 03/BC-ABC",StringComparison.Ordinal)
                .Replace(originalDate,"Địa danh, ngày 03 tháng 01 năm 2026",StringComparison.Ordinal);
            static Issue[] HeaderEdits(IReadOnlyList<Issue> found)=>found.Where(i=>i.CanFix &&
                i.Kind is FixKind.AdministrativeNumber or FixKind.AdministrativeDate).ToArray();
            var originalHeader=HeaderEdits(test.Check());
            var one=originalHeader.First(i=>i.Kind==FixKind.AdministrativeNumber && i.Replacement=="03");
            test.Fix(one);
            if(HeaderEdits(test.Check()).Length>=originalHeader.Length || test.ReadCheckText()!=originalStory.Replace(originalNumber,"số  :  03 / BC – ABC",StringComparison.Ordinal))
                throw new InvalidOperationException("Sửa riêng số văn bản chưa giảm lỗi");
            test.Undo();
            if(HeaderEdits(test.Check()).Length!=originalHeader.Length || test.ReadCheckText()!=originalStory)
                throw new InvalidOperationException("Hoàn tác sửa riêng chưa phục hồi mẫu số/ngày");
            Pass(stage);
            stage="Sửa chung số/ký hiệu và ngày tháng, lưu và Hoàn tác cả lượt";
            test.FixMany(HeaderEdits(test.Check()));
            if(HeaderEdits(test.Check()).Length!=0 || test.ReadCheckText()!=expectedHeader)
                throw new InvalidOperationException("Sửa nhóm số/ngày chưa đạt hoặc thay chữ ngoài phạm vi");
            string headerSaved=Path.Combine(folder,"header-fixed.docx");test.SaveCopy(headerSaved);
            test.Undo();
            if(HeaderEdits(test.Check()).Length!=originalHeader.Length || test.ReadCheckText()!=originalStory)
                throw new InvalidOperationException("Hoàn tác một lần chưa phục hồi cả nhóm số/ngày");
            Pass(stage);
            stage="Mở lại bản đã sửa số/ngày và kiểm mẫu trường trống";
            test.Open(headerSaved);
            if(HeaderEdits(test.Check()).Length!=0 || test.ReadCheckText()!=expectedHeader)
                throw new InvalidOperationException("Số/ngày chưa giữ nguyên kết quả sau mở lại");
            test.Open(Path.Combine(AppContext.BaseDirectory,"samples","TruongSoNgayDeTrong.docx"));
            if(test.Check().Any(i=>i.CanFix && i.Kind is FixKind.Text or FixKind.AdministrativeNumber or FixKind.AdministrativeDate) ||
                !test.ReadCheckText().Contains("Số:        /BC-ABC",StringComparison.Ordinal) ||
                !test.ReadCheckText().Contains("Địa danh, ngày        tháng 10 năm 2026",StringComparison.Ordinal))
                throw new InvalidOperationException("Mẫu trường trống bị đề xuất thay đổi hoặc mất khoảng trống");
            Pass(stage);
            stage="Trường trống có ghi chú và điểm dừng tab: sửa nhóm, lưu/mở, Hoàn tác";
            string annotated=CreateAnnotatedHeaderSample(Path.Combine(AppContext.BaseDirectory,"samples","TruongSoNgayDeTrong.docx"),folder);
            test.Open(annotated);
            string annotatedBefore=test.ReadCheckText();
            const string annotatedDate="Địa danh, ngày        tháng 10 năm 2026 (dự thảo)  ghi chú";
            if(!annotatedBefore.Contains(annotatedDate,StringComparison.Ordinal) ||
                !annotatedBefore.Contains("Số: 3/BC-ABC",StringComparison.Ordinal))
                throw new InvalidOperationException("Mẫu có ghi chú không còn đúng nội dung kiểm thử");
            var annotatedIssues=test.Check();
            var annotatedEdits=annotatedIssues.Where(i=>i.CanFix &&
                i.Kind is FixKind.Text or FixKind.AdministrativeNumber or FixKind.AdministrativeDate).ToArray();
            if(annotatedEdits.Length!=2 || !annotatedEdits.Any(i=>i.Kind==FixKind.AdministrativeNumber && i.Replacement=="03") ||
                !annotatedEdits.Any(i=>i.Kind==FixKind.Text && i.Original=="  " && i.Replacement==" ") ||
                !annotatedIssues.Any(i=>i.Kind==FixKind.AdministrativeDate && !i.CanFix))
                throw new InvalidOperationException("Không phân biệt đúng ô ngày trống, ghi chú và tab định dạng");
            test.FixMany(annotatedEdits);
            string annotatedExpected=annotatedBefore.Replace("Số: 3/BC-ABC","Số: 03/BC-ABC",StringComparison.Ordinal)
                .Replace(annotatedDate,annotatedDate.Replace(")  ghi",") ghi",StringComparison.Ordinal),StringComparison.Ordinal);
            if(test.ReadCheckText()!=annotatedExpected || test.Check().Any(i=>i.CanFix &&
                i.Kind is FixKind.Text or FixKind.AdministrativeNumber or FixKind.AdministrativeDate))
                throw new InvalidOperationException("Sửa nhóm làm mất ô trống hoặc chưa hết gợi ý được phép sửa");
            string annotatedSaved=Path.Combine(folder,"annotated-header-fixed.docx");test.SaveCopy(annotatedSaved);
            test.Undo();
            if(test.ReadCheckText()!=annotatedBefore)throw new InvalidOperationException("Hoàn tác không phục hồi toàn bộ mẫu có ghi chú");
            test.Open(annotatedSaved);
            if(test.ReadCheckText()!=annotatedExpected)throw new InvalidOperationException("Mở lại không giữ kết quả và khoảng trống của mẫu có ghi chú");
            Pass(stage);
            stage="Lỗi giữa/cuối nhóm chữ: rollback và giữ lượt Hoàn tác trước đó";
            test.Open(headerSample);
            string faultBaseline=test.ReadCheckText();
            var faultInitial=HeaderEdits(test.Check());
            test.Fix(faultInitial.First(i=>i.Kind==FixKind.AdministrativeNumber && i.Replacement=="03"));
            string beforeFailure=test.ReadCheckText();
            if(beforeFailure==faultBaseline)throw new InvalidOperationException("Chưa tạo được lượt sửa trước để kiểm Hoàn tác");
            var remainingBeforeFailure=HeaderEdits(test.Check());
            if(remainingBeforeFailure.Length<2)throw new InvalidOperationException("Thiếu gợi ý để kiểm lỗi giữa nhóm");
            foreach(int failAfter in new[]{1,remainingBeforeFailure.Length})
            {
                bool injected=false;
                try{test.FixTextFailureProbe(HeaderEdits(test.Check()),failAfter);}
                catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){injected=true;}
                if(!injected || test.ReadCheckText()!=beforeFailure || HeaderEdits(test.Check()).Length!=remainingBeforeFailure.Length)
                    throw new InvalidOperationException("Lỗi giả lập chưa phục hồi nguyên văn và trạng thái trước nhóm");
            }
            test.Undo();
            if(test.ReadCheckText()!=faultBaseline || HeaderEdits(test.Check()).Length!=faultInitial.Length)
                throw new InvalidOperationException("Nhóm thất bại đã làm thay đổi lượt Hoàn tác thành công trước đó");
            test.FixMany(HeaderEdits(test.Check()));
            if(test.ReadCheckText()!=expectedHeader || HeaderEdits(test.Check()).Length!=0)
                throw new InvalidOperationException("Không sửa tiếp được sau rollback lỗi giả lập");
            test.Undo();
            if(test.ReadCheckText()!=faultBaseline)throw new InvalidOperationException("Một Hoàn tác không phục hồi nhóm sửa tiếp sau rollback");
            Pass(stage);
            stage="Định dạng riêng/chung với số-ngày, giữ chữ và kiểu khác, lưu/mở, Hoàn tác";
            string typography=CreateTypographySample(headerSample,folder);
            test.Open(typography);
            string typographyBefore=test.ReadCheckText();
            var typographyIssues=test.Check();
            var formats=typographyIssues.Where(i=>i.CharacterFormat is not null).OrderBy(i=>i.Start).ToArray();
            if(formats.Length!=2 || formats[0].CharacterFormat!=new CharacterFormat(13,false) ||
                formats[1].CharacterFormat!=new CharacterFormat(14,true))
                throw new InvalidOperationException("Chưa nhận diện đúng hai lỗi định dạng và cỡ ngày hợp lệ");
            var decorations=formats.Select(test.ReadCheckDecorations).ToArray();
            if(decorations.Any(d=>d.Bold!=-1 || d.Underline!=1))throw new InvalidOperationException("Mẫu giả thiếu kiểu cần giữ");
            test.FixMany(formats);
            if(test.ReadCheckText()!=typographyBefore || test.Check().Any(i=>i.CharacterFormat is not null) ||
                !formats.Select(test.ReadCheckDecorations).SequenceEqual(decorations))
                throw new InvalidOperationException("Sửa định dạng làm đổi chữ/kiểu khác hoặc chưa đạt hậu kiểm");
            string typographySaved=Path.Combine(folder,"typography-only-fixed.docx");test.SaveCopy(typographySaved);
            test.Undo();
            if(test.ReadCheckText()!=typographyBefore || test.Check().Count(i=>i.CharacterFormat is not null)!=2)
                throw new InvalidOperationException("Hoàn tác chưa phục hồi hai lỗi định dạng");
            test.Open(typographySaved);
            if(test.ReadCheckText()!=typographyBefore || test.Check().Any(i=>i.CharacterFormat is not null) ||
                !formats.Select(test.ReadCheckDecorations).SequenceEqual(decorations))
                throw new InvalidOperationException("Mở lại chưa giữ chữ, định dạng hoặc kiểu cần giữ");
            test.Open(typography);
            var combined=test.Check().Where(i=>i.CanFix && (i.CharacterFormat is not null ||
                i.Kind is FixKind.AdministrativeNumber or FixKind.AdministrativeDate)).ToArray();
            bool formatRollback=false;
            try{test.FixTextFailureProbe(combined,3);}
            catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){formatRollback=true;}
            if(!formatRollback || test.ReadCheckText()!=typographyBefore || test.Check().Count(i=>i.CharacterFormat is not null)!=2 ||
                !formats.Select(test.ReadCheckDecorations).SequenceEqual(decorations))
                throw new InvalidOperationException("Lỗi sau bước định dạng chưa phục hồi cả nhóm chữ/định dạng");
            test.FixMany(test.Check().Where(i=>i.CanFix && (i.CharacterFormat is not null ||
                i.Kind is FixKind.AdministrativeNumber or FixKind.AdministrativeDate)).ToArray());
            if(test.ReadCheckText()!=expectedHeader || test.Check().Any(i=>i.CharacterFormat is not null ||
                i.CanFix && i.Kind is FixKind.AdministrativeNumber or FixKind.AdministrativeDate))
                throw new InvalidOperationException("Sửa chung chữ/định dạng chưa đạt trong một lượt");
            Pass(stage);
            stage="Lưu bản sao: lỗi giữa chừng/đích bị khóa giữ tệp cũ, tài liệu gốc và trạng thái chưa lưu";
            string existingCopy=Path.Combine(folder,"save-copy-existing.docx");File.Copy(test.Source,existingCopy);
            byte[] existingDigest=SaveDigest(existingCopy),sourceDigest=SaveDigest(test.Source);
            if(!test.Changed)throw new InvalidOperationException("Mẫu thiếu lượt sửa chưa lưu để kiểm lỗi lưu");
            bool interruptedSave=false;
            try{test.SaveCopyFailureProbe(existingCopy);}
            catch(OfficeSaveProbeException){interruptedSave=true;}
            if(!interruptedSave || !test.Changed || !SaveDigest(existingCopy).SequenceEqual(existingDigest) ||
                !SaveDigest(test.Source).SequenceEqual(sourceDigest) || test.ReadCheckText()!=expectedHeader)
                throw new InvalidOperationException("Lỗi giữa chừng đã làm đổi tệp đích/gốc hoặc mất trạng thái chưa lưu");
            bool lockedSave=false;
            using(var held=new FileStream(existingCopy,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                try{test.SaveCopy(existingCopy);}
                catch(Exception error) when(error is IOException or UnauthorizedAccessException){lockedSave=true;}
            }
            if(!lockedSave || !test.Changed || !SaveDigest(existingCopy).SequenceEqual(existingDigest))
                throw new InvalidOperationException("Tệp đang khóa không được giữ nguyên sau lỗi công bố bản sao");
            foreach(string protectedPath in new[]{test.Source,test.RecoveryPath})
            {
                bool rejected=false;try{test.SaveCopy(protectedPath);}catch(InvalidOperationException){rejected=true;}
                if(!rejected || !test.Changed || !SaveDigest(test.Source).SequenceEqual(sourceDigest))
                    throw new InvalidOperationException("Chưa chặn lưu đè tài liệu gốc/bản làm việc");
            }
            if(Directory.EnumerateFiles(folder,".ChinhTaViet-*.tmp").Any())
                throw new InvalidOperationException("Còn bản sao tạm sau lỗi lưu đã xử lý");
            Pass(stage);
            stage="Thay bản sao đã có bằng bản đầy đủ, lưu/mở và giữ một Undo của nhóm chữ/phông";
            test.SaveCopy(existingCopy);
            if(test.Changed || !SaveDigest(existingCopy).SequenceEqual(SaveDigest(test.RecoveryPath)) ||
                !SaveDigest(test.Source).SequenceEqual(sourceDigest))
                throw new InvalidOperationException("Bản công bố chưa khớp checkpoint hoặc trạng thái lưu/tệp gốc bị sai");
            string combinedSaved=Path.Combine(folder,"typography-and-text-fixed.docx");test.SaveCopy(combinedSaved);
            test.Undo();
            if(test.ReadCheckText()!=typographyBefore || test.Check().Count(i=>i.CharacterFormat is not null)!=2)
                throw new InvalidOperationException("Một Hoàn tác chưa phục hồi cả chữ và định dạng");
            test.Open(combinedSaved);
            if(test.ReadCheckText()!=expectedHeader || test.Check().Any(i=>i.CharacterFormat is not null ||
                i.CanFix && i.Kind is FixKind.AdministrativeNumber or FixKind.AdministrativeDate))
                throw new InvalidOperationException("Mở lại nhóm chữ/định dạng còn lỗi");
            Pass(stage);
            stage="Đo vị trí số/ngày trên XPS và phát hiện mẫu giả cố ý lệch tâm/khác hàng";
            string positionSource=Path.Combine(AppContext.BaseDirectory,"samples","TruongSoNgayDeTrong.docx");
            test.Open(positionSource);test.Check();
            string positionBefore=test.ReadCheckText();
            if(test.AdministrativePositionTargets.Count!=2 || test.AdministrativePositionTargets.Any(t=>t.References.Count!=2))
                throw new InvalidOperationException("Chưa nhận diện đủ hai dòng và các mốc đo vị trí của mẫu giả");
            var basePositions=InspectPositions(test,folder,"position-base.xps");
            if(basePositions.Length!=0 || test.ReadCheckText()!=positionBefore)
                throw new InvalidOperationException("Mẫu vị trí chuẩn chưa đo đạt hoặc việc kiểm đã làm đổi chữ");
            string shiftedPosition=CreatePositionSample(positionSource,folder);
            test.Open(shiftedPosition);test.Check();
            var shiftedPositions=InspectPositions(test,folder,"position-shifted.xps");
            if(shiftedPositions.Count(i=>i.Message.Contains("lệch tâm"))<2 ||
                !shiftedPositions.Any(i=>i.Title=="Số và ngày tháng chưa cùng dòng") ||
                shiftedPositions.Count(i=>i.CenterFix is not null)!=2 ||
                shiftedPositions.Any(i=>i.Title=="Số và ngày tháng chưa cùng dòng" && i.CanFix) || test.ReadCheckText()!=positionBefore)
                throw new InvalidOperationException("Chưa đo đủ điều kiện căn giữa hai dòng hoặc cảnh báo khác hàng bị cho sửa tự động");
            Pass(stage);
            stage="Rollback cả nhóm căn giữa khi lỗi sau dòng đầu, không thêm lượt Undo";
            var centerPlan=shiftedPositions.Where(i=>i.CenterFix is not null).ToArray();
            bool positionRollback=false;
            try{test.FixTextFailureProbe(centerPlan,1);}
            catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){positionRollback=true;}
            test.Check();
            if(!positionRollback || test.ReadCheckText()!=positionBefore ||
                InspectPositions(test,folder,"position-rollback.xps").Count(i=>i.CenterFix is not null)!=2)
                throw new InvalidOperationException("Rollback căn giữa chưa phục hồi cả hai tâm lệch");
            test.Undo();test.Check();
            if(InspectPositions(test,folder,"position-no-undo.xps").Count(i=>i.CenterFix is not null)!=2)
                throw new InvalidOperationException("Lượt căn giữa lỗi đã làm sai stack Undo");
            Pass(stage);
            stage="Hậu kiểm từ chối và phục hồi khi dòng không chọn bị dịch sau sửa";
            var numberOnly=InspectPositions(test,folder,"position-drift-before.xps").Single(i=>i.CenterFix?.Role==AdministrativePositionRole.Number);
            bool driftRejected=false;
            try{test.FixPositionDriftProbe([numberOnly]);}
            catch(InvalidOperationException error) when(error.InnerException is PositionVerificationException){driftRejected=true;}
            test.Check();
            var driftRestored=InspectPositions(test,folder,"position-drift-restored.xps");
            if(!driftRejected || test.ReadCheckText()!=positionBefore || driftRestored.Count(i=>i.CenterFix is not null)!=2 ||
                !driftRestored.Any(i=>i.Title=="Số và ngày tháng chưa cùng dòng"))
                throw new InvalidOperationException("Chưa từ chối/khôi phục đầy đủ khi dòng không chọn bị dịch");
            test.Undo();test.Check();
            if(InspectPositions(test,folder,"position-drift-no-undo.xps").Count(i=>i.CenterFix is not null)!=2)
                throw new InvalidOperationException("Hậu kiểm thất bại đã thêm Undo sai");
            Pass(stage);
            foreach(var role in Enum.GetValues<AdministrativePositionRole>())
            {
                stage="Căn giữa riêng dòng "+role+", kiểm tâm thật và hoàn tác";
                var current=InspectPositions(test,folder,"position-single-before.xps").Single(i=>i.CenterFix?.Role==role);
                test.FixMany([current]);test.Check();
                var single=InspectPositions(test,folder,"position-single-after.xps");
                if(single.Any(i=>i.CenterFix?.Role==role) || single.Count(i=>i.CenterFix is not null)!=1 || test.ReadCheckText()!=positionBefore)
                    throw new InvalidOperationException("Sửa riêng căn giữa chưa đúng hoặc làm đổi chữ");
                test.Undo();test.Check();
                if(InspectPositions(test,folder,"position-single-undo.xps").Count(i=>i.CenterFix is not null)!=2)
                    throw new InvalidOperationException("Hoàn tác sửa riêng chưa trả tâm ban đầu");
                Pass(stage);
            }
            stage="Căn giữa cả hai dòng trong một lượt, giữ cảnh báo khác hàng và chữ/ô trống";
            centerPlan=InspectPositions(test,folder,"position-group-before.xps").Where(i=>i.CenterFix is not null).ToArray();
            test.FixMany(centerPlan);test.Check();
            var centered=InspectPositions(test,folder,"position-centered.xps");
            if(centered.Length!=1 || centered[0].Title!="Số và ngày tháng chưa cùng dòng" || centered[0].CanFix || test.ReadCheckText()!=positionBefore)
                throw new InvalidOperationException("Còn lệch tâm/chưa đo được hoặc cảnh báo khác hàng bị xóa sai");
            string centerSaved=Path.Combine(folder,"position-centered.docx");test.SaveCopy(centerSaved);Pass(stage);
            stage="Một Undo khôi phục cả hai tâm; lưu/mở lại giữ hai tâm đã sửa";
            test.Undo();test.Check();
            if(InspectPositions(test,folder,"position-group-undo.xps").Count(i=>i.CenterFix is not null)!=2 || test.ReadCheckText()!=positionBefore)
                throw new InvalidOperationException("Một Undo chưa phục hồi cả nhóm căn giữa");
            test.Open(centerSaved);test.Check();
            var reopened=InspectPositions(test,folder,"position-reopened.xps");
            if(reopened.Length!=1 || reopened[0].Title!="Số và ngày tháng chưa cùng dòng" || test.ReadCheckText()!=positionBefore)
                throw new InvalidOperationException("Tâm đã sửa chưa giữ sau mở lại hoặc chữ bị thay đổi");
            Pass(stage);
            stage="Nhận diện ba đoạn tên loại/trích yếu, giữ cỡ 14/13,5 và bỏ qua phông khác ở dấu đoạn";
            string docHeadingSource=CreateDocumentHeadingSample(positionSource,folder,false);
            byte[] docHeadingDigest=SaveDigest(docHeadingSource);
            test.Open(docHeadingSource);
            string docHeadingOriginal=test.ReadCheckText();
            var docFormats=test.Check().Where(i=>i.Kind==FixKind.DocumentHeadingFormat && i.CanFix).OrderBy(i=>i.Start).ToArray();
            if(docFormats.Length!=3 || !docFormats.Select(i=>i.CharacterFormat!.Size).SequenceEqual(new[]{13f,14f,13.5f}))
                throw new InvalidOperationException("Chưa nhận diện đúng tên loại/trích yếu hoặc chưa giữ cỡ đang hợp lệ");
            var docDecorations=docFormats.Select(test.ReadCheckDecorations).ToArray();
            if(docDecorations.Any(d=>d.Bold!=0 || d.Underline!=1 || d.Alignment!=1))
                throw new InvalidOperationException("Mẫu giả thiếu kiểu cần đổi/giữ để kiểm tên loại/trích yếu");
            Pass(stage);
            stage="Sửa riêng tên loại, quét lại chỉ còn hai trích yếu và một Undo";
            test.FixMany([docFormats[0]]);
            if(test.ReadCheckText()!=docHeadingOriginal || test.Check().Count(i=>i.Kind==FixKind.DocumentHeadingFormat)!=2)
                throw new InvalidOperationException("Sửa tên loại chưa bỏ đúng mục hoặc làm đổi chữ");
            test.Undo();
            if(test.ReadCheckText()!=docHeadingOriginal || test.Check().Count(i=>i.Kind==FixKind.DocumentHeadingFormat && i.CanFix)!=3)
                throw new InvalidOperationException("Undo sửa riêng chưa phục hồi đúng ba gợi ý");
            Pass(stage);
            stage="Rollback nhóm tên loại/trích yếu khi lỗi sau bước định dạng thứ hai";
            bool docRollback=false;
            try{test.FixTextFailureProbe(docFormats,2);}
            catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){docRollback=true;}
            if(!docRollback || test.ReadCheckText()!=docHeadingOriginal ||
                test.Check().Count(i=>i.Kind==FixKind.DocumentHeadingFormat && i.CanFix)!=3 ||
                !docFormats.Select(test.ReadCheckDecorations).SequenceEqual(docDecorations))
                throw new InvalidOperationException("Rollback chưa giữ nguyên chữ/kiểu của cả nhóm tên loại/trích yếu");
            Pass(stage);
            stage="Sửa nhóm tên loại/trích yếu cùng một lỗi khoảng trắng đã chọn, kiểm toàn chữ và giữ gạch chân/căn đoạn";
            var docFound=test.Check();
            var docStyles=docFound.Where(i=>i.Kind==FixKind.DocumentHeadingFormat && i.CanFix).ToArray();
            var subjectSpacing=docFound.Single(i=>i.Kind==FixKind.Text && i.Start>=docFormats[1].Start &&
                i.Start<docFormats[1].Start+docFormats[1].Length);
            string docExpected=docHeadingOriginal.Replace("kết quả  thử","kết quả thử",StringComparison.Ordinal);
            test.FixMany(docStyles.Append(subjectSpacing));
            var docAfter=test.Check();
            if(test.ReadCheckText()!=docExpected || docAfter.Any(i=>i.Kind==FixKind.DocumentHeadingFormat))
                throw new InvalidOperationException("Sửa nhóm tên loại/trích yếu còn lỗi hoặc làm đổi chữ ngoài lựa chọn");
            var docTargets=test.LineTargets.Single(t=>t.Role==LineRole.Subject);
            string firstSubjectAfter=docFormats[1].Original.Replace("kết quả  thử","kết quả thử",StringComparison.Ordinal);
            var currentDocRows=new[]{docFormats[0],docFormats[1] with {Length=firstSubjectAfter.Length,Original=firstSubjectAfter,Replacement=firstSubjectAfter},
                docFormats[2] with {Start=docTargets.Start}};
            if(currentDocRows.Select(test.ReadCheckDecorations).Any(d=>d.Bold!=-1 || d.Underline!=1 || d.Alignment!=1))
                throw new InvalidOperationException("Nhóm tên loại/trích yếu chưa đậm hoặc làm mất kiểu cần giữ");
            string docHeadingSaved=Path.Combine(folder,"document-heading-fixed.docx");test.SaveCopy(docHeadingSaved);
            test.Undo();
            if(test.ReadCheckText()!=docHeadingOriginal || test.Check().Count(i=>i.Kind==FixKind.DocumentHeadingFormat && i.CanFix)!=3)
                throw new InvalidOperationException("Một Undo chưa phục hồi cả chữ/định dạng nhóm tên loại/trích yếu");
            test.Open(docHeadingSaved);
            if(test.ReadCheckText()!=docExpected || test.Check().Any(i=>i.Kind==FixKind.DocumentHeadingFormat) ||
                !SaveDigest(docHeadingSource).SequenceEqual(docHeadingDigest))
                throw new InvalidOperationException("Mở lại bản tên loại/trích yếu đã sửa chưa đạt hoặc tệp nguồn bị thay đổi");
            Pass(stage);
            stage="Trích yếu trộn kiểu đậm giữ thủ công, không cho sửa tự động dòng đó";
            string mixedHeading=CreateDocumentHeadingSample(positionSource,folder,true);test.Open(mixedHeading);
            var mixedDoc=test.Check().Where(i=>i.Kind==FixKind.DocumentHeadingFormat).ToArray();
            if(mixedDoc.Count(i=>i.CanFix)!=2 || mixedDoc.Count(i=>!i.CanFix)!=1)
                throw new InvalidOperationException("Chưa giữ thủ công riêng trích yếu có kiểu đậm trộn");
            Pass(stage);
            foreach(string structure in new[]{"hyperlink","sdt"})
            {
                stage="Giữ thủ công trích yếu có "+structure+", chặn nhóm gợi ý cũ trước ghi";
                string structuredSource=CreateStructuredTextSample(positionSource,folder,structure);
                string plainSource=RemoveTextStructure(structuredSource);
                byte[] structuredDigest=SaveDigest(structuredSource);
                test.Open(plainSource);string plainStory=test.ReadCheckText();
                var oldTextPlan=test.Check().Where(i=>i.Kind==FixKind.Text && i.CanFix).ToArray();
                if(oldTextPlan.Length!=3)throw new InvalidOperationException("Mẫu chữ thuần chưa có đúng ba lựa chọn chữ/khoảng trắng");
                test.Open(structuredSource);string structuredStory=test.ReadCheckText();
                if(structuredStory!=plainStory)throw new InvalidOperationException("Hai mẫu cấu trúc chưa cùng nguyên văn");
                byte[] checkpointDigest=SaveDigest(test.RecoveryPath);bool staleRejected=false;
                try{test.FixMany(oldTextPlan);}
                catch(InvalidOperationException error) when(error.Message.Contains("Gợi ý chữ/định dạng đã thay đổi",StringComparison.Ordinal)){staleRejected=true;}
                if(!staleRejected || test.Changed || test.ReadCheckText()!=structuredStory ||
                    !SaveDigest(test.RecoveryPath).SequenceEqual(checkpointDigest))
                    throw new InvalidOperationException("Nhóm cũ chưa bị từ chối trước ghi/checkpoint");
                test.Undo();
                if(test.ReadCheckText()!=structuredStory)throw new InvalidOperationException("Lượt bị chặn không được tạo Undo");
                Pass(stage);
                stage="Sửa riêng chữ/phông vùng thường bên cạnh "+structure+", lưu/mở và một Undo";
                var structuredFound=test.Check();
                var manual=structuredFound.Single(i=>!i.CanFix && i.Title=="Kiểm tra chữ trong vùng chưa cho phép sửa");
                if(structuredFound.Any(i=>i.CanFix && i.LineFix is null && i.Start>=manual.Start && i.Start<manual.Start+manual.Length))
                    throw new InvalidOperationException("Vùng có cấu trúc vẫn có lựa chọn chữ/phông tự sửa");
                var safeTextPlan=structuredFound.Where(i=>i.CanFix && i.Kind is FixKind.Text or FixKind.DocumentHeadingFormat).ToArray();
                if(safeTextPlan.Length!=3)throw new InvalidOperationException("Vùng thường phải còn hai định dạng và một lỗi khoảng trắng");
                string safeExpected=structuredStory.Replace("đợt  công tác","đợt công tác",StringComparison.Ordinal);
                test.FixMany(safeTextPlan);
                var afterStructured=test.Check();
                if(test.ReadCheckText()!=safeExpected || afterStructured.Any(i=>i.CanFix && i.Kind is FixKind.Text or FixKind.DocumentHeadingFormat) ||
                    !afterStructured.Any(i=>!i.CanFix && i.Title==manual.Title && i.Original==manual.Original))
                    throw new InvalidOperationException("Sửa vùng thường làm đổi/mất cảnh báo của vùng có cấu trúc hoặc còn lỗi cũ");
                string structuredSaved=Path.Combine(Path.GetDirectoryName(structuredSource)!,"structured-fixed.docx");
                test.SaveCopy(structuredSaved);
                VerifyTextStructure(structuredSource,structuredSaved,structure);
                test.Undo();
                if(test.ReadCheckText()!=structuredStory || test.Check().Count(i=>i.CanFix && i.Kind is FixKind.Text or FixKind.DocumentHeadingFormat)!=3)
                    throw new InvalidOperationException("Một Undo chưa phục hồi đầy đủ nhóm vùng thường");
                test.Open(structuredSaved);
                if(test.ReadCheckText()!=safeExpected || !test.Check().Any(i=>!i.CanFix && i.Original==manual.Original) ||
                    !SaveDigest(structuredSource).SequenceEqual(structuredDigest))
                    throw new InvalidOperationException("Mở lại không giữ chữ/cấu trúc hoặc làm đổi nguồn");
                Pass(stage);
            }
            foreach(var headingProfile in new[]{DocumentProfile.State,DocumentProfile.Party})
            {
                test.Profile=headingProfile;
                stage="Hậu kiểm chữ/phông tiêu đề "+headingProfile+": drift sau lưu phải rollback cả nhóm";
                string legacySource=CreateLegacyHeadingSample(headingProfile==DocumentProfile.State?positionSource:
                    Path.Combine(AppContext.BaseDirectory,"samples","VanBanDangMau.docx"),folder,headingProfile);
                byte[] legacySourceDigest=SaveDigest(legacySource);test.Open(legacySource);
                string legacyOriginal=test.ReadCheckText();
                var legacyFound=test.Check();
                var legacyFormats=legacyFound.Where(i=>i.CanFix && i.Format is not null).ToArray();
                var legacySpacing=legacyFound.Single(i=>i.CanFix && i.Kind==FixKind.Text);
                if(legacyFormats.Length!=(headingProfile==DocumentProfile.State?2:1) || legacyFormats.Any(i=>i.Original==i.Replacement))
                    throw new InvalidOperationException("Mẫu tiêu đề cần đủ lỗi chữ + phông và lỗi khoảng trắng riêng");
                var legacyPlan=legacyFormats.Append(legacySpacing).ToArray();
                byte[] legacyCheckpointDigest=SaveDigest(test.RecoveryPath);bool postSaveRejected=false;
                try{test.FixHeadingDriftProbe(legacyPlan);}
                catch(InvalidOperationException error) when(error.InnerException is InvalidOperationException cause &&
                    cause.Message.Contains("Định dạng sau lưu chưa khớp",StringComparison.Ordinal)){postSaveRejected=true;}
                if(!postSaveRejected || test.Changed || test.ReadCheckText()!=legacyOriginal ||
                    !SaveDigest(test.RecoveryPath).SequenceEqual(legacyCheckpointDigest) ||
                    test.Check().Count(i=>i.CanFix && i.Format is not null)!=legacyFormats.Length)
                    throw new InvalidOperationException("Drift phông sau lưu chưa bị phát hiện hoặc rollback chưa phục hồi cả nhóm");
                test.Undo();
                if(test.ReadCheckText()!=legacyOriginal)throw new InvalidOperationException("Nhóm thất bại không được tạo Undo");
                Pass(stage);
                stage="Sửa nhóm tiêu đề "+headingProfile+" và khoảng trắng, quét lại, lưu/mở, một Undo";
                // Word CPs can differ after reopening the checkpoint: use fresh proposals.
                var legacyFresh=test.Check().Where(i=>i.CanFix && (i.Format is not null || i.Kind==FixKind.Text)).ToArray();
                string legacyExpected=legacyOriginal.Replace("nội dung  thử nghiệm","nội dung thử nghiệm",StringComparison.Ordinal);
                foreach(var item in legacyFresh.Where(i=>i.Format is not null))
                    legacyExpected=legacyExpected.Replace(item.Original,item.Replacement,StringComparison.Ordinal);
                test.FixMany(legacyFresh);
                if(test.ReadCheckText()!=legacyExpected || test.Check().Any(i=>i.CanFix && (i.Format is not null || i.Kind==FixKind.Text)))
                    throw new InvalidOperationException("Sửa tiêu đề chưa khớp chữ/phông hoặc dấu đoạn làm giữ cảnh báo cũ");
                string legacySaved=Path.Combine(Path.GetDirectoryName(legacySource)!,"heading-fixed.docx");test.SaveCopy(legacySaved);
                test.Undo();
                if(test.ReadCheckText()!=legacyOriginal || test.Check().Count(i=>i.CanFix && i.Format is not null)!=legacyFormats.Length)
                    throw new InvalidOperationException("Một Undo chưa phục hồi toàn bộ chữ/phông của nhóm tiêu đề");
                test.Open(legacySaved);
                if(test.ReadCheckText()!=legacyExpected || test.Check().Any(i=>i.CanFix && (i.Format is not null || i.Kind==FixKind.Text)) ||
                    !SaveDigest(legacySource).SequenceEqual(legacySourceDigest))
                    throw new InvalidOperationException("Lưu/mở chưa giữ kết quả tiêu đề hoặc làm đổi nguồn");
                Pass(stage);
            }
            test.Profile=DocumentProfile.State;
            stage="Hoàn tác thành công dù dọn backup lỗi: tiêu thụ đúng lượt, lần sau về lượt trước";
            string cleanupSource=Path.Combine(folder,"heading-proof-State","heading-input.docx");
            byte[] cleanupSourceDigest=SaveDigest(cleanupSource);test.Profile=DocumentProfile.State;test.Open(cleanupSource);
            string cleanupBaseline=test.ReadCheckText();
            var firstCleanupEdit=test.Check().Where(i=>i.CanFix && i.Format is not null).OrderBy(i=>i.Start).First();
            test.Fix(firstCleanupEdit);string cleanupFirst=test.ReadCheckText();
            var secondCleanupEdit=test.Check().Single(i=>i.CanFix && i.Format is not null);
            test.Fix(secondCleanupEdit);string cleanupSecond=test.ReadCheckText();
            if(cleanupFirst==cleanupBaseline || cleanupSecond==cleanupFirst)
                throw new InvalidOperationException("Chưa tạo đủ hai lượt sửa độc lập để kiểm Hoàn tác");
            if(!test.UndoCleanupProbe() || test.ReadCheckText()!=cleanupFirst ||
                test.Check().Count(i=>i.CanFix && i.Format is not null)!=1 || !test.Changed)
                throw new InvalidOperationException("Lỗi dọn backup làm sai kết quả Hoàn tác hoặc không giữ backup");
            test.Undo();
            if(test.ReadCheckText()!=cleanupBaseline || test.Check().Count(i=>i.CanFix && i.Format is not null)!=2)
                throw new InvalidOperationException("Lần Hoàn tác kế tiếp chưa về lượt trước, có thể lặp lại checkpoint đã dùng");
            test.Undo();
            if(test.ReadCheckText()!=cleanupBaseline || !SaveDigest(cleanupSource).SequenceEqual(cleanupSourceDigest))
                throw new InvalidOperationException("Hết lượt Hoàn tác phải giữ nguyên trạng thái và nguồn mẫu");
            Pass(stage);
            stage="Lặp cụm từ: hai đề xuất xóa, giữ từ láy và không chồng lỗi khoảng trắng";
            string repeatedSource=CreateRepeatedTextSample(positionSource,folder);
            byte[] repeatedDigest=SaveDigest(repeatedSource);test.Profile=DocumentProfile.State;test.Open(repeatedSource);
            string repeatedOriginal=test.ReadCheckText();
            var repeatedFound=test.Check();
            var repeatedEdits=repeatedFound.Where(i=>i.CanFix && i.Kind is FixKind.RepeatedText or FixKind.Text).ToArray();
            var repeatedOnly=repeatedEdits.Where(i=>i.Kind==FixKind.RepeatedText).OrderBy(i=>i.Start).ToArray();
            if(repeatedOnly.Length!=2 || repeatedEdits.Length!=3 || repeatedFound.Count(i=>i.Kind==FixKind.RepeatedText && !i.CanFix)!=2)
                throw new InvalidOperationException("Chưa phân biệt đủ cụm lặp, khoảng trắng và từ láy thủ công");
            var retainedProbe=new Issue(repeatedOnly[0].Start-"kế hoạch".Length,"kế hoạch".Length,"kế hoạch","kế hoạch","",false);
            var retainedDecorations=test.ReadCheckDecorations(retainedProbe);
            if(retainedDecorations.Bold!=-1 || retainedDecorations.Underline!=1)
                throw new InvalidOperationException("Mẫu phần giữ lại chưa có đậm/gạch chân riêng");
            Pass(stage);
            stage="Lỗi sau xóa cụm lặp đầu tiên: rollback đủ chữ và không tạo Undo";
            bool repeatRollback=false;
            try{test.FixTextFailureProbe(repeatedEdits,1);}
            catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){repeatRollback=true;}
            if(!repeatRollback || test.ReadCheckText()!=repeatedOriginal || test.Changed ||
                test.Check().Count(i=>i.Kind==FixKind.RepeatedText && i.CanFix)!=2)
                throw new InvalidOperationException("Rollback sau xóa cụm lặp chưa giữ nguyên cả nhóm");
            test.Undo();if(test.ReadCheckText()!=repeatedOriginal)throw new InvalidOperationException("Nhóm lặp thất bại không được tạo Undo");
            Pass(stage);
            stage="Sửa riêng một cụm lặp, giữ nguyên kiểu lần đầu và Undo";
            test.Fix(test.Check().Where(i=>i.Kind==FixKind.RepeatedText && i.CanFix).OrderBy(i=>i.Start).First());
            if(test.ReadCheckText()!=repeatedOriginal.Replace("kế hoạch kế hoạch","kế hoạch",StringComparison.Ordinal) ||
                test.Check().Count(i=>i.Kind==FixKind.RepeatedText && i.CanFix)!=1 || test.ReadCheckDecorations(retainedProbe)!=retainedDecorations)
                throw new InvalidOperationException("Sửa riêng cụm lặp chưa đúng chữ/kiểu hoặc mục đã sửa còn hiện");
            test.Undo();if(test.ReadCheckText()!=repeatedOriginal)throw new InvalidOperationException("Undo sửa riêng cụm lặp chưa về nguyên bản");
            Pass(stage);
            stage="Sửa nhóm lặp và khoảng trắng: một lượt, lưu/mở và một Undo";
            var repeatFresh=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.RepeatedText or FixKind.Text).ToArray();
            string repeatedExpected=repeatedOriginal.Replace("Nội dung  kế hoạch kế hoạch đã được duyệt.","Nội dung kế hoạch đã được duyệt.",StringComparison.Ordinal)
                .Replace("công tác công tác công tác","công tác",StringComparison.Ordinal);
            test.FixMany(repeatFresh);var repeatAfter=test.Check();
            if(test.ReadCheckText()!=repeatedExpected || repeatAfter.Any(i=>i.CanFix && i.Kind is FixKind.RepeatedText or FixKind.Text) ||
                repeatAfter.Count(i=>i.Kind==FixKind.RepeatedText && !i.CanFix)!=2 ||
                test.ReadCheckDecorations(retainedProbe with {Start=retainedProbe.Start-1})!=retainedDecorations)
                throw new InvalidOperationException("Sửa nhóm lặp chưa hết mục đã sửa, làm đổi kiểu giữ lại hoặc xóa từ láy thủ công");
            string repeatedSaved=Path.Combine(folder,"repeated-text-fixed.docx");test.SaveCopy(repeatedSaved);
            test.Undo();
            if(test.ReadCheckText()!=repeatedOriginal || test.Check().Count(i=>i.Kind==FixKind.RepeatedText && i.CanFix)!=2)
                throw new InvalidOperationException("Một Undo chưa phục hồi toàn nhóm lặp/khoảng trắng");
            test.Open(repeatedSaved);
            if(test.ReadCheckText()!=repeatedExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.RepeatedText or FixKind.Text) ||
                !SaveDigest(repeatedSource).SequenceEqual(repeatedDigest))
                throw new InvalidOperationException("Mở lại bản sửa lặp chưa đạt hoặc nguồn mẫu thay đổi");
            Pass(stage);
            stage="Xuất tiến độ khi Word có thay đổi: giữ chữ, Changed, checkpoint và Undo";
            test.Open(repeatedSource);string beforeReport=test.ReadCheckText();
            test.Fix(test.Check().Where(i=>i.Kind==FixKind.RepeatedText && i.CanFix).OrderBy(i=>i.Start).First());
            string reportWorking=test.ReadCheckText();byte[] reportCheckpoint=SaveDigest(test.RecoveryPath);
            string programReport=DevelopmentReport.Build(typeof(OfficeIntegrationCheck).Assembly.GetName().Version!.ToString(3),DateTimeOffset.Now);
            string programReportPath=Path.Combine(folder,"program-progress.txt");test.SaveProgressReport(programReportPath,programReport);
            if(!test.Changed || test.ReadCheckText()!=reportWorking || File.ReadAllText(programReportPath)!=programReport ||
                !SaveDigest(test.RecoveryPath).SequenceEqual(reportCheckpoint))
                throw new InvalidOperationException("Xuất tiến độ làm đổi chữ/Changed/checkpoint hoặc nội dung báo cáo");
            bool reportSourceBlocked=false,reportWorkingBlocked=false;
            try{test.SaveProgressReport(test.Source,programReport);}catch(InvalidOperationException){reportSourceBlocked=true;}
            try{test.SaveProgressReport(test.RecoveryPath,programReport);}catch(InvalidOperationException){reportWorkingBlocked=true;}
            if(!reportSourceBlocked || !reportWorkingBlocked || test.ReadCheckText()!=reportWorking || !test.Changed ||
                !SaveDigest(repeatedSource).SequenceEqual(repeatedDigest))
                throw new InvalidOperationException("Xuất báo cáo chưa chặn nguồn/bản làm việc hoặc làm đổi trạng thái");
            test.Undo();
            if(test.ReadCheckText()!=beforeReport || test.Check().Count(i=>i.Kind==FixKind.RepeatedText && i.CanFix)!=2)
                throw new InvalidOperationException("Xuất báo cáo đã thêm hoặc làm mất lượt Undo chữ trước đó");
            Pass(stage);
            stage="Dấu phẩy/chấm phẩy: nhận diện riêng và sửa nhóm với khoảng trắng/cụm lặp";
            string punctuationSource=CreatePunctuationSample(positionSource,folder);
            byte[] punctuationDigest=SaveDigest(punctuationSource);test.Open(punctuationSource);
            string punctuationOriginal=test.ReadCheckText();
            var punctuationEdits=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            var punctuationOnly=punctuationEdits.Where(i=>i.Kind==FixKind.PunctuationSpace).OrderBy(i=>i.Start).ToArray();
            if(punctuationOnly.Length!=2 || punctuationEdits.Length!=4)
                throw new InvalidOperationException("Chưa tách đúng dấu câu, cụm lặp và khoảng trắng; không được chạm dòng số/địa chỉ");
            var punctuationProbe=new Issue(punctuationOnly[0].Start-"Nội dung".Length,"Nội dung".Length,"Nội dung","Nội dung","",false);
            var punctuationDecoration=test.ReadCheckDecorations(punctuationProbe);
            if(punctuationDecoration.Bold!=-1 || punctuationDecoration.Underline!=1)
                throw new InvalidOperationException("Mẫu chữ trước dấu câu chưa có kiểu riêng");
            test.Fix(punctuationOnly[0]);
            if(test.ReadCheckText()!=punctuationOriginal.Replace("Nội dung  ,  báo cáo","Nội dung, báo cáo",StringComparison.Ordinal) ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.PunctuationSpace)!=1 || test.ReadCheckDecorations(punctuationProbe)!=punctuationDecoration)
                throw new InvalidOperationException("Sửa riêng dấu câu làm đổi phần không chọn/kiểu hoặc chưa bỏ mục đã sửa");
            test.Undo();if(test.ReadCheckText()!=punctuationOriginal)throw new InvalidOperationException("Undo dấu câu chưa về nguyên bản");
            Pass(stage);
            test.Open(punctuationSource);
            stage="Lỗi sau sửa dấu câu đầu tiên: phục hồi toàn nhóm, không tạo Undo";
            bool punctuationRollback=false;
            try{test.FixTextFailureProbe(punctuationEdits,3);}
            catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){punctuationRollback=true;}
            if(!punctuationRollback || test.ReadCheckText()!=punctuationOriginal || test.Changed ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.PunctuationSpace)!=2)
                throw new InvalidOperationException("Rollback nhóm dấu câu chưa giữ nguyên cả nhóm");
            test.Undo();if(test.ReadCheckText()!=punctuationOriginal)throw new InvalidOperationException("Lượt dấu câu lỗi đã tạo Undo");
            Pass(stage);
            stage="Sửa cả nhóm dấu câu/chữ: quét hết mục đã sửa, một Undo, lưu và mở lại";
            string punctuationExpected=punctuationOriginal.Replace("Nội dung  ,  báo cáo;công tác công tác đã  xong.","Nội dung, báo cáo; công tác đã xong.",StringComparison.Ordinal);
            test.FixMany(test.Check().Where(i=>i.CanFix && i.Kind is FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text));
            if(test.ReadCheckText()!=punctuationExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(punctuationProbe)!=punctuationDecoration)
                throw new InvalidOperationException("Nhóm dấu câu chưa đúng chữ/kiểu hoặc còn cảnh báo đã sửa");
            string punctuationSaved=Path.Combine(folder,"punctuation-fixed.docx");test.SaveCopy(punctuationSaved);test.Undo();
            if(test.ReadCheckText()!=punctuationOriginal || test.Check().Count(i=>i.CanFix && i.Kind==FixKind.PunctuationSpace)!=2)
                throw new InvalidOperationException("Một Undo chưa trả đủ nhóm dấu câu/chữ");
            test.Open(punctuationSaved);
            if(test.ReadCheckText()!=punctuationExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(punctuationProbe)!=punctuationDecoration || !SaveDigest(punctuationSource).SequenceEqual(punctuationDigest))
                throw new InvalidOperationException("Bản lưu dấu câu đổi chữ/kiểu hoặc nguồn mẫu thay đổi");
            Pass(stage);
            stage="Ngoặc tròn: sửa riêng khoảng trắng, giữ kiểu và các mục không chọn";
            string parenthesisSource=CreateParenthesisSample(positionSource,folder);
            byte[] parenthesisDigest=SaveDigest(parenthesisSource);test.Open(parenthesisSource);
            string parenthesisOriginal=test.ReadCheckText();
            var parenthesisEdits=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            var parenthesisOnly=parenthesisEdits.Where(i=>i.Kind==FixKind.ParenthesisSpace).OrderBy(i=>i.Start).ToArray();
            if(parenthesisOnly.Length!=2 || parenthesisEdits.Length!=5)
                throw new InvalidOperationException("Chưa phân biệt đúng hai mép ngoặc, dấu câu, cụm lặp, khoảng trắng hoặc ô trống bị sửa");
            var parenthesisProbe=new Issue(parenthesisOnly[0].Start+parenthesisOnly[0].Length,"kế hoạch".Length,"kế hoạch","kế hoạch","",false);
            var parenthesisDecoration=test.ReadCheckDecorations(parenthesisProbe);
            if(parenthesisDecoration.Bold!=-1 || parenthesisDecoration.Underline!=1)
                throw new InvalidOperationException("Mẫu chữ giữ lại trong ngoặc chưa có kiểu riêng");
            test.Fix(parenthesisOnly[0]);
            if(test.ReadCheckText()!=parenthesisOriginal.Replace("Nội dung  (  kế hoạch","Nội dung  (kế hoạch",StringComparison.Ordinal) ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.ParenthesisSpace)!=1 ||
                test.ReadCheckDecorations(parenthesisProbe with {Start=parenthesisProbe.Start-parenthesisOnly[0].Length})!=parenthesisDecoration)
                throw new InvalidOperationException("Sửa riêng mép ngoặc chưa đúng chữ/kiểu hoặc đã làm mất mục không chọn");
            test.Undo();if(test.ReadCheckText()!=parenthesisOriginal)throw new InvalidOperationException("Undo sửa riêng ngoặc chưa về nguyên bản");
            Pass(stage);
            test.Open(parenthesisSource);
            stage="Lỗi sau xóa mép ngoặc đầu trong nhóm: rollback đủ chữ/kiểu, không tạo Undo";
            bool parenthesisRollback=false;
            try{test.FixTextFailureProbe(parenthesisEdits,2);}
            catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){parenthesisRollback=true;}
            if(!parenthesisRollback || test.ReadCheckText()!=parenthesisOriginal || test.Changed ||
                test.ReadCheckDecorations(parenthesisProbe)!=parenthesisDecoration || test.Check().Count(i=>i.CanFix && i.Kind==FixKind.ParenthesisSpace)!=2)
                throw new InvalidOperationException("Rollback nhóm ngoặc chưa giữ nguyên toàn chữ/kiểu");
            test.Undo();if(test.ReadCheckText()!=parenthesisOriginal)throw new InvalidOperationException("Nhóm ngoặc lỗi đã tạo Undo");
            Pass(stage);
            stage="Sửa nhóm ngoặc/chữ: quét, một Undo, lưu/mở, giữ ô trống và nguồn";
            string parenthesisExpected=parenthesisOriginal.Replace("Nội dung  (  kế hoạch kế hoạch  ) đã duyệt;hoàn thành.","Nội dung (kế hoạch) đã duyệt; hoàn thành.",StringComparison.Ordinal);
            var parenthesisFresh=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            int parenthesisShift=parenthesisFresh.Where(i=>(long)i.Start+i.Length<=parenthesisProbe.Start).Sum(i=>i.Replacement.Length-i.Length);
            var parenthesisAfterProbe=parenthesisProbe with {Start=parenthesisProbe.Start+parenthesisShift};
            test.FixMany(parenthesisFresh);
            if(test.ReadCheckText()!=parenthesisExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(parenthesisAfterProbe)!=parenthesisDecoration)
                throw new InvalidOperationException("Nhóm ngoặc chưa đúng chữ/kiểu/ô trống hoặc còn mục đã sửa");
            string parenthesisSaved=Path.Combine(folder,"parenthesis-fixed.docx");test.SaveCopy(parenthesisSaved);test.Undo();
            if(test.ReadCheckText()!=parenthesisOriginal || test.ReadCheckDecorations(parenthesisProbe)!=parenthesisDecoration ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.ParenthesisSpace)!=2)
                throw new InvalidOperationException("Một Undo chưa phục hồi cả nhóm ngoặc/chữ/kiểu");
            test.Open(parenthesisSaved);
            if(test.ReadCheckText()!=parenthesisExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(parenthesisAfterProbe)!=parenthesisDecoration || !SaveDigest(parenthesisSource).SequenceEqual(parenthesisDigest))
                throw new InvalidOperationException("Bản lưu ngoặc chưa đúng chữ/kiểu hoặc nguồn mẫu thay đổi");
            Pass(stage);
            stage="Dấu kết câu cuối đoạn: sửa riêng, giữ kiểu và các mục không chọn";
            string sentenceEndSource=CreateSentenceEndSample(positionSource,folder);
            byte[] sentenceEndDigest=SaveDigest(sentenceEndSource);test.Open(sentenceEndSource);
            string sentenceEndOriginal=test.ReadCheckText();
            var sentenceEndEdits=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            var sentenceEndOnly=sentenceEndEdits.Where(i=>i.Kind==FixKind.SentenceEndSpace).OrderBy(i=>i.Start).ToArray();
            if(sentenceEndOnly.Length!=3 || sentenceEndEdits.Length!=8)
                throw new InvalidOperationException("Chưa phân biệt đúng ba dấu kết câu và năm lỗi chữ/ngoặc độc lập hoặc đã sửa mẫu ngoại lệ");
            var sentenceOpen=sentenceEndEdits.Where(i=>i.Kind==FixKind.ParenthesisSpace).OrderBy(i=>i.Start).First();
            var sentenceEndProbe=new Issue(sentenceOpen.Start+sentenceOpen.Length,"kế hoạch".Length,"kế hoạch","kế hoạch","",false);
            var sentenceEndDecoration=test.ReadCheckDecorations(sentenceEndProbe);
            if(sentenceEndDecoration.Bold!=-1 || sentenceEndDecoration.Underline!=1)
                throw new InvalidOperationException("Mẫu chữ giữ lại chưa có kiểu riêng");
            test.Fix(sentenceEndOnly[0]);
            if(test.ReadCheckText()!=sentenceEndOriginal.Replace("hoàn thành  .","hoàn thành.",StringComparison.Ordinal) ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.SentenceEndSpace)!=2 || test.ReadCheckDecorations(sentenceEndProbe)!=sentenceEndDecoration)
                throw new InvalidOperationException("Sửa riêng dấu kết câu chưa đúng chữ/kiểu hoặc làm mất mục không chọn");
            test.Undo();if(test.ReadCheckText()!=sentenceEndOriginal)throw new InvalidOperationException("Undo dấu kết câu chưa về nguyên bản");
            Pass(stage);
            test.Open(sentenceEndSource);
            stage="Lỗi sau xóa khoảng trước dấu kết câu: phục hồi cả nhóm, không tạo Undo";
            bool sentenceEndRollback=false;
            try{test.FixTextFailureProbe(sentenceEndEdits,1);}
            catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){sentenceEndRollback=true;}
            if(!sentenceEndRollback || test.ReadCheckText()!=sentenceEndOriginal || test.Changed ||
                test.ReadCheckDecorations(sentenceEndProbe)!=sentenceEndDecoration || test.Check().Count(i=>i.CanFix && i.Kind==FixKind.SentenceEndSpace)!=3)
                throw new InvalidOperationException("Rollback nhóm dấu kết câu chưa giữ nguyên chữ/kiểu");
            test.Undo();if(test.ReadCheckText()!=sentenceEndOriginal)throw new InvalidOperationException("Nhóm dấu kết câu lỗi đã tạo Undo");
            Pass(stage);
            stage="Sửa nhóm dấu kết câu/chữ: quét, một Undo, lưu/mở và giữ nguồn";
            string sentenceEndExpected=sentenceEndOriginal.Replace("Nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .","Nội dung (kế hoạch) đã duyệt; hoàn thành.",StringComparison.Ordinal)
                .Replace("Nội dung đã duyệt ?","Nội dung đã duyệt?",StringComparison.Ordinal).Replace("Xin cảm ơn  !","Xin cảm ơn!",StringComparison.Ordinal);
            var sentenceEndFresh=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            int sentenceEndShift=sentenceEndFresh.Where(i=>(long)i.Start+i.Length<=sentenceEndProbe.Start).Sum(i=>i.Replacement.Length-i.Length);
            var sentenceEndAfterProbe=sentenceEndProbe with {Start=sentenceEndProbe.Start+sentenceEndShift};
            test.FixMany(sentenceEndFresh);
            if(test.ReadCheckText()!=sentenceEndExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(sentenceEndAfterProbe)!=sentenceEndDecoration)
                throw new InvalidOperationException("Nhóm dấu kết câu chưa đúng chữ/kiểu hoặc còn mục đã sửa");
            string sentenceEndSaved=Path.Combine(folder,"sentence-end-fixed.docx");test.SaveCopy(sentenceEndSaved);test.Undo();
            if(test.ReadCheckText()!=sentenceEndOriginal || test.ReadCheckDecorations(sentenceEndProbe)!=sentenceEndDecoration ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.SentenceEndSpace)!=3)
                throw new InvalidOperationException("Một Undo chưa phục hồi toàn nhóm dấu kết câu");
            test.Open(sentenceEndSaved);
            if(test.ReadCheckText()!=sentenceEndExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(sentenceEndAfterProbe)!=sentenceEndDecoration || !SaveDigest(sentenceEndSource).SequenceEqual(sentenceEndDigest))
                throw new InvalidOperationException("Bản lưu dấu kết câu chưa đúng chữ/kiểu hoặc nguồn mẫu thay đổi");
            Pass(stage);
            stage="Dấu hai chấm: sửa riêng, giữ kiểu và khoảng trắng chưa chọn";
            string colonSource=CreateColonSample(positionSource,folder);
            byte[] colonDigest=SaveDigest(colonSource);test.Open(colonSource);
            string colonOriginal=test.ReadCheckText();
            var colonEdits=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            var colonOnly=colonEdits.Where(i=>i.Kind==FixKind.ColonSpace).OrderBy(i=>i.Start).ToArray();
            if(colonOnly.Length!=3 || colonEdits.Length!=9)
                throw new InvalidOperationException("Chưa phân biệt đúng ba lỗi khoảng trắng hai chấm và sáu lỗi độc lập hoặc mẫu giờ/tỉ lệ/đường dẫn bị sửa");
            var colonOpen=colonEdits.Where(i=>i.Kind==FixKind.ParenthesisSpace).OrderBy(i=>i.Start).First();
            var colonProbe=new Issue(colonOpen.Start+colonOpen.Length,"kế hoạch".Length,"kế hoạch","kế hoạch","",false);
            var colonDecoration=test.ReadCheckDecorations(colonProbe);
            if(colonDecoration.Bold!=-1 || colonDecoration.Underline!=1)
                throw new InvalidOperationException("Mẫu chữ giữ lại chưa có kiểu riêng");
            test.Fix(colonOnly[0]);
            if(test.ReadCheckText()!=colonOriginal.Replace("Ghi chú  :nội dung","Ghi chú:nội dung",StringComparison.Ordinal) ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.ColonSpace)!=2 ||
                test.ReadCheckDecorations(colonProbe with {Start=colonProbe.Start-colonOnly[0].Length})!=colonDecoration)
                throw new InvalidOperationException("Sửa riêng hai chấm đổi chữ/kiểu hoặc làm mất khoảng trắng chưa chọn");
            test.Undo();if(test.ReadCheckText()!=colonOriginal)throw new InvalidOperationException("Undo hai chấm chưa về nguyên bản");
            var missingColonGap=colonOnly.Single(i=>i.AddsSpaceAfterColon);
            test.Fix(missingColonGap);
            if(test.ReadCheckText()!=colonOriginal.Replace("Ghi chú  :nội dung","Ghi chú  : nội dung",StringComparison.Ordinal) ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.ColonSpace)!=2 ||
                test.ReadCheckDecorations(colonProbe with {Start=colonProbe.Start+1})!=colonDecoration)
                throw new InvalidOperationException("Thêm khoảng sau hai chấm chưa giữ khoảng trước/chữ/kiểu hoặc còn lỗi đã sửa.");
            test.Undo();if(test.ReadCheckText()!=colonOriginal || test.ReadCheckDecorations(colonProbe)!=colonDecoration)
                throw new InvalidOperationException("Undo thêm khoảng sau hai chấm chưa phục hồi nguyên văn/kiểu.");
            Pass(stage);
            test.Open(colonSource);
            stage="Lỗi giữa nhóm hai chấm/chữ: rollback toàn bộ, không tạo Undo";
            bool colonRollback=false;
            try{test.FixTextFailureProbe(colonEdits,4);}
            catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){colonRollback=true;}
            if(!colonRollback || test.ReadCheckText()!=colonOriginal || test.Changed || test.ReadCheckDecorations(colonProbe)!=colonDecoration ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.ColonSpace)!=3)
                throw new InvalidOperationException("Rollback nhóm hai chấm chưa giữ nguyên chữ/kiểu");
            test.Undo();if(test.ReadCheckText()!=colonOriginal)throw new InvalidOperationException("Nhóm hai chấm lỗi đã tạo Undo");
            Pass(stage);
            stage="Sửa nhóm hai chấm/chữ: quét, một Undo, lưu/mở và giữ nguồn";
            string colonExpected=colonOriginal.Replace("Ghi chú  :nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .","Ghi chú: nội dung (kế hoạch) đã duyệt; hoàn thành.",StringComparison.Ordinal)
                .Replace("Kính gửi : cơ quan.","Kính gửi: cơ quan.",StringComparison.Ordinal);
            var colonFresh=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            int colonShift=colonFresh.Where(i=>(long)i.Start+i.Length<=colonProbe.Start).Sum(i=>i.Replacement.Length-i.Length);
            var colonAfterProbe=colonProbe with {Start=colonProbe.Start+colonShift};
            test.FixMany(colonFresh);
            if(test.ReadCheckText()!=colonExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(colonAfterProbe)!=colonDecoration)
                throw new InvalidOperationException("Nhóm hai chấm chưa đúng chữ/kiểu hoặc còn mục đã sửa");
            string colonSaved=Path.Combine(folder,"colon-fixed.docx");test.SaveCopy(colonSaved);test.Undo();
            if(test.ReadCheckText()!=colonOriginal || test.ReadCheckDecorations(colonProbe)!=colonDecoration ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.ColonSpace)!=3)
                throw new InvalidOperationException("Một Undo chưa phục hồi toàn nhóm hai chấm");
            test.Open(colonSaved);
            if(test.ReadCheckText()!=colonExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(colonAfterProbe)!=colonDecoration || !SaveDigest(colonSource).SequenceEqual(colonDigest))
                throw new InvalidOperationException("Bản lưu hai chấm chưa đúng chữ/kiểu hoặc nguồn mẫu thay đổi");
            Pass(stage);
            stage="Dấu kết câu giữa đoạn: sửa riêng, giữ kiểu và khoảng trắng chưa chọn";
            string sentenceInnerSource=CreateSentenceInnerSample(positionSource,folder);
            byte[] sentenceInnerDigest=SaveDigest(sentenceInnerSource);test.Open(sentenceInnerSource);
            string sentenceInnerOriginal=test.ReadCheckText();
            var sentenceInnerEdits=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.SentenceInnerSpace or FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            var sentenceInnerOnly=sentenceInnerEdits.Where(i=>i.Kind==FixKind.SentenceInnerSpace).OrderBy(i=>i.Start).ToArray();
            if(sentenceInnerOnly.Length!=3 || sentenceInnerEdits.Length!=12)
                throw new InvalidOperationException("Chưa phân biệt đúng ba dấu kết câu giữa đoạn và chín lỗi độc lập hoặc mẫu ngoại lệ bị sửa");
            var sentenceInnerOpen=sentenceInnerEdits.Where(i=>i.Kind==FixKind.ParenthesisSpace).OrderBy(i=>i.Start).First();
            var sentenceInnerProbe=new Issue(sentenceInnerOpen.Start+sentenceInnerOpen.Length,"kế hoạch".Length,"kế hoạch","kế hoạch","",false);
            var sentenceInnerDecoration=test.ReadCheckDecorations(sentenceInnerProbe);
            if(sentenceInnerDecoration.Bold!=-1 || sentenceInnerDecoration.Underline!=1)
                throw new InvalidOperationException("Mẫu chữ giữ lại chưa có kiểu riêng");
            test.Fix(sentenceInnerOnly[0]);
            if(test.ReadCheckText()!=sentenceInnerOriginal.Replace("hoàn thành  .  Tiếp","hoàn thành.  Tiếp",StringComparison.Ordinal) ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.SentenceInnerSpace)!=2 ||
                test.ReadCheckDecorations(sentenceInnerProbe)!=sentenceInnerDecoration)
                throw new InvalidOperationException("Sửa riêng dấu kết câu giữa đoạn đổi chữ/kiểu hoặc làm mất khoảng trắng chưa chọn");
            test.Undo();if(test.ReadCheckText()!=sentenceInnerOriginal)throw new InvalidOperationException("Undo dấu kết câu giữa đoạn chưa về nguyên bản");
            Pass(stage);
            test.Open(sentenceInnerSource);
            stage="Lỗi giữa nhóm dấu kết câu giữa đoạn/chữ: rollback toàn bộ, không tạo Undo";
            bool sentenceInnerRollback=false;
            try{test.FixTextFailureProbe(sentenceInnerEdits,4);}
            catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){sentenceInnerRollback=true;}
            if(!sentenceInnerRollback || test.ReadCheckText()!=sentenceInnerOriginal || test.Changed || test.ReadCheckDecorations(sentenceInnerProbe)!=sentenceInnerDecoration ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.SentenceInnerSpace)!=3)
                throw new InvalidOperationException("Rollback nhóm dấu kết câu giữa đoạn chưa giữ nguyên chữ/kiểu");
            test.Undo();if(test.ReadCheckText()!=sentenceInnerOriginal)throw new InvalidOperationException("Nhóm dấu kết câu giữa đoạn lỗi đã tạo Undo");
            Pass(stage);
            stage="Sửa nhóm dấu kết câu giữa đoạn/chữ: quét, một Undo, lưu/mở và giữ nguồn";
            string sentenceInnerExpected=sentenceInnerOriginal.Replace("Ghi chú  :  nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .  Tiếp tục báo cáo  ! Chúng tôi đã xong  .","Ghi chú: nội dung (kế hoạch) đã duyệt; hoàn thành. Tiếp tục báo cáo! Chúng tôi đã xong.",StringComparison.Ordinal)
                .Replace("Đã hoàn thành ? Tiếp tục báo cáo","Đã hoàn thành? Tiếp tục báo cáo",StringComparison.Ordinal);
            var sentenceInnerFresh=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.SentenceInnerSpace or FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            int sentenceInnerShift=sentenceInnerFresh.Where(i=>(long)i.Start+i.Length<=sentenceInnerProbe.Start).Sum(i=>i.Replacement.Length-i.Length);
            var sentenceInnerAfterProbe=sentenceInnerProbe with {Start=sentenceInnerProbe.Start+sentenceInnerShift};
            test.FixMany(sentenceInnerFresh);
            if(test.ReadCheckText()!=sentenceInnerExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.SentenceInnerSpace or FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(sentenceInnerAfterProbe)!=sentenceInnerDecoration)
                throw new InvalidOperationException("Nhóm dấu kết câu giữa đoạn chưa đúng chữ/kiểu hoặc còn mục đã sửa");
            string sentenceInnerSaved=Path.Combine(folder,"sentenceInner-fixed.docx");test.SaveCopy(sentenceInnerSaved);test.Undo();
            if(test.ReadCheckText()!=sentenceInnerOriginal || test.ReadCheckDecorations(sentenceInnerProbe)!=sentenceInnerDecoration ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.SentenceInnerSpace)!=3)
                throw new InvalidOperationException("Một Undo chưa phục hồi toàn nhóm dấu kết câu giữa đoạn");
            test.Open(sentenceInnerSaved);
            if(test.ReadCheckText()!=sentenceInnerExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.SentenceInnerSpace or FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(sentenceInnerAfterProbe)!=sentenceInnerDecoration || !SaveDigest(sentenceInnerSource).SequenceEqual(sentenceInnerDigest))
                throw new InvalidOperationException("Bản lưu dấu kết câu giữa đoạn chưa đúng chữ/kiểu hoặc nguồn mẫu thay đổi");
            Pass(stage);
            stage="Thiếu khoảng sau dấu kết câu: sửa riêng, giữ kiểu và khoảng trắng chưa chọn";
            string sentenceAfterSource=CreateSentenceAfterSample(positionSource,folder);
            byte[] sentenceAfterDigest=SaveDigest(sentenceAfterSource);test.Open(sentenceAfterSource);
            string sentenceAfterOriginal=test.ReadCheckText();
            var sentenceAfterEdits=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.SentenceAfterSpace or FixKind.SentenceInnerSpace or FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            var sentenceAfterOnly=sentenceAfterEdits.Where(i=>i.Kind==FixKind.SentenceAfterSpace).OrderBy(i=>i.Start).ToArray();
            if(sentenceAfterOnly.Length!=3 || sentenceAfterEdits.Length!=12)
                throw new InvalidOperationException("Chưa phân biệt đúng ba thiếu khoảng sau dấu kết câu và chín lỗi độc lập hoặc mẫu ngoại lệ bị sửa");
            var sentenceAfterOpen=sentenceAfterEdits.Where(i=>i.Kind==FixKind.ParenthesisSpace).OrderBy(i=>i.Start).First();
            var sentenceAfterProbe=new Issue(sentenceAfterOpen.Start+sentenceAfterOpen.Length,"kế hoạch".Length,"kế hoạch","kế hoạch","",false);
            var sentenceAfterDecoration=test.ReadCheckDecorations(sentenceAfterProbe);
            if(sentenceAfterDecoration.Bold!=-1 || sentenceAfterDecoration.Underline!=1)
                throw new InvalidOperationException("Mẫu chữ giữ lại chưa có kiểu riêng");
            var sentenceAfterRight=new Issue(sentenceAfterOnly[0].Start+1,"Tiếp".Length,"Tiếp","Tiếp","",false);
            var sentenceAfterGap=sentenceAfterEdits.Single(i=>i.Kind==FixKind.SentenceInnerSpace && i.Start+i.Length==sentenceAfterOnly[0].Start);
            var sentenceAfterLeft=new Issue(sentenceAfterGap.Start-"thành".Length,"thành".Length,"thành","thành","",false);
            var sentenceAfterRightStyle=test.ReadCheckDecorations(sentenceAfterRight);
            var sentenceAfterLeftStyle=test.ReadCheckDecorations(sentenceAfterLeft);
            if(sentenceAfterRightStyle.Bold!=-1 || sentenceAfterRightStyle.Underline!=1)
                throw new InvalidOperationException("Từ ngay sau dấu câu chưa có kiểu riêng trong mẫu");
            test.Fix(sentenceAfterOnly[0]);
            if(test.ReadCheckText()!=sentenceAfterOriginal.Replace("hoàn thành  .Tiếp","hoàn thành  . Tiếp",StringComparison.Ordinal) ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.SentenceAfterSpace)!=2 ||
                test.ReadCheckDecorations(sentenceAfterProbe)!=sentenceAfterDecoration ||
                test.ReadCheckDecorations(sentenceAfterRight with {Start=sentenceAfterRight.Start+1})!=sentenceAfterRightStyle ||
                test.ReadCheckDecorations(sentenceAfterLeft)!=sentenceAfterLeftStyle ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.SentenceInnerSpace)!=1)
                throw new InvalidOperationException("Sửa riêng thiếu khoảng sau dấu kết câu đổi chữ/kiểu hoặc làm mất khoảng trắng chưa chọn");
            test.Undo();if(test.ReadCheckText()!=sentenceAfterOriginal)throw new InvalidOperationException("Undo thiếu khoảng sau dấu kết câu chưa về nguyên bản");
            Pass(stage);
            test.Open(sentenceAfterSource);
            stage="Lỗi giữa nhóm thiếu khoảng sau dấu kết câu/chữ: rollback toàn bộ, không tạo Undo";
            bool sentenceAfterRollback=false;
            try{test.FixTextFailureProbe(sentenceAfterEdits,4);}
            catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){sentenceAfterRollback=true;}
            if(!sentenceAfterRollback || test.ReadCheckText()!=sentenceAfterOriginal || test.Changed || test.ReadCheckDecorations(sentenceAfterProbe)!=sentenceAfterDecoration ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.SentenceAfterSpace)!=3 ||
                test.ReadCheckDecorations(sentenceAfterRight)!=sentenceAfterRightStyle || test.ReadCheckDecorations(sentenceAfterLeft)!=sentenceAfterLeftStyle)
                throw new InvalidOperationException("Rollback nhóm thiếu khoảng sau dấu kết câu chưa giữ nguyên chữ/kiểu");
            test.Undo();if(test.ReadCheckText()!=sentenceAfterOriginal)throw new InvalidOperationException("Nhóm thiếu khoảng sau dấu kết câu lỗi đã tạo Undo");
            Pass(stage);
            stage="Sửa nhóm thiếu khoảng sau dấu kết câu/chữ: quét, một Undo, lưu/mở và giữ nguồn";
            string sentenceAfterExpected=sentenceAfterOriginal.Replace("Ghi chú  :  nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .Tiếp tục báo cáo!Chúng tôi đã xong  .","Ghi chú: nội dung (kế hoạch) đã duyệt; hoàn thành. Tiếp tục báo cáo! Chúng tôi đã xong.",StringComparison.Ordinal)
                .Replace("Đã hoàn thành?Tiếp tục báo cáo","Đã hoàn thành? Tiếp tục báo cáo",StringComparison.Ordinal);
            var sentenceAfterFresh=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.SentenceAfterSpace or FixKind.SentenceInnerSpace or FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            int sentenceAfterShift=sentenceAfterFresh.Where(i=>(long)i.Start+i.Length<=sentenceAfterProbe.Start).Sum(i=>i.Replacement.Length-i.Length);
            var sentenceAfterAfterProbe=sentenceAfterProbe with {Start=sentenceAfterProbe.Start+sentenceAfterShift};
            var sentenceAfterRightFixed=sentenceAfterRight with {Start=sentenceAfterRight.Start+sentenceAfterFresh.Where(i=>(long)i.Start+i.Length<=sentenceAfterRight.Start).Sum(i=>i.Replacement.Length-i.Length)};
            var sentenceAfterLeftFixed=sentenceAfterLeft with {Start=sentenceAfterLeft.Start+sentenceAfterFresh.Where(i=>(long)i.Start+i.Length<=sentenceAfterLeft.Start).Sum(i=>i.Replacement.Length-i.Length)};
            test.FixMany(sentenceAfterFresh);
            if(test.ReadCheckText()!=sentenceAfterExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.SentenceAfterSpace or FixKind.SentenceInnerSpace or FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(sentenceAfterAfterProbe)!=sentenceAfterDecoration ||
                test.ReadCheckDecorations(sentenceAfterRightFixed)!=sentenceAfterRightStyle || test.ReadCheckDecorations(sentenceAfterLeftFixed)!=sentenceAfterLeftStyle)
                throw new InvalidOperationException("Nhóm thiếu khoảng sau dấu kết câu chưa đúng chữ/kiểu hoặc còn mục đã sửa");
            string sentenceAfterSaved=Path.Combine(folder,"sentenceAfter-fixed.docx");test.SaveCopy(sentenceAfterSaved);test.Undo();
            if(test.ReadCheckText()!=sentenceAfterOriginal || test.ReadCheckDecorations(sentenceAfterProbe)!=sentenceAfterDecoration ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.SentenceAfterSpace)!=3 ||
                test.ReadCheckDecorations(sentenceAfterRight)!=sentenceAfterRightStyle || test.ReadCheckDecorations(sentenceAfterLeft)!=sentenceAfterLeftStyle)
                throw new InvalidOperationException("Một Undo chưa phục hồi toàn nhóm thiếu khoảng sau dấu kết câu");
            test.Open(sentenceAfterSaved);
            if(test.ReadCheckText()!=sentenceAfterExpected || test.Check().Any(i=>i.CanFix && i.Kind is FixKind.SentenceAfterSpace or FixKind.SentenceInnerSpace or FixKind.ColonSpace or FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) ||
                test.ReadCheckDecorations(sentenceAfterAfterProbe)!=sentenceAfterDecoration ||
                test.ReadCheckDecorations(sentenceAfterRightFixed)!=sentenceAfterRightStyle || test.ReadCheckDecorations(sentenceAfterLeftFixed)!=sentenceAfterLeftStyle || !SaveDigest(sentenceAfterSource).SequenceEqual(sentenceAfterDigest))
                throw new InvalidOperationException("Bản lưu thiếu khoảng sau dấu kết câu chưa đúng chữ/kiểu hoặc nguồn mẫu thay đổi");
            Pass(stage);
            stage="Cấu trúc mục: ba chế độ, thứ tự theo mục cha và dấu đầu dòng đơn";
            string outlineSource=CreateOutlineSample(positionSource,folder,false);
            string outlineExtra=CreateOutlineSample(positionSource,folder,true);
            byte[] outlineDigest=SaveDigest(outlineSource),outlineExtraDigest=SaveDigest(outlineExtra);
            bool OutlineEdit(Issue issue)=>issue.CanFix && issue.Kind is FixKind.OutlineOrder or FixKind.OutlineMarker or FixKind.Text or FixKind.RepeatedText or FixKind.PunctuationSpace or FixKind.ParenthesisSpace or FixKind.SentenceEndSpace or FixKind.SentenceInnerSpace or FixKind.SentenceAfterSpace or FixKind.ColonSpace;
            foreach(var outlineProfile in Enum.GetValues<DocumentProfile>())
            {
                test.Profile=outlineProfile;test.Open(outlineSource);var outlineFound=test.Check();
                if(outlineFound.Count(i=>i.CanFix && i.Kind==FixKind.OutlineOrder)!=3 || outlineFound.Count(i=>i.CanFix && i.Kind==FixKind.OutlineMarker)!=4)
                    throw new InvalidOperationException("Chưa tách đúng thứ tự/dấu đơn theo mục cha trong "+outlineProfile);
            }
            test.Profile=DocumentProfile.State;test.Open(outlineSource);
            string outlineOriginal=test.ReadCheckText();var outlineAll=test.Check().Where(OutlineEdit).ToArray();
            if(outlineAll.Length!=14)throw new InvalidOperationException("Chưa nhận đúng nhóm cấu trúc/chữ mẫu");
            var outlineOrder=outlineAll.Where(i=>i.Kind==FixKind.OutlineOrder).OrderBy(i=>i.Start).ToArray();
            var outlineMarker=outlineAll.Where(i=>i.Kind==FixKind.OutlineMarker).OrderBy(i=>i.Start).ToArray();
            var outlineOpen=outlineAll.Where(i=>i.Kind==FixKind.ParenthesisSpace).OrderBy(i=>i.Start).First();
            var outlineProbe=new Issue(outlineOpen.Start+outlineOpen.Length,"kế hoạch".Length,"kế hoạch","kế hoạch","",false);
            var outlineRoman=outlineOrder.Single(i=>i.Original=="IV");
            var outlineRomanProbe=new Issue(outlineRoman.Start+outlineRoman.Length+2,"Kiến".Length,"Kiến","Kiến","",false);
            var outlineDecoration=test.ReadCheckDecorations(outlineProbe);var outlineRomanDecoration=test.ReadCheckDecorations(outlineRomanProbe);
            if(outlineDecoration.Bold!=-1 || outlineDecoration.Underline!=1 || outlineRomanDecoration.Bold!=-1)
                throw new InvalidOperationException("Mẫu cấu trúc chưa có kiểu chữ giữ lại");
            stage="Cấu trúc mục: sửa riêng giữ nội dung, dấu khác và kiểu";
            test.Fix(outlineOrder[0]);
            if(test.ReadCheckText()!=outlineOriginal.Replace("4. Đề xuất","3. Đề xuất",StringComparison.Ordinal) ||
                test.Check().Count(i=>i.CanFix && i.Kind==FixKind.OutlineOrder)!=2 || test.ReadCheckDecorations(outlineProbe)!=outlineDecoration)
                throw new InvalidOperationException("Sửa riêng số mục làm thay nội dung hoặc mục chưa chọn");
            test.Undo();if(test.ReadCheckText()!=outlineOriginal)throw new InvalidOperationException("Undo số mục chưa về nguyên bản");
            test.Open(outlineSource);test.Fix(outlineMarker[0]);
            if(test.ReadCheckText()!=outlineOriginal.Replace("+ Nội dung  ( kế hoạch kế hoạch )","Nội dung  ( kế hoạch kế hoạch )",StringComparison.Ordinal) ||
                test.ReadCheckDecorations(outlineProbe with {Start=outlineProbe.Start-outlineMarker[0].Length})!=outlineDecoration)
                throw new InvalidOperationException("Bỏ riêng dấu + đổi nội dung hoặc kiểu giữ lại");
            test.Undo();if(test.ReadCheckText()!=outlineOriginal)throw new InvalidOperationException("Undo dấu đơn chưa về nguyên bản");
            Pass(stage);
            stage="Cấu trúc mục: thêm dấu cùng cấp làm lựa chọn cũ hết hiệu lực trước checkpoint";
            test.Open(outlineExtra);string outlineChanged=test.ReadCheckText();byte[] outlineCheckpoint=SaveDigest(test.RecoveryPath);bool outlineStale=false;
            try{test.FixMany([outlineMarker[0]]);}catch(InvalidOperationException){outlineStale=true;}
            if(!outlineStale || test.Changed || test.ReadCheckText()!=outlineChanged || !SaveDigest(test.RecoveryPath).SequenceEqual(outlineCheckpoint))
                throw new InvalidOperationException("Lựa chọn dấu đơn cũ vượt kiểm ngữ cảnh mới hoặc đã ghi checkpoint");
            test.Undo();if(test.ReadCheckText()!=outlineChanged)throw new InvalidOperationException("Từ chối lựa chọn cũ đã tạo Undo");
            Pass(stage);
            stage="Cấu trúc mục: lỗi giữa nhóm phục hồi toàn bộ chữ/kiểu, không tạo Undo";
            test.Open(outlineSource);bool outlineRollback=false;
            try{test.FixTextFailureProbe(outlineAll,4);}catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){outlineRollback=true;}
            if(!outlineRollback || test.Changed || test.ReadCheckText()!=outlineOriginal || test.ReadCheckDecorations(outlineProbe)!=outlineDecoration ||
                test.ReadCheckDecorations(outlineRomanProbe)!=outlineRomanDecoration)
                throw new InvalidOperationException("Rollback cấu trúc/chữ chưa giữ toàn bộ nguyên bản");
            test.Undo();if(test.ReadCheckText()!=outlineOriginal)throw new InvalidOperationException("Lượt cấu trúc lỗi đã tạo Undo");
            Pass(stage);
            stage="Cấu trúc mục: sửa nhóm một lượt, một Undo, lưu/mở và giữ nguồn";
            string outlineExpected=outlineOriginal.Replace("4. Đề xuất","3. Đề xuất",StringComparison.Ordinal)
                .Replace("IV. Kiến nghị","III. Kiến nghị",StringComparison.Ordinal).Replace("d) Đề xuất","c) Đề xuất",StringComparison.Ordinal)
                .Replace("+ Nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .Tiếp tục báo cáo","Nội dung (kế hoạch) đã duyệt; hoàn thành. Tiếp tục báo cáo",StringComparison.Ordinal)
                .Replace("+ Nội dung riêng","Nội dung riêng",StringComparison.Ordinal).Replace("- Nội dung đơn","Nội dung đơn",StringComparison.Ordinal)
                .Replace("- Nhánh bổ sung","Nhánh bổ sung",StringComparison.Ordinal);
            outlineAll=test.Check().Where(OutlineEdit).ToArray();
            var outlineFixedProbe=outlineProbe with {Start=outlineProbe.Start+outlineAll.Where(i=>(long)i.Start+i.Length<=outlineProbe.Start).Sum(i=>i.Replacement.Length-i.Length)};
            var outlineFixedRomanProbe=outlineRomanProbe with {Start=outlineRomanProbe.Start+outlineAll.Where(i=>(long)i.Start+i.Length<=outlineRomanProbe.Start).Sum(i=>i.Replacement.Length-i.Length)};
            test.FixMany(outlineAll);
            if(test.ReadCheckText()!=outlineExpected || test.Check().Any(OutlineEdit) || test.ReadCheckDecorations(outlineFixedProbe)!=outlineDecoration ||
                test.ReadCheckDecorations(outlineFixedRomanProbe)!=outlineRomanDecoration)
                throw new InvalidOperationException("Sửa nhóm cấu trúc còn mục đã sửa hoặc đổi chữ/kiểu không chọn");
            string outlineSaved=Path.Combine(folder,"outline-fixed.docx");test.SaveCopy(outlineSaved);test.Undo();
            if(test.ReadCheckText()!=outlineOriginal || test.ReadCheckDecorations(outlineProbe)!=outlineDecoration || test.ReadCheckDecorations(outlineRomanProbe)!=outlineRomanDecoration)
                throw new InvalidOperationException("Một Undo chưa phục hồi toàn lượt cấu trúc");
            test.Open(outlineSaved);
            if(test.ReadCheckText()!=outlineExpected || test.Check().Any(OutlineEdit) || test.ReadCheckDecorations(outlineFixedProbe)!=outlineDecoration ||
                test.ReadCheckDecorations(outlineFixedRomanProbe)!=outlineRomanDecoration || !SaveDigest(outlineSource).SequenceEqual(outlineDigest) || !SaveDigest(outlineExtra).SequenceEqual(outlineExtraDigest))
                throw new InvalidOperationException("Bản lưu cấu trúc chưa đúng hoặc nguồn mẫu bị đổi");
            Pass(stage);
            stage="Đối chiếu XML sau đoạn 40: ba chế độ, liên kết thủ công và từ chối kế hoạch cũ";
            string bodyPlain=CreateBodyProofSample(positionSource,folder,"plain");
            string bodyLink=CreateBodyProofSample(positionSource,folder,"link");
            string bodyComplex=CreateBodyProofSample(positionSource,folder,"complex");
            byte[] bodyPlainDigest=SaveDigest(bodyPlain),bodyLinkDigest=SaveDigest(bodyLink),bodyComplexDigest=SaveDigest(bodyComplex);
            const string bodyNeighbor="Nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .";
            foreach(var bodyProfile in Enum.GetValues<DocumentProfile>())
            {
                test.Profile=bodyProfile;test.Open(bodyPlain);
                var staleBody=test.Check().Single(i=>i.CanFix && i.Kind==FixKind.ColonSpace);
                test.Open(bodyLink);string bodyOriginal=test.ReadCheckText();byte[] bodyCheckpoint=SaveDigest(test.RecoveryPath);
                var bodyFound=test.Check();
                if(!bodyFound.Any(i=>!i.CanFix && i.Original=="Ghi chú : nội dung") ||
                    bodyFound.Count(i=>i.CanFix && i.Kind is FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text)!=6)
                    throw new InvalidOperationException("Vùng liên kết sau đoạn 40 chưa giữ thủ công hoặc chữ thuần bên cạnh bị chặn trong "+bodyProfile);
                bool staleBodyBlocked=false;
                try{test.Fix(staleBody);}catch(InvalidOperationException){staleBodyBlocked=true;}
                if(!staleBodyBlocked || test.Changed || test.ReadCheckText()!=bodyOriginal || !SaveDigest(test.RecoveryPath).SequenceEqual(bodyCheckpoint))
                    throw new InvalidOperationException("Kế hoạch cũ đã ghi hoặc tạo checkpoint khi vùng chữ trở thành liên kết trong "+bodyProfile);
                test.Undo();if(test.ReadCheckText()!=bodyOriginal)throw new InvalidOperationException("Kế hoạch cũ bị chặn đã tạo Undo");
                Pass(stage+" — "+bodyProfile);
            }
            test.Profile=DocumentProfile.State;test.Open(bodyLink);
            string bodyBaseline=test.ReadCheckText();
            var bodyEdits=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            var bodyRepeat=bodyEdits.Single(i=>i.Kind==FixKind.RepeatedText);
            var bodyProbe=new Issue(bodyRepeat.Start-"kế hoạch".Length,"kế hoạch".Length,"kế hoạch","kế hoạch","",false);
            var bodyDecoration=test.ReadCheckDecorations(bodyProbe);
            if(bodyDecoration.Bold!=-1 || bodyDecoration.Underline!=1)throw new InvalidOperationException("Mẫu chữ thuần phía sau chưa có kiểu riêng");
            stage="Lỗi giữa nhóm chữ sau đoạn 40: giữ liên kết, rollback và không tạo Undo";
            bool bodyRollback=false;
            try{test.FixTextFailureProbe(bodyEdits,2);}
            catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){bodyRollback=true;}
            if(!bodyRollback || test.Changed || test.ReadCheckText()!=bodyBaseline || test.ReadCheckDecorations(bodyProbe)!=bodyDecoration ||
                !test.Check().Any(i=>!i.CanFix && i.Original=="Ghi chú : nội dung"))
                throw new InvalidOperationException("Rollback sau đoạn 40 chưa giữ đủ chữ/kiểu hoặc vùng liên kết");
            test.Undo();if(test.ReadCheckText()!=bodyBaseline)throw new InvalidOperationException("Nhóm chữ sau đoạn 40 bị lỗi đã tạo Undo");
            Pass(stage);
            stage="Sửa nhóm chữ thuần sau đoạn 40: liên kết nguyên, quét, một Undo, lưu/mở";
            bodyEdits=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
            int bodyShift=bodyEdits.Where(i=>(long)i.Start+i.Length<=bodyProbe.Start).Sum(i=>i.Replacement.Length-i.Length);
            var bodyAfterProbe=bodyProbe with {Start=bodyProbe.Start+bodyShift};
            string bodyExpected=bodyBaseline.Replace(bodyNeighbor,"Nội dung (kế hoạch) đã duyệt; hoàn thành.",StringComparison.Ordinal);
            test.FixMany(bodyEdits);string bodySaved=Path.Combine(folder,"body-proof-fixed.docx");test.SaveCopy(bodySaved);
            if(test.ReadCheckText()!=bodyExpected || test.ReadCheckDecorations(bodyAfterProbe)!=bodyDecoration ||
                test.Check().Any(i=>i.CanFix && i.Kind is FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text))
                throw new InvalidOperationException("Nhóm chữ thuần sau đoạn 40 chưa đúng hoặc còn mục đã sửa");
            VerifyBodyProofLink(bodySaved);test.Undo();
            if(test.ReadCheckText()!=bodyBaseline || test.ReadCheckDecorations(bodyProbe)!=bodyDecoration)
                throw new InvalidOperationException("Một Undo chưa phục hồi toàn nhóm chữ sau đoạn 40");
            test.Open(bodySaved);
            if(test.ReadCheckText()!=bodyExpected || !test.Check().Any(i=>!i.CanFix && i.Original=="Ghi chú : nội dung"))
                throw new InvalidOperationException("Mở lại bản sửa sau đoạn 40 chưa đúng hoặc liên kết không còn giữ thủ công");
            VerifyBodyProofLink(bodySaved);Pass(stage);
            stage="Field phức tạp sau đoạn 40: chặn phần chưa chứng minh ở cả ba chế độ";
            foreach(var bodyProfile in Enum.GetValues<DocumentProfile>())
            {
                test.Profile=bodyProfile;test.Open(bodyComplex);var complexFound=test.Check();
                if(string.IsNullOrWhiteSpace(test.CoverageNotice) ||
                    !complexFound.Any(i=>!i.CanFix && i.Original==bodyNeighbor) ||
                    complexFound.Any(i=>i.CanFix && i.Kind is FixKind.SentenceEndSpace or FixKind.ParenthesisSpace or FixKind.PunctuationSpace or FixKind.RepeatedText or FixKind.Text) || test.Changed)
                    throw new InvalidOperationException("Field phức tạp chưa chặn đủ phần chữ phía sau trong "+bodyProfile);
            }
            if(!SaveDigest(bodyPlain).SequenceEqual(bodyPlainDigest) || !SaveDigest(bodyLink).SequenceEqual(bodyLinkDigest) ||
                !SaveDigest(bodyComplex).SequenceEqual(bodyComplexDigest))throw new InvalidOperationException("Nguồn mẫu đối chiếu toàn thân đã bị thay đổi");
            test.Profile=DocumentProfile.State;Pass(stage);
            stage="Nhiều nhóm: chữ, thứ tự mục, dấu đơn, căn giữa và Line";
            OfficeStagedCheck.Run(test,modern,folder,Pass);
            stage="Tên cơ quan, trích yếu xuống dòng và nơi nhận";
            OfficeComponentCheck.Run(test,modern,folder,Pass);
            stage="Khoảng trắng cuối đoạn sau dấu kết câu";
            OfficeParagraphEndCheck.Run(test,modern,folder,Pass);
            stage="Khoảng trắng sau dấu đóng ngoặc";
            OfficeParenthesisAfterCheck.Run(test,modern,folder,Pass);
            stage="Cụm lặp chỉ khác chữ hoa đầu đoạn";
            OfficeInitialRepeatCheck.Run(test,modern,folder,Pass);
            stage="Dấu phẩy/chấm phẩy sau ngoặc tròn";
            OfficeParentheticalPunctuationCheck.Run(test,modern,folder,Pass);
            stage="Khoảng trắng với câu cuối chỉ một từ";
            OfficeShortFinalSentenceCheck.Run(test,modern,folder,Pass);
            stage="Định dạng hệ thống và cảnh báo giả sau sửa nhóm";
            OfficeFormatSystemCheck.Run(test,modern,folder,Pass);
            File.AppendAllText(report,"RESULT PASS\nKiểm thao tác Word/Line, vị trí nét XPS, sửa riêng/nhóm số-ngày, lưu/mở và Hoàn tác trên mẫu giả; không xác nhận mọi tài liệu.\n");
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
    private static string CreateDocumentHeadingSample(string source,string folder,bool mixed)
    {
        string path=Path.Combine(folder,mixed?"document-heading-mixed.docx":"document-heading-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);
        var entry=zip.GetEntry("word/document.xml") ?? throw new InvalidOperationException("Thiếu XML mẫu giả");
        XDocument xml;using(var stream=entry.Open())xml=XDocument.Load(stream,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var body=xml.Descendants(w+"body").Single();
        var title=body.Elements(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))=="BÁO CÁO");
        var first=title.ElementsAfterSelf(w+"p").First();
        var second=new XElement(first);first.AddAfterSelf(second);
        first.Descendants(w+"t").Single().Value="Về kết quả  thử nghiệm mẫu";
        second.Descendants(w+"t").Single().Value="đợt công tác năm 2026";
        foreach(var item in new[]{(Paragraph:title,Size:32),(Paragraph:first,Size:28),(Paragraph:second,Size:27)})
        {
            var run=item.Paragraph.Element(w+"r")!;
            var properties=run.Element(w+"rPr")!;
            properties.Element(w+"rFonts")!.SetAttributeValue(w+"ascii","Arial");
            properties.Element(w+"rFonts")!.SetAttributeValue(w+"hAnsi","Arial");
            properties.Element(w+"sz")!.SetAttributeValue(w+"val",item.Size);
            foreach(string name in new[]{"i","b","u","color"})properties.Element(w+name)?.Remove();
            properties.Add(new XElement(w+"b",new XAttribute(w+"val",0)),new XElement(w+"i"),
                new XElement(w+"u",new XAttribute(w+"val","single")),new XElement(w+"color",new XAttribute(w+"val","FF0000")));
            var paragraphProperties=item.Paragraph.Element(w+"pPr")!;
            paragraphProperties.Element(w+"rPr")?.Remove();
            paragraphProperties.Add(new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii","Arial"),new XAttribute(w+"hAnsi","Arial")),
                new XElement(w+"sz",new XAttribute(w+"val",16))));
        }
        if(mixed)
        {
            var run=first.Element(w+"r")!;var other=new XElement(run);
            string text=run.Element(w+"t")!.Value;run.Element(w+"t")!.Value=text[..6];other.Element(w+"t")!.Value=text[6..];
            run.Element(w+"t")!.SetAttributeValue(XNamespace.Xml+"space","preserve");
            other.Element(w+"t")!.SetAttributeValue(XNamespace.Xml+"space","preserve");
            other.Element(w+"rPr")!.Element(w+"b")!.SetAttributeValue(w+"val",1);run.AddAfterSelf(other);
        }
        entry.Delete();using(var stream=zip.CreateEntry("word/document.xml").Open())xml.Save(stream);return path;
    }
    private static string CreateBodyProofSample(string source,string folder,string kind)
    {
        string path=Path.Combine(folder,"body-proof-"+kind+".docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main",r="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XElement Run(string text,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),new XElement(w+"sz",new XAttribute(w+"val",26)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),
            new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        var rows=Enumerable.Range(1,45).Select(i=>new XElement(w+"p",Run("Đoạn đối chiếu "+i+"."))).ToList();
        XElement target=kind switch {
            "plain"=>new(w+"p",Run("Ghi chú : nội dung")),
            "link"=>new(w+"p",new XElement(w+"hyperlink",new XAttribute(r+"id","rIdBodyProofCheck"),Run("Ghi chú : nội dung"))),
            "complex"=>new(w+"p",new XElement(w+"r",new XElement(w+"fldChar",new XAttribute(w+"fldCharType","begin"))),
                new XElement(w+"r",new XElement(w+"instrText",new XAttribute(XNamespace.Xml+"space","preserve")," QUOTE \"Vùng kiểm tra field\" ")),
                new XElement(w+"r",new XElement(w+"fldChar",new XAttribute(w+"fldCharType","separate"))),Run("Vùng kiểm tra field"),
                new XElement(w+"r",new XElement(w+"fldChar",new XAttribute(w+"fldCharType","end")))),
            _=>throw new InvalidOperationException("Mẫu đối chiếu không hợp lệ") };
        rows.Add(target);rows.Add(new XElement(w+"p",Run("Nội dung  ( "),Run("kế hoạch",true),Run(" kế hoạch ) đã duyệt;hoàn thành  .")));
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        if(kind=="link")
        {
            XNamespace rel="http://schemas.openxmlformats.org/package/2006/relationships";
            var relEntry=zip.GetEntry("word/_rels/document.xml.rels")!;XDocument relationships;
            using(var input=relEntry.Open())relationships=XDocument.Load(input);
            relationships.Root!.Add(new XElement(rel+"Relationship",new XAttribute("Id","rIdBodyProofCheck"),
                new XAttribute("Type",r.NamespaceName+"/hyperlink"),new XAttribute("Target","https://example.invalid/body-proof"),new XAttribute("TargetMode","External")));
            relEntry.Delete();using(var output=zip.CreateEntry("word/_rels/document.xml.rels").Open())relationships.Save(output);
        }
        return path;
    }
    private static void VerifyBodyProofLink(string path)
    {
        using var zip=ZipFile.OpenRead(path);XDocument document,relationships;
        using(var input=zip.GetEntry("word/document.xml")!.Open())document=XDocument.Load(input);
        using(var input=zip.GetEntry("word/_rels/document.xml.rels")!.Open())relationships=XDocument.Load(input);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main",r="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var link=document.Descendants(w+"hyperlink").SingleOrDefault(e=>string.Concat(e.Descendants(w+"t").Select(t=>t.Value))=="Ghi chú : nội dung");
        string? id=(string?)link?.Attribute(r+"id");
        if(id is null || !relationships.Root!.Elements().Any(e=>(string?)e.Attribute("Id")==id &&
            (string?)e.Attribute("Target")=="https://example.invalid/body-proof" && (string?)e.Attribute("TargetMode")=="External"))
            throw new InvalidOperationException("Bản lưu đã làm mất chữ/quan hệ của liên kết được giữ thủ công");
    }
    private static string CreateOutlineSample(string source,string folder,bool extra)
    {
        string path=Path.Combine(folder,extra?"outline-extra-input.docx":"outline-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),new XElement(w+"sz",new XAttribute(w+"val",26)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),
            text.Split('\t').SelectMany((part,index)=>index==0?new[]{new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),part)}:
                new[]{new XElement(w+"tab"),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),part)}));
        XElement Row(string text)=>new(w+"p",Run(text));
        var rows=new List<XElement>{Row("\tI. Tình hình"),Row("\t1. Nội dung thứ nhất"),Row("\t2. Kết quả công tác"),Row("\t4. Đề xuất"),
            Row("\tII. Kết quả"),Row("1. Nội dung chính"),Row("- Nhánh đầu"),new(w+"p",Run("+ Nội dung  ( "),Run("kế hoạch",true),Run(" kế hoạch ) đã duyệt;hoàn thành  .Tiếp tục báo cáo"))};
        if(extra)rows.Add(Row("+ Nội dung thêm cùng cấp"));
        rows.AddRange(new[]{Row("- Nhánh hai"),Row("+ Nội dung riêng"),Row("2. Mục khác"),Row("- Nội dung đơn"),Row("3. Mục bổ sung"),
            Row("- Nhánh bổ sung"),Row("+ Nội dung giữ một"),Row("+ Nội dung giữ hai"),new XElement(w+"p",Run("\tIV. "),Run("Kiến nghị",true)),
            Row("a) Tình hình"),Row("b) Kết quả công tác"),Row("d) Đề xuất")});
        var body=xml.Descendants(w+"body").Single();foreach(var row in rows)
            if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
    private static string CreateColonSample(string source,string folder)
    {
        string path=Path.Combine(folder,"colon-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),new XElement(w+"sz",new XAttribute(w+"val",26)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),
            new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        var rows=new[]{new XElement(w+"p",Run("Ghi chú  :nội dung  ( "),Run("kế hoạch",true),Run(" kế hoạch ) đã duyệt;hoàn thành  .")),
            new XElement(w+"p",Run("Kính gửi : cơ quan.")),new XElement(w+"p",Run("Giờ mở cửa 12 : 30.")),
            new XElement(w+"p",Run("Tỉ lệ 1 : 2.")),new XElement(w+"p",Run("Truy cập http : host.")),
            new XElement(w+"p",Run("Số:    /BC-ABC")),new XElement(w+"p",Run("Địa chỉ https://host/a và name@example.com."))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
    private static string CreateSentenceInnerSample(string source,string folder)
    {
        string path=Path.Combine(folder,"sentence-inner-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),new XElement(w+"sz",new XAttribute(w+"val",26)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),
            new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        var rows=new[]{new XElement(w+"p",Run("Ghi chú  :  nội dung  ( "),Run("kế hoạch",true),Run(" kế hoạch ) đã duyệt;hoàn thành  .  Tiếp tục báo cáo  ! Chúng tôi đã xong  .")),
            new XElement(w+"p",Run("Đã hoàn thành ? Tiếp tục báo cáo")),new XElement(w+"p",Run("Giá trị 1,25 . Tiếp theo")),
            new XElement(w+"p",Run("Tại tp . Hồ Chí Minh")),new XElement(w+"p",Run("Mở file . Docx tiếp theo")),
            new XElement(w+"p",Run("Số:    /BC-ABC")),new XElement(w+"p",Run("Địa chỉ https://host/a và name@example.com."))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
    private static string CreateSentenceAfterSample(string source,string folder)
    {
        string path=Path.Combine(folder,"sentence-after-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),new XElement(w+"sz",new XAttribute(w+"val",26)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),
            new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        var rows=new[]{new XElement(w+"p",Run("Ghi chú  :  nội dung  ( "),Run("kế hoạch",true),Run(" kế hoạch ) đã duyệt;hoàn thành  ."),Run("Tiếp",true),Run(" tục báo cáo!Chúng tôi đã xong  .")),
            new XElement(w+"p",Run("Đã hoàn thành?Tiếp tục báo cáo")),new XElement(w+"p",Run("Giá trị 1,25.Tiếp theo")),
            new XElement(w+"p",Run("Tại tp.Hồ Chí Minh")),new XElement(w+"p",Run("Mở file.Docx tiếp theo")),
            new XElement(w+"p",Run("Số:    /BC-ABC")),new XElement(w+"p",Run("Địa chỉ https://host/a và name@example.com."))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
    private static string CreateSentenceEndSample(string source,string folder)
    {
        string path=Path.Combine(folder,"sentence-end-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),new XElement(w+"sz",new XAttribute(w+"val",26)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),
            new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        var rows=new[]{new XElement(w+"p",Run("Nội dung  ( "),Run("kế hoạch",true),Run(" kế hoạch ) đã duyệt;hoàn thành  .")),
            new XElement(w+"p",Run("Nội dung đã duyệt ?")),new XElement(w+"p",Run("Xin cảm ơn  !")),
            new XElement(w+"p",Run("Giá trị 1,25 .")),new XElement(w+"p",Run("Nội dung TS .")),
            new XElement(w+"p",Run("www . com .")),new XElement(w+"p",Run("Nội dung ..."))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
    private static string CreateParenthesisSample(string source,string folder)
    {
        string path=Path.Combine(folder,"parenthesis-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),new XElement(w+"sz",new XAttribute(w+"val",26)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),
            new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        var rows=new[]{new XElement(w+"p",Run("Nội dung  (  "),Run("kế hoạch",true),Run(" kế hoạch  ) đã duyệt;hoàn thành.")),
            new XElement(w+"p",Run("Biểu mẫu (    ); số ( 12 ); mã ( ABC ); công thức ( x + y )."))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
    private static string CreatePunctuationSample(string source,string folder)
    {
        string path=Path.Combine(folder,"punctuation-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),new XElement(w+"sz",new XAttribute(w+"val",26)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),
            new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        var rows=new[]{new XElement(w+"p",Run("Nội dung",true),Run("  ,  báo cáo;công tác công tác đã  xong.")),
            new XElement(w+"p",Run("Giá trị 1,25; mã BC,AB; https://host/foo,bar; name,part@example.com."))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
    private static string CreateRepeatedTextSample(string source,string folder)
    {
        string path=Path.Combine(folder,"repeated-text-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,bool kept=false)=>new(w+"r",new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),new XElement(w+"sz",new XAttribute(w+"val",26)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        var rows=new[]{new XElement(w+"p",Run("Nội dung  "),Run("kế hoạch",true),Run(" kế hoạch đã được duyệt.")),
            new XElement(w+"p",Run("Báo cáo công tác công tác công tác đã hoàn thành.")),
            new XElement(w+"p",Run("Nhịp sống đều đều, nhà nhà vui vẻ."))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
    private static string CreateLegacyHeadingSample(string source,string folder,DocumentProfile profile)
    {
        string targetFolder=Path.Combine(folder,"heading-proof-"+profile);Directory.CreateDirectory(targetFolder);
        string path=Path.Combine(targetFolder,"heading-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var headings=xml.Descendants(w+"p").Where(p=>HeadingRules.IsCandidate(string.Concat(p.Descendants(w+"t").Select(t=>t.Value)),profile)).ToArray();
        if(headings.Length!=(profile==DocumentProfile.State?2:1))throw new InvalidOperationException("Không nhận đủ tiêu đề trong mẫu giả");
        foreach(var p in headings)
        {
            string original=string.Concat(p.Descendants(w+"t").Select(t=>t.Value));
            string lower=original.ToLowerInvariant();int firstSpace=lower.IndexOf(' ');
            string changed=lower.Insert(firstSpace," ");
            var properties=p.Element(w+"pPr");if(properties is null){properties=new(w+"pPr");p.AddFirst(properties);}
            // The paragraph terminator intentionally differs from the visible heading.
            properties.Element(w+"rPr")?.Remove();
            properties.Element(w+"jc")?.Remove();properties.Add(new XElement(w+"jc",new XAttribute(w+"val","left")));
            properties.Add(new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii","Arial"),new XAttribute(w+"hAnsi","Arial")),
                new XElement(w+"sz",new XAttribute(w+"val",16))));
            foreach(var node in p.Elements().Where(e=>e.Name!=w+"pPr").ToArray())node.Remove();
            p.Add(new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii","Arial"),new XAttribute(w+"hAnsi","Arial")),
                new XElement(w+"b",new XAttribute(w+"val",0)),new XElement(w+"i"),new XElement(w+"color",new XAttribute(w+"val","FF0000")),
                new XElement(w+"sz",new XAttribute(w+"val",32))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),changed)));
        }
        // Remove the unrelated old spelling demonstration from this new Party fixture.
        foreach(var t in xml.Descendants(w+"t").Where(t=>t.Value=="Tôi đi trên một con đo. Đây  là ví dụ."))
            t.Value="Đây là nội dung giả được giữ nguyên.";
        var body=xml.Descendants(w+"body").Single();
        var extra=new XElement(w+"p",new XElement(w+"r",new XElement(w+"t","nội dung  thử nghiệm")));
        if(body.Element(w+"sectPr") is XElement section) section.AddBeforeSelf(extra);else body.Add(extra);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
    private static string CreateStructuredTextSample(string source,string folder,string kind)
    {
        string sampleFolder=Path.Combine(folder,"structured-"+kind);Directory.CreateDirectory(sampleFolder);
        string path=CreateDocumentHeadingSample(source,sampleFolder,false);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);
        var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var first=xml.Descendants(w+"t").Single(t=>t.Value=="Về kết quả  thử nghiệm mẫu");
        first.Value="Về con đo và kết quả  thử nghiệm mẫu";
        xml.Descendants(w+"t").Single(t=>t.Value=="đợt công tác năm 2026").Value="đợt  công tác năm 2026";
        if(kind=="hyperlink")
        {
            var title=xml.Descendants(w+"t").Single(t=>t.Value=="BÁO CÁO").Parent!;
            title.AddBeforeSelf(new XElement(w+"bookmarkStart",new XAttribute(w+"id",120037),new XAttribute(w+"name","ctv_fixture")));
            title.AddAfterSelf(new XElement(w+"bookmarkEnd",new XAttribute(w+"id",120037)));
        }
        var run=first.Parent!;run.ReplaceWith(kind=="hyperlink"
            ?new XElement(w+"hyperlink",new XAttribute(w+"anchor","ctv_fixture"),new XElement(run))
            :new XElement(w+"sdt",new XElement(w+"sdtPr",new XElement(w+"tag",new XAttribute(w+"val","ctv_fixture")),
                new XElement(w+"id",new XAttribute(w+"val",120037))),new XElement(w+"sdtContent",new XElement(run))));
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
    private static string RemoveTextStructure(string source)
    {
        string path=Path.Combine(Path.GetDirectoryName(source)!,"plain-same-text.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        foreach(var node in xml.Descendants(w+"hyperlink").ToArray())node.ReplaceWith(node.Nodes().ToArray());
        foreach(var node in xml.Descendants(w+"sdt").ToArray())node.ReplaceWith(node.Element(w+"sdtContent")!.Nodes().ToArray());
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
    private static void VerifyTextStructure(string source,string saved,string kind)
    {
        XElement Read(string path)
        {
            using var zip=ZipFile.OpenRead(path);using var input=zip.GetEntry("word/document.xml")!.Open();
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            return XDocument.Load(input).Descendants(w+kind).Single();
        }
        XNamespace word="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var before=Read(source);var after=Read(saved);
        if(string.Concat(before.Descendants(word+"t").Select(t=>t.Value))!=string.Concat(after.Descendants(word+"t").Select(t=>t.Value)) ||
            (kind=="hyperlink"?(string?)before.Attribute(word+"anchor")!=(string?)after.Attribute(word+"anchor"):
                (string?)before.Descendants(word+"tag").Single().Attribute(word+"val")!=(string?)after.Descendants(word+"tag").Single().Attribute(word+"val")))
            throw new InvalidOperationException("Bản lưu chưa giữ nguyên chữ/định danh cấu trúc mẫu");
    }
    private static byte[] SaveDigest(string path)
    {
        using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        return System.Security.Cryptography.SHA256.HashData(input);
    }
    private static Issue[] InspectPositions(WordSession test,string folder,string artifact)
    {
        var probes=test.AdministrativePositionTargets.SelectMany(t=>t.References.Select(r=>r.Probe).Append(t.Probe)).Distinct().ToArray();
        var anchors=new Dictionary<Issue,PreviewAnchor>();string path=test.ExportPreview(probes,anchors);
        File.Copy(path,Path.Combine(folder,artifact),true);
        using var input=new XpsDocument(path,FileAccess.Read);
        var issues=AdministrativePositionInspection.Inspect(input.GetFixedDocumentSequence(),new PreviewHighlighter(),test.AdministrativePositionTargets,anchors)
            .Select(x=>x.Issue).ToArray();
        File.AppendAllText(Path.Combine(folder,"office-check.txt"),$"PositionTargets={test.AdministrativePositionTargets.Count}; PositionWarnings={issues.Length}; Artifact={artifact}\n");
        foreach(var issue in issues)File.AppendAllText(Path.Combine(folder,"office-check.txt"),issue.Title+": "+issue.Message+"\n");
        return issues;
    }
    private static string CreatePositionSample(string source,string folder)
    {
        string path=Path.Combine(folder,"position-shifted-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);
        var entry=zip.GetEntry("word/document.xml") ?? throw new InvalidOperationException("Thiếu XML mẫu giả");
        XDocument xml;using(var stream=entry.Open())xml=XDocument.Load(stream,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        foreach(string text in new[]{"Số:        /BC-ABC","Địa danh, ngày        tháng 10 năm 2026"})
        {
            var p=xml.Descendants(w+"t").Single(t=>t.Value==text).Ancestors(w+"p").First();
            var properties=p.Element(w+"pPr")!;
            var indent=properties.Element(w+"ind");
            if(indent is null){indent=new(w+"ind");properties.Add(indent);}
            indent.SetAttributeValue(w+"left",720);indent.SetAttributeValue(w+"right",0);
            if(text.StartsWith("Địa",StringComparison.Ordinal))
            {
                var spacing=properties.Element(w+"spacing");
                if(spacing is null){spacing=new(w+"spacing");properties.Add(spacing);}
                spacing.SetAttributeValue(w+"before",240);
            }
        }
        entry.Delete();using(var stream=zip.CreateEntry("word/document.xml").Open())xml.Save(stream);
        return path;
    }
    private static string CreateTypographySample(string source,string folder)
    {
        string path=Path.Combine(folder,"typography-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);
        var entry=zip.GetEntry("word/document.xml") ?? throw new InvalidOperationException("Thiếu XML mẫu giả");
        XDocument xml;using(var stream=entry.Open())xml=XDocument.Load(stream,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        foreach(string text in new[]{"số  :  3 / BC – ABC","Địa danh  Ngày3  Tháng1  Năm2026"})
        {
            var run=xml.Descendants(w+"t").Single(t=>t.Value==text).Parent!;
            var properties=run.Element(w+"rPr")!;
            properties.Element(w+"rFonts")!.SetAttributeValue(w+"ascii","Arial");
            properties.Element(w+"rFonts")!.SetAttributeValue(w+"hAnsi","Arial");
            properties.Element(w+"sz")!.SetAttributeValue(w+"val",28);
            foreach(string name in new[]{"i","b","u","color"})properties.Element(w+name)?.Remove();
            properties.Add(new XElement(w+"i",new XAttribute(w+"val",text.StartsWith("số",StringComparison.Ordinal)?1:0)),
                new XElement(w+"b"),new XElement(w+"u",new XAttribute(w+"val","single")),
                new XElement(w+"color",new XAttribute(w+"val","FF0000")));
        }
        entry.Delete();using(var stream=zip.CreateEntry("word/document.xml").Open())xml.Save(stream);
        return path;
    }
    private static void VerifyCurrentLines(WordSession test,IReadOnlyList<Issue> expected)
    {
        // Synthetic expected geometry stays unchanged; only use fresh CPs for its text anchors.
        var current=expected.Select(i=>i with {Start=test.LineTargets.Single(t=>
            t.Role==i.LineFix!.Role && t.Text==i.Original).Start}).ToArray();
        test.VerifyLineResult(current,verifyReference:true);
    }
    private static string CreateAnnotatedHeaderSample(string source,string folder)
    {
        // Only the bundled fictional blank-field fixture, copied into the isolated check folder.
        string path=Path.Combine(folder,"annotated-header-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);
        var entry=zip.GetEntry("word/document.xml") ?? throw new InvalidOperationException("Thiếu XML của mẫu giả");
        XDocument xml;
        using(var stream=entry.Open())xml=XDocument.Load(stream,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var number=xml.Descendants(w+"t").Single(t=>t.Value=="Số:        /BC-ABC");
        number.Value="Số: 3/BC-ABC";
        var paragraph=number.Ancestors(w+"p").First();
        var properties=paragraph.Element(w+"pPr");
        if(properties is null){properties=new(w+"pPr");paragraph.AddFirst(properties);}
        properties.Element(w+"tabs")?.Remove();
        properties.AddFirst(new XElement(w+"tabs",new XElement(w+"tab",new XAttribute(w+"val","center"),new XAttribute(w+"pos","1000"))));
        xml.Descendants(w+"t").Single(t=>t.Value=="Địa danh, ngày        tháng 10 năm 2026").Value=
            "Địa danh, ngày        tháng 10 năm 2026 (dự thảo)  ghi chú";
        entry.Delete();using(var stream=zip.CreateEntry("word/document.xml").Open())xml.Save(stream);
        return path;
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

