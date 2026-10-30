namespace ChinhTa.Core;

// Uses current native/XML observations, never the absence of a finding as proof
// that a rule evaluated a family. It does not widen any target or edit permission.
public static class DocumentCoverageBuilder
{
    private static string Clean(Paragraph p)=>p.Text.TrimEnd('\r','\a','\n');
    private static bool FontKnown(Paragraph p,float min,float max,bool italic,bool? bold,bool emphasis=false)=>
        new FormatPolicy(min,max,italic,bold,emphasis).Assess(p,true).State is FormatState.Correct or FormatState.Repairable;
    public static DocumentCoverageSnapshot Build(DocumentCoverageInput input)
    {
        var paragraphs=input.Paragraphs.ToArray();var layout=input.HeaderLayout.ToArray();var profile=input.Profile;
        if(paragraphs.Any(p=>p.Start<0) || paragraphs.Select(p=>p.Start).Distinct().Count()!=paragraphs.Length)
            throw new ArgumentException("Các vùng quan sát phải có mốc native riêng.");
        var rows=new List<DocumentCoverageRow>();var bindings=new Dictionary<string,IReadOnlySet<int>>(StringComparer.Ordinal);
        var caps=DevelopmentReport.Capabilities.Where(c=>c.Mode==CapabilityMode.Reviewable).ToArray();
        var nonempty=paragraphs.Where(p=>!string.IsNullOrWhiteSpace(Clean(p))).ToArray();
        var admin=AdministrativeHeaderRules.Identify(paragraphs,profile);
        var numbers=admin.Where(p=>AdministrativeHeaderRules.IsNumber(p.Text)).ToArray();
        var dates=admin.Where(p=>!AdministrativeHeaderRules.IsNumber(p.Text)).ToArray();
        var headings=paragraphs.Where(p=>p.Page==1 && HeadingRules.IsCandidate(p.Text,profile)).ToArray();
        var national=headings.Where(p=>!Clean(p).TrimStart().StartsWith("Độc",StringComparison.OrdinalIgnoreCase)).ToArray();
        var mottos=headings.Where(p=>Clean(p).TrimStart().StartsWith("Độc",StringComparison.OrdinalIgnoreCase)).ToArray();
        var letter=OfficialLetterRules.Identify(layout,profile);
        var titles=DocumentHeadingRules.ObservationTargets(paragraphs,layout,profile);
        var types=titles.Where(p=>ShapeLineRules.IsDocumentType(p.Text)).ToArray();
        var subjects=titles.Where(p=>!ShapeLineRules.IsDocumentType(p.Text)).ToArray();
        var components=ComponentRules.Identify(paragraphs,layout,profile);
        var agency=components.Where(c=>c.Role is ComponentRole.Agency or ComponentRole.AgencyParent).ToArray();
        var recipients=components.Where(c=>c.Role is ComponentRole.RecipientLabel or ComponentRole.RecipientItem).ToArray();
        var body=BodyFormatRules.ObservationTargets(paragraphs,layout,profile).ToArray();
        var signature=SignatureRules.ObservationTargets(paragraphs,profile).ToArray();
        var lineTargets=ShapeLineRules.Identify(layout,profile);
        var positions=AdministrativePositionRules.Identify(paragraphs,layout,profile);
        static bool Applicable(string id,DocumentProfile p)=>id=="C04"?p==DocumentProfile.Party:
            id is "C01" or "C13" or "C14" or "C15" or "C16" or "C17" or "C18" or "C19" or "C20" or "C21" or "C25" or "C30" || p==DocumentProfile.State;
        CoverageObservation Observation(IReadOnlyList<Paragraph> candidates,Func<Paragraph,bool> observed,string reason)
        {
            int total=candidates.Count,known=candidates.Count(observed);
            if(!input.XmlAvailable)return new(CoverageState.Unavailable,total,0,"Không đọc được XML hiện tại; chưa đối chiếu phạm vi. "+reason);
            if(total==0)return new(CoverageState.NotRecognized,0,0,"Chưa nhận diện vùng đủ chắc; không kết luận thành phần đúng hoặc thiếu. "+reason);
            var state=known==0?CoverageState.Unavailable:known<total || !input.XmlComplete?CoverageState.Partial:CoverageState.Observed;
            return new(state,total,known,reason+(input.XmlComplete?"":" Còn vùng XML/native chưa đối chiếu đầy đủ."));
        }
        void Add(ReportCapability cap,IReadOnlyList<Paragraph> candidates,Func<Paragraph,bool> observed,string reason,Func<Paragraph,bool>? excluded=null)
        {
            bindings[cap.Id]=new HashSet<int>(candidates.Select(p=>p.Start));
            var observation=letter is not null && cap.Id is "C09" or "C24"?new CoverageObservation(CoverageState.NotApplicable,0,0,"Đã nhận diện công văn V/v; không có tên loại riêng như BÁO CÁO/QUYẾT ĐỊNH. Trích yếu nằm dưới số/ký hiệu, kiểm theo chính sách công văn."):
                Applicable(cap.Id,profile)?Observation(candidates,observed,reason):new(CoverageState.NotApplicable,0,0,"Nhóm này không áp dụng chế độ tài liệu đang chọn.");
            if(observation.State==CoverageState.Unavailable && input.XmlAvailable && excluded is not null && candidates.Any(excluded))
                observation=observation with{State=CoverageState.Partial,Reason="Có nguồn/bố cục đã quan sát nhưng còn ngoài phạm vi được xác minh. "+observation.Reason};
            rows.Add(new(cap.Id,cap.Name,cap.Scope,observation));
        }
        static bool TextKnown(Paragraph p)=>p.TextEditable;
        static bool ParagraphKnown(Paragraph p)=>p.TextEditable && p.FormatObserved && p.FormatEvidence is {Fonts.Count:>0} e &&
            e.Text==Clean(p) && BodyParagraphRules.KnownProseFonts(e.Fonts) && BodySpacingRules.Ordinary(p.ParagraphStyle) && p.Outline?.Alignment==p.ParagraphStyle!.Alignment;
        static bool LineKnown(Paragraph p)=>ParagraphKnown(p) && p.Flow is {Known:true} && p.Outline?.SpacingContext is {Supported:true,Grid:false,PageBreakBefore:false,KeepNext:false} &&
            p.ParagraphStyle!.Spacing!.Rule is 0 or 1 or 2 or 5;
        static bool LineUnsupported(Paragraph p)=>p.Outline?.SpacingContext is { } c &&
            (!c.Supported || c.Grid || c.PageBreakBefore || c.KeepNext || p.ParagraphStyle?.Spacing is {Known:true,Rule:3 or 4});
        bool AgencyKnown(Paragraph p)
        {var role=agency.Single(t=>t.Row.Start==p.Start).Role;return FontKnown(p,12,13,false,role==ComponentRole.Agency);}
        bool RecipientKnown(Paragraph p)
        {var role=recipients.Single(t=>t.Row.Start==p.Start).Role;return FontKnown(p,role==ComponentRole.RecipientLabel?12:11,role==ComponentRole.RecipientLabel?12:11,role==ComponentRole.RecipientLabel,role==ComponentRole.RecipientLabel?true:null);}
        foreach(var cap in caps)
        {
            switch(cap.Id)
            {
                case "C02":Add(cap,national,p=>FontKnown(p,12,13,false,true),"Chỉ quốc hiệu trang đầu đã nhận diện; vị trí và nét kiểm riêng.");break;
                case "C03":Add(cap,mottos,p=>FontKnown(p,13,14,false,true),"Chỉ tiêu ngữ trang đầu đã nhận diện; vị trí và nét kiểm riêng.");break;
                case "C04":Add(cap,headings,p=>FontKnown(p,15,15,false,true),"Chỉ dòng tiêu đề Đảng đã nhận diện; chưa xác minh vị trí toàn trang.");break;
                case "C05":Add(cap,numbers,TextKnown,"Chỉ cách ghi số/ký hiệu đã nhận diện; chưa xác minh giá trị và mã chính thức.");break;
                case "C06":Add(cap,dates,TextKnown,"Chỉ mẫu ngày/địa danh đã nhận diện; không xác minh địa danh chính thức.");break;
                case "C07":Add(cap,numbers,p=>FontKnown(p,13,13,false,null),"Chỉ phông dòng số/ký hiệu đã nhận diện.");break;
                case "C08":Add(cap,dates,p=>FontKnown(p,13,14,true,null),"Chỉ phông dòng ngày/địa danh đã nhận diện.");break;
                case "C09":Add(cap,types,p=>FontKnown(p,13,14,false,true),"Chỉ quan sát phông vùng tên loại đã nhận diện; không xác minh mọi biến thể tên loại.");break;
                case "C10":Add(cap,subjects,p=>letter is null?FontKnown(p,13,14,false,true):FontKnown(p,12,13,false,false),
                    letter is null?"Văn bản có tên loại: trích yếu dưới tên loại, đứng, đậm, cỡ 13–14; nét kẻ kiểm riêng.":
                    "Công văn V/v: trích yếu dưới số/ký hiệu, đứng, không đậm, cỡ 12–13; không yêu cầu đường kẻ. Khoảng 6 pt và vị trí dọc cần duyệt riêng; không chứng nhận từ phông.");break;
                case "C22":Add(cap,agency.Select(c=>c.Row).ToArray(),AgencyKnown,"Chỉ tên cơ quan đã nhận diện; chưa đối chiếu danh tính/mã chính thức.");break;
                case "C23":Add(cap,recipients.Select(c=>c.Row).ToArray(),RecipientKnown,"Chỉ nhãn/dòng nơi nhận đã khớp cấu trúc XML; không xác minh địa chỉ người nhận.");break;
                case "C24":Add(cap,types,TextKnown,"Chỉ chữ hoa/thường tên loại đã nhận diện; không suy đoán tên loại bị thiếu.");break;
                case "C26":Add(cap,body,p=>p.ParagraphStyle is {Known:true,Spacing:{Known:true}} && FontKnown(p,13,14,false,null,true),"Chỉ văn xuôi thường trong vùng đã nhận diện; không bao gồm toàn bộ thân bài.");break;
                case "C27":Add(cap,body,ParagraphKnown,"Chỉ căn/thụt đoạn văn xuôi thường đã đối chiếu; bố cục khác chưa được xác minh.");break;
                case "C28":
                    var bodyStarts=body.Select(p=>p.Start).ToHashSet();var pairs=paragraphs.Zip(paragraphs.Skip(1)).Where(p=>bodyStarts.Contains(p.First.Start) && bodyStarts.Contains(p.Second.Start)).ToArray();
                    var left=pairs.Select(p=>p.First).ToArray();
                    bool GapKnown(Paragraph p)
                    {
                        var next=pairs.Single(pair=>pair.First.Start==p.Start).Second;
                        bool Safe(Paragraph r)=>ParagraphKnown(r) && r.Flow is {Known:true} && r.Outline?.SpacingContext is
                            {Supported:true,Grid:false,Contextual:false,PageBreakBefore:false,KeepNext:false} && r.ParagraphStyle!.Spacing is {BeforeAuto:0,AfterAuto:0};
                        return Safe(p) && Safe(next) && p.Flow!.EndPage==next.Flow!.StartPage &&
                            (p.ParagraphStyle!.Spacing!.After+next.ParagraphStyle!.Spacing!.Before<5.99f || p.ParagraphStyle.Spacing.After>=5.99f || next.ParagraphStyle.Spacing.Before>=5.99f);
                    }
                    bool GapUnsupported(Paragraph p)
                    {
                        var next=pairs.Single(pair=>pair.First.Start==p.Start).Second;
                        bool Excluded(Paragraph r)=>LineUnsupported(r) || r.Outline?.SpacingContext is {Contextual:true} ||
                            r.ParagraphStyle?.Spacing is {Known:true,BeforeAuto:not 0} or {Known:true,AfterAuto:not 0};
                        return Excluded(p) || Excluded(next) || p.Flow is {Known:true} && next.Flow is {Known:true} && p.Flow.EndPage!=next.Flow.StartPage ||
                            p.ParagraphStyle?.Spacing is {Known:true} a && next.ParagraphStyle?.Spacing is {Known:true} b && a.After+b.Before>=5.99f && a.After<5.99f && b.Before<5.99f;
                    }
                    Add(cap,left,GapKnown,"Đếm cặp đoạn văn xuôi liền nhau. Khoảng bị gộp, auto/grid/đơn vị dòng hoặc khác trang còn cần đo riêng.",GapUnsupported);break;
                case "C29":Add(cap,body,LineKnown,"Chỉ Single/1,5/Double/Multiple với nguồn rõ; Exactly/AtLeast, grid hoặc bố cục đặc biệt còn cần đo tương đương.",LineUnsupported);break;
                case "C31":Add(cap,signature,p=>FontKnown(p,13,14,false,true),"Chỉ phông vai trò ký đã chứng minh; khoảng ký tay 80 pt chỉ đo khi chủ động chạy tiện ích, chưa được xác nhận bởi quét thường.");break;
                case "C11":
                case "C12":
                    int count=cap.Id=="C11"?positions.Count:lineTargets.Count;
                    bindings[cap.Id]=new HashSet<int>();
                    var geometry=!Applicable(cap.Id,profile)?new CoverageObservation(CoverageState.NotApplicable,0,0,"Không áp dụng chế độ tài liệu đang chọn."):
                        !input.XmlAvailable?new(CoverageState.Unavailable,count,0,"Chưa đọc được XML hiện tại để nhận diện mốc hình học."):
                        count==0?new(CoverageState.NotRecognized,0,0,"Chưa nhận diện mốc đủ chắc; không suy ra đường kẻ/vị trí đúng hoặc thiếu."):
                        new(CoverageState.PendingGeometry,count,0,"Đã nhận diện mốc; còn chờ xác minh bản xem trước XPS hiện tại.");
                    rows.Add(new(cap.Id,cap.Name,cap.Scope,geometry));break;
                case "C32":
                case "C33":
                    bindings[cap.Id]=new HashSet<int>();
                    rows.Add(new(cap.Id,cap.Name,cap.Scope,Applicable(cap.Id,profile)?new(CoverageState.Unavailable,0,0,"Chưa có bằng chứng section/PAGE hiện tại; không suy ra đúng từ số cảnh báo bằng 0."):
                        new(CoverageState.NotApplicable,0,0,"Không áp dụng chế độ tài liệu đang chọn.")));break;
                case "C20":
                case "C21":Add(cap,nonempty,p=>p.TextEditable && p.Outline is {Scope:OutlineScope.PlainBody},"Chỉ quan sát đoạn có bằng chứng cấu trúc XML; nhãn/mục tự động, bảng và cấu trúc chưa rõ không được coi đã kiểm thứ tự đầy đủ.");break;
                default:Add(cap,nonempty,TextKnown,"Chỉ các quy tắc chữ/khoảng trắng/dấu câu trong phạm vi catalog và vùng XML được phép; không phải kiểm chính tả đầy đủ.");break;
            }
        }
        var snapshot=new DocumentCoverageSnapshot(rows,bindings,expectedLineTargets:ShapeLineRules.RequiredRoles(layout,profile).Count);
        if(input.External is not null)foreach(var entry in input.External)snapshot=snapshot.WithExternalEvidence(entry.Key,entry.Value);
        return snapshot.WithFindings(input.Findings??[]);
    }
}
