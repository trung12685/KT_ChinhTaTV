using System.IO;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
public sealed partial class WordSession
{
    public ManualTextTarget PrepareManualText(Issue issue)=>ReadWithRecovery("Word.Manual.Prepare",()=>
    {
        CheckCore();return ManualTextTarget.Prepare(ReadPageBodyXml(),lastWordParagraphs,issue.Start,issue.Length,issue.Original);
    });
    public InlineWordDraft PrepareInlineWord(PreviewAnchor hit,string previewPath)=>ReadWithRecovery("Word.Inline.Prepare",()=>
    {
        if(spacingReferencePath!=previewPath || spacingReferenceFlat is null)throw new InvalidOperationException("Bản xem trước không còn gắn với tài liệu; hãy Quét lại.");
        string reference=spacingReferenceFlat;CheckCore();
        var binding=InlineWordLocator.Locate(hit,lastWordParagraphs);
        if(binding is null)throw new InlineEditUnavailableException("Chưa ghép chắc chữ vừa bấm với một vị trí duy nhất trong Word. Không chọn đoán chữ trùng; hãy dùng ô sửa đoạn hoặc sửa trong Word.");
        ManualTextTarget target;
        try{target=ManualTextTarget.Prepare(ReadPageBodyXml(),lastWordParagraphs,binding.Paragraph.Start+binding.Offset,binding.Word.Length,binding.Word);}
        catch(InvalidOperationException error){throw new InlineEditUnavailableException(error.Message,error);}
        target.RequireCurrent(reference);
        return new InlineWordDraft(target,binding.Offset,binding.Word);
    });
    internal void ApplyInlineFailureProbe(InlineWordDraft draft,string replacement)
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong bài Word riêng.");
        ApplyManualTextCore(draft.Target,"",true,draft.Change(replacement));
    }
    public void ApplyInlineWord(InlineWordDraft draft,string replacement)=>ApplyManualTextCore(draft.Target,"",false,draft.Change(replacement));
    public void ApplyManualText(ManualTextTarget target,string edited)=>ApplyManualTextCore(target,edited,false);
    internal void ApplyManualFailureProbe(ManualTextTarget target,string edited)
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong bài Word riêng.");
        ApplyManualTextCore(target,edited,true);
    }
    private void ApplyManualTextCore(ManualTextTarget target,string edited,bool failAfterSave,ManualTextChange? exactChange=null)
    {
        target.RequireCurrent(ReadPageBodyXml());var change=exactChange??target.Edit(edited);
        string story=ReadStoryText();dynamic content=document!.Content;int start;
        try{start=(int)content.Start;}finally{Release(content);}
        dynamic prefixRange=document!.Range(start,target.Start);string prefix;
        try{prefix=(string)prefixRange.Text;}finally{Release(prefixRange);}
        string expected=change.ExpectedStory(story,prefix);
        Checkpoint();
        try{target.VerifyUnchangedAfterCheckpoint(ReadPageBodyXml());}
        catch{snapshotCurrent=false;Changed=true;throw;}
        string before=Path.Combine(folder,Guid.NewGuid()+".manual.undo.docx"),candidate=Path.Combine(folder,Guid.NewGuid()+".manual.candidate.docx");
        File.Copy(RecoveryPath,before);bool previousChanged=Changed,started=false,restored=false,committed=false;
        try
        {
            change.WriteNew(candidate);
            ManualEditTransaction.Apply(()=>
            {
                snapshotCurrent=false;Changed=true;started=true;
                SwapCheckpoint(RecoveryPath,candidate,CloseDocumentOnly,Connect);
            },()=>
            {
                if(ReadStoryText()!=expected)throw new InvalidOperationException("Nội dung sau sửa thủ công chưa đúng đoạn đã duyệt.");
                change.VerifyLive(ReadPageBodyXml());Checkpoint();
                if(failAfterSave)throw new OfficeTextProbeException();
                if(ReadStoryText()!=expected)throw new InvalidOperationException("Nội dung sau lưu chưa đúng lượt sửa thủ công.");
                change.VerifyLive(ReadPageBodyXml());
            },()=>
            {
                undoCheckpoints.Push(before);Changed=true;snapshotCurrent=true;verifiedLineEdits.Clear();committed=true;
            },()=>
            {
                if(!started){restored=true;return;}
                snapshotCurrent=false;SwapCheckpoint(RecoveryPath,before,CloseDocumentOnly,Connect);
                if(ReadStoryText()!=story)throw new InvalidOperationException("Chưa khôi phục được chữ trước sửa thủ công.");
                target.VerifyUnchangedAfterCheckpoint(ReadPageBodyXml());restored=true;
            });
        }
        catch(Exception error)
        {
            Changed=restored || !started?previousChanged:true;snapshotCurrent=restored || !started;
            Diagnostics.Write("Word.ManualEdit",error,$"Restored={(restored?1:0)}");
            if(!snapshotCurrent)error.Data["ChinhTa.RecoveryInstruction"]="Giữ checkpoint trước sửa thủ công: "+before;
            throw;
        }
        finally
        {
            try{if(File.Exists(candidate))File.Delete(candidate);}catch(Exception cleanup){Diagnostics.Write("Word.ManualEdit.Cleanup",cleanup);}
            if(!committed && (!started || restored))
                try{File.Delete(before);}catch(Exception cleanup){Diagnostics.Write("Word.ManualEdit.Cleanup",cleanup);}
        }
    }
}

public sealed class InlineEditUnavailableException(string message,Exception? inner=null):InvalidOperationException(message,inner);
