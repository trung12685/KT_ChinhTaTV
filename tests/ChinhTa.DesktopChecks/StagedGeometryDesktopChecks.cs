using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class StagedGeometryDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string text="TRÍCH YẾU THỬ NGHIỆM";
        var page=new FixedPage {Width=794,Height=1123};var canvas=new Canvas {RenderTransform=new ScaleTransform(4d/3,4d/3)};
        var glyph=new Glyphs {FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf")),
            FontRenderingEmSize=13,OriginX=100,OriginY=100,UnicodeString=text,Fill=Brushes.Black};
        canvas.Children.Add(glyph);page.Children.Add(canvas);
        var content=new PageContent();((IAddChild)content).AddChild(page);var doc=new FixedDocument();doc.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(doc);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);
        int cp=1000,commits=0,restores=0;ShapeLineFix? accepted=null;
        var preview=new PreviewHighlighter();
        StagedGeometryReview Review()
        {
            var target=new LineTarget(LineRole.Subject,cp,text,cp-1,cp+text.Length+2,true);
            var anchors=new Dictionary<Issue,PreviewAnchor>{{target.Probe,new(1,text,0,text.Length)}};
            WordLineShape[] shapes=accepted is null?[]:[new(1,cp,1,new(accepted.Left,accepted.Top,accepted.Width),true)];
            var warnings=LineInspection.Inspect(sequence,preview,[target],shapes,anchors,[],LinePolicy.Decree30,false).Select(e=>e.Issue).ToArray();
            return new([target],warnings,[],[]);
        }
        var missing=Review().LineIssues.Single();double oldWidth=missing.LineFix!.Width;
        var order=new Issue(10,1,"5","3","",false,FixKind.OutlineOrder);
        var marker=new Issue(20,2,"- ","","",false,FixKind.OutlineMarker);
        DeferredRepair[] selection=[new(order,"3"),new(marker,""),new(missing,text)];StagedGeometryReview? measured=null;
        void Restore()
        {
            foreach(var path in canvas.Children.OfType<System.Windows.Shapes.Path>().ToArray())canvas.Children.Remove(path);
            glyph.FontRenderingEmSize=13;cp=1000;accepted=null;restores++;
        }
        RepairBatchResult Apply(bool displace)
        {
            return StagedRepairBatch.Apply(selection,_=>measured=Review(),r=>StagedGeometryResolver.Resolve(r,measured!),plan=>
            {
                if(StagedRepairBatch.Stage(plan[0])==RepairStage.Text){cp=998;glyph.FontRenderingEmSize=18;return;}
                var fix=plan.Single().LineFix!;
                assert(plan.Single().Start==998 && fix.Width>oldWidth*1.2,
                    "Mixed-stage Line repair uses fresh CP and real glyph width after text/layout changes");
                accepted=fix;
                canvas.Children.Add(new System.Windows.Shapes.Path {Data=new System.Windows.Media.LineGeometry(
                    new Point(fix.Left,fix.Top+(displace?20:0)),new Point(fix.Left+fix.Width,fix.Top+(displace?20:0))),
                    Stroke=Brushes.Black,StrokeThickness=.5});
            },()=>
            {
                if(StagedGeometryResolver.Resolve(selection[2],Review()) is not null)
                    throw new InvalidOperationException("Current stroke still fails");
            },()=>commits++,Restore);
        }
        var result=Apply(false);
        assert(result==new RepairBatchResult(3,3,0) && commits==1 && restores==0 && Review().LineIssues.Count==0,
            "Two outline families and a Line finish one action only after the actual fresh XPS stroke passes");
        Restore();commits=0;restores=0;bool failed=false;
        try{Apply(true);}catch(InvalidOperationException){failed=true;}
        assert(failed && commits==0 && restores==1 && cp==1000 && glyph.FontRenderingEmSize==13 && accepted is null,
            "A declared Shape with the wrong real stroke rolls back prior stages and never commits success");
        var unknown=Review();
        assert(unknown.LineIssues.Count==1 && unknown.LineIssues[0].CanFix,"Restoration leaves the original genuine missing-Line warning available");
    }
}
