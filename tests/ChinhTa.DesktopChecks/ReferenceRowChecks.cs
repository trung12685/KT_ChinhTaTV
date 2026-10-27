using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Core;
using ChinhTa.Desktop;

static class ReferenceRowChecks
{
    private sealed record Part(string Text,double X,double Y,double Size=14);
    private static (FixedDocumentSequence Page,PreviewAnchor Anchor) Page(params Part[] parts)
    {
        var page=new FixedPage{Width=800,Height=1100};
        var canvas=new Canvas{RenderTransform=new ScaleTransform(4d/3,4d/3)};page.Children.Add(canvas);
        foreach(var p in parts)canvas.Children.Add(new Glyphs{
            FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf")),
            UnicodeString=p.Text,FontRenderingEmSize=p.Size,OriginX=p.X,OriginY=p.Y,Fill=Brushes.Black});
        var content=new PageContent();((IAddChild)content).AddChild(page);var doc=new FixedDocument();doc.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(doc);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);
        string text=string.Concat(parts.Select(p=>p.Text));return(sequence,new(1,text,0,text.Length));
    }
    public static void Run(Action<bool,string> assert)
    {
        var mixed=Page(new("CƠ QUAN ",100,100),new("BAN HÀNH",210,100,28));
        var measured=ReferenceMeasurement.Read(mixed.Page,new PreviewHighlighter(),mixed.Anchor);
        var close=Page(new("DÒNG ĐẦU ",100,100),new("DÒNG CUỐI",200,102));
        var ambiguous=ReferenceMeasurement.Read(close.Page,new PreviewHighlighter(),close.Anchor);
        assert(measured is not null && Math.Abs(measured.Left-100)<.001 && ambiguous is null,
            $"Reference rows use baselines instead of matching box bottoms (mixed-font left={measured?.Left}, close rows accepted={ambiguous is not null})");
        foreach(double size in new[]{8d,12d,18d,28d})
        {
            foreach(bool largeFirst in new[]{false,true})
            {
                var fixture=Page(new("CƠ QUAN ",100,100,largeFirst?size:14),new("BAN HÀNH",210,100,largeFirst?14:size));
                var preview=new PreviewHighlighter();var boxes=preview.MeasureReference(fixture.Page,fixture.Anchor);
                var result=ReferenceMeasurement.Read(fixture.Page,preview,fixture.Anchor);
                double left=boxes.Min(b=>b.Left)*.75,right=boxes.Max(b=>b.Right)*.75,bottom=boxes.Max(b=>b.Bottom)*.75;
                assert(result is not null && Math.Abs(result.Left-left)<.001 && Math.Abs(result.Width-(right-left))<.001 && Math.Abs(result.Bottom-bottom)<.001,
                    $"One baseline retains both font sizes and the full row bounds: size={size}, first={largeFirst}");
                var single=preview.MeasureSingleLine(fixture.Page,fixture.Anchor);
                assert(single is not null && Math.Abs(single.Width-(right-left))<.001,
                    "Administrative single-row measurement agrees with the complete mixed-size Line reference");
            }
        }
        var wrapped=Page(new("DÒNG ĐẦU ",100,100,28),new("DÒNG CUỐI ",180,160),new("ĐẦY ĐỦ",310,160,22));
        var wrappedPreview=new PreviewHighlighter();var wrappedResult=ReferenceMeasurement.Read(wrapped.Page,wrappedPreview,wrapped.Anchor);
        var finalBoxes=wrappedPreview.MeasureReference(wrapped.Page,wrapped.Anchor).Skip(1).ToArray();
        assert(wrappedResult is not null && Math.Abs(wrappedResult.Left-180)<.001 &&
            Math.Abs(wrappedResult.Width-(finalBoxes.Max(b=>b.Right)-finalBoxes.Min(b=>b.Left))*.75)<.001 &&
            Math.Abs(wrappedResult.Bottom-finalBoxes.Max(b=>b.Bottom)*.75)<.001,
            "A genuinely wrapped reference uses every run on the final baseline, including smaller text");
        assert(wrappedPreview.MeasureSingleLine(wrapped.Page,wrapped.Anchor) is null,
            "A valid multiline Line reference still cannot authorize a single administrative row");
        foreach(double shift in new[]{0d,.2,.49})
        {
            var fixture=Page(new("CÙNG ",100,100),new("DÒNG",200,100+shift));
            var preview=new PreviewHighlighter();var result=ReferenceMeasurement.Read(fixture.Page,preview,fixture.Anchor);
            assert(result is not null && Math.Abs(result.Left-100)<.001 && preview.MeasureSingleLine(fixture.Page,fixture.Anchor) is not null,
                $"Sub-half-point baseline drift retains the entire single row: {shift}");
        }
        foreach(double gap in new[]{30d,45d,80d})
        {
            var fixture=Page(new("DÒNG MỘT ",100,100),new("DÒNG HAI ",150,100+gap),new("DÒNG BA",220,100+gap*2));
            var preview=new PreviewHighlighter();var result=ReferenceMeasurement.Read(fixture.Page,preview,fixture.Anchor);
            var boxes=preview.MeasureReference(fixture.Page,fixture.Anchor);
            assert(result is not null && Math.Abs(result.Left-220)<.001 && Math.Abs(result.Width-boxes[^1].Width*.75)<.001,
                "Three separated rows select only the final row regardless of spacing");
            int offset="DÒNG MỘT ".Length;var partial=fixture.Anchor with{Offset=offset,Length=fixture.Anchor.Length-offset};
            var selected=ReferenceMeasurement.Read(fixture.Page,preview,partial);
            assert(selected is not null && selected==result,"A selection beginning on a later row retains its own final row");
        }
        foreach(var fixture in new[]{
            close,
            Page(new("DÒNG TRÊN ",100,100,40),new("DÒNG DƯỚI",210,106,8)),
            Page(new("SỐ ",100,100),new("2",150,94,8)),
            Page(new("H",100,100),new("2",120,106,8),new("O",135,100)),
            Page(new("ĐẦU ",100,100),new("CUỐI ",200,140),new("QUAY LẠI",300,100)),
            Page(new("DƯỚI ",100,140),new("TRÊN",200,100)),
            Page(new("A ",100,100),new("B ",150,100.6),new("C",200,101.2))})
        {
            var preview=new PreviewHighlighter();
            assert(ReferenceMeasurement.Read(fixture.Page,preview,fixture.Anchor) is null,
                "Overlapping, raised/lowered, returning or reversed baselines do not prove a final reference row");
            assert(preview.MeasureSingleLine(fixture.Page,fixture.Anchor) is null,
                "An ambiguous baseline layout cannot authorize administrative positioning");
            assert(preview.Measure(fixture.Page,fixture.Anchor).Count>0,
                "Ambiguous row geometry remains highlightable for manual inspection");
        }
        var target=new LineTarget(LineRole.Agency,0,close.Anchor.PageText,0,close.Anchor.PageText.Length+1,true);
        var anchors=new Dictionary<Issue,PreviewAnchor>{{target.Probe,close.Anchor}};
        var notice=LineInspection.Inspect(close.Page,new PreviewHighlighter(),[target],[],anchors,[],LinePolicy.Decree30,false).Single().Issue;
        assert(!notice.CanFix && notice.LineFix is null,"An ambiguous final row offers manual review instead of a guessed Line");
        var history=target.Probe with{CanFix=true,LineFix=new(1,100,130,80,LineRole.Agency)};
        var after=LineInspection.Inspect(close.Page,new PreviewHighlighter(),[target],[],anchors,[],LinePolicy.Decree30,false,[history]).Single().Issue;
        assert(!after.CanFix && after.LineFix is null,"Prior Line history cannot hide a newly ambiguous row layout");
        var validTarget=new LineTarget(LineRole.Motto,0,wrapped.Anchor.PageText,0,wrapped.Anchor.PageText.Length+1,true);
        var validAnchors=new Dictionary<Issue,PreviewAnchor>{{validTarget.Probe,wrapped.Anchor}};
        var proposal=LineInspection.Inspect(wrapped.Page,new PreviewHighlighter(),[validTarget],[],validAnchors,[],LinePolicy.Decree30,false).Single().Issue;
        assert(proposal.CanFix && proposal.LineFix is not null && wrappedResult is not null &&
            Math.Abs(proposal.LineFix.Width-wrappedResult.Width)<.001,
            "A Line proposal uses the complete final mixed-size row width");
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTaReferenceRows-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string current=Path.Combine(folder,"working"),candidate=Path.Combine(folder,"candidate");
            File.WriteAllText(current,"before");File.WriteAllText(candidate,"overlapping rows");int opens=0;bool failed=false;
            try{CheckpointSwap.Apply(current,candidate,()=>{},()=>{if(++opens==1 && ReferenceMeasurement.Read(close.Page,new PreviewHighlighter(),close.Anchor) is null)
                throw new InvalidOperationException("Ambiguous final row after reopening");});}catch(InvalidOperationException){failed=true;}
            assert(failed && opens==2 && File.ReadAllText(current)=="before","Late loss of an unambiguous final row restores the whole checkpoint");
        }
        finally{Directory.Delete(folder,true);}
    }
}
