using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Core;
using ChinhTa.Desktop;
using LineGeometry = ChinhTa.Core.LineGeometry;

static class Program
{
    const string Text="Độc lập – Tự do – Hạnh phúc";
    static int checks;
    static void Assert(bool condition,string name)
    {
        if(!condition)throw new InvalidOperationException(name);
        Console.WriteLine("PASS "+name);checks++;
    }
    static FixedDocumentSequence Page(Geometry? clip, bool split=false, Action<Canvas>? built=null)
    {
        var page=new FixedPage {Width=794,Height=1123};
        var root=new Canvas {RenderTransform=new ScaleTransform(4d/3,4d/3)};
        page.Children.Add(root);
        void Add(string text,double x,Geometry? region)
        {
            var canvas=new Canvas {Clip=region};root.Children.Add(canvas);
            canvas.Children.Add(new Glyphs {FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf")),
                FontRenderingEmSize=14,OriginX=x,OriginY=95.064,UnicodeString=text,Fill=Brushes.Black});
        }
        if(split)
        {
            Add(Text[..10],325.99,clip);
            Add(Text[10..],400,new RectangleGeometry(new Rect(85,56.67,191,104)));
        }
        else Add(Text,325.99,clip);
        built?.Invoke(root);
        var content=new PageContent();((IAddChild)content).AddChild(page);
        var document=new FixedDocument();document.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(document);
        var sequence=new FixedDocumentSequence();sequence.References.Add(reference);
        return sequence;
    }
    [STAThread]
    static void Main(string[] args)
    {
        if(args.Length==2 && args[0]=="--verify-report-package")
        {
            ProgressReportDesktopChecks.VerifyPackage(args[1],Assert);
            Console.WriteLine($"{checks} package checks passed.");
            return;
        }
        var anchor=new PreviewAnchor(1,Text,0,Text.Length);
        var clip=new RectangleGeometry(new Rect(276.2,56.67,290.55,104));
        var frame=new PreviewHighlighter().TableFrame(Page(clip),anchor);
        Assert(frame is not null && Math.Abs(frame.Left-276.2)<.01 && Math.Abs(frame.Top-56.67)<.01 &&
            Math.Abs(frame.Width-290.55)<.01,"WPF ancestor transforms yield the actual right-cell frame in page points");
        Assert(new PreviewHighlighter().TableFrame(Page(null),anchor) is null,"No frame without clipping evidence");
        Assert(new PreviewHighlighter().TableFrame(Page(new RectangleGeometry(new Rect(0,0,595,842))),anchor) is null,
            "Full-page clip cannot masquerade as a table-cell frame");
        Assert(new PreviewHighlighter().TableFrame(Page(Geometry.Parse("M 276,56 L 566,56 L 276,160 Z")),anchor) is null,
            "Non-rectangular clip cannot establish a cell origin");
        Assert(new PreviewHighlighter().TableFrame(Page(clip,true),anchor) is null,"Reference text split across different frames is ambiguous");
        var motto=new Issue(0,Text.Length,Text,Text,"",false,FixKind.ShapeLine,LineFix:new(null,333.67,93.76,175.94,LineRole.Motto));
        var existing=new[]{new LineGeometry(145.15,92.7,70.85),new LineGeometry(254.95,206.45,92.25)};
        var correction=LinePlacementFeedback.CorrectHiddenMotto([motto],existing,existing,frame);
        Assert(correction is not null && Math.Abs(correction[0].LineFix!.Left-57.47)<.01,
            "Measured WPF table frame feeds the invisible-Motto correction without reusing the left-cell offset");
        Canvas? lineCanvas=null;
        var linePage=Page(clip,built:canvas=>lineCanvas=canvas);
        var highlighter=new PreviewHighlighter();
        var boxes=highlighter.Measure(linePage,anchor);
        Assert(boxes.Count>0,"Reference glyph measurement is available for rescan regression");
        double bottom=boxes.Max(b=>b.Bottom), left=boxes.Min(b=>b.Left), right=boxes.Max(b=>b.Right);
        var target=new LineTarget(LineRole.Motto,0,Text,0,Text.Length+1,true);
        var referenceLine=new LineReference(left*.75,bottom*.75,(right-left)*.75);
        var proposed=ShapeLineRules.Evaluate(target,referenceLine,null,null,LinePolicy.Decree30).Fix;
        lineCanvas!.Children.Add(new System.Windows.Shapes.Path {
            Data=new System.Windows.Media.LineGeometry(new Point(proposed.Left,proposed.Top),new Point(proposed.Left+proposed.Width,proposed.Top)),
            Stroke=Brushes.Black,StrokeThickness=.5});
        lineCanvas.UpdateLayout();
        var known=new[]{new WordLineShape(1,0,1,new(proposed.Left,proposed.Top,proposed.Width),true)};
        var anchors=new Dictionary<Issue,PreviewAnchor>{{target.Probe,anchor}};
        var absent=LineInspection.Inspect(Page(clip),new PreviewHighlighter(),[target],[],anchors,[],LinePolicy.Decree30,false).Single().Issue;
        Assert(absent.Title=="Thiếu đường kẻ dưới tiêu ngữ" && absent.CanFix,
            "A verified absent Line has a concise missing-Line title from the absence branch");

        var refreshed=LineInspection.Inspect(linePage,highlighter,[target],known,anchors,[],LinePolicy.Decree30,true);
        Assert(refreshed.Count==0,"A verified corrected owned Line disappears after rescan despite unrelated unmapped shapes");
        var confirmed=target.Probe with {CanFix=true,LineFix=proposed};
        Assert(LineInspection.Inspect(linePage,highlighter,[target],[],anchors,[],LinePolicy.Decree30,true,[confirmed]).Count==0,
            "A successful edit is removed from the fresh list after its current XPS and reference are independently reverified");
        var wrongConfirmed=confirmed with {LineFix=proposed with {Top=proposed.Top+20}};
        Assert(LineInspection.Inspect(linePage,highlighter,[target],[],anchors,[],LinePolicy.Decree30,true,[wrongConfirmed]).Count==1,
            "An edit history entry cannot hide an error when current rendered geometry no longer matches");
        var displaced=known.Select(l=>l with {Geometry=l.Geometry! with {Y=l.Geometry!.Y+20}}).ToArray();
        Assert(LineInspection.Inspect(linePage,highlighter,[target],displaced,anchors,[],LinePolicy.Decree30,true).Count==1,
            "Rescan still reports a genuinely unverified or displaced Line");
        Canvas? changedCanvas=null;
        var changedPage=Page(clip,built:canvas=>
        {
            changedCanvas=canvas;
            foreach(var cell in canvas.Children.OfType<Canvas>())
                foreach(var glyph in cell.Children.OfType<Glyphs>())glyph.FontRenderingEmSize=15;
        });
        var changedPreview=new PreviewHighlighter();
        var changedReference=ReferenceMeasurement.Read(changedPage,changedPreview,anchor)!;
        var oldSubject=new LineTarget(LineRole.Subject,0,Text,0,Text.Length+1,true);
        var oldSubjectFix=ShapeLineRules.Evaluate(oldSubject,referenceLine,null,null,LinePolicy.Decree30).Fix;
        var oldPath=new System.Windows.Shapes.Path {Data=new System.Windows.Media.LineGeometry(new Point(oldSubjectFix.Left,oldSubjectFix.Top),
            new Point(oldSubjectFix.Left+oldSubjectFix.Width,oldSubjectFix.Top)),Stroke=Brushes.Black,StrokeThickness=.5};
        changedCanvas!.Children.Add(oldPath);changedCanvas.UpdateLayout();
        var subjectAnchors=new Dictionary<Issue,PreviewAnchor>{{oldSubject.Probe,anchor}};
        var oldShape=new WordLineShape(2,0,1,new(oldSubjectFix.Left,oldSubjectFix.Top,oldSubjectFix.Width),true);
        var oldEdit=oldSubject.Probe with {CanFix=true,LineFix=oldSubjectFix};
        var residual=LineInspection.Inspect(changedPage,changedPreview,[oldSubject],[oldShape],subjectAnchors,[],LinePolicy.Decree30,false,[oldEdit]);
        Assert(residual.Count==1 && residual[0].Issue.Message.Contains("lệch tâm"),
            "A changed glyph measurement produces a real residual-center warning rather than a stale-list entry");
        var finalFix=ShapeLineRules.Evaluate(oldSubject,changedReference,null,null,LinePolicy.Decree30).Fix;
        oldPath.Data=new System.Windows.Media.LineGeometry(new Point(finalFix.Left,finalFix.Top),new Point(finalFix.Left+finalFix.Width,finalFix.Top));
        changedCanvas.UpdateLayout();
        var finalShape=oldShape with {Geometry=new(finalFix.Left,finalFix.Top,finalFix.Width)};
        Assert(LineInspection.Inspect(changedPage,changedPreview,[oldSubject],[finalShape],subjectAnchors,[],LinePolicy.Decree30,false).Count==0,
            "Repositioning from the final glyph reference leaves no residual Subject warning on rescan");
        oldPath.Data=new System.Windows.Media.LineGeometry(new Point(finalFix.Left,finalFix.Top-.75),new Point(finalFix.Left+finalFix.Width,finalFix.Top-.75));
        changedCanvas.UpdateLayout();
        var nearRendered=changedPreview.HorizontalLines(changedPage,1);
        var nearAssessment=RenderedLineVerifier.Assess(oldSubject,changedReference,finalFix,nearRendered,LinePolicy.Decree30);
        Assert(nearAssessment.Actual is not null && nearAssessment.Errors.Any(e=>e.Contains("Khoảng cách")) &&
            LineInspection.Inspect(changedPage,changedPreview,[oldSubject],[finalShape],subjectAnchors,[],LinePolicy.Decree30,false).Count==1,
            "Candidate verification and the UI scanner both reject a real stroke 0.75pt above the allowed gap");
        var finalIssue=oldEdit with {LineFix=finalFix};
        var gapCorrection=LinePlacementFeedback.Correct([finalIssue],[],nearRendered)![0].LineFix!;
        oldPath.Data=new System.Windows.Media.LineGeometry(new Point(gapCorrection.Left,gapCorrection.Top-.75),
            new Point(gapCorrection.Left+gapCorrection.Width,gapCorrection.Top-.75));
        changedCanvas.UpdateLayout();
        Assert(LineInspection.Inspect(changedPage,changedPreview,[oldSubject],[finalShape],subjectAnchors,[],LinePolicy.Decree30,false).Count==0 &&
            RenderedLineVerifier.Assess(oldSubject,changedReference,finalFix,changedPreview.HorizontalLines(changedPage,1),LinePolicy.Decree30).Errors.Length==0,
            "Measured correction makes both candidate acceptance and the next UI rescan pass on the actual stroke");
        Glyphs? caretGlyph=null;
        var caretPage=Page(clip,built:canvas=>caretGlyph=canvas.Children.OfType<Canvas>().SelectMany(c=>c.Children.OfType<Glyphs>()).Single());
        var caretPreview=new PreviewHighlighter();
        var fullReference=ReferenceMeasurement.Read(caretPage,caretPreview,anchor)!;
        // All character positions allow a caret, except the trailing boundary of the last character.
        string mask=string.Concat(Enumerable.Range(0,(Text.Length+4)/4).Select(group=>
        {
            int value=0;for(int bit=0;bit<4;bit++)if(group*4+bit<Text.Length)value|=8>>bit;
            return value.ToString("X");
        }));
        caretGlyph!.CaretStops=mask;
        var stoppedRun=caretGlyph.ToGlyphRun();
        double snapped=stoppedRun.GetDistanceFromCaretCharacterHit(new System.Windows.Media.TextFormatting.CharacterHit(Text.Length,0));
        Assert(stoppedRun.AdvanceWidths.Sum()-snapped>1,"WPF caret hit testing reproduces omission of the last glyph when the trailing caret stop is disabled");
        var stoppedReference=ReferenceMeasurement.Read(caretPage,caretPreview,anchor)!;
        Assert(Math.Abs(fullReference.Width-stoppedReference.Width)<.001 && Math.Abs(fullReference.Left-stoppedReference.Left)<.001,
            "Reference geometry remains identical when caret navigation changes on the same fixed page");
        var viewer=new DocumentViewer {Document=caretPage};
        viewer.Measure(new Size(1000,800));viewer.Arrange(new Rect(0,0,1000,800));viewer.UpdateLayout();
        var viewedReference=ReferenceMeasurement.Read(caretPage,caretPreview,anchor)!;
        Assert(Math.Abs(fullReference.Width-viewedReference.Width)<.001,
            "Background verification and a DocumentViewer preview use the same full-glyph measurement");
        viewer.Document=null;
        string tablePrefix="CƠ QUAN\r\a";
        string tableStory=tablePrefix+Text+"\r\aBÁO CÁO\rTrích yếu\r";
        var tableAnchor=WordPreviewAnchor.Create(1,tableStory,tablePrefix,Text);
        var tableReference=ReferenceMeasurement.Read(caretPage,caretPreview,tableAnchor)!;
        var cpAsIndex=new PreviewAnchor(1,tableStory,tablePrefix.Length-1,Text.Length);
        var shortened=ReferenceMeasurement.Read(caretPage,caretPreview,cpAsIndex)!;
        Assert(Math.Abs(tableReference.Width-fullReference.Width)<.001,
            "Mapping from Word's actual prefix text measures the complete Motto even with table markers in the story");
        Assert(tableReference.Width-shortened.Width>1,
            "A CP-to-string mismatch reproduces a short Motto even after glyph-advance measurement is fixed");
        AdministrativePositionDesktopChecks.Run(Assert);
        ProgressReportDesktopChecks.Run(Assert);
        IssueReviewPanelChecks.Run(Assert);
        StagedGeometryDesktopChecks.Run(Assert);
        VisibleFormatChecks.Run(Assert);
        RepairProgressDesktopChecks.Run(Assert);
        DocumentTypeCatalogDesktopChecks.Run(Assert);
        Console.WriteLine($"{checks} WPF checks passed.");
    }
}
