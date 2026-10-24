using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class CaseGeometryDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var page=new FixedPage {Width=794,Height=1123};var canvas=new Canvas {RenderTransform=new ScaleTransform(4d/3,4d/3)};
        var uri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf"));
        Glyphs Glyph(string text,double x,double y,double size)=>new(){FontUri=uri,UnicodeString=text,OriginX=x,OriginY=y,
            FontRenderingEmSize=size,Fill=Brushes.Black};
        var first=Glyph("Báo ",100,50,13);var last=Glyph("cáo",140,50,14);var subject=Glyph("Trích yếu",100,100,13);
        canvas.Children.Add(first);canvas.Children.Add(last);canvas.Children.Add(subject);page.Children.Add(canvas);
        var content=new PageContent();((IAddChild)content).AddChild(page);var doc=new FixedDocument();doc.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(doc);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);
        var target=new LineTarget(LineRole.Subject,100,"Trích yếu",10,150,true);
        var capital=new Issue(10,7,"Báo cáo","BÁO CÁO","",false,FixKind.DocumentTypeText);
        bool changed=false;ShapeLineFix? accepted=null;int commits=0,restores=0;
        CharacterStyleEvidence Styles()
        {
            string text=first.UnicodeString+last.UnicodeString;
            return new(text,text.Select((c,i)=>char.IsWhiteSpace(c)?null:new ObservedCharacterStyle(
                new("Times New Roman",(float)(i<first.UnicodeString.Length?first.FontRenderingEmSize:last.FontRenderingEmSize),-1,0,0),0)),new(1,0,0,0));
        }
        var expected=Styles().Project([capital],i=>"");
        StagedGeometryReview Review()
        {
            string text=first.UnicodeString+last.UnicodeString+subject.UnicodeString;
            var anchors=new Dictionary<Issue,PreviewAnchor>{{target.Probe,new(1,text,text.Length-target.Text.Length,target.Text.Length)}};
            WordLineShape[] shapes=accepted is null?[]:[new(1,100,1,new(accepted.Left,accepted.Top,accepted.Width),true)];
            var issues=LineInspection.Inspect(sequence,new PreviewHighlighter(),[target],shapes,anchors,changed?[]:[capital],LinePolicy.Decree30,false).Select(e=>e.Issue).ToArray();
            return new([target],issues,[],[]);
        }
        void Restore()
        {
            foreach(var path in canvas.Children.OfType<System.Windows.Shapes.Path>().ToArray())canvas.Children.Remove(path);
            first.UnicodeString="Báo ";last.UnicodeString="cáo";last.FontRenderingEmSize=14;changed=false;accepted=null;restores++;
        }
        foreach(bool drift in new[]{false,true})
        {
            var line=Review().LineIssues.Single();var selected=RepairDependencies.Expand([line],[line,capital]);
            var requests=selected.Select(i=>new DeferredRepair(i,i.LineFix is null?i.Replacement:i.Original)).ToArray();StagedGeometryReview? fresh=null;bool failed=false;
            try
            {
                StagedRepairBatch.Apply(requests,_=>fresh=Review(),r=>StagedGeometryResolver.Resolve(r,fresh!),plan=>
                {
                    if(StagedRepairBatch.Stage(plan[0])==RepairStage.Text)
                    {first.UnicodeString="BÁO ";last.UnicodeString="CÁO";if(drift)last.FontRenderingEmSize=13;changed=true;return;}
                    accepted=plan.Single().LineFix!;
                    canvas.Children.Add(new System.Windows.Shapes.Path {Data=new System.Windows.Media.LineGeometry(
                        new Point(accepted.Left,accepted.Top),new Point(accepted.Left+accepted.Width,accepted.Top)),Stroke=Brushes.Black,StrokeThickness=.5});
                },()=>
                {
                    assert(StagedGeometryResolver.Resolve(new(line,line.Original),Review()) is null,
                        "Case/Line verification first proves the actual fresh stroke even when a retained title style has drifted");
                    if(!expected.Matches(Styles()))throw new InvalidOperationException("Unselected title size changed");
                },()=>commits++,Restore);
            }
            catch(InvalidOperationException){failed=true;}
            assert(drift?failed && commits==0 && restores==1 && !changed && accepted is null && last.FontRenderingEmSize==14:
                !failed && commits==1 && restores==0 && changed && last.FontRenderingEmSize==14,
                "A valid real Line cannot conceal style loss during capitalization; the whole multi-stage action commits or restores together");
            Restore();commits=restores=0;
        }
    }
}
