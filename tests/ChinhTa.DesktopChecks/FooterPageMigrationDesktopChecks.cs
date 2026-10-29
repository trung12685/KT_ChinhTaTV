using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ChinhTa.Core;
using ChinhTa.Desktop;
using ShapePath=System.Windows.Shapes.Path;

public static class FooterPageMigrationDesktopChecks
{
    private static readonly double Width=210*96/25.4,Height=297*96/25.4;
    private static readonly PageFooterSource Source=new("Times New Roman",14,false,"000000");
    private static PageFooterMigration Migration(bool different=false)=>new(Source,null,different,2);
    private static Glyphs Glyph(string text,double x,double y)=>new()
    {
        FontUri=new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"times.ttf")),
        FontRenderingEmSize=14*96d/72,UnicodeString=text,Fill=Brushes.Black,OriginX=x,OriginY=y
    };
    private static FixedDocumentSequence Pages(bool after=false,Action<Glyphs,Canvas,int>? footer=null,
        bool different=false,int count=2,bool unrelated=true,Action<Glyphs>? note=null)
    {
        var document=new FixedDocument();
        for(int n=1;n<=count;n++)
        {
            var page=new FixedPage{Width=Width,Height=Height};
            var canvas=new Canvas();page.Children.Add(canvas);
            canvas.Children.Add(Glyph("Nội dung độc lập",120,150));
            canvas.Children.Add(new ShapePath{Data=new RectangleGeometry(new Rect(120,190,70,25)),Fill=Brushes.Blue});
            // This unrelated digit is never approved for removal just because it
            // shares the footer band with the selected PAGE field.
            if(unrelated){var digit=Glyph("7",80,Height-30);note?.Invoke(digit);canvas.Children.Add(digit);}
            if(!after || different && n==1)
            {
                var number=Glyph(n.ToString(System.Globalization.CultureInfo.InvariantCulture),380,Height-30);
                var group=new Canvas();group.Children.Add(number);footer?.Invoke(number,group,n);canvas.Children.Add(group);
            }
            if(after && n>1)
            {
                var header=Glyph(n.ToString(System.Globalization.CultureInfo.InvariantCulture),0,40);
                double center=(30*96/25.4+Width-15*96/25.4)/2;
                header.OriginX=center-header.ToGlyphRun().ComputeAlignmentBox().Width/2;canvas.Children.Add(header);
            }
            var content=new PageContent();((IAddChild)content).AddChild(page);document.Pages.Add(content);
        }
        var reference=new DocumentReference();reference.SetDocument(document);
        var sequence=new FixedDocumentSequence();sequence.References.Add(reference);return sequence;
    }
    private static bool Reject(Action action)
    {try{action();return false;}catch(InvalidOperationException){return true;}}
    public static void Run(Action<bool,string> assert)
    {
        var source=PageNumberPreviewVerifier.Capture(Pages(),20,Migration(),20);
        var target=Pages(after:true);
        PageNumberPreviewVerifier.VerifyNumbers(target,30,15,20);
        PageNumberPreviewVerifier.VerifyNoMigratedFooter(target,Migration(),20);
        var result=PageNumberPreviewVerifier.Capture(target,20);
        PageNumberPreviewVerifier.Preserve(source,result);
        assert(source.Body.Count==6 && source.Body.Count(v=>v.Text=="7")==2,
            "A PAGE-only migration excludes exactly one approved numeral per active page while retaining unrelated footer digits, body text and body shapes");
        assert(Reject(()=>PageNumberPreviewVerifier.Preserve(source,PageNumberPreviewVerifier.Capture(Pages(after:true,note:g=>g.Fill=Brushes.Red),20))),
            "An unselected footer digit cannot change paint while the selected PAGE field migrates");
        assert(Reject(()=>PageNumberPreviewVerifier.VerifyNoMigratedFooter(Pages(),Migration(),20)),
            "The rendered migration result rejects a PAGE numeral still present at the source footer");
        assert(Reject(()=>PageNumberPreviewVerifier.Preserve(source,PageNumberPreviewVerifier.Capture(Pages(),20))),
            "Removing the source-only whitelist from the after snapshot detects retained footer PAGE numerals");

        var invalid=new (string Name,Action<Glyphs,Canvas,int> Change)[]
        {
            ("missing glyph",(g,_,_)=>g.UnicodeString=""),
            ("wrong cached physical value",(g,_,_)=>g.UnicodeString="8"),
            ("hidden glyph",(g,_,_)=>g.Opacity=0),
            ("hidden ancestor",(_,c,_)=>c.Visibility=Visibility.Hidden),
            ("faded glyph",(g,_,_)=>g.Opacity=.5),
            ("faded ancestor",(_,c,_)=>c.Opacity=.5),
            ("wrong color",(g,_,_)=>g.Fill=Brushes.Red),
            ("translucent fill",(g,_,_)=>g.Fill=new SolidColorBrush(Color.FromArgb(128,0,0,0))),
            ("wrong size",(g,_,_)=>g.FontRenderingEmSize=13*96d/72),
            ("wrong family",(g,_,_)=>g.FontUri=new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf"))),
            ("bold simulation",(g,_,_)=>g.StyleSimulations=StyleSimulations.BoldSimulation),
            ("bold typeface",(g,_,_)=>g.FontUri=new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"timesbd.ttf"))),
            ("italic simulation",(g,_,_)=>g.StyleSimulations=StyleSimulations.ItalicSimulation),
            ("partial clip",(g,c,_)=>c.Clip=new RectangleGeometry(new Rect(g.OriginX+2,0,Width,Height))),
            ("unknown mask",(_,c,_)=>c.OpacityMask=new LinearGradientBrush(Colors.Black,Colors.White,0)),
            ("unknown effect",(g,_,_)=>g.Effect=new DropShadowEffect()),
            ("mirrored source",(g,c,_)=>c.RenderTransform=new MatrixTransform(-1,0,0,1,2*g.OriginX+10,0)),
            ("crosses body boundary",(g,_,_)=>g.OriginY=Height-20*96/25.4+3),
            ("outside page",(g,_,_)=>g.OriginY=Height+8),
            ("ambiguous duplicated source",(g,c,_)=>c.Children.Add(Glyph(g.UnicodeString,g.OriginX+30,g.OriginY)))
        };
        foreach(var test in invalid)
            assert(Reject(()=>PageNumberPreviewVerifier.Capture(Pages(footer:test.Change),20,Migration(),20)),
                "A footer migration requires one fully visible numeral matching its approved physical page and source style: "+test.Name);

        var colored=new PageFooterMigration(Source with {Color="FF0000"},null,false,2);
        var coloredBefore=PageNumberPreviewVerifier.Capture(Pages(footer:(g,_,_)=>g.Fill=Brushes.Red),20,colored,20);
        PageNumberPreviewVerifier.Preserve(coloredBefore,result);
        assert(true,"The source-only binding accepts the actual approved known source color before normalizing PAGE to black in the new header");
        var italic=new PageFooterMigration(Source with {Italic=true},null,false,2);
        var italicBefore=PageNumberPreviewVerifier.Capture(Pages(footer:(g,_,_)=>g.StyleSimulations=StyleSimulations.ItalicSimulation),20,italic,20);
        PageNumberPreviewVerifier.Preserve(italicBefore,result);
        assert(true,"An approved italic source PAGE can migrate to the required upright header without exempting any other footer paint");

        var bold=new PageFooterMigration(Source with {Bold=true},null,false,2);
        foreach(bool simulation in new[]{false,true})
        {
            var boldBefore=PageNumberPreviewVerifier.Capture(Pages(footer:(g,_,_)=>
            {if(simulation)g.StyleSimulations=StyleSimulations.BoldSimulation;
             else g.FontUri=new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"timesbd.ttf"));}),20,bold,20);
            PageNumberPreviewVerifier.Preserve(boldBefore,result);
            assert(true,"Approved bold source PAGE is bound to its actual typeface weight or bold simulation before migration");
        }
        assert(Reject(()=>PageNumberPreviewVerifier.Capture(Pages(),20,bold,20)),
            "A normal glyph cannot substitute for an XML-approved bold footer PAGE result");

        var firstSource=PageNumberPreviewVerifier.Capture(Pages(different:true),20,Migration(true),20);
        var firstTarget=Pages(after:true,different:true);
        PageNumberPreviewVerifier.VerifyNumbers(firstTarget,30,15,20);
        PageNumberPreviewVerifier.VerifyNoMigratedFooter(firstTarget,Migration(true),20);
        PageNumberPreviewVerifier.Preserve(firstSource,PageNumberPreviewVerifier.Capture(firstTarget,20));
        assert(firstSource.Body.Count==7 && firstSource.Body.Any(v=>v.Page==1 && v.Text=="1"),
            "Different-first migration retains the independent visible first footer and excludes only the active default footer on later pages");
        assert(Reject(()=>PageNumberPreviewVerifier.Preserve(firstSource,PageNumberPreviewVerifier.Capture(Pages(after:true,different:true,footer:(g,_,n)=>{if(n==1)g.Fill=Brushes.Red;}),20))),
            "An independent first-footer numeral remains subject to body preservation after the default footer PAGE migrates");
        foreach(double margin in new[]{double.NaN,double.PositiveInfinity,0d,-1d})
            assert(Reject(()=>PageNumberPreviewVerifier.Capture(Pages(),20,Migration(),margin)) &&
                Reject(()=>PageNumberPreviewVerifier.VerifyNoMigratedFooter(target,Migration(),margin)),
                "Unknown or invalid footer boundaries cannot fabricate an approved source-only exclusion");
        assert(Reject(()=>PageNumberPreviewVerifier.Capture(Pages(count:1),20,Migration(),20)) &&
            Reject(()=>PageNumberPreviewVerifier.VerifyNoMigratedFooter(Pages(after:true,count:3),Migration(),20)),
            "Both source and target page counts must match the immutable footer migration plan");
        bool restored=false;
        try{PageLayoutBatch.Apply(()=>{},()=>{},()=>PageNumberPreviewVerifier.Capture(Pages(footer:(g,_,_)=>g.Opacity=0),20,Migration(),20),
            ()=>throw new InvalidOperationException("An unproven footer must not commit"),()=>restored=true);}
        catch(InvalidOperationException){}
        assert(restored,"An unproven footer source aborts the whole selected repair through the existing rollback boundary");
    }
}
