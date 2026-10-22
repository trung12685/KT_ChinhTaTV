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

