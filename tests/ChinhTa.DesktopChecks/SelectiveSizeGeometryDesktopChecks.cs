using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class SelectiveSizeGeometryDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
        RunCase(assert,false);RunCase(assert,true);
    }
    private static void RunCase(Action<bool,string> assert,bool motto)
    {
        string text=motto?HeadingRules.Motto:"TRÍCH YẾU THỬ NGHIỆM";
        var page=new FixedPage {Width=794,Height=1123};var canvas=new Canvas {RenderTransform=new ScaleTransform(4d/3,4d/3)};
        var uri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf"));
        var left=new Glyphs {FontUri=uri,FontRenderingEmSize=12,OriginX=100,OriginY=100,UnicodeString=motto?"Độc lập - ":"TRÍCH YẾU ",Fill=Brushes.Black};
        var right=new Glyphs {FontUri=uri,FontRenderingEmSize=14,OriginX=180,OriginY=100,UnicodeString=motto?"Tự do - Hạnh phúc":"THỬ NGHIỆM",Fill=Brushes.Black};
        canvas.Children.Add(left);canvas.Children.Add(right);page.Children.Add(canvas);
        var content=new PageContent();((IAddChild)content).AddChild(page);var document=new FixedDocument();document.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(document);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);
        var target=new LineTarget(motto?LineRole.Motto:LineRole.Subject,100,text,90,150,true);var highlighter=new PreviewHighlighter();
        var anchors=new Dictionary<Issue,PreviewAnchor>{{target.Probe,new(1,text,0,text.Length)}};
        var font=motto?new Issue(100,text.Length,text,text,"",false,FixKind.StateMotto,Format:new(13,1,14,true)):
            new Issue(100,text.Length,text,text,"",false,FixKind.DocumentHeadingFormat,
                CharacterFormat:new(13,false,true,PreserveSize:true,RepairInvalidSizes:true));
        bool repaired=false;ShapeLineFix? accepted=null;
        StagedGeometryReview Review()
        {
            WordLineShape[] shapes=accepted is null?[]:[new(1,100,1,new(accepted.Left,accepted.Top,accepted.Width),true)];
            var issues=LineInspection.Inspect(sequence,highlighter,[target],shapes,anchors,repaired?[]:[font],LinePolicy.Decree30,false)
                .Select(e=>e.Issue).ToArray();
            return new([target],issues,[],[]);
        }
        var old=Review().LineIssues.Single();double width=old.LineFix!.Width;
        var selected=RepairDependencies.Expand([old],[old,font]);
        var requests=selected.Select(i=>new DeferredRepair(i,i.Original)).ToArray();
        int commits=0,restores=0;StagedGeometryReview? fresh=null;
        var result=StagedRepairBatch.Apply(requests,_=>fresh=Review(),r=>StagedGeometryResolver.Resolve(r,fresh!),plan=>
        {
            if(StagedRepairBatch.Stage(plan[0])==RepairStage.Text)
            {left.FontRenderingEmSize=13;right.OriginX=187;repaired=true;return;}
            accepted=plan.Single().LineFix!;
            assert(accepted.Width>width && right.FontRenderingEmSize==14,
                (motto?"Motto: ":"Subject: ")+"Fresh real glyph measurement after selective 12-to-13pt repair preserves 14pt and replaces provisional Line geometry");
            canvas.Children.Add(new System.Windows.Shapes.Path {Data=new System.Windows.Media.LineGeometry(
                new Point(accepted.Left,accepted.Top),new Point(accepted.Left+accepted.Width,accepted.Top)),Stroke=Brushes.Black,StrokeThickness=.5});
        },()=>
        {
            if(StagedGeometryResolver.Resolve(new(old,text),Review()) is not null)
                throw new InvalidOperationException("Selective size/Line result has not passed its real stroke proof");
        },()=>commits++,()=>restores++);
        assert(result==new RepairBatchResult(2,2,0) && commits==1 && restores==0 && Review().LineIssues.Count==0,
            (motto?"Motto: ":"Subject: ")+"Selective sizing and dependent Line complete one verified action with no stale format/Line issue");
    }
}
