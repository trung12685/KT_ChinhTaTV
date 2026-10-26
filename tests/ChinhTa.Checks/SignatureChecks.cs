using System.Xml.Linq;
using ChinhTa.Core;

public static class SignatureChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    public static (XDocument Xml,Paragraph[] Rows) Sample(bool soft=false,int padding=0,Action<XDocument>? mutate=null)
    {
        XElement P(string text)=>new(W+"p",new XElement(W+"r",text.Split('\v').SelectMany((s,i)=>i==0?
            new[]{new XElement(W+"t",s)}:new[]{new XElement(W+"br"),new XElement(W+"t",s)})));
        var body=new XElement(W+"body",Enumerable.Range(0,padding).Select(i=>P("Đoạn nội dung thử nghiệm thứ "+i+".")),
            new XElement(W+"tbl",new XElement(W+"tr",
                new XElement(W+"tc",P("Nơi nhận:"),P("- Như trên;"),P("- Lưu: VT, TEST.")),
                new XElement(W+"tc",P(soft?"KT. TRƯỞNG PHÒNG\vPHÓ TRƯỞNG PHÒNG":"TRƯỞNG PHÒNG"),P(""),P(""),P("Đại tá Nguyễn Văn An")))),P(""),new XElement(W+"sectPr"));
        var xml=new XDocument(new XElement(W+"document",body));mutate?.Invoke(xml);
        var live=new List<WordParagraphSnapshot>();var rows=new List<Paragraph>();int cp=0;
        foreach(var p in body.Descendants(W+"p").Where(p=>!p.Ancestors(W+"txbxContent").Any()))
        {
            string text=string.Concat(p.Descendants().Where(e=>e.Parent?.Name==W+"r").Select(e=>e.Name==W+"t"?e.Value:e.Name==W+"br"?"\v":""));
            bool cell=p.Ancestors(W+"tc").Any(),last=cell && !p.ElementsAfterSelf(W+"p").Any();
            string full=text+"\r"+(last?"\a":"");
            live.Add(new(cp,cp+full.Length,full,1,cell,true,true,true));
            rows.Add(new(cp,full,Bold:true,Size:14,InTable:cell,FormatObserved:true));cp+=full.Length;
        }
        var outline=new Dictionary<int,OutlineParagraphInfo>();var formats=new HashSet<int>();
        var editable=WordTextProof.Read(xml.ToString(),live,onOutline:(start,info)=>outline[start]=info,
            onFormatEditable:(start,ok)=>{if(ok)formats.Add(start);});
        return(xml,WordTextProof.Apply(rows,editable,outline,formats).ToArray());
    }
    private static XElement Cell(XDocument x)=>x.Descendants(W+"tc").Last();
    private static XElement Name(XDocument x)=>Cell(x).Elements(W+"p").Last();
    public static void Run(Action<bool,string> assert)
    {
        Paragraph[] Targets(Paragraph[] rows)=>SignatureRules.ObservationTargets(rows,DocumentProfile.State).ToArray();
        foreach(bool soft in new[]{false,true})
        {
            var sample=Sample(soft,padding:50);var targets=Targets(sample.Rows);
            assert(targets.Length==2 && targets.Select(p=>p.Outline!.SignatureRole).Distinct().Count()==2 &&
                targets.All(p=>Array.IndexOf(sample.Rows,p)>40),"Signature roles come from a terminal recipient/signature table beyond paragraph 40");
            assert(SignatureRules.Check(sample.Rows,DocumentProfile.State).Count==0,
                "Correct upright bold 14pt signing roles do not generate a warning, including ranks and soft breaks");
            foreach(var target in targets)
            foreach(var defect in new[]{target with {Font="Arial"},target with {Color=255},target with {Size=12},target with {Size=15},target with {Italic=true},target with {Bold=false}})
            {
                var rows=sample.Rows.Select(p=>p==target?defect:p).ToArray();
                var fix=SignatureRules.Check(rows,DocumentProfile.State).Single();
                assert(fix.CanFix && fix.IsStyleOnly && fix.Original==fix.Replacement && fix.CharacterFormat!.Role==target.Outline!.SignatureRole,
                    "Each real font/color/size/italic/bold defect yields one role-bound, content-preserving proposal");
                BatchPlan.Create([fix]);CurrentTextPlan.Validate([fix],[fix]);
                var corrected=defect with {Font="Times New Roman",Color=0,Size=fix.CharacterFormat!.Size,Italic=false,Bold=true};
                assert(SignatureRules.Check(rows.Select(p=>p==defect?corrected:p).ToArray(),DocumentProfile.State).Count==0,
                    "A fresh signature scan removes the repaired row rather than retaining a disabled warning");
                var proven=new WordParagraphSnapshot(target.Start,target.Start+target.Text.Length,target.Text,1,true,true);
                assert(CasePreservationPlan.Regions([fix],[proven]).Count==1,
                    "Every signature repair tracks a full proven paragraph for character, underline and paragraph preservation");
            }
            foreach(var target in targets)
            {
                string text=target.Text.TrimEnd('\r','\a','\n');
                ObservedFont Font(float size,string family="Times New Roman",int bold=-1,int italic=0,int color=0)=>new(family,size,bold,italic,color);
                var mixed=target with {FormatMixed=true,FormatEvidence=new(text,[Font(13),Font(14,color:-16777216)])};
                assert(SignatureRules.Check(sample.Rows.Select(p=>p==target?mixed:p).ToArray(),DocumentProfile.State).Count==0,"Mixed legal 13/14pt and equivalent black encodings are accepted");
                mixed=mixed with {FormatEvidence=new(text,[Font(12,"Arial",0,-1,255),Font(13),Font(14)])};
                var fix=SignatureRules.Check(sample.Rows.Select(p=>p==target?mixed:p).ToArray(),DocumentProfile.State).Single();
                assert(fix.CharacterFormat is {Size:13,PreserveSize:true,RepairInvalidSizes:true},"Only invalid signature glyph sizes are normalized while valid 13/14pt remain");
                var layout=new ObservedParagraphStyle(1,4,5,0,new(0,10,0,12,0,0));
                var source=new CharacterStyleEvidence(text,text.Select((c,i)=>char.IsWhiteSpace(c)?null:new ObservedCharacterStyle(Font(i%3==0?12:i%3==1?13:14,"Arial",0,-1,255),i%2)),layout);
                var expected=CasePreservationPlan.Expected(fix,[fix],source,_=>"");
                assert(expected.Text==text && expected.Complete && expected.Paragraph==layout &&
                    source.Styles.Select((s,i)=>s is null || expected.Styles[i]!.Underline==s.Underline &&
                        expected.Styles[i]!.Font.Size==(s.Font.Size<13?13:s.Font.Size)).All(x=>x),
                    "Signature proof preserves names, rank, breaks, valid sizes, underline, alignment, indentation and spacing");
                assert(!expected.Matches(new(text,expected.Styles,layout with {First=5})),"Postcondition detects unrequested signing-block layout drift");
                foreach(var unknown in new[]{mixed with {FormatObserved=false},mixed with {TextEditable=false,FormatEditable=false},
                    mixed with {FormatEvidence=new("stale",[Font(12)])},mixed with {FormatEvidence=new(text,[Font(12),Font(14,bold:9999999)])}})
                    assert(SignatureRules.Check(sample.Rows.Select(p=>p==target?unknown:p).ToArray(),DocumentProfile.State).Count==0,"Unknown, stale or uneditable signing data does not become a formatting defect");
                foreach(var role in new[]{ComponentRole.Agency,ComponentRole.Body,ComponentRole.RecipientItem})
                {
                    bool rejected=false;try{BatchPlan.Create([fix with {CharacterFormat=fix.CharacterFormat! with {Role=role}}]);}catch(InvalidOperationException){rejected=true;}
                    assert(rejected,"A signature-format payload cannot borrow an unrelated role policy");
                }
                bool stale=false;try{CurrentTextPlan.Validate([fix],[]);}catch(InvalidOperationException){stale=true;}
                assert(stale,"A stale signature choice must still exist in a fresh scan before a write");
            }
        }
        foreach(var mutate in new Action<XDocument>[]
        {
            x=>Name(x).Descendants(W+"t").Single().Value="Người ký",
            x=>Name(x).Descendants(W+"t").Single().Value="Họ và tên",
            x=>Name(x).Descendants(W+"t").Single().Value="Nội dung chưa kết thúc.",
            x=>Cell(x).Elements(W+"p").First().Descendants(W+"t").Single().Value="Một tiêu đề bất kỳ",
            x=>Cell(x).Add(new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","Ghi chú sau chữ ký.")))),
            x=>x.Descendants(W+"body").Single().Add(new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","Phụ lục tiếp theo")))),
            x=>Cell(x).Add(new XElement(W+"tbl")),
            x=>Cell(x).AddFirst(new XElement(W+"tcPr",new XElement(W+"gridSpan",new XAttribute(W+"val",2)))),
            x=>Name(x).AddFirst(new XElement(W+"pPr",new XElement(W+"numPr",new XElement(W+"numId",new XAttribute(W+"val",1))))),
            x=>Name(x).AddFirst(new XElement(W+"pPr",new XElement(W+"pStyle",new XAttribute(W+"val","Unknown")))),
            x=>Name(x).Elements(W+"r").First().Add(new XElement(W+"fldChar",new XAttribute(W+"fldCharType","begin"))),
            x=>x.Descendants(W+"tc").First().Descendants(W+"t").Last().Value="- Mục chưa kết thúc;",
            x=>Name(x).ReplaceWith(new XElement(W+"sdt",new XElement(W+"sdtContent",Name(x))))
        })
            assert(Targets(Sample(mutate:mutate).Rows).Length==0,"Ambiguous names, following content, nested/merged/controlled/numbered/unproven structures do not grant signature repairs");
        var incomplete=Sample().Rows;var signer=incomplete.Single(p=>p.Outline?.SignatureRole==ComponentRole.SignatureName);
        assert(Targets(incomplete.Select(p=>p==signer?p with {TextEditable=false,FormatEditable=false}:p).ToArray()).Length==0 &&
            Targets(incomplete.Where(p=>p!=signer).ToArray()).Length==0,
            "A partially mapped signing block cannot use an unproven or absent name as permission to repair the authority");
        var image=Sample(mutate:x=>Cell(x).Elements(W+"p").Skip(1).First().Add(new XElement(W+"r",new XElement(W+"drawing"))));
        assert(Targets(image.Rows).Length==2,"A separate image-only signature paragraph remains outside the editable text targets");
        var objectLive=new List<WordParagraphSnapshot>();int objectCp=0;
        foreach(var p in image.Xml.Descendants(W+"body").Single().Descendants(W+"p"))
        {
            bool cell=p.Ancestors(W+"tc").Any();
            string text=string.Concat(p.Descendants(W+"t").Select(t=>t.Value))+(p.Descendants(W+"drawing").Any()?"\u0001":"")+"\r"+
                (cell && !p.ElementsAfterSelf(W+"p").Any()?"\a":"");
            objectLive.Add(new(objectCp,objectCp+text.Length,text,1,cell,true));objectCp+=text.Length;
        }
        var objectRoles=new Dictionary<int,OutlineParagraphInfo>();
        var objectEditable=WordTextProof.Read(image.Xml.ToString(),objectLive,onOutline:(start,info)=>objectRoles[start]=info);
        var objectRows=objectLive.Select(p=>new Paragraph(p.Start,p.Text,InTable:p.InTable,FormatObserved:true)).ToArray();
        assert(Targets(WordTextProof.Apply(objectRows,objectEditable,objectRoles).ToArray()).Length==0,
            "An inline image object CP before the signer blocks the incomplete anchor group rather than guessing past lost text proof");
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(SignatureRules.Check(Sample().Rows,profile).Count==0 && SignatureRules.ObservationTargets(Sample().Rows,profile).Count==0,
                "State signature rules do not spill into Party or personal profiles");
        var good=Sample().Rows;var broken=Targets(good).Select(p=>p with {Font="Arial",Color=255}).ToArray();
        var formats=SignatureRules.Check(broken,DocumentProfile.State);var space=new Issue(5000,2,"  "," ","",false);
        assert(BatchPlan.Create(formats.Append(space)).Count==3,"Both signing roles can be selected together with another independent repair family");
        foreach(var fix in formats)
        {
            var info=IssueReviewCatalog.Describe(fix);var tip=IssueTooltipCatalog.Describe(fix);
            assert(info.Section==IssueReviewSection.RequiredFormat && info.FamilyKey=="signature-font" && tip.Text.Split('\n').Length==3 &&
                tip.Basis.Contains("II.7, điểm đ") && tip.Action.Contains("giữ nguyên chữ"),"Signature findings use one compact required group and a three-line legal tooltip");
        }
        var line=new Issue(6000,4,"Mẫu.","Mẫu.","",false,FixKind.ShapeLine,LineFix:new(null,100,100,80,LineRole.Subject));
        var selected=formats.Append(space).Append(line).ToArray();var requests=selected.Select(i=>new DeferredRepair(i,i.Original)).ToArray();
        int writes=0,commits=0,restores=0,refreshes=0;
        var applied=StagedRepairBatch.Apply(requests,_=>refreshes++,r=>r.Requested,plan=>writes+=plan.Count,()=>{},()=>commits++,()=>restores++);
        assert(applied.Applied==4 && commits==1 && refreshes==1 && restores==0 && writes==4,
            "Signing roles, another text family and a remeasured Line share one staged completion/Undo boundary");
        writes=commits=restores=0;bool failed=false;
        try{StagedRepairBatch.Apply(requests,_=>{},r=>r.Requested,plan=>{writes+=plan.Count;},
            ()=>throw new IOException("synthetic final verification"),()=>commits++,()=>{restores++;writes=0;});}
        catch(IOException){failed=true;}
        assert(failed && restores==1 && writes==0 && commits==0,
            "A final failure after signature and Line writes restores the whole selection and publishes no successful Undo");
        assert(SignatureRules.ObservationTargets([good[0],good[0]],DocumentProfile.State).Count==0,"Duplicate live starts cannot become signature observation targets");
    }
}
