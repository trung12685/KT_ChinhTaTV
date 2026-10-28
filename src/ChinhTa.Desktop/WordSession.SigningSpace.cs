using System.IO;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    private SignatureWritingSpaceTarget ReadWritingTarget()=>SignatureWritingSpaceTarget.Read(ReadPageBodyXml(),lastWordParagraphs,Profile);
    private CharacterStyleEvidence ReadWritingStyles(WordParagraphSnapshot row)
    {
        string text=SignatureWritingSpaceTarget.Text(row);dynamic range=document!.Range(row.Start,row.Start+text.Length);
        try{return VisibleFormatReader.ReadStyles((object)range,text,includeSpacing:true);}finally{Release(range);}
    }
    private SignatureSpaceMeasurement MeasureWritingSpace(SignatureWritingSpaceTarget target)
    {
        document!.Repaginate();var anchors=new Dictionary<Issue,PreviewAnchor>();
        string path=ExportPreviewCore([SignatureWritingSpaceTarget.Probe(target.Authorities[^1]),SignatureWritingSpaceTarget.Probe(target.Name)],anchors);
        try
        {
            using var input=new XpsDocument(path,FileAccess.Read);
            return SignatureSpaceMeasurementReader.Read(input.GetFixedDocumentSequence(),new PreviewHighlighter(),target,anchors);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    public SignatureSpaceResult ReserveHandSigningSpace()=>ReserveHandSigningSpaceCore(false);
    internal SignatureSpaceMeasurement ReadSigningSpaceProbe()
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng.");
        CheckCore();return MeasureWritingSpace(ReadWritingTarget());
    }
    internal SignatureSpaceResult SigningSpaceFailureProbe()
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng.");
        return ReserveHandSigningSpaceCore(true);
    }
    private SignatureSpaceResult ReserveHandSigningSpaceCore(bool failFinal)
    {
        if(!IsOpen)throw new InvalidOperationException("Mở tài liệu Word trước khi chừa chỗ ký tay.");
        CheckCore();var target=ReadWritingTarget();var initial=MeasureWritingSpace(target);
        if(!initial.Verified || initial.AuthorityPage!=initial.NamePage)throw new InvalidOperationException("Chưa đo được chức vụ/họ tên trên cùng một trang.");
        if(initial.Gap>=SignatureWritingSpace.Target-SignatureWritingSpace.Tolerance)return new(false,initial.Gap!.Value,0,0);
        string story=ReadStoryText();Checkpoint();CheckCore();target=ReadWritingTarget();initial=MeasureWritingSpace(target);
        if(ReadStoryText()!=story)throw new InvalidOperationException("Nội dung thay đổi khi chuẩn bị vùng ký; hãy quét lại.");
        var originalTarget=target;string xmlBefore=ReadPageBodyXml();
        var nameStyles=ReadWritingStyles(target.Name);var authorityStyles=target.Authorities.Select(ReadWritingStyles).ToArray();
        if(!nameStyles.Complete || nameStyles.Paragraph?.Spacing is not {Known:true} || authorityStyles.Any(s=>!s.Complete))
            throw new InvalidOperationException("Chưa đọc đủ định dạng khối ký để giữ nguyên chữ/phông.");
        var originalStyle=nameStyles.Paragraph!;var expectedName=nameStyles;
        string before=Path.Combine(folder,Guid.NewGuid()+".signing-space.undo.docx");File.Copy(RecoveryPath,before);
        bool previousChanged=Changed,restored=false,started=false;var previousProgress=repairProgress;
        repairProgress=RepairProgressTracker.ForSelection(repairProgress,1,ProgressReporter);
        bool SameTarget(SignatureWritingSpaceTarget value)=>SignatureWritingSpaceTarget.Text(value.Name)==nameStyles.Text &&
            value.Authorities.Select(SignatureWritingSpaceTarget.Text).SequenceEqual(authorityStyles.Select(s=>s.Text));
        try
        {
            var result=SignatureWritingSpace.Apply(originalStyle.Spacing!.Before,initial,value=>
            {
                started=true;
                Progress(RepairActivity.Measuring,"Đo và chừa khoảng trống ký tay khoảng 80 pt.");
                if(ReadStoryText()!=story || !expectedName.Matches(ReadWritingStyles(target.Name)))throw new InvalidOperationException("Mốc chữ/định dạng đã đổi trước khi chừa chỗ ký.");
                string text=SignatureWritingSpaceTarget.Text(target.Name);dynamic range=document!.Range(target.Name.Start,target.Name.Start+text.Length);
                snapshotCurrent=false;
                try
                {
                    var style=SignatureSpaceWriter.SetBefore((object)range,text,expectedName.Paragraph!,value,Release);
                    expectedName=new(nameStyles.Text,nameStyles.Styles,style);
                }
                finally{Release(range);}
                if(!expectedName.Matches(ReadWritingStyles(target.Name)))throw new InvalidOperationException("Phông hoặc thuộc tính khác bị thay đổi khi chừa chỗ ký.");
                return MeasureWritingSpace(target);
            },_=>
            {
                Progress(RepairActivity.Verifying,"Kiểm chỗ ký, giữ chữ/phông/bảng/phần khác rồi lưu và mở lại.");
                SignatureSpaceXmlProof.Verify(xmlBefore,ReadPageBodyXml(),originalTarget.NameXmlIndex);
                Checkpoint();CloseDocumentOnly();Connect();CheckCore();target=ReadWritingTarget();
                var measured=MeasureWritingSpace(target);
                if(!SameTarget(target) || ReadStoryText()!=story || !measured.Verified || measured.AuthorityPage!=initial.AuthorityPage ||
                    measured.NamePage!=initial.NamePage || measured.PageCount!=initial.PageCount || measured.Gap is not double gap ||
                    Math.Abs(gap-SignatureWritingSpace.Target)>SignatureWritingSpace.Tolerance || !expectedName.Matches(ReadWritingStyles(target.Name)) ||
                    target.Authorities.Select(ReadWritingStyles).Where((s,i)=>!authorityStyles[i].Matches(s)).Any())
                    throw new InvalidOperationException("Khoảng ký hoặc phần cần giữ chưa đạt sau lưu/mở; phải phục hồi.");
                SignatureSpaceXmlProof.Verify(xmlBefore,ReadPageBodyXml(),target.NameXmlIndex);
                if(failFinal)throw new OfficeSigningSpaceProbeException();
            },()=>
            {
                undoCheckpoints.Push(before);Changed=true;snapshotCurrent=true;repairProgress?.Confirm(1);
            },()=>
            {
                Progress(RepairActivity.Restoring,"Đang phục hồi bản trước khi chừa chỗ ký.");
                SwapCheckpoint(RecoveryPath,before,CloseDocumentOnly,Connect);CheckCore();target=ReadWritingTarget();
                if(!SameTarget(target) || ReadStoryText()!=story || !nameStyles.Matches(ReadWritingStyles(target.Name)))throw new InvalidOperationException("Chưa xác minh được vùng ký sau phục hồi.");
                SignatureSpaceXmlProof.Verify(xmlBefore,ReadPageBodyXml(),target.NameXmlIndex);restored=true;
            });
            return result;
        }
        catch(Exception failure)
        {
            Changed=restored || !started?previousChanged:true;snapshotCurrent=restored || !started;
            Diagnostics.Write(restored?"Word.SigningSpace.RolledBack":"Word.SigningSpace.Failed",failure);
            throw new InvalidOperationException(restored?"Chưa chừa được khoảng 80 pt; đã phục hồi bản trước. "+failure.Message:
                "Chưa hoàn tất chừa chỗ ký; giữ bản trước để phục hồi: "+before,failure);
        }
        finally{repairProgress=previousProgress;}
    }
}
internal sealed class OfficeSigningSpaceProbeException():InvalidOperationException("OFFICE-CHECK-INJECTED-SIGNING-SPACE-FAILURE");
