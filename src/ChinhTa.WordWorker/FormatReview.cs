using ChinhTa.Core;
using ChinhTa.WordProtocol;
namespace ChinhTa.WordWorker;
internal static class FormatReview
{
    internal static IEnumerable<FindingDto> Check(BridgeRequest request)
    {
        var profile=Enum.Parse<DocumentProfile>(request.Profile);
        var items=request.Paragraphs.Where(p=>TextSegments.Supported(p.Text)).ToArray();
        var rows=new List<Paragraph>();var map=new List<(int Start,ParagraphDto Row)>();int offset=0;
        foreach(var dto in items)
        {
            var f=dto.Format;string text=dto.Text.TrimEnd('\r','\a','\n');
            bool known=f is {Observed:true} && new ObservedFont(f.Font,f.Size,f.Bold,f.Italic,f.Color).Known();
            rows.Add(new(offset,dto.Text,known && f!.Bold==-1,known?f!.Size:0,f?.Alignment??-1,known?f!.Font:"",known && f!.Italic==-1,
                known?f!.Color:9999999,dto.InTable,f?.Page??0,known,TextEditable:true,FormatEditable:false,
                FormatEvidence:new FormatEvidence(text,known?[new(f!.Font,f.Size,f.Bold,f.Italic,f.Color)]:[])));
            map.Add((offset,dto));offset+=dto.Text.Length+1;
        }
        int MapBoundary(int cp,bool end=false)
        {
            if(cp<0)return -1;
            var group=map.Where(m=>end?m.Row.End<=cp:m.Row.Start>=cp).ToArray();
            return group.Length==0?-1:end?group[^1].Start+group[^1].Row.Text.Length+1:group[0].Start;
        }
        var layout=map.Where(m=>m.Row.Format is {LayoutKnown:true}).Select(m=>
        {
            var f=m.Row.Format!;
            return new LayoutParagraph(m.Start,m.Row.Text,f.Page,m.Row.InTable,MapBoundary(f.CellStart),MapBoundary(f.CellEnd,true),f.Alignment==1,
                MapBoundary(f.TableStart),f.Alignment==1,f.UnindentedCenter);
        }).ToArray();
        var issues=Checker.Check(rows,profile).Where(i=>i.Kind is not (FixKind.Text or FixKind.RepeatedText or FixKind.PunctuationSpace or FixKind.ParenthesisSpace or FixKind.QuoteSpace or FixKind.SentenceEndSpace or FixKind.ParagraphEndSpace or FixKind.ColonSpace or FixKind.SentenceInnerSpace or FixKind.SentenceAfterSpace)).ToList();
        issues.AddRange(DocumentHeadingRules.Check(rows,layout,profile));issues.AddRange(OfficialLetterRules.Check(rows,layout,profile));
        foreach(var s in request.Sections)
        {
            var anchor=map.FirstOrDefault(m=>m.Row.Start>=s.Start && m.Row.Start<s.End && !string.IsNullOrWhiteSpace(m.Row.Text));
            if(anchor.Row is null)continue;
            double[] values=[s.WidthMm,s.HeightMm,s.TopMm,s.BottomMm,s.LeftMm,s.RightMm];
            if(values.Any(v=>!double.IsFinite(v) || v<0 || v>2000) || s.WidthMm<=0 || s.HeightMm<=0)continue;
            issues.AddRange(PageRules.Check(new(s.Number,anchor.Start,anchor.Row.Text.TrimEnd('\r','\a','\n'),s.WidthMm,s.HeightMm,s.TopMm,s.BottomMm,s.LeftMm,s.RightMm,s.MirrorMargins),profile));
        }
        foreach(var issue in issues)
        {
            var owner=map.SingleOrDefault(m=>issue.Start>=m.Start && issue.Start<(long)m.Start+m.Row.Text.Length);
            if(owner.Row is null || issue.Length<=0 || issue.Start-owner.Start>owner.Row.Text.Length-issue.Length || owner.Row.Text.Substring(issue.Start-owner.Start,issue.Length)!=issue.Original)continue;
            yield return new(){ParagraphIndex=owner.Row.Index,InTable=owner.Row.InTable,Offset=issue.Start-owner.Start,Length=issue.Length,Original=issue.Original,
                Category="Format",Message=(issue.Title is null?"":issue.Title+": ")+issue.Message+(issue.Citation is null?"":"\r\n"+issue.Citation),Suggestions=[issue.Suggestion]};
        }
    }
}
