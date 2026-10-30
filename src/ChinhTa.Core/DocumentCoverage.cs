using System.Collections.ObjectModel;
using System.Text;

namespace ChinhTa.Core;

public enum CoverageState { NotApplicable,NotRecognized,Unavailable,Partial,Observed,PendingGeometry }
public sealed record CoverageObservation(CoverageState State,int RecognizedCount,int ObservedCount,string Reason)
{
    public bool Valid=>Enum.IsDefined(State) && RecognizedCount>=0 && ObservedCount>=0 && ObservedCount<=RecognizedCount &&
        !string.IsNullOrWhiteSpace(Reason) && (State!=CoverageState.NotRecognized || RecognizedCount==0) &&
        (State!=CoverageState.Observed || RecognizedCount>0 && ObservedCount==RecognizedCount);
}
public sealed record DocumentCoverageRow(string Id,string Name,string Scope,CoverageObservation Observation,
    int RepairableFindings=0,int ManualFindings=0)
{
    public CoverageState State=>Observation.State;
    public int RecognizedCount=>Observation.RecognizedCount;
    public int ObservedCount=>Observation.ObservedCount;
    public string Reason=>Observation.Reason;
}

// An immutable read-only account of this scan's observations. It is never a
// repair plan and never contains document text in the public report.
public sealed class DocumentCoverageSnapshot
{
    public const string Legend="Đã quan sát chỉ áp dụng các vùng đã nhận diện trong phạm vi hỗ trợ; không xác nhận toàn tài liệu đúng. Số cảnh báo độc lập với độ phủ. 8 nhóm thủ công và 1 quy tắc minh họa vẫn cần người dùng duyệt.";
    private readonly IReadOnlyDictionary<string,IReadOnlySet<int>> starts;
    private readonly int expectedLineTargets;
    public IReadOnlyList<DocumentCoverageRow> Rows {get;}
    public int OtherRepairableFindings {get;}
    public int OtherManualFindings {get;}
    internal DocumentCoverageSnapshot(IEnumerable<DocumentCoverageRow> rows,IReadOnlyDictionary<string,IReadOnlySet<int>> bindings,
        int otherRepairable=0,int otherManual=0,int expectedLineTargets=3)
    {
        var copy=rows.ToArray();
        if(copy.Any(r=>!r.Observation.Valid || r.RepairableFindings<0 || r.ManualFindings<0) || copy.Select(r=>r.Id).Distinct().Count()!=copy.Length)
            throw new ArgumentException("Thông tin phạm vi kiểm chưa hợp lệ.");
        if(expectedLineTargets is not (2 or 3))throw new ArgumentOutOfRangeException(nameof(expectedLineTargets));
        this.expectedLineTargets=expectedLineTargets;
        Rows=Array.AsReadOnly(copy);
        starts=new ReadOnlyDictionary<string,IReadOnlySet<int>>(bindings.ToDictionary(p=>p.Key,p=>(IReadOnlySet<int>)new HashSet<int>(p.Value),StringComparer.Ordinal));
        OtherRepairableFindings=otherRepairable;OtherManualFindings=otherManual;
    }
    public string Summary
    {
        get
        {
            var applicable=Rows.Where(r=>r.State!=CoverageState.NotApplicable).ToArray();
            int partial=applicable.Count(r=>r.State!=CoverageState.Observed);
            return $"Phạm vi kiểm: {applicable.Length-partial}/{applicable.Length} nhóm đã quan sát trong phạm vi hỗ trợ; {partial} nhóm còn phần chưa xác minh. Không có cảnh báo không có nghĩa đã kiểm hết tài liệu.";
        }
    }
    public static string Label(CoverageState state)=>state switch
    {
        CoverageState.NotApplicable=>"Không áp dụng chế độ này",CoverageState.NotRecognized=>"Chưa nhận diện vùng",
        CoverageState.Unavailable=>"Thiếu dữ liệu quan sát",CoverageState.Partial=>"Mới quan sát một phần",
        CoverageState.Observed=>"Đã quan sát trong phạm vi hỗ trợ",CoverageState.PendingGeometry=>"Chưa xác minh bản xem trước",_=>throw new ArgumentOutOfRangeException(nameof(state))
    };
    public string ToText()
    {
        var text=new StringBuilder().AppendLine(Summary).AppendLine(Legend).AppendLine();
        foreach(var row in Rows)
            text.AppendLine($"{row.Id} — {row.Name}: {Label(row.State)}; vùng đã quan sát/nhận diện {row.ObservedCount}/{row.RecognizedCount}; đề xuất {row.RepairableFindings}; thủ công {row.ManualFindings}.")
                .AppendLine("Phạm vi: "+row.Scope).AppendLine("Ghi nhận: "+row.Reason).AppendLine();
        text.AppendLine($"Cảnh báo ngoài các mã C đã ghép chắc: {OtherRepairableFindings} đề xuất; {OtherManualFindings} thủ công.");
        return text.ToString();
    }
    public DocumentCoverageSnapshot WithExternalEvidence(string id,CoverageObservation observation)
    {
        if(id is not ("C11" or "C12" or "C32" or "C33") || !observation.Valid || !Rows.Any(r=>r.Id==id))
            throw new ArgumentException("Mã hoặc bằng chứng phạm vi hình học/trang chưa hợp lệ.");
        if(observation.State==CoverageState.Observed && (id=="C12" && observation.RecognizedCount!=expectedLineTargets || id=="C11" && observation.RecognizedCount!=2))
            observation=observation with{State=CoverageState.Partial,Reason=observation.Reason+" Chưa nhận diện đủ các mốc của nhóm."};
        return new(Rows.Select(r=>r.Id==id && r.State!=CoverageState.NotApplicable?r with{Observation=observation}:r),starts,
            OtherRepairableFindings,OtherManualFindings,expectedLineTargets);
    }
    public DocumentCoverageSnapshot WithFindings(IReadOnlyList<Issue> findings)
    {
        var counts=Rows.ToDictionary(r=>r.Id,_=>(Repair:0,Manual:0),StringComparer.Ordinal);int otherRepair=0,otherManual=0;
        foreach(var issue in findings.Distinct())
        {
            string? id=Family(issue);
            if(id is null || !counts.ContainsKey(id) || Rows.Single(r=>r.Id==id).State==CoverageState.NotApplicable){if(issue.CanFix)otherRepair++;else otherManual++;continue;}
            var prior=counts[id];counts[id]=(prior.Repair+(issue.CanFix?1:0),prior.Manual+(issue.CanFix?0:1));
        }
        return new(Rows.Select(r=>r with{RepairableFindings=counts[r.Id].Repair,ManualFindings=counts[r.Id].Manual}),starts,otherRepair,otherManual,expectedLineTargets);
    }
    private string? Family(Issue issue)=>issue.Kind switch
    {
        FixKind.Text when issue.Original.Length>=2 && issue.Original.All(c=>c==' ') && issue.Replacement==" "=>"C01",
        FixKind.NationalHeading=>"C02",FixKind.StateMotto=>"C03",FixKind.PartyHeading=>"C04",
        FixKind.AdministrativeNumber=>"C05",FixKind.AdministrativeDate=>"C06",
        FixKind.AdministrativeFormat=>At("C07",issue.Start)?"C07":At("C08",issue.Start)?"C08":null,
        FixKind.DocumentHeadingFormat=>At("C09",issue.Start)?"C09":At("C10",issue.Start)?"C10":null,
        FixKind.AdministrativePosition=>"C11",FixKind.ShapeLine=>"C12",FixKind.RepeatedText=>"C13",
        FixKind.PunctuationSpace=>"C14",FixKind.ParenthesisSpace=>"C15",FixKind.SentenceEndSpace=>"C16",
        FixKind.ColonSpace=>"C17",FixKind.SentenceInnerSpace=>"C18",FixKind.SentenceAfterSpace=>"C19",
        FixKind.OutlineOrder=>"C20",FixKind.OutlineMarker=>"C21",FixKind.AgencyFormat or FixKind.AgencyText=>"C22",
        FixKind.RecipientFormat or FixKind.RecipientText=>"C23",FixKind.DocumentTypeText=>"C24",FixKind.ParagraphEndSpace=>"C25",
        FixKind.BodyFormat=>"C26",FixKind.BodyParagraph=>"C27",FixKind.BodyGap=>"C28",FixKind.BodyLineSpacing=>"C29",
        FixKind.QuoteSpace=>"C30",FixKind.SignatureFormat=>"C31",FixKind.PageLayout=>"C32",FixKind.PageNumber=>"C33",_=>null
    };
    private bool At(string id,int start)=>starts.TryGetValue(id,out var values) && values.Contains(start);
}

public sealed record DocumentCoverageInput(DocumentProfile Profile,IReadOnlyList<Paragraph> Paragraphs,
    IReadOnlyList<LayoutParagraph> HeaderLayout,bool XmlAvailable,bool XmlComplete,IReadOnlyList<Issue>? Findings=null,
    IReadOnlyDictionary<string,CoverageObservation>? External=null);
