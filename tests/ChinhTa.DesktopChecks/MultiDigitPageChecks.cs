using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class MultiDigitPageChecks
{
    private static readonly double W=210*96/25.4,H=297*96/25.4;
    private static Glyphs Glyph(string value,double x,double y)=>new(){FontUri=new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"times.ttf")),
        FontRenderingEmSize=14*96d/72,UnicodeString=value,OriginX=x,OriginY=y,Fill=Brushes.Black};
    private static FixedDocumentSequence Scene(bool after=false,Action<Glyphs,int,int>? change=null,bool split=true,int count=12)
    {
        var document=new FixedDocument();
        for(int n=1;n<=count;n++)
        {
            var page=new FixedPage{Width=W,Height=H};var canvas=new Canvas();page.Children.Add(canvas);
            canvas.Children.Add(Glyph("Nội dung cần giữ",120,150));
            if(!after)
            {
                string text=n.ToString(System.Globalization.CultureInfo.InvariantCulture);double x=350;int index=0;
                foreach(string chunk in split?text.Select(c=>c.ToString()):new[]{text})
                {
                    var glyph=Glyph(chunk,x,H-30);x+=glyph.ToGlyphRun().AdvanceWidths.Sum();change?.Invoke(glyph,n,index++);canvas.Children.Add(glyph);
                }
            }
            else if(n>1)
            {
                var glyph=Glyph(n.ToString(System.Globalization.CultureInfo.InvariantCulture),0,40);
                glyph.OriginX=(30*96/25.4+W-15*96/25.4)/2-glyph.ToGlyphRun().ComputeAlignmentBox().Width/2;canvas.Children.Add(glyph);
            }
            var content=new PageContent();((IAddChild)content).AddChild(page);document.Pages.Add(content);
        }
        var reference=new DocumentReference();reference.SetDocument(document);var result=new FixedDocumentSequence();result.References.Add(reference);return result;
    }
    private static PageFooterMigration Plan(int count=12)=>new(new("Times New Roman",14,false,"000000"),null,false,count);
    private static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    public static void Run(Action<bool,string> assert)
    {
        foreach(int count in new[]{12,101})
        foreach(bool split in new[]{false,true})
        {
            var before=PageNumberPreviewVerifier.Capture(Scene(split:split,count:count),20,Plan(count),20);
            var after=Scene(after:true,count:count);PageNumberPreviewVerifier.VerifyNumbers(after,30,15,20);
            PageNumberPreviewVerifier.VerifyNoMigratedFooter(after,Plan(count),20);
            PageNumberPreviewVerifier.Preserve(before,PageNumberPreviewVerifier.Capture(after,20));
            assert(before.Body.Count==count,"Multi-digit PAGE migration binds every complete or split numeral and preserves all body paint through page "+count);
        }
        foreach(var change in new Action<Glyphs,int,int>[] {
            (g,n,i)=>{if(n==10 && i==1)g.OriginX+=20;},
            (g,n,i)=>{if(n==10 && i==1)g.OriginY+=4;},
            (g,n,i)=>{if(n==10 && i==1)g.OriginX-=4;},
            (g,n,i)=>{if(n==10 && i==1)g.Opacity=0;},
            (g,n,i)=>{if(n==10 && i==1)g.Fill=Brushes.Red;},
            (g,n,i)=>{if(n==10 && i==1)g.UnicodeString="00";},
            (g,n,i)=>{if(n==10 && i==0)g.UnicodeString="11";},
            (g,n,i)=>{if(n==10 && i==1)g.StyleSimulations=StyleSimulations.BoldSimulation;}})
            assert(Reject(()=>PageNumberPreviewVerifier.Capture(Scene(change:change),20,Plan(),20)),
                "A multi-digit source cannot bind missing, displaced, overlapping, extra or differently styled numeral chunks");
        assert(Reject(()=>PageNumberPreviewVerifier.VerifyNoMigratedFooter(Scene(),Plan(),20)),
            "After migration, split footer numerals cannot evade the residual PAGE check");
    }
}
