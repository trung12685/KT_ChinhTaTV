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
            if(request.Version!=BridgeContract.Version || !Guid.TryParse(request.Id,out _) || request.Operation is not ("scan" or "terms" or "add" or "remove" or "mirror"))throw new InvalidOperationException("Yêu cầu kết nối chưa đúng phiên bản hoặc thao tác. Hãy cài lại đầy đủ cùng một gói Add-in.");
            PersonalLexiconData data=request.Operation switch {"add"=>personal.Add(request.Term),"remove"=>personal.Remove(request.Term),_=>personal.Load()};
            response.DictionaryRevision=data.Revision;response.Terms=request.Operation=="scan"?Array.Empty<string>():data.Terms.Select(t=>t.Text).ToArray();response.Source=model.Source.Name;
            if(request.Operation=="scan")
            {
                if(!Guid.TryParse(request.DocumentId,out _) || request.SnapshotHash is null || request.SnapshotHash.Length!=64 || request.Paragraphs is null || request.Paragraphs.Count>2000 || request.Paragraphs.Any(p=>p is null || p.Text is null || p.Text.Length>64000) || request.Paragraphs.Sum(p=>(long)p.Text.Length)>500000 || request.Paragraphs.Select(p=>p.Index).Distinct().Count()!=request.Paragraphs.Count)
                    throw new InvalidOperationException("Bản thử nhận tối đa 2.000 đoạn/500.000 ký tự trong nội dung chính; dữ liệu quét chưa hợp lệ.");
                response.TotalParagraphs=request.Paragraphs.Count;
                foreach(var row in request.Paragraphs)
                {
                    if(!BridgeContract.HasBounds(row) || !TextSegments.Supported(row.Text)){response.SkippedParagraphs++;continue;}
                    foreach(var segment in TextSegments.Read(row.Text))
                    {
                        var spelling=model.Check(segment.Text,data.Terms);
                        var regular=Checker.Check([new(0,segment.Text,Alignment:0)],DocumentProfile.Personal)
                            .Where(i=>TextKinds.Contains(i.Kind) && (segment.ParagraphEnd || i.Kind is not (FixKind.ParagraphEndSpace or FixKind.SentenceEndSpace))).ToArray();
                        foreach(var issue in regular)response.Findings.Add(new(){ParagraphIndex=row.Index,Offset=segment.Offset+issue.Start,Length=issue.Length,InTable=row.InTable,Original=issue.Original,Message=issue.Message,Category="TextReview",Suggestions=Array.Empty<string>()});
                        foreach(var issue in spelling)
                            if(!regular.Any(i=>i.Start<(long)issue.Start+issue.Length && issue.Start<(long)i.Start+i.Length))
                                response.Findings.Add(new(){ParagraphIndex=row.Index,Offset=segment.Offset+issue.Start,Length=issue.Length,InTable=row.InTable,Original=issue.Original,Message=issue.Reason,Category=issue.Kind.ToString(),Suggestions=issue.Suggestions.ToArray()});
                        response.ScannedSegments++;
                        if(response.Findings.Count>1000){response.Findings=response.Findings.Take(1000).ToList();response.Truncated=true;break;}
                    }
                    if(response.Truncated)break;
                    response.ScannedParagraphs++;if(row.InTable)response.ScannedTableParagraphs++;
                }
                response.Notice="Kiểm chữ/dấu câu trong nội dung chính và ô bảng; tách tại tab/ngắt dòng, bỏ qua đoạn có mã trường/điều khiển chưa hỗ trợ. Chưa kiểm đầy đủ thể thức. Bạn sửa trong Word rồi Quét lại.";
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
