using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ChinhTa.Desktop;

// Shared visual/paint and coverage proof for strokes and reference glyphs.
// Coordinates passed to ClipToPage are already in FixedPage space.
internal static class PreviewPaintProof
{
    internal enum Paint { None, Uniform, Unknown }
    internal const double GeometryTolerance=.001;
    internal const double AreaTolerance=GeometryTolerance*GeometryTolerance;
    internal readonly record struct Exposure(Geometry Visible,bool Known);
    internal static Exposure ClipToPage(Geometry original,Visual leaf,FixedPage page)
    {
        Geometry visible=original;bool known=true;
        for(DependencyObject? node=leaf;node is Visual visual;node=VisualTreeHelper.GetParent(node))
        {
            var clip=VisualTreeHelper.GetClip(visual) ?? (node as UIElement)?.Clip;
            if(clip is not null)
            {
                var toPage=ToPage(visual,page);
                if(toPage is null){known=false;break;}
                visible=Intersect(visible,InPage(clip,toPage));
            }
            if(node is UIElement {ClipToBounds:true} element)
            {
                var toPage=ToPage(visual,page);
                if(toPage is null){known=false;break;}
                visible=Intersect(visible,InPage(new RectangleGeometry(new Rect(element.RenderSize)),toPage));
            }
            if(ReferenceEquals(node,page))break;
        }
        if(!double.IsFinite(page.Width) || !double.IsFinite(page.Height) || page.Width<=0 || page.Height<=0)
            return new(visible,false);
        visible=Intersect(visible,new RectangleGeometry(new Rect(0,0,page.Width,page.Height)));
        return new(visible,known);
    }
    internal static bool FullyExposed(Geometry pageArea,Visual leaf,FixedPage page)
    {
        var exposure=ClipToPage(pageArea,leaf,page);
        return exposure.Known && exposure.Visible.GetArea(GeometryTolerance,ToleranceType.Absolute)>0 &&
            Combine(pageArea,exposure.Visible,GeometryCombineMode.Exclude).GetArea(GeometryTolerance,ToleranceType.Absolute)<=AreaTolerance;
    }
    internal static Paint VisibilityOf(Visual path,Visual page)
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
    internal static Paint BrushPaint(Brush? brush)
    {
        if(brush is null || brush.Opacity==0)return Paint.None;
        if(!double.IsFinite(brush.Opacity) || brush.Opacity<0)return Paint.Unknown;
        if(brush is SolidColorBrush solid)
            return solid.Color.ScA==0?Paint.None:float.IsFinite(solid.Color.ScA) && solid.Color.ScA>0?Paint.Uniform:Paint.Unknown;
        if(brush is GradientBrush gradient && gradient.GradientStops.Count>0 && gradient.GradientStops.All(s=>s.Color.ScA==0))return Paint.None;
        return Paint.Unknown;
    }
    internal static Transform? ToPage(Visual visual,FixedPage page)
    {
        if(ReferenceEquals(visual,page))return Transform.Identity;
        if(visual.TransformToAncestor(page) is not Transform transform)return null;
        var m=transform.Value;
        return new[]{m.M11,m.M12,m.M21,m.M22,m.OffsetX,m.OffsetY}.All(double.IsFinite)?transform:null;
    }
    internal static Geometry InPage(Geometry geometry,Transform transform)
    {
        var copy=geometry.CloneCurrentValue();
        copy.Transform=new TransformGroup{Children={copy.Transform??Transform.Identity,transform}};
        return copy;
    }
    internal static Geometry Combine(Geometry first,Geometry second,GeometryCombineMode mode)
        =>Geometry.Combine(first,second,mode,Transform.Identity,GeometryTolerance,ToleranceType.Absolute);
    internal static Geometry Intersect(Geometry painted,Geometry clip)
        // Avoid rounding a fully contained fractional stroke through a Boolean
        // intersection and then mistaking that rounding for a clipped sliver.
        =>clip.FillContainsWithDetail(painted,GeometryTolerance,ToleranceType.Absolute)==IntersectionDetail.FullyContains
            ?painted:Combine(painted,clip,GeometryCombineMode.Intersect);
}
