using ChinhTa.Core;

public static class DocumentCoverageChecks
{
    public static void Run(Action<bool,string> assert)
    {
        DocumentCoverageSnapshot Build(DocumentProfile profile=DocumentProfile.State,Paragraph[]? paragraphs=null,LayoutParagraph[]? layout=null,bool xml=true,bool complete=true,IReadOnlyList<Issue>? issues=null,
            IReadOnlyDictionary<string,CoverageObservation>? external=null)=>DocumentCoverageBuilder.Build(new(profile,paragraphs??[],layout??[],xml,complete,issues,external));
        var empty=Build();
        assert(empty.Rows.Count==33 && empty.Rows.Select(r=>r.Id).Distinct().Count()==33 && empty.Rows.All(r=>r.Observation.Valid),
            "The document coverage ledger always lists all33reviewable capability IDs even when no findings exist");
        foreach(string id in new[]{"C02","C03","C05","C06","C09","C10","C11","C12","C22","C23","C24","C26","C27","C28","C29","C31"})
            assert(empty.Rows.Single(r=>r.Id==id).State==CoverageState.NotRecognized,
                "Zero findings and absent semantic anchors do not imply that a family was observed: "+id);
        assert(empty.Rows.Single(r=>r.Id=="C04").State==CoverageState.NotApplicable && empty.Rows.Where(r=>r.Id is "C32" or "C33").All(r=>r.State==CoverageState.Unavailable),
            "Inactive profile and missing page evidence are distinct from not recognizing a body/header role");
        assert(empty.Summary.Contains("Không có cảnh báo không có nghĩa",StringComparison.Ordinal) && empty.ToText().Contains(DocumentCoverageSnapshot.Legend,StringComparison.Ordinal),
            "Coverage output explicitly separates observed scoped ranges from correctness or full-document validation");
        var personal=Build(DocumentProfile.Personal);var party=Build(DocumentProfile.Party);
        assert(personal.Rows.Where(r=>r.Id is "C02" or "C03" or "C04" or "C31" or "C32" or "C33").All(r=>r.State==CoverageState.NotApplicable) &&
            party.Rows.Single(r=>r.Id=="C04").State==CoverageState.NotRecognized && party.Rows.Single(r=>r.Id=="C02").State==CoverageState.NotApplicable,
            "Coverage applicability follows the active profile and never imports State roles into Personal or Party scans");
        const string secret="Private-document-sentinel-9dfe4e";
        var plain=new Paragraph(0,secret+"\r");var unproven=new Paragraph(plain.Text.Length,"Vùng chưa đối chiếu XML.\r",TextEditable:false);
        var text=Build(paragraphs:[plain]);var partial=Build(paragraphs:[plain,unproven]);var missingXml=Build(paragraphs:[plain],xml:false);
        assert(text.Rows.Single(r=>r.Id=="C01") is {State:CoverageState.Observed,RecognizedCount:1,ObservedCount:1} &&
            partial.Rows.Single(r=>r.Id=="C01") is {State:CoverageState.Partial,RecognizedCount:2,ObservedCount:1},
            "Basic text coverage reports actual proven paragraph counts and preserves an unsupported paragraph as a coverage gap");
        assert(missingXml.Rows.Where(r=>r.State!=CoverageState.NotApplicable).All(r=>r.State!=CoverageState.Observed) &&
            Build(paragraphs:[plain],complete:false).Rows.Single(r=>r.Id=="C01").State==CoverageState.Partial,
            "Missing or incomplete current XML cannot yield a fully observed family from native text alone");
        assert(!text.ToText().Contains(secret,StringComparison.Ordinal),"The coverage summary stores no document text or private content in its exported ledger");
        var manual=new Issue(0,1,"X","X","unrelated",false,FixKind.Advisory,CanFix:false);
        var space=new Issue(4,2,"  "," ","space",false);
        var demo=new Issue(1,2,"đo","đò","demo",false);
        var counted=empty.WithFindings([manual,space,space with{},demo]);
        assert(counted.Rows.Single(r=>r.Id=="C01") is {State:CoverageState.NotRecognized,RepairableFindings:1,ManualFindings:0} && counted.OtherManualFindings==1 && counted.OtherRepairableFindings==1,
            "Finding counts remain independent of observation status, deduplicate repeated records and never assign an unrelated advisory/demo to a capability");
        assert(empty.Rows.Single(r=>r.Id=="C01").RepairableFindings==0 && counted.WithFindings([]).Rows.All(r=>r.RepairableFindings==0 && r.ManualFindings==0),
            "A fresh finding projection does not mutate an earlier snapshot or retain stale repaired findings");
        var sample=BodySpacingChecks.Sample();var observed=Build(paragraphs:sample.P,layout:sample.L);
        assert(observed.Rows.Single(r=>r.Id=="C26").ObservedCount==3 && observed.Rows.Single(r=>r.Id=="C28").RecognizedCount==2 &&
            observed.Rows.Single(r=>r.Id=="C29").State==CoverageState.Observed,
            "Body coverage uses recognized prose ranges and actual adjacent pairs rather than all paragraphs after a title");
        var rows=sample.P.ToArray();var first=rows[10];
        rows[10]=first with{ParagraphStyle=first.ParagraphStyle! with{Spacing=first.ParagraphStyle!.Spacing! with{Rule=4,Line=18}}};
        var exact=Build(paragraphs:rows,layout:sample.L);
        assert(exact.Rows.Single(r=>r.Id=="C29") is {State:CoverageState.Partial,RecognizedCount:3,ObservedCount:2} &&
            exact.Rows.Single(r=>r.Id=="C26").ObservedCount==3,
            "Exactly spacing remains an explicit partial line-coverage state without hiding independent proven font observations");
        rows[10]=first with{Outline=first.Outline! with{SpacingContext=new(LineUnits:true)}};
        var lineUnits=Build(paragraphs:rows,layout:sample.L);
        assert(lineUnits.Rows.Single(r=>r.Id=="C29").ObservedCount==2 && lineUnits.Rows.Single(r=>r.Id=="C28").ObservedCount==1,
            "Unknown inherited line-unit spacing prevents only affected line/pair coverage while the independent later pair remains observed");
        rows[10]=first with{ParagraphStyle=first.ParagraphStyle! with{Spacing=first.ParagraphStyle!.Spacing! with{After=3}}};
        rows[11]=rows[11] with{ParagraphStyle=rows[11].ParagraphStyle! with{Spacing=rows[11].ParagraphStyle!.Spacing! with{Before=3}}};
        var collapsed=Build(paragraphs:rows,layout:sample.L);
        assert(collapsed.Rows.Single(r=>r.Id=="C28") is {State:CoverageState.Partial,RecognizedCount:2,ObservedCount:1},
            "A3plus3paragraph pair remains unmeasured coverage instead of treating the sum or ink gap as6pt");
        var allUnsupported=sample.P.Select(p=>p.Outline?.SpacingContext is null?p:p with{Outline=p.Outline with{SpacingContext=new(LineUnits:true)}}).ToArray();
        var limited=Build(paragraphs:allUnsupported,layout:sample.L);
        assert(limited.Rows.Single(r=>r.Id=="C29") is {State:CoverageState.Partial,ObservedCount:0} &&
            limited.Rows.Single(r=>r.Id=="C28") is {State:CoverageState.Partial,ObservedCount:0},
            "Observed but unsupported spacing sources remain partial scope rather than an unavailable native observation or a claimed full check");
        assert(observed.Rows.Single(r=>r.Id=="C12") is {State:CoverageState.PendingGeometry,RecognizedCount:3,ObservedCount:0},
            "Recognized Line roles cannot become rendered geometry coverage before XPS inspection completes");
        var painted=observed.WithExternalEvidence("C12",new(CoverageState.Observed,3,3,"Mốc và nét đã được quan sát trên bản XPS hiện tại."));
        var ambiguous=observed.WithExternalEvidence("C12",new(CoverageState.Partial,3,2,"Một mốc/nét chưa đo chắc."));
        var shortRoles=observed.WithExternalEvidence("C12",new(CoverageState.Observed,2,2,"Chỉ hai mốc hiện tại."));
        assert(painted.Rows.Single(r=>r.Id=="C12").State==CoverageState.Observed && ambiguous.Rows.Single(r=>r.Id=="C12").State==CoverageState.Partial &&
            shortRoles.Rows.Single(r=>r.Id=="C12").State==CoverageState.Partial && observed.Rows.Single(r=>r.Id=="C12").State==CoverageState.PendingGeometry,
            "Rendered coverage preserves pending old snapshots and cannot call an incomplete three-role group fully observed");
        var page=empty.WithExternalEvidence("C32",new(CoverageState.Partial,2,0,"Hai section còn ngoài phạm vi tự sửa."))
            .WithExternalEvidence("C33",new(CoverageState.PendingGeometry,1,0,"PAGE đã đọc; chưa kiểm hình ảnh."));
        assert(page.Rows.Single(r=>r.Id=="C32").State==CoverageState.Partial && page.Rows.Single(r=>r.Id=="C33").State==CoverageState.PendingGeometry && empty.Rows.Single(r=>r.Id=="C32").State==CoverageState.Unavailable,
            "Page settings and actual PAGE rendering carry explicit independent evidence and cannot be inferred from zero findings");
        assert(personal.WithExternalEvidence("C32",new(CoverageState.Observed,1,1,"external")).Rows.Single(r=>r.Id=="C32").State==CoverageState.NotApplicable,
            "External page observations cannot activate a capability outside the selected document profile");
        foreach(var observation in new[]{new CoverageObservation(CoverageState.Observed,0,0,"empty"),new(CoverageState.Partial,1,2,"excess"),new(CoverageState.Partial,-1,0,"negative"),new(CoverageState.NotRecognized,1,0,"recognized"),new(CoverageState.Partial,1,0,"")})
        {
            bool rejected=false;try{empty.WithExternalEvidence("C12",observation);}catch(ArgumentException){rejected=true;}
            assert(rejected,"Invalid geometry/coverage counts cannot be recorded as successful observation");
        }
        bool unknownId=false;try{empty.WithExternalEvidence("C26",new(CoverageState.Observed,1,1,"external"));}catch(ArgumentException){unknownId=true;}
        assert(unknownId,"External geometry/page updates cannot replace native font-family observations");
        var mutable=new List<Paragraph>{plain};var external=new Dictionary<string,CoverageObservation>{{"C32",new(CoverageState.Partial,2,0,"sections")}};
        var frozen=DocumentCoverageBuilder.Build(new(DocumentProfile.State,mutable,[],true,true,External:external));mutable.Clear();external["C32"]=new(CoverageState.Observed,1,1,"changed");
        assert(frozen.Rows.Single(r=>r.Id=="C01").RecognizedCount==1 && frozen.Rows.Single(r=>r.Id=="C32").RecognizedCount==2,
            "Coverage snapshots copy mutable input collections and cannot silently change after a document scan");
        bool duplicate=false;try{Build(paragraphs:[plain,plain]);}catch(ArgumentException){duplicate=true;}
        assert(duplicate,"Repeated native paragraph starts are rejected before computing misleading family counts");
    }
}
