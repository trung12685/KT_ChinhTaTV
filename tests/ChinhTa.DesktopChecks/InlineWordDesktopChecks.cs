using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class InlineWordDesktopChecks
{
    private const string Text="Sai nội dung.";
    private static FixedDocumentSequence Page(out FixedPage page,Action<Glyphs>? change=null,bool duplicate=false)
    {
        page=new FixedPage{Width=794,Height=1123};
        Glyphs Glyph()=>new(){FontUri=new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"times.ttf")),FontRenderingEmSize=18,UnicodeString=Text,OriginX=100,OriginY=120,Fill=Brushes.Black};
        var glyph=Glyph();change?.Invoke(glyph);page.Children.Add(glyph);if(duplicate)page.Children.Add(Glyph());
        var content=new PageContent();((IAddChild)content).AddChild(page);var document=new FixedDocument();document.Pages.Add(content);var reference=new DocumentReference();reference.SetDocument(document);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);return sequence;
    }
    public static void Run(Action<bool,string> assert)
    {
        var document=Page(out var page);var highlighter=new PreviewHighlighter();var anchor=new PreviewAnchor(1,Text,4,3);
        var bounds=highlighter.MeasureReference(document,anchor).Single();var point=new Point(bounds.Left+bounds.Width/2,bounds.Top+bounds.Height/2);
        var hit=highlighter.HitWord(document,1,point);
        assert(hit is {Word:"nội"} && hit.Anchor.Offset==4 && ReferenceEquals(hit.Page,page),"A real XPS glyph hit selects the clicked complete Vietnamese word, without an issue list or automatic suggestion");
        assert(highlighter.HitWord(document,1,new Point(10,10)) is null && highlighter.HitWord(document,1,new Point(double.NaN,10)) is null && highlighter.HitWord(document,0,point) is null,"Blank space, invalid coordinates or an unknown page cannot pick a nearby word by guesswork");
        var space=highlighter.MeasureReference(document,new(1,Text,3,1)).Single();
        assert(highlighter.HitWord(document,1,new Point(space.Left+space.Width/2,space.Top+space.Height/2)) is null,"Clicking the gap between words cannot open an editor on either neighboring word");
        foreach(Action<Glyphs> modify in new Action<Glyphs>[] {g=>g.Opacity=0,g=>g.Visibility=Visibility.Hidden,g=>g.RenderTransform=new RotateTransform(20),g=>g.Clip=new RectangleGeometry(new Rect(100,100,5,25))})
        {
            var unsupported=Page(out _,modify);
            assert(new PreviewHighlighter().HitWord(unsupported,1,point) is null,"Hidden, rotated or incompletely exposed glyphs never authorize an inline hit");
        }
        assert(new PreviewHighlighter().HitWord(Page(out _,duplicate:true),1,point) is null,"Overlapping distinct glyph occurrences cannot silently choose the first word");
        var owner=new Window{Width=1050,Height=700};var viewer=new DocumentViewer{Document=document};owner.Content=viewer;
        try
        {
            owner.Show();owner.UpdateLayout();owner.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
            var viewerPoint=page.TranslatePoint(point,viewer);var original=viewer.InputHitTest(viewerPoint) as DependencyObject;
            var routed=original is null?null:highlighter.HitWord(document,original,element=>viewer.TranslatePoint(viewerPoint,(UIElement)element));
            assert(routed?.Word=="nội","The actual DocumentViewer visual tree routes a page mouse hit to the correct page and word");
            var target=ManualModeChecks.Target();var draft=new InlineWordDraft(target,4,"nội");int writes=0,closed=0;
            var editor=new InlineWordEditor(owner,hit!,draft,_=>{writes++;return new(true,true,null);},()=>closed++);
            try
            {
                editor.Open();assert(editor.IsOpen && editor.Editor.Text=="nội" && !editor.ApplyButton.IsEnabled && !editor.Dirty,"The anchored editor begins with only the clicked word selected and no pending write");
                editor.Apply();assert(writes==0,"Opening or applying an unchanged inline editor never writes Word");
                editor.Editor.Text="mới";assert(editor.Dirty && editor.ApplyButton.IsEnabled && !editor.TryClose(()=>false) && editor.IsOpen,"A dirty inline draft survives cancellation of a mode/open/close action");
                editor.DetachAnchor();assert(editor.IsOpen && editor.Editor.Text=="mới","Rebuilding or closing the XPS view detaches the anchor without destroying the user's draft");
                editor.Apply();editor.Apply();assert(writes==1 && closed==1 && !editor.IsOpen,"A successful inline commit closes once and cannot replay the word write");
            }
            finally{editor.ForceClose();}
            var failed=new InlineWordEditor(owner,hit!,draft,_=>{writes++;return new(false,false,"Lỗi ghi mẫu.");},()=>{});
            try
            {
                failed.Open();failed.Editor.Text="mới";failed.Apply();int after=writes;failed.Apply();
                assert(failed.IsOpen && failed.Editor.Text=="mới" && !failed.Editor.IsReadOnly && !failed.ApplyButton.IsEnabled && writes==after && failed.Feedback.Text.Contains("bản nháp",StringComparison.OrdinalIgnoreCase),"A failed inline write retains the copyable draft and actual error while blocking stale retries");
                assert(failed.TryClose(()=>true),"Discarding a failed inline draft requires explicit user confirmation");
            }
            finally{failed.ForceClose();}
            var scanFailed=new InlineWordEditor(owner,hit!,draft,_=>new(true,false,"Quét lại lỗi."),()=>{});
            try
            {
                scanFailed.Open();scanFailed.Editor.Text="mới";scanFailed.Apply();
                assert(scanFailed.IsOpen && scanFailed.Editor.IsReadOnly && !scanFailed.ApplyButton.IsEnabled && scanFailed.Feedback.Text.Contains("Đã ghi",StringComparison.Ordinal),"A committed word with failed rescan is reported as committed and cannot be submitted again");
            }
            finally{scanFailed.ForceClose();}
        }
        finally{owner.Close();}
        var suite=OfficeFocusedCheck.Cases.Single(s=>s.Id=="inline-word");
        assert(OfficeFocusedCheck.Scenario(suite.Id).Method.DeclaringType==typeof(OfficeInlineWordCheck) && WordCheckCommand.TryParse(["--word-check",suite.Id],out var selected) && selected.Single()==suite,"The inline-word real Word scenario is independently selectable and reaches its own glyph/native/checkpoint fixture");
    }
}
