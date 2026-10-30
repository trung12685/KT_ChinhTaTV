using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
public sealed record PreviewWordHit(PreviewAnchor Anchor,string Word,Rect Bounds,FixedPage Page);
public sealed partial class PreviewHighlighter
{
    public PreviewWordHit? SelectedPhrase(FixedDocumentSequence document,PreviewAnchor anchor,string original)
    {
        if(anchor.Offset<0 || anchor.Length!=original.Length || anchor.Offset>anchor.PageText.Length-anchor.Length || anchor.PageText.Substring(anchor.Offset,anchor.Length)!=original)return null;
        var index=GetIndex(document,anchor.Page);var boxes=MeasureReference(document,anchor);
        return index is null || boxes.Count==0?null:new(anchor,original,boxes[0],index.Page);
    }
    public PreviewWordHit? HitWord(FixedDocumentSequence document,int pageNumber,Point point)
    {
        if(!double.IsFinite(point.X) || !double.IsFinite(point.Y))return null;
        var index=GetIndex(document,pageNumber);if(index is null)return null;
        var hits=new Dictionary<(int,int),PreviewWordHit>();
        foreach(var run in index.Runs)
        {
            if(run.Length==0)continue;
            var runAnchor=new PreviewAnchor(pageNumber,index.Text,run.Start,run.Length);
            if(!MeasureReference(document,runAnchor).Any(b=>b.Contains(point)))continue;
            foreach(Match word in InlineWordLocator.Words.Matches(index.Text))
            {
                if(word.Index>=run.Start+run.Length)break;
                if(word.Index+word.Length<=run.Start || word.Length>128)continue;
                var anchor=new PreviewAnchor(pageNumber,index.Text,word.Index,word.Length);
                var boxes=MeasureReference(document,anchor);
                if(boxes.Count==0 || !boxes.Any(b=>b.Contains(point)))continue;
                var bounds=boxes[0];
                if(boxes.Any(b=>Math.Abs(b.Top-bounds.Top)>2 || Math.Abs(b.Bottom-bounds.Bottom)>2))continue;
                foreach(var box in boxes)bounds.Union(box);
                hits[(word.Index,word.Length)]=new(anchor,word.Value,bounds,index.Page);
            }
        }
        return hits.Count==1?hits.Values.Single():null;
    }
    public PreviewWordHit? HitWord(FixedDocumentSequence document,DependencyObject original,Func<IInputElement,Point> position)
    {
        FixedPage? page=null;DocumentPageView? view=null;DependencyObject? current=original;
        while(current is not null)
        {
            if(current is FixedPage found)page=found;
            if(current is DocumentPageView parent){view=parent;break;}
            current=current is Visual || current is System.Windows.Media.Media3D.Visual3D?VisualTreeHelper.GetParent(current):LogicalTreeHelper.GetParent(current);
        }
        if(page is null && view?.DocumentPage?.Visual is DependencyObject visual)page=Descendants<FixedPage>(visual).FirstOrDefault();
        return page is not null && view is not null && view.PageNumber>=0?HitWord(document,view.PageNumber+1,position(page)):null;
    }
}
