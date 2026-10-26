using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ChinhTa.Core;
using Path=System.Windows.Shapes.Path;
using LineGeometry=ChinhTa.Core.LineGeometry;

namespace ChinhTa.Desktop;

public sealed record HorizontalStrokeEvidence(IReadOnlyList<LineGeometry> Lines,bool HasUnverifiedPaths);

// Geometry bounds alone do not establish painted XPS evidence. Only a complete
// painted thin figure qualifies; a partial/unknown figure must block automatic
// repair rather than disappear from the inventory and authorize another Line.
internal static class VisibleHorizontalStrokeReader
{
    private enum Paint { None, Uniform, Unknown }
    private const double GeometryTolerance=.001;
    private const double AreaTolerance=GeometryTolerance*GeometryTolerance;
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
                Geometry visible=original;
                for(DependencyObject? node=path;node is Visual visual;node=VisualTreeHelper.GetParent(node))
                {
                    var clip=VisualTreeHelper.GetClip(visual) ?? (node as UIElement)?.Clip;
                    if(clip is not null)
                    {
                        var toPage=ToPage(visual,page);
                        if(toPage is null){uncertain=true;break;}
                        visible=Intersect(visible,InPage(clip,toPage));
                    }
                    if(node is UIElement {ClipToBounds:true} element)
                    {
                        var toPage=ToPage(visual,page);
                        if(toPage is null){uncertain=true;break;}
                        visible=Intersect(visible,InPage(new RectangleGeometry(new Rect(element.RenderSize)),toPage));
                    }
                    if(ReferenceEquals(node,page))break;
                }
                if(!double.IsFinite(page.Width) || !double.IsFinite(page.Height) || page.Width<=0 || page.Height<=0)
                    return new(result,true);
                visible=Intersect(visible,new RectangleGeometry(new Rect(0,0,page.Width,page.Height)));
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
    private static Paint VisibilityOf(Visual path,Visual page)
    {
        var result=Paint.Uniform;
        for(DependencyObject? node=path;node is not null;node=VisualTreeHelper.GetParent(node))
        {
            if(node is UIElement element)
            {
                if(element.Visibility!=Visibility.Visible || element.Opacity==0)return Paint.None;
                if(!double.IsFinite(element.Opacity) || element.Opacity<0)result=Paint.Unknown;
                if(element.OpacityMask is Brush mask)
                {
                    var paint=BrushPaint(mask);
                    if(paint==Paint.None)return Paint.None;
                    if(paint==Paint.Unknown)result=Paint.Unknown;
                }
            }
            if(ReferenceEquals(node,page))return result;
        }
        return Paint.Unknown;
    }
    private static Paint BrushPaint(Brush? brush)
    {
        if(brush is null || brush.Opacity==0)return Paint.None;
        if(!double.IsFinite(brush.Opacity) || brush.Opacity<0)return Paint.Unknown;
        if(brush is SolidColorBrush solid)
            return solid.Color.ScA==0?Paint.None:float.IsFinite(solid.Color.ScA) && solid.Color.ScA>0?Paint.Uniform:Paint.Unknown;
        if(brush is GradientBrush gradient && gradient.GradientStops.Count>0 && gradient.GradientStops.All(s=>s.Color.ScA==0))return Paint.None;
        return Paint.Unknown;
    }
    private static Transform? ToPage(Visual visual,FixedPage page)
    {
        if(ReferenceEquals(visual,page))return Transform.Identity;
        if(visual.TransformToAncestor(page) is not Transform transform)return null;
        var m=transform.Value;
        return new[]{m.M11,m.M12,m.M21,m.M22,m.OffsetX,m.OffsetY}.All(double.IsFinite)?transform:null;
    }
    private static Geometry InPage(Geometry geometry,Transform transform)
    {
        var copy=geometry.CloneCurrentValue();
        copy.Transform=new TransformGroup{Children={copy.Transform??Transform.Identity,transform}};
        return copy;
    }
    private static Geometry Combine(Geometry first,Geometry second,GeometryCombineMode mode)
        =>Geometry.Combine(first,second,mode,Transform.Identity,GeometryTolerance,ToleranceType.Absolute);
    private static Geometry Intersect(Geometry painted,Geometry clip)
        // Avoid rounding a fully contained fractional stroke through a Boolean
        // intersection and then mistaking that rounding for a clipped sliver.
        =>clip.FillContainsWithDetail(painted,GeometryTolerance,ToleranceType.Absolute)==IntersectionDetail.FullyContains
            ?painted:Combine(painted,clip,GeometryCombineMode.Intersect);
    private static bool Finite(Rect rect)=>!rect.IsEmpty && new[]{rect.Left,rect.Top,rect.Width,rect.Height}.All(double.IsFinite);
}
