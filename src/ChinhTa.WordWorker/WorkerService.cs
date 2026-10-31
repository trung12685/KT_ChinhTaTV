using ChinhTa.Core;
using ChinhTa.WordProtocol;
namespace ChinhTa.WordWorker;
public sealed class WorkerService(VietnameseSpellingModel model,PersonalLexiconStore personal,string mirrorPath)
{
    private static readonly HashSet<FixKind> TextKinds=[FixKind.Text,FixKind.RepeatedText,FixKind.PunctuationSpace,FixKind.ParenthesisSpace,FixKind.QuoteSpace,FixKind.SentenceEndSpace,FixKind.ParagraphEndSpace,FixKind.ColonSpace,FixKind.SentenceInnerSpace,FixKind.SentenceAfterSpace];
    public BridgeResponse Handle(BridgeRequest request)
    {
        var response=new BridgeResponse{Id=request.Id,DocumentId=request.DocumentId,SnapshotHash=request.SnapshotHash};
        try
        {
            if(request.Version!=1 || !Guid.TryParse(request.Id,out _) || request.Operation is not ("scan" or "terms" or "add" or "remove" or "mirror"))throw new InvalidOperationException("Yêu cầu kết nối chưa đúng phiên bản hoặc thao tác.");
            PersonalLexiconData data=request.Operation switch {"add"=>personal.Add(request.Term),"remove"=>personal.Remove(request.Term),_=>personal.Load()};
            response.DictionaryRevision=data.Revision;response.Terms=request.Operation=="scan"?Array.Empty<string>():data.Terms.Select(t=>t.Text).ToArray();response.Source=model.Source.Name;
            if(request.Operation=="scan")
            {
                if(!Guid.TryParse(request.DocumentId,out _) || request.SnapshotHash is null || request.SnapshotHash.Length!=64 || request.Paragraphs is null || request.Paragraphs.Count>2000 || request.Paragraphs.Any(p=>p is null || p.Text is null || p.Text.Length>64000) || request.Paragraphs.Sum(p=>(long)p.Text.Length)>500000 || request.Paragraphs.Select(p=>p.Index).Distinct().Count()!=request.Paragraphs.Count)
                    throw new InvalidOperationException("Bản thử nhận tối đa 2.000 đoạn/500.000 ký tự trong nội dung chính; dữ liệu quét chưa hợp lệ.");
                foreach(var row in request.Paragraphs)
                {
                    if(!BridgeContract.Linear(row)){response.SkippedParagraphs++;continue;}
                    string text=row.Text.TrimEnd('\r','\a');
                    if(row.InTable || text.Contains('\t')){response.SkippedParagraphs++;continue;}
                    var spelling=model.Check(text,data.Terms);
                    var regular=Checker.Check([new(row.Start,text,Alignment:0)],DocumentProfile.Personal).Where(i=>TextKinds.Contains(i.Kind)).ToArray();
                    foreach(var issue in regular)response.Findings.Add(new(){ParagraphIndex=row.Index,Start=issue.Start,Length=issue.Length,Original=issue.Original,Message=issue.Message,Category="TextReview",Suggestions=Array.Empty<string>()});
                    foreach(var issue in spelling)
                        if(!regular.Any(i=>i.Start<(long)row.Start+issue.Start+issue.Length && row.Start+issue.Start<(long)i.Start+i.Length))
                            response.Findings.Add(new(){ParagraphIndex=row.Index,Start=row.Start+issue.Start,Length=issue.Length,Original=issue.Original,Message=issue.Reason,Category=issue.Kind.ToString(),Suggestions=issue.Suggestions.ToArray()});
                    if(response.Findings.Count>1000){response.Findings=response.Findings.Take(1000).ToList();response.Truncated=true;break;}
                }
                response.Notice="Bản thử kiểm chữ, khoảng trắng/dấu câu trong nội dung chính ngoài bảng; chưa chuyển toàn bộ luật thể thức. Bạn sửa trực tiếp trong Word rồi Quét lại.";
            }
            if(request.Operation=="mirror")
            {
                WordDictionaryMirror.Write(mirrorPath,data);response.MirrorPath=mirrorPath;
                response.Notice="Đã tạo tệp từ đơn riêng. Cụm nhiều từ và mục có số/gạch nối vẫn được lưu trong Add-in; chưa xác nhận Word đã nạp lại.";
            }
            response.Success=true;
        }
        catch(Exception error){response.Error=error.Message;}
        return response;
    }
}
