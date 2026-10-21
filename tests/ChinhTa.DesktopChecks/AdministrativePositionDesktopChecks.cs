using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class AdministrativePositionDesktopChecks
{
    private static readonly Uri Font=new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf"));
    private static FixedDocumentSequence Build(IEnumerable<Glyphs> glyphs,bool framed=false,bool splitReference=false)
    {
        var page=new FixedPage {Width=794,Height=1123};
        var root=new Canvas {RenderTransform=new ScaleTransform(4d/3,4d/3)};page.Children.Add(root);
        var items=glyphs.ToArray();
        if(!framed)foreach(var glyph in items)root.Children.Add(glyph);
        else
        {
            for(int side=0;side<2;side++)
            {
                var cell=new Canvas {Clip=new RectangleGeometry(new Rect(side==0?20:280,30,side==0?260:310,110))};
                root.Children.Add(cell);
                for(int i=side*3;i<Math.Min(items.Length,side*3+3);i++)
                {
                    if(splitReference && i==0)
                    {
                        var other=new Canvas {Clip=new RectangleGeometry(new Rect(21,30,259,110))};
                        root.Children.Add(other);other.Children.Add(items[i]);
                    }
                    else cell.Children.Add(items[i]);
                }
            }
        }
        var content=new PageContent();((IAddChild)content).AddChild(page);
        var document=new FixedDocument();document.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(document);
        var sequence=new FixedDocumentSequence();sequence.References.Add(reference);return sequence;
    }
    private static Glyphs Glyph(string text,double center,double baseline,double size=14)
    {
        var glyph=new Glyphs {FontUri=Font,FontRenderingEmSize=size,OriginX=0,OriginY=baseline,UnicodeString=text,Fill=Brushes.Black};
        double width=glyph.ToGlyphRun().AdvanceWidths.Sum();glyph.OriginX=center-width/2;return glyph;
    }
    public static void Run(Action<bool,string> assert)
    {
        string[] text=["AGENCY TEST","UNIT TEST","Số:        /BC-ABC","NATIONAL HEADING","MOTTO TEST","Địa danh, ngày        tháng 10 năm 2026"];
        var glyphs=text.Select((s,i)=>Glyph(s,i<3?150:400,i%3==0?50:i%3==1?70:110,i==2?13:14)).ToArray();
        var page=Build(glyphs);string story=string.Join("\r",text)+"\r";
        PositionTextAnchor Source(int i)=>new(i*100,text[i]);
        var number=new AdministrativePositionTarget(AdministrativePositionRole.Number,Source(2),[Source(0),Source(1)]);
        var date=new AdministrativePositionTarget(AdministrativePositionRole.Date,Source(5),[Source(3),Source(4)]);
        var targets=new[]{number,date};
        var anchors=new Dictionary<Issue,PreviewAnchor>();
        foreach(var probe in targets.SelectMany(t=>t.References.Select(r=>r.Probe).Append(t.Probe)))
        {
            int offset=story.IndexOf(probe.Original,StringComparison.Ordinal);
            anchors[probe]=WordPreviewAnchor.Create(1,story,story[..offset],probe.Original);
        }
        var preview=new PreviewHighlighter();
        var n=preview.MeasureSingleLine(page,anchors[number.Probe]);var d=preview.MeasureSingleLine(page,anchors[date.Probe]);
        assert(n is not null && d is not null && Math.Abs(n.Baseline-110)<.01 && Math.Abs(d.Baseline-110)<.01,
            "WPF glyph baselines transform into page points and remain equal across 13/14pt font heights");
        assert(AdministrativePositionInspection.Inspect(page,preview,targets,anchors).Count==0,
            "Verified single-line number/date and complete centered reference blocks have no position warning");
        glyphs[2].OriginX+=8;
        var shifted=AdministrativePositionInspection.Inspect(page,new PreviewHighlighter(),targets,anchors);
        assert(shifted.Any(x=>x.Issue.Start==number.Row.Start && x.Issue.Message.Contains("lệch tâm")) && shifted.All(x=>!x.Issue.CanFix),
            "A real rendered center shift creates a manual warning without an automatic Word edit");
        glyphs[2].OriginX-=8;glyphs[5].OriginY+=10;
        assert(AdministrativePositionInspection.Inspect(page,new PreviewHighlighter(),targets,anchors).Any(x=>x.Issue.Title=="Số và ngày tháng chưa cùng dòng"),
            "A rendered baseline shift creates the number/date same-row warning");
        glyphs[5].OriginY-=10;
        var missing=new Dictionary<Issue,PreviewAnchor>(anchors);missing.Remove(date.References[0].Probe);
        var incomplete=AdministrativePositionInspection.Inspect(page,new PreviewHighlighter(),targets,missing);
        assert(incomplete.Count==1 && incomplete[0].Issue.Message.Contains("Chưa đủ") && !incomplete[0].Issue.CanFix,
            "A missing rendered reference is explicitly unverified and cannot yield a same-row pass");
        var noRow=new Dictionary<Issue,PreviewAnchor>(anchors);noRow.Remove(date.Probe);
        var unmapped=AdministrativePositionInspection.Inspect(page,new PreviewHighlighter(),targets,noRow);
        assert(unmapped.Any(x=>x.Issue.Start==date.Row.Start && x.Anchor.Length==0),
            "A missing Word row anchor cannot fall back to highlighting text at an unrelated occurrence");
        var wrongPage=new Dictionary<Issue,PreviewAnchor>(anchors) {[date.Probe]=anchors[date.Probe] with {Page=2}};
        assert(AdministrativePositionInspection.Inspect(page,new PreviewHighlighter(),targets,wrongPage).Any(x=>x.Issue.Message.Contains("Chưa đủ")),
            "Cross-page anchors cannot borrow geometry from page one");
        glyphs[4].OriginX+=8;
        assert(AdministrativePositionInspection.Inspect(page,new PreviewHighlighter(),targets,anchors).Any(x=>x.Issue.Message.Contains("chưa cùng tâm")),
            "Divergent national/motto centers are reported as ambiguous instead of choosing one reference");
        glyphs[4].OriginX-=8;
        glyphs[5].Clip=new RectangleGeometry(new Rect(glyphs[5].OriginX,70,5,60));
        assert(new PreviewHighlighter().MeasureSingleLine(page,anchors[date.Probe]) is null,
            "Clipped glyphs cannot establish complete administrative position evidence");
        glyphs[5].Clip=null;
        const string wrapped="Địa danh ngày tháng";
        var a=Glyph(wrapped[..8],150,100);var b=Glyph(wrapped[8..],150,125);
        var wrappedPage=Build([a,b]);
        assert(new PreviewHighlighter().MeasureSingleLine(wrappedPage,new(1,wrapped,0,wrapped.Length)) is null,
            "A wrapped paragraph is not measured using only its last visual line");
        b.OriginY=100;
        b.OriginX=a.OriginX+a.ToGlyphRun().AdvanceWidths.Sum();
        assert(new PreviewHighlighter().MeasureSingleLine(wrappedPage,new(1,wrapped,0,wrapped.Length)) is not null,
            "Multiple glyph runs on one baseline can establish one measured row");
        var trusted=targets.Select(t=>t with {ReferencesCentered=true}).ToArray();
        FixedDocumentSequence Framed(bool shifted,bool split=false)=>Build(text.Select((s,i)=>
            Glyph(s,(i<3?150:435)+(shifted && i%3==2?10:0),i%3==0?50:i%3==1?70:110,i==2?13:14)),true,split);
        var offsetPage=Framed(true);
        var offered=AdministrativePositionInspection.Inspect(offsetPage,new PreviewHighlighter(),trusted,anchors);
        assert(offered.Count==2 && offered.All(x=>x.Issue.CanFix && x.Issue.CenterFix is not null),
            "Real single-line offsets with aligned reference blocks in the same cell clips offer two center repairs");
        var prior=AdministrativePositionInspection.MeasureCentering(offsetPage,new PreviewHighlighter(),trusted[0],anchors);
        var centered=Framed(false);
        var corrected=AdministrativePositionInspection.MeasureCentering(centered,new PreviewHighlighter(),trusted[0],anchors);
        assert(AdministrativeCentering.Verify(prior,corrected) &&
            AdministrativePositionInspection.Inspect(centered,new PreviewHighlighter(),trusted,anchors).Count==0,
            "Fresh measured centering passes and the corrected items leave the issue list");
        assert(!AdministrativeCentering.Verify(prior,prior),"Successful Word properties cannot replace actual post-save glyph movement");
        assert(AdministrativePositionInspection.Inspect(offsetPage,new PreviewHighlighter(),targets,anchors).All(x=>!x.Issue.CanFix),
            "Cell clipping alone cannot authorize repair when reference paragraph centering is unproven");
        var split=AdministrativePositionInspection.Inspect(Framed(true,true),new PreviewHighlighter(),trusted,anchors);
        assert(split.Single(x=>x.Issue.Start==number.Row.Start).Issue.CanFix==false,
            "Reference text in a different clip frame cannot authorize a center edit");
        var verticalGlyphs=text.Select((s,i)=>Glyph(s,i<3?150:435,(i%3==0?50:i%3==1?70:110)+(i==2?1:0),i==2?13:14));
        var movedDown=Build(verticalGlyphs,true);
        assert(!AdministrativeCentering.Verify(prior,AdministrativePositionInspection.MeasureCentering(movedDown,new PreviewHighlighter(),trusted[0],anchors)),
            "A horizontally centered result that moves the baseline fails final acceptance");
        var lost=AdministrativePositionInspection.MeasureCentering(centered,new PreviewHighlighter(),trusted[0],new Dictionary<Issue,PreviewAnchor>());
        assert(!AdministrativeCentering.Verify(prior,lost),"Losing post-save anchors rolls back instead of mistaking missing warnings for success");
        var rotated=Glyph(wrapped,150,100);rotated.RenderTransform=new RotateTransform(5);
        var rotatedPage=Build([rotated]);
        assert(new PreviewHighlighter().MeasureSingleLine(rotatedPage,new(1,wrapped,0,wrapped.Length)) is null,
            "Rotated glyphs stay unverified rather than producing axis-aligned position claims");
    }
}
