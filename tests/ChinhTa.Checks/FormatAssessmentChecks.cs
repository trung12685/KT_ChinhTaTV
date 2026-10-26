using ChinhTa.Core;

public static class FormatAssessmentChecks
{
    public static void Run(Action<bool,string> assert)
    {
        ObservedFont Font(float size=13,int bold=-1,int italic=0,int color=0,string name="Times New Roman")=>new(name,size,bold,italic,color);
        Paragraph Evidence(Paragraph row,params ObservedFont[] fonts)=>row with {FormatObserved=true,Font="",Size=9999999,
            FormatMixed=true,BoldMixed=true,FormatEvidence=new(row.Text.TrimEnd('\r','\a','\n'),fonts)};
        var sample=ComponentChecks.Sample(softBreak:true);var roles=ComponentRules.Identify(sample.Rows,sample.Layout,DocumentProfile.State);
        foreach(var role in roles)
        {
            bool agency=role.Role is ComponentRole.Agency or ComponentRole.AgencyParent,label=role.Role==ComponentRole.RecipientLabel;
            float min=agency?12:label?12:11,max=agency?13:min;int bold=agency?role.Role==ComponentRole.Agency?-1:0:label?-1:0;
            int italic=label?-1:0;
            var good=new[]{Font(min,bold,italic),Font(max,bold,italic,-16777216,"TIMES NEW ROMAN")};
            if(role.Role==ComponentRole.RecipientItem)good=[Font(11,0),Font(11,-1),Font(11,9999999)];
            var observed=Evidence(role.Row,good);
            var rows=sample.Rows.Select(p=>p.Start==observed.Start?observed:p).ToArray();
            assert(ComponentRules.Check(rows,sample.Layout,DocumentProfile.State).Count==0,
                "Each component role accepts all proven legal sizes/black encodings and ignores bold only when its policy leaves bold unchanged");
            var bad=Evidence(role.Row,good.Select(f=>f with {Name="Arial",Color=255}).ToArray());
            var defects=ComponentRules.Check(sample.Rows.Select(p=>p.Start==bad.Start?bad:p).ToArray(),sample.Layout,DocumentProfile.State);
            var fix=defects.Single();
            assert(fix.CanFix && fix.CharacterFormat?.Role==role.Role && fix.Original==fix.Replacement &&
                fix.CharacterFormat.PreserveSize==agency,"Known mixed component font/color defects are repairable without flattening legal agency sizes");
            BatchPlan.Create([fix]);
            var repaired=Evidence(role.Row,good.Select(f=>f with {Name="Times New Roman",Color=0,Italic=italic,
                Bold=fix.CharacterFormat!.Bold is bool value?value?-1:0:f.Bold}).ToArray());
            assert(ComponentRules.Check(sample.Rows.Select(p=>p.Start==repaired.Start?repaired:p).ToArray(),sample.Layout,DocumentProfile.State).Count==0,
                "Fresh component evidence after repair removes the defect while preserving legitimate size/bold variation");
        }
        foreach(var row in DocumentHeadingRules.ObservationTargets(sample.Rows,sample.Layout,DocumentProfile.State))
        {
            var good=Evidence(row,Font(13),Font(14,color:-16777216,name:"TIMES NEW ROMAN"));
            var rows=sample.Rows.Select(p=>p.Start==row.Start?good:p).ToArray();
            assert(DocumentHeadingRules.Check(rows,sample.Layout,DocumentProfile.State).Count==0,
                "Named title/subject evidence accepts valid 13/14pt, equivalent black and soft breaks without trusting an undefined aggregate");
            var bad=Evidence(row,Font(13,name:"Arial"),Font(14,color:255));
            var issue=DocumentHeadingRules.Check(sample.Rows.Select(p=>p.Start==row.Start?bad:p).ToArray(),sample.Layout,DocumentProfile.State).Single();
            assert(issue.CanFix && issue.CharacterFormat is {PreserveSize:true,Size:13} && issue.Original==issue.Replacement &&
                issue.CharacterFormat.Matches("Times New Roman",14,0,0,-1) && issue.Suggestion.Contains("giữ cỡ hợp lệ"),
                "Mixed legal title/subject sizes receive a font/color repair explicitly preserving every valid size");
            BatchPlan.Create([issue]);
        }
        var agencyTarget=roles.Single(t=>t.Role==ComponentRole.Agency).Row;
        var subject=DocumentHeadingRules.ObservationTargets(sample.Rows,sample.Layout,DocumentProfile.State).Last();
        foreach(var row in new[]{agencyTarget,subject})foreach(var invalid in new[]{Font(name:""),Font(size:float.NaN),Font(bold:9999999),Font(italic:9999999),Font(color:9999999)})
        {
            var bad=Evidence(row,Font(),invalid);var rows=sample.Rows.Select(p=>p.Start==row.Start?bad:p).ToArray();
            var issues=ComponentRules.Check(rows,sample.Layout,DocumentProfile.State).Concat(DocumentHeadingRules.Check(rows,sample.Layout,DocumentProfile.State)).Where(i=>i.Start==row.Start).ToArray();
            assert(issues.Length==1 && !issues[0].CanFix,"One unreadable required property on any visible glyph keeps its component review-only");
        }
        var policy=new FormatPolicy(13,14,false,true);var normal=new Paragraph(0,"Nội dung",FormatObserved:true);
        assert(policy.Assess(Evidence(normal,Font(12),Font(14))).State==FormatState.NeedsReview,
            "An invalid size mixed with a different valid size cannot gain a destructive whole-range size write");
        assert(policy.Assess(Evidence(normal,Font(12),Font(13))).State==FormatState.Repairable &&
            !policy.Assess(Evidence(normal,Font(12),Font(13))).PreserveSize,"A shared safe target may fix an invalid size while preserving an already valid size");
        foreach(var row in new[]{Evidence(normal,Font()) with {Text="Khác"},Evidence(normal,Font()) with {TextEditable=false,FormatEditable=false},
            Evidence(normal,Font()) with {FormatObserved=false},Evidence(normal,Array.Empty<ObservedFont>())})
            assert(policy.Assess(row).State==FormatState.Unknown,"Old text, lost XML permission, unobserved and empty evidence never establish valid formatting or permission");
        foreach(bool motto in new[]{false,true})
        {
            float min=motto?13:12,max=min+1;string text=motto?HeadingRules.Motto:Checker.Heading;
            var row=Evidence(new(0,text,Bold:true,Size:min,InTable:true,FormatEditable:true,TextEditable:false),Font(min),Font(max,color:-16777216));
            assert(!HeadingRules.Check(row,DocumentProfile.State).Any(),"A correct national heading/motto stays correct after an owned Line even with mixed legal sizes");
            var bad=Evidence(row,Font(min,name:"Arial"),Font(max,color:255));
            var fix=HeadingRules.Check(bad,DocumentProfile.State).Single();
            assert(fix.CanFix && fix.Original==fix.Replacement && fix.Format?.MaximumSize==max && fix.Format.Matches("Times New Roman",max,-1,0,0,1),
                "A proven heading format-only repair can preserve legal size variation without opening text replacement through a Line");
            BatchPlan.Create([fix]);
            assert(!HeadingRules.Check(bad with {TextEditable=false,FormatEditable=false},DocumentProfile.State).Single().CanFix,
                "Owned-Line format eligibility cannot be forged from matching heading text alone");
        }
        var headers=new[]{Evidence(new(0,Checker.Heading,InTable:true),Font(12),Font(13)),
            Evidence(new(100,HeadingRules.Motto,InTable:true),Font(13),Font(14))};
        assert(!HeadingRules.CheckConsistency(headers,DocumentProfile.State).Any(),"A scalar first size cannot invent a pair-size warning when actual heading sizes vary legally");
        assert(!HeadingRules.CheckConsistency(headers.Select(p=>p with {FormatObserved=false}).ToArray(),DocumentProfile.State).Any(),
            "Unknown heading sizes cannot invent a pair-size warning from default scalar placeholders");
        var number=sample.Rows.Single(p=>p.Text.StartsWith("Số:"));
        var numbered=sample.Rows.Select(p=>p.Start==number.Start?Evidence(p,Font(13,0),Font(13,-1)):p).ToArray();
        assert(!AdministrativeHeaderRules.Check(numbered,DocumentProfile.State).Any(i=>i.Kind==FixKind.AdministrativeFormat),
            "Number formatting does not reject legitimate bold variation when bold is intentionally preserved");
        var front=DocumentHeadingChecks.Sample();var date=front.P.Single(p=>p.Text.StartsWith("Địa danh,"));
        var dated=front.P.Select(p=>p.Start==date.Start?Evidence(p,Font(13,0,-1),Font(14,-1,-1,-16777216)):p).ToArray();
        assert(!AdministrativeHeaderRules.Check(dated,DocumentProfile.State).Any(i=>i.Kind==FixKind.AdministrativeFormat && i.Start==date.Start),
            "Date formatting accepts legal 13/14pt italic sizes and retained bold variation without touching blank day fields");
        dated=front.P.Select(p=>p.Start==date.Start?Evidence(p,Font(13,0,0,name:"Arial"),Font(14,-1,0,name:"Arial")):p).ToArray();
        var dateFix=AdministrativeHeaderRules.Check(dated,DocumentProfile.State).Single(i=>i.Kind==FixKind.AdministrativeFormat && i.Start==date.Start);
        assert(dateFix.CanFix && dateFix.CharacterFormat is {PreserveSize:true,Italic:true,Bold:null} && dateFix.Original==dateFix.Replacement,
            "A date font/italic repair preserves both legal sizes, blank fields and unconstrained bold");
        BatchPlan.Create([dateFix]);
        bool Reject(Issue issue){try{BatchPlan.Create([issue]);return false;}catch(InvalidOperationException){return true;}}
        var headingFix=new Issue(0,Checker.Heading.Length,Checker.Heading,Checker.Heading,"",false,FixKind.NationalHeading,Format:new(12,1,13));
        assert(Reject(headingFix with {Replacement="Khác"}) && Reject(headingFix with {Kind=FixKind.Text}) &&
            Reject(headingFix with {Format=new(12,1,14)}),"Preserve-size heading payloads cannot change text, cross kinds or invent a legal size range");
        assert(!new CharacterFormat(13,false,null,PreserveSize:true).Valid && !new CharacterFormat(14,false,true,PreserveSize:true).Valid &&
            new CharacterFormat(13,false,true,PreserveSize:true).Valid,"Preserve-size character patches are restricted to real variable-size role policies");
        var oldIssue=new Issue(0,7,"BÁO CÁO","BÁO CÁO","",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true,PreserveSize:true));
        bool stale=false;try{CurrentTextPlan.Validate([oldIssue],[oldIssue with {CharacterFormat=new(13,false,true)}]);}catch(InvalidOperationException){stale=true;}
        assert(stale,"A stale preserve-size patch loses permission when the current proposed size behavior changes");
        var report=DevelopmentReport.Build("0.1.61",DateTimeOffset.UnixEpoch);
        assert(report.Contains("B04") && report.Contains("từng cỡ 13/14") && report.Contains("aggregate → từ → ký tự") &&
            DevelopmentReport.Count(CapabilityMode.Reviewable)==32,"The report records the system-level formatting work and a preserve-size example without inflating the capability count");
    }
}
