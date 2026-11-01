using ChinhTa.Core;
using ChinhTa.WordProtocol;
namespace ChinhTa.WordWorker;
public sealed class WorkerService(VietnameseSpellingModel model,PersonalLexiconStore personal,string mirrorPath,SyllableDictionary? syllables=null,bool experimentalQuality=false)
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
                if(request.ReviewMode is not ("text" or "spelling" or "deep" or "format") || request.Profile is not ("State" or "Party" or "Personal") || request.Sections is null || request.Sections.Count>128)
                    throw new InvalidOperationException("Chế độ/phạm vi kiểm chưa hợp lệ.");
                if(request.StrictSpelling && request.ReviewMode!="format" && syllables is null)throw new InvalidOperationException("Thiếu từ điển tiếng Việt cho chế độ kiểm khi gõ.");
                if(!Guid.TryParse(request.DocumentId,out _) || request.SnapshotHash is null || request.SnapshotHash.Length!=64 || request.Paragraphs is null || request.Paragraphs.Count>2000 || request.Paragraphs.Any(p=>p is null || p.Text is null || p.Text.Length>64000) || request.Paragraphs.Sum(p=>(long)p.Text.Length)>500000 || request.Paragraphs.Select(p=>p.Index).Distinct().Count()!=request.Paragraphs.Count)
                    throw new InvalidOperationException("Bản thử nhận tối đa 2.000 đoạn/500.000 ký tự trong nội dung chính; dữ liệu quét chưa hợp lệ.");
                response.TotalParagraphs=request.Paragraphs.Count;
                foreach(var row in request.Paragraphs)
                {
                    if(!BridgeContract.HasBounds(row) || !TextSegments.Supported(row.Text)){response.SkippedParagraphs++;continue;}
                    foreach(var segment in TextSegments.Read(row.Text))
                    {
                        var spelling=request.ReviewMode=="format"?Array.Empty<SpellingFinding>():model.Check(segment.Text,data.Terms,experimentalQuality)
                            .Where(f=>request.ReviewMode!="spelling" || f.Kind!=SpellingFindingKind.ContextConfusion).ToArray();
                        var regular=Checker.Check([new(0,segment.Text,Alignment:0)],DocumentProfile.Personal)
                            .Where(i=>TextKinds.Contains(i.Kind) && (segment.ParagraphEnd || i.Kind is not (FixKind.ParagraphEndSpace or FixKind.SentenceEndSpace))).ToArray();
                        if(request.ReviewMode!="format")foreach(var issue in regular)response.Findings.Add(new(){ParagraphIndex=row.Index,Offset=segment.Offset+issue.Start,Length=issue.Length,InTable=row.InTable,Original=issue.Original,Message=issue.Message,Category="TextReview",Suggestions=Array.Empty<string>()});
                        foreach(var issue in spelling)
                            if(!regular.Any(i=>i.Start<(long)issue.Start+issue.Length && issue.Start<(long)i.Start+i.Length))
                                response.Findings.Add(new(){ParagraphIndex=row.Index,Offset=segment.Offset+issue.Start,Length=issue.Length,InTable=row.InTable,Original=issue.Original,Message=issue.Reason,Category=issue.Kind==SpellingFindingKind.ContextConfusion?"Context":"Spelling",Suggestions=issue.Suggestions.ToArray()});
                        if(request.StrictSpelling && request.ReviewMode!="format" && syllables is not null)
                            foreach(var issue in experimentalQuality?syllables.Check(segment.Text,data.Terms,model.Recognizes):syllables.Check(segment.Text,data.Terms))
                                if(!response.Findings.Any(f=>f.ParagraphIndex==row.Index && f.Offset<segment.Offset+issue.Start+issue.Length && segment.Offset+issue.Start<f.Offset+f.Length))
                                    response.Findings.Add(new(){ParagraphIndex=row.Index,Offset=segment.Offset+issue.Start,Length=issue.Length,InTable=row.InTable,Original=issue.Original,Message=issue.Reason,Category="Spelling",Suggestions=issue.Suggestions.ToArray()});
                        if(request.ReviewMode=="deep" && !TechnicalTextContext.IsTechnical(segment.Text))
                            foreach(var issue in DeepReview.Check(segment.Text))
                            {issue.ParagraphIndex=row.Index;issue.InTable=row.InTable;issue.Offset+=segment.Offset;response.Findings.Add(issue);}
                        response.ScannedSegments++;
                        if(response.Findings.Count>1000){response.Findings=response.Findings.Take(1000).ToList();response.Truncated=true;break;}
                    }
                    if(response.Truncated)break;
                    response.ScannedParagraphs++;if(row.InTable)response.ScannedTableParagraphs++;
                }
                if(request.ReviewMode=="format")response.Findings.AddRange(FormatReview.Check(request).Take(1001));
                if(response.Findings.Count>1000){response.Findings=response.Findings.Take(1000).ToList();response.Truncated=true;}
                response.Scope=request.ReviewMode;
                response.ObservedFormatParagraphs=request.Paragraphs.Count(p=>p.Format is {Observed:true} f && new ObservedFont(f.Font,f.Size,f.Bold,f.Italic,f.Color).Known());
                response.ObservedSections=request.Sections.Count;
                response.Notice=request.ReviewMode=="format"?"Đã kiểm các thành phần/lề trang có dữ liệu quan sát. Chưa kiểm đầy đủ tọa độ, đường kẻ XPS, chữ ký, số trang, đầu/cuối trang và hộp chữ; chưa xác nhận toàn bộ thể thức.":
                    "Kiểm chính tả theo chữ và cụm từ, dấu câu trong nội dung chính/ô bảng. Kiểm sâu thêm gợi ý ngữ cảnh ngoại tuyến có dữ liệu; chưa phải đánh giá toàn bộ ý nghĩa/pháp lý. Bạn tự sửa trong Word.";
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
