using System.Text.Json;
using ChinhTa.Core;

public static class DevelopmentReportChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string version="0.1.59";
        var now=new DateTimeOffset(2026,10,23,9,15,0,TimeSpan.FromHours(7));
        var evidence=new ReportEvidence(version,17,29,new string('a',40),"https://github.com/trung12685/KT_ChinhTaTV/actions/runs/123");
        string text=DevelopmentReport.Build(version,now,evidence);
        assert(DevelopmentReport.Capabilities.Select(c=>c.Id).Distinct().Count()==DevelopmentReport.Capabilities.Count &&
            DevelopmentReport.Capabilities.All(c=>c.Name.Length>0 && c.Scope.Length>0 && c.Detail.Length>0 && !string.IsNullOrWhiteSpace(c.Example.Before) && !string.IsNullOrWhiteSpace(c.Example.Outcome)),
            "The exported capability catalog has unique IDs, scopes and limits instead of empty headline claims");
        assert(DevelopmentReport.Count(CapabilityMode.Reviewable)==31 && DevelopmentReport.Count(CapabilityMode.Manual)==8 &&
            DevelopmentReport.Count(CapabilityMode.Demonstration)==1 && DevelopmentReport.Capabilities.Count==40,
            "Feature totals distinguish thirty-one reviewed repair groups, eight manual groups and one demo rather than counting repaired document errors");
        assert(text.Contains("Phiên bản: 0.1.59") && text.Contains("2026-10-23 09:15 +07:00") &&
            text.Contains("17 Core/OpenXML và 29 WPF đạt.") && text.Contains(evidence.RunUrl) && text.Contains(evidence.Commit),
            "The report renders the supplied version, local offset and actual matching build counts without hard-coded test totals");
        assert(text.Contains("không phải số lỗi đã sửa") && text.Contains("CI không có Word") && text.Contains("chưa có xác nhận bài Word") &&
            text.Contains("kể cả sau đoạn 40") && text.Contains("chưa hoàn thành"),
            "Program capabilities and automated checks never become claims of document repair, complete spelling coverage or Office PASS");
        assert(DevelopmentReport.Capabilities.All(item=>text.Contains(item.Id+" — "+item.Name) && text.Contains(item.Example.Before) && text.Contains(item.Example.Outcome)),
            "Every catalog item, including manual and demo examples, appears in the exported report");
        assert(DevelopmentReport.Milestones.Select(m=>m.Version).Distinct().Count()==DevelopmentReport.Milestones.Count && DevelopmentReport.Milestones.All(item=>text.Contains(item.Version+": "+item.Detail)) && text.Contains("0.1.18–0.1.27") && text.Contains("0.1.40") && text.Contains("0.1.59"),
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
            "{\"Version\":\"0.1.59\",\"CoreChecks\":999999999999999999999}"})
            assert(ReportEvidence.Parse(json,version) is null,"Invalid, wrong-version or unrelated build metadata cannot claim current-version validation");
        assert(text.Contains("CHƯA TỰ SỬA — CHỈ CẢNH BÁO") && text.Contains("CÁC VIỆC DỰ KIẾN — CHƯA TRIỂN KHAI") &&
            text.Contains("không phải lịch sử sửa tài liệu") && text.Contains("[TAB] là ký hiệu tab") &&
            text.Contains("Ví dụ: “hôm       nay là thứ 2”") && text.Contains("Kết quả minh họa khi duyệt sửa: hôm nay là thứ 2"),
            "The report preserves the requested spaced example and distinguishes supported, manual and future work from document repair history");
        assert(DevelopmentReport.PlannedWork.Select(p=>p.Id).Distinct().Count()==DevelopmentReport.PlannedWork.Count &&
            DevelopmentReport.PlannedWork.All(p=>!string.IsNullOrWhiteSpace(p.Example.Before) && !string.IsNullOrWhiteSpace(p.Example.Outcome) &&
                text.Contains(p.Id+" — "+p.Name) && text.Contains(p.Example.Before) && text.Contains(p.Example.Outcome)),
            "Each planned item has a complete example and a visible research/implementation limit without becoming a current capability");
        foreach(string id in new[]{"C01","C05","C06","C13","C14","C15","C16","C17","C18","C19","C20","C21","C25","C30"})
        {
            var sample=DevelopmentReport.Capabilities.Single(c=>c.Id==id).Example;
            int start=0;IReadOnlyList<Issue> issues;
            if(id is "C05" or "C06")
            {
                var rows=new[]{new Paragraph(0,"CƠ QUAN THỬ NGHIỆM",Bold:true),
                    new Paragraph(100,id=="C05"?sample.Before:"Số: 01/BC-ABC"),
                    new Paragraph(200,Checker.Heading,Bold:true),new Paragraph(300,HeadingRules.Motto,Bold:true,Size:14),
                    new Paragraph(400,id=="C06"?sample.Before:"Hà Nội, ngày 03 tháng 3 năm 2026"),
                    new Paragraph(500,"BÁO CÁO"),new Paragraph(600,"Nội dung thử nghiệm.")};
                start=id=="C05"?100:400;
                var kind=id=="C05"?FixKind.AdministrativeNumber:FixKind.AdministrativeDate;
                issues=Checker.Check(rows).Where(i=>i.CanFix && i.Kind==kind && i.Start>=start && (long)i.Start+i.Length<=start+sample.Before.Length).ToArray();
            }
            else if(id is "C20" or "C21")
            {
                int offset=0;var lines=sample.Before.Split('\n');
                var rows=lines.Select((line,index)=>{string text=line+(index<lines.Length-1?"\n":"");var row=new Paragraph(offset,text,Outline:new(OutlineScope.PlainBody));offset+=text.Length;return row;}).ToArray();
                issues=Checker.Check(rows).Where(i=>i.CanFix).ToArray();
            }
            else issues=Checker.Check([new(0,sample.Before)]).Where(i=>i.CanFix).ToArray();
            var plan=BatchPlan.Create(issues);
            assert(plan.Count>0 && AtomicTextBatch.ExpectedStory(plan,sample.Before,i=>sample.Before[..(i.Start-start)])==sample.Outcome,
                "Published plain-text before/after example agrees with current executable repair rules: "+id);
        }
        foreach(var planned in DevelopmentReport.PlannedWork)
            assert(!Checker.Check([new(0,planned.Example.Before)]).Any(i=>i.CanFix),
                "A future-work example is not already claimed as an automatic text repair: "+planned.Id);
        assert(DevelopmentReport.Build(version,now,evidence)==text,"Viewing and exporting one report snapshot are deterministic and do not require a document or Word connection");
    }
}
