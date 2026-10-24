using System.Xml.Linq;
using ChinhTa.Core;

public static class BodyOpeningChecks
{
    public static (Paragraph[] P,LayoutParagraph[] L) Sample()
    {
        var source=BodySpacingChecks.Sample();int greeting=source.P[9].Start;int start=0;
        var rows=source.P.Where(p=>p.Start!=greeting).Select((p,i)=>
        {
            var row=p with {Start=start};start+=p.Text.Length;
            if(i is >=9 and <=11)
            {
                int first=i==9?567:i==10?720:0,alignment=i==11?0:3;
                row=row with {Page=1,Outline=row.Outline! with {Alignment=alignment,BodyIndent=new(0,0,first,0)},
                    ParagraphStyle=row.ParagraphStyle! with {Alignment=alignment,First=first/20f}};
            }
            return row;
        }).ToArray();
        var layout=source.L.Where(p=>p.Start<greeting).Concat(rows.Skip(9).Take(2).Select(p=>new LayoutParagraph(p.Start,p.Text,1,false,0,0,false,
            ParagraphCentered:false,UnindentedCenter:false))).ToArray();return (rows,layout);
    }
    public static void Run(Action<bool,string> assert)
    {
        var (p,l)=Sample();var first=p[9];var second=p[10];
        IReadOnlyList<Issue> Check(Paragraph[]? rows=null,LayoutParagraph[]? layout=null)=>
            BodyFormatRules.Check(rows??p,layout??l,DocumentProfile.State).Concat(BodyParagraphRules.Check(rows??p,layout??l,DocumentProfile.State))
            .Concat(BodySpacingRules.Check(rows??p,layout??l,DocumentProfile.State)).ToArray();
        var found=Check();
        assert(found.Count(i=>i.Kind==FixKind.BodyFormat)==3 && found.Count(i=>i.Kind==FixKind.BodyParagraph)==1 &&
            found.Count(i=>i.Kind==FixKind.BodyGap)==2 && found.Count(i=>i.Kind==FixKind.BodyLineSpacing)==3,
            "A named document without Kính gửi gains all four body repair families only behind two proven ordinary justified/indented opening paragraphs");
        assert(found.Single(i=>i.Kind==FixKind.BodyParagraph).Start==p[11].Start,
            "The proven opening layout stays unchanged while a later ordinary paragraph can receive a layout repair");
        foreach(var unsafeRow in new[]{first with {TextEditable=false},first with {InTable=true},first with {Page=2},
            first with {Outline=first.Outline! with {BodyIndent=null}},first with {Outline=first.Outline! with {BodyIndent=new(720,0,0,0)}},
            first with {Outline=first.Outline! with {BodyIndent=new(0,0,720,100)}},first with {Outline=first.Outline! with {BodyIndent=new(0,40,720,0)}},
            first with {Outline=first.Outline! with {BodyIndent=new(0,0,200,0)}},first with {Outline=first.Outline! with {Alignment=0}},
            first with {Outline=first.Outline! with {HeadingLevel=1}},first with {Outline=new(OutlineScope.AutomaticList)},
            first with {Text="1. Nội dung văn bản đã được kiểm tra đầy đủ.\r"},first with {Text="Căn cứ văn bản mẫu đã được kiểm tra.\r"}})
        {var rows=p.ToArray();rows[9]=unsafeRow;assert(Check(rows).Count==0,"An uncertain, inset, hanging, later-page, numbered or legal-preamble opening cannot grant downstream body writes");}
        foreach(var unsafeRow in new[]{first with {FormatObserved=false},first with {FormatEvidence=null},
            first with {ParagraphStyle=null},first with {ParagraphStyle=first.ParagraphStyle! with {First=0}},
            first with {ParagraphStyle=first.ParagraphStyle! with {Alignment=0}},
            first with {FormatEvidence=new("old",first.FormatEvidence!.Fonts)},first with {FormatEvidence=new(first.Text.TrimEnd('\r'),[new("Arial",12,-1,0,255)])},
            first with {FormatEvidence=new(first.Text.TrimEnd('\r'),[new("Arial",12,0,-1,255)])},first with {FormatEvidence=new(first.Text.TrimEnd('\r'),[new("",12,0,0,255)])}})
        {
            var rows=p.ToArray();rows[9]=unsafeRow;
            assert(BodyFormatRules.ObservationTargets(rows,l,DocumentProfile.State).Count==3 && Check(rows).Count==0,
                "Provisional font observation never grants repairs when either opening anchor has unknown or emphasized typography");
        }
        var secondUnknown=p.ToArray();secondUnknown[10]=second with {FormatObserved=false};
        assert(Check(secondUnknown).Count==0,"A single proven opening paragraph cannot substitute for the second anchor");
        foreach(var layout in new[]{l.Where(x=>x.Start!=first.Start).ToArray(),l.Select(x=>x.Start==second.Start?x with {Centered=true,ParagraphCentered=true}:x).ToArray(),
            l.Select(x=>x.Start==first.Start?x with {Text="Old body text\r"}:x).ToArray()})
            assert(Check(layout:layout).Count==0,"Missing/mismatched or centered front-page opening geometry does not become body evidence");
        assert(Check(p[..^1]).Count==0,"A no-greeting opening still requires a later signature/recipient end boundary");
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(BodyFormatRules.RepairTargets(p,l,profile).Count==0,"No-greeting role evidence stays inside named State documents");
        var sourceIssue=found.Single(i=>i.Kind==FixKind.BodyFormat && i.Start==p[11].Start);
        bool rejected=false;try{CurrentTextPlan.Validate([sourceIssue],Check(secondUnknown));}catch(InvalidOperationException){rejected=true;}
        assert(rejected,"Losing an opening anchor invalidates a stale later-body selection even when that later text and font proposal are unchanged");
        const string shortText="Nội dung.";
        var shortened=p.ToArray();shortened[9]=first with {Text=shortText+"\r",FormatEvidence=new(shortText,first.FormatEvidence!.Fonts)};
        var shortLayout=l.Select(x=>x.Start==first.Start?x with {Text=shortText+"\r"}:x).ToArray();
        assert(Check(shortened,shortLayout).Any(i=>i.Kind==FixKind.BodyFormat && i.Start==p[11].Start),
            "An opening shortened by an approved duplicate deletion retains its strong layout role and does not hide unselected later-body defects");
        var correct=p.Select(row=>row.FormatEvidence is null?row:row with {FormatEvidence=new(row.Text.TrimEnd('\r'),row.FormatEvidence.Fonts.Select(f=>f with {Name="Times New Roman",Size=14,Color=0}))}).ToArray();
        assert(!BodyFormatRules.Check(correct,l,DocumentProfile.State).Any(),"Correct typography in a proven no-greeting body does not create warnings");
        Xml(assert);
    }
    private static void Xml(Action<bool,string> assert)
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";const string text="Nội dung văn bản đã được kiểm tra đầy đủ.";
        foreach(int first in new[]{567,720})
        {
            var xml=new XDocument(new XElement(w+"document",new XElement(w+"styles",new XElement(w+"style",new XAttribute(w+"type","paragraph"),new XAttribute(w+"styleId","Body"),
                new XElement(w+"pPr",new XElement(w+"jc",new XAttribute(w+"val","both")),new XElement(w+"ind",new XAttribute(w+"firstLine",first))))),
                new XElement(w+"body",new XElement(w+"p",new XElement(w+"pPr",new XElement(w+"pStyle",new XAttribute(w+"val","Body")),new XElement(w+"bidi",new XAttribute(w+"val",0))),
                    new XElement(w+"r",new XElement(w+"t",text))))));
            OutlineParagraphInfo? found=null;WordTextProof.Read(xml.ToString(),[new(100,101+text.Length,text+"\r",1,false,false)],onOutline:(_,x)=>found=x);
            assert(found is {Alignment:3,BodyIndent:{Left:0,Right:0,Hanging:0} indent} && indent.First==first,
                "No-greeting anchors use inherited XML first-line/side indents and explicit left-to-right evidence, not a guessed aggregate indent");
        }
    }
}
