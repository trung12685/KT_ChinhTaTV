using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ChinhTa.Core;
using Path=System.Windows.Shapes.Path;
using LineGeometry=ChinhTa.Core.LineGeometry;
using static ChinhTa.Desktop.PreviewPaintProof;

namespace ChinhTa.Desktop;

public sealed record HorizontalStrokeEvidence(IReadOnlyList<LineGeometry> Lines,bool HasUnverifiedPaths);

// Geometry bounds alone do not establish painted XPS evidence. Only a complete
// painted thin figure qualifies; a partial/unknown figure must block automatic
// repair rather than disappear from the inventory and authorize another Line.
internal static class VisibleHorizontalStrokeReader
{
    internal static HorizontalStrokeEvidence Read(Path path,FixedPage page)
    {
        var result=new List<LineGeometry>();bool unverified=false;
        try
        {
            if(path.Data is null)return new(result,false);
            var visibility=VisibilityOf(path,page);
            if(visibility==Paint.None)return new(result,false);
            var stroke=path.StrokeThickness>0?BrushPaint(path.Stroke):Paint.None;
            var fill=BrushPaint(path.Fill);
            if(stroke==Paint.None && fill==Paint.None)return new(result,false);
            var geometry=path.Data.GetFlattenedPathGeometry();
            foreach(var figure in geometry.Figures)
            {
                var piece=new PathGeometry([figure]){Transform=geometry.Transform,FillRule=geometry.FillRule};
                var matrix=ToPage(path,page);
                if(matrix is null)return new(result,true);
                var world=InPage(piece,matrix);
                var rect=world.Bounds;
                if(!Finite(rect)){unverified=true;continue;}
                // Retain the existing thin-figure thresholds; this change does
                // not widen the scanner's geometric tolerance or Line policy.
                if(rect.Width<6 || rect.Height>2)continue;
                bool filled=figure.IsFilled && piece.GetArea()>0 && geometry.GetArea()>0;
                if(stroke==Paint.None && !filled)continue;
                bool uncertain=visibility==Paint.Unknown || stroke==Paint.Unknown || filled && fill==Paint.Unknown ||
                    stroke!=Paint.None && path.StrokeDashArray is {Count:>0};
                Geometry painted=Geometry.Empty;
                if(stroke!=Paint.None)
                {
                    var pen=new Pen(Brushes.Black,path.StrokeThickness){StartLineCap=path.StrokeStartLineCap,
                        EndLineCap=path.StrokeEndLineCap,LineJoin=path.StrokeLineJoin,MiterLimit=path.StrokeMiterLimit};
                    // Dashed paint remains unverified. This solid envelope is
                    // used only to prove complete exclusion by a clip; include
                    // possible dash caps rather than clipping an undersized bound.
                    if(path.StrokeDashArray is {Count:>0})pen.StartLineCap=pen.EndLineCap=PenLineCap.Square;
                    painted=piece.GetWidenedPathGeometry(pen,GeometryTolerance,ToleranceType.Absolute);
                }
                if(filled && fill!=Paint.None)
                {
                    // Respect the whole Path's fill rule: separate filled bars
                    // are valid, whereas an even-odd hole must not be filled in
                    // by considering its surrounding figure alone.
                    var actualFill=geometry.Figures.Count==1?piece:Combine(piece,geometry,GeometryCombineMode.Intersect);
                    if(Combine(piece,actualFill,GeometryCombineMode.Exclude).GetArea(GeometryTolerance,ToleranceType.Absolute)>AreaTolerance)
                        uncertain=true;
                    painted=Combine(painted,actualFill,GeometryCombineMode.Union);
                }
                var original=InPage(painted,matrix);
                if(original.GetArea(GeometryTolerance,ToleranceType.Absolute)<=0)continue;
                double bandHeight=Math.Min(.05,original.Bounds.Height/4);
                var spine=new RectangleGeometry(new Rect(rect.Left+GeometryTolerance,
                    (rect.Top+rect.Bottom-bandHeight)/2,rect.Width-2*GeometryTolerance,bandHeight));
                if(bandHeight<=0 || original.FillContainsWithDetail(spine,GeometryTolerance,ToleranceType.Absolute)!=IntersectionDetail.FullyContains)
                    uncertain=true;
                var exposure=ClipToPage(original,path,page);
                var visible=exposure.Visible;uncertain|=!exposure.Known;
                if(visible.GetArea(GeometryTolerance,ToleranceType.Absolute)<=0)continue;
                if(uncertain || Combine(original,visible,GeometryCombineMode.Exclude).GetArea(GeometryTolerance,ToleranceType.Absolute)>AreaTolerance)
                {unverified=true;continue;}
                result.Add(new(rect.Left*.75,(rect.Top+rect.Height/2)*.75,rect.Width*.75));
            }
        }
        catch(Exception error) when(error is InvalidOperationException or ArgumentException)
        {unverified=true;}
        return new(result,unverified);
    }
    private static bool Finite(Rect rect)=>!rect.IsEmpty && new[]{rect.Left,rect.Top,rect.Width,rect.Height}.All(double.IsFinite);
}
