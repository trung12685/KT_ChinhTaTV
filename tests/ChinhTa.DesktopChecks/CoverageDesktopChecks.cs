using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class CoverageDesktopChecks
{
    private const string Motto="Độc lập – Tự do – Hạnh phúc";
    private static readonly Uri Font=new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf"));
    private static Glyphs Glyph(string text,double x,double y)=>new()
    {FontUri=Font,UnicodeString=text,FontRenderingEmSize=14,OriginX=x,OriginY=y,Fill=Brushes.Black};
    private static FixedDocumentSequence Document(FixedPage page)
    {
        var content=new PageContent();((IAddChild)content).AddChild(page);var document=new FixedDocument();document.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(document);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);return sequence;
    }
    private static FixedDocumentSequence LinePage(int variant)
    {
        var page=new FixedPage{Width=800,Height=1100};var canvas=new Canvas{RenderTransform=new ScaleTransform(4d/3,4d/3)};
        var glyph=Glyph(Motto,100,100);canvas.Children.Add(glyph);page.Children.Add(canvas);
        if(variant==2)canvas.Opacity=0;
        if(variant==3)glyph.Clip=new RectangleGeometry(new Rect(100,70,5,50));
        if(variant==4)
            for(int n=0;n<2;n++)canvas.Children.Add(new System.Windows.Shapes.Path
            {Data=new System.Windows.Media.LineGeometry(new Point(100,120),new Point(180,120)),Stroke=Brushes.Black,StrokeThickness=.75});
        return Document(page);
    }
    private static (FixedDocumentSequence Page,AdministrativePositionTarget Target,Dictionary<Issue,PreviewAnchor> Anchors) PositionPage(int variant)
    {
        string[] text=["AGENCY TEST","UNIT TEST","Số:        /BC-ABC"];
        var page=new FixedPage{Width=794,Height=1123};var root=new Canvas{RenderTransform=new ScaleTransform(4d/3,4d/3)};
        var cell=new Canvas{Clip=new RectangleGeometry(new Rect(20,30,260,110))};page.Children.Add(root);root.Children.Add(cell);
        for(int i=0;i<text.Length;i++)
        {
            var glyph=Glyph(text[i],0,i==0?50:i==1?70:110);glyph.OriginX=150-glyph.ToGlyphRun().AdvanceWidths.Sum()/2;
            if(i==2 && variant==1)glyph.Opacity=0;
            if(i==2 && variant==2)glyph.Clip=new RectangleGeometry(new Rect(glyph.OriginX,80,5,50));
            cell.Children.Add(glyph);
        }
        var target=new AdministrativePositionTarget(AdministrativePositionRole.Number,new(200,text[2]),[new(0,text[0]),new(100,text[1])],true);
        string story=string.Join("\r",text)+"\r";var anchors=new Dictionary<Issue,PreviewAnchor>();
        foreach(var probe in target.References.Select(r=>r.Probe).Append(target.Probe))
        {
            int offset=story.IndexOf(probe.Original,StringComparison.Ordinal);
            anchors[probe]=WordPreviewAnchor.Create(1,story,story[..offset],probe.Original);
        }
        return (Document(page),target,anchors);
    }
    public static void Run(Action<bool,string> assert)
    {
        const string secret="Nội dung riêng tư không xuất trong phạm vi kiểm tra.";const int privateStart=18273645;
        var snapshot=DocumentCoverageBuilder.Build(new(DocumentProfile.State,[new(privateStart,secret,Alignment:0)],[],true,true));
        var window=new DocumentCoverageWindow(snapshot);
        try
        {
            window.Measure(new Size(1080,720));window.Arrange(new Rect(0,0,1080,720));window.UpdateLayout();
            var rows=window.Table.ItemsSource.Cast<DocumentCoverageWindow.DisplayRow>().ToArray();
            assert(rows.Length==33 && rows.Select(r=>r.Id).Distinct().Count()==33,
                "The document coverage window displays every capability even when this scan produced no findings");
            assert(window.Table.IsReadOnly && !window.Table.CanUserAddRows && !window.Table.CanUserDeleteRows &&
                window.Table.Columns.All(c=>c is DataGridTextColumn),
                "Coverage is a read-only report with no checkboxes or controls that grant document repair permission");
            assert(rows.Single(r=>r.Id=="C31").State==DocumentCoverageSnapshot.Label(CoverageState.NotRecognized) &&
                rows.Single(r=>r.Id=="C33").State==DocumentCoverageSnapshot.Label(CoverageState.Unavailable) &&
                rows.All(r=>r.Findings=="0 đề xuất; 0 thủ công"),
                "Zero findings visibly retain the distinction between an unrecognized signature and unavailable page evidence");
            assert(window.ReportText==snapshot.ToText() && window.ReportText.Contains(DocumentCoverageSnapshot.Legend,StringComparison.Ordinal) &&
                !window.ReportText.Contains(secret,StringComparison.Ordinal) && !window.ReportText.Contains(privateStart.ToString(),StringComparison.Ordinal),
                "The copyable coverage report includes its scope legend but no private document text or native positions");
            var later=snapshot.WithExternalEvidence("C33",new(CoverageState.Observed,1,1,"Đã kiểm bản xem trước sau."));
            assert(later.Rows.Single(r=>r.Id=="C33").State==CoverageState.Observed &&
                rows.Single(r=>r.Id=="C33").State==DocumentCoverageSnapshot.Label(CoverageState.Unavailable) && window.ReportText==snapshot.ToText(),
                "A later completed observation cannot mutate the open report for an earlier scan");
        }
        finally{window.Close();}
        var target=new LineTarget(LineRole.Motto,0,Motto,0,Motto.Length+1,true);
        var anchors=new Dictionary<Issue,PreviewAnchor>{{target.Probe,new(1,Motto,0,Motto.Length)}};
        foreach(int variant in new[]{0,1,2,3,4})
        {
            var observed=new List<bool>();
            IReadOnlyList<WordLineShape> shapes=variant==4?[new(1,0,1,new ChinhTa.Core.LineGeometry(100,120,80),true)]:[];
            var issues=LineInspection.Inspect(LinePage(variant),new PreviewHighlighter(),[target],shapes,
                variant==1?new Dictionary<Issue,PreviewAnchor>():anchors,[],LinePolicy.Decree30,false,null,(_,known)=>observed.Add(known));
            assert(observed.Count==1 && observed[0]==(variant==0) && issues.Count==1 && (variant==0?issues[0].Issue.CanFix:!issues[0].Issue.CanFix),
                "Line coverage records the actual geometry outcome once, independently of whether it found a repair: "+variant);
        }
        foreach(int variant in new[]{0,1,2,3,4})
        {
            var fixture=PositionPage(variant);var measured=AdministrativePositionInspection.MeasureCentering(fixture.Page,new PreviewHighlighter(),fixture.Target,fixture.Anchors);
            if(variant==4)measured=measured with{ReferenceFrames=measured.ReferenceFrames.Select((f,i)=>i==0 && f is not null?f with{Left=f.Left+2}:f).ToArray()};
            var observed=new List<bool>();
            var findings=AdministrativePositionInspection.InspectMeasured([measured],variant==3?new Dictionary<Issue,PreviewAnchor>():fixture.Anchors,
                (_,known)=>observed.Add(known));
            assert(observed.Count==1 && observed[0]==(variant==0) && (variant==0?findings.Count==0:findings.All(f=>!f.Issue.CanFix)),
                "Administrative coverage needs shown complete text, current anchors and a consistent frame rather than an empty issue list: "+variant);
        }
    }
}
