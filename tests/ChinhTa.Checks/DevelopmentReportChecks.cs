using System.Text.Json;
using ChinhTa.Core;

public static class DevelopmentReportChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string version="0.1.42";
        var now=new DateTimeOffset(2026,10,23,9,15,0,TimeSpan.FromHours(7));
        var evidence=new ReportEvidence(version,17,29,new string('a',40),"https://github.com/trung12685/KT_ChinhTaTV/actions/runs/123");
        string text=DevelopmentReport.Build(version,now,evidence);
        assert(DevelopmentReport.Capabilities.Select(c=>c.Id).Distinct().Count()==DevelopmentReport.Capabilities.Count &&
            DevelopmentReport.Capabilities.All(c=>c.Name.Length>0 && c.Scope.Length>0 && c.Detail.Length>0),
            "The exported capability catalog has unique IDs, scopes and limits instead of empty headline claims");
        assert(DevelopmentReport.Count(CapabilityMode.Reviewable)==14 && DevelopmentReport.Count(CapabilityMode.Manual)==8 &&
            DevelopmentReport.Count(CapabilityMode.Demonstration)==1 && DevelopmentReport.Capabilities.Count==23,
            "Feature totals distinguish fourteen reviewed repair groups, eight manual groups and one demo rather than counting repaired document errors");
        assert(text.Contains("Phiên bản: 0.1.42") && text.Contains("2026-10-23 09:15 +07:00") &&
            text.Contains("17 Core/OpenXML và 29 WPF đạt.") && text.Contains(evidence.RunUrl) && text.Contains(evidence.Commit),
            "The report renders the supplied version, local offset and actual matching build counts without hard-coded test totals");
        assert(text.Contains("không phải số lỗi đã sửa") && text.Contains("CI không có Word") && text.Contains("chưa có xác nhận bài Word") &&
            text.Contains("40 đoạn đầu Nhà nước") && text.Contains("chưa hoàn thành"),
            "Program capabilities and automated checks never become claims of document repair, complete spelling coverage or Office PASS");
        assert(DevelopmentReport.Capabilities.All(item=>text.Contains(item.Id+" — "+item.Name)),
            "Every catalog item, including manual and demo entries, appears in the exported report");
        assert(DevelopmentReport.Milestones.All(item=>text.Contains(item.Version+": "+item.Detail)) && text.Contains("0.1.18–0.1.27") && text.Contains("0.1.40") && text.Contains("0.1.42"),
            "The report retains the delivered Line, formatting, save/recovery and repetition milestones for future development");
        int format=text.IndexOf("1. Thể thức",StringComparison.Ordinal),basic=text.IndexOf("2. Lỗi chữ",StringComparison.Ordinal),
            context=text.IndexOf("3. Sau cùng",StringComparison.Ordinal);
        assert(format>=0 && basic>format && context>basic && text.Contains("ngữ cảnh, ngữ nghĩa và thư viện"),
            "The exported next steps preserve the user's order: format, basic language/repetition, then contextual library work");
        foreach(ReportEvidence? absent in new ReportEvidence?[]{null,evidence with {Version="0.1.40"},evidence with {CoreChecks=0},
            evidence with {WpfChecks=-1},evidence with {Commit="bad"},evidence with {RunUrl="https://example.com/PASS"}})
        {
            string unverified=DevelopmentReport.Build(version,now,absent);
            assert(!unverified.Contains("Windows tự động:") && unverified.Contains("Chưa có thông tin kiểm thử đi kèm phù hợp phiên bản"),
                "Missing, stale or malformed evidence renders an explicit unknown state instead of fabricated validation numbers");
        }
        string valid=JsonSerializer.Serialize(evidence);
        assert(ReportEvidence.Parse(valid,version)==evidence,"A matching packaged validation manifest retains its actual counts and source");
        foreach(string json in new[]{"not json","null","[]","{}",JsonSerializer.Serialize(evidence with {Version="0.1.40"}),
            JsonSerializer.Serialize(evidence with {CoreChecks=-1}),JsonSerializer.Serialize(evidence with {WpfChecks=0}),
            JsonSerializer.Serialize(evidence with {RunUrl="https://github.com/trung12685/KT_ChinhTaTV/actions/runs/123/extra"}),
            JsonSerializer.Serialize(evidence with {RunUrl="https://github.com/other/repo/actions/runs/123"}),
            JsonSerializer.Serialize(evidence with {Commit=null!}),JsonSerializer.Serialize(evidence with {RunUrl=null!}),
            "{\"Version\":\"0.1.42\",\"CoreChecks\":999999999999999999999}"})
            assert(ReportEvidence.Parse(json,version) is null,"Invalid, wrong-version or unrelated build metadata cannot claim current-version validation");
        assert(DevelopmentReport.Build(version,now,evidence)==text,"Viewing and exporting one report snapshot are deterministic and do not require a document or Word connection");
    }
}
