using System.Xml.Linq;

namespace ChinhTa.Core;

// Word exposes a standard embedded inline picture as one U+0001 object CP.
// This only supplies a token for comparison with ACTUAL Word range text. It
// never computes Word positions or grants text/font edits on the picture.
internal static class WordInlineImageText
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main",
        WP="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",A="http://schemas.openxmlformats.org/drawingml/2006/main",
        PIC="http://schemas.openxmlformats.org/drawingml/2006/picture",R="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    internal static bool IsImage(XElement drawing)
    {
        if(drawing.Name!=W+"drawing" || drawing.Parent?.Name!=W+"r" || drawing.Parent.Parent?.Name!=W+"p" ||
            drawing.Elements().Count()!=1 || drawing.Element(WP+"inline") is not XElement inline)return false;
        if(inline.Elements().Any(e=>e.Name!=WP+"extent" && e.Name!=WP+"effectExtent" && e.Name!=WP+"docPr" &&
            e.Name!=WP+"cNvGraphicFramePr" && e.Name!=A+"graphic") ||
            inline.Elements(WP+"extent").Count()!=1 || inline.Elements(WP+"docPr").Count()!=1 ||
            inline.Elements(A+"graphic").Count()!=1)return false;
        var graphic=inline.Element(A+"graphic")!;var data=graphic.Element(A+"graphicData");
        if(graphic.Elements().Count()!=1 || data is null || (string?)data.Attribute("uri")!=PIC.NamespaceName ||
            data.Elements().Count()!=1 || data.Element(PIC+"pic") is not XElement picture)return false;
        if(picture.Elements().Any(e=>e.Name!=PIC+"nvPicPr" && e.Name!=PIC+"blipFill" && e.Name!=PIC+"spPr") ||
            picture.Elements(PIC+"nvPicPr").Count()!=1 || picture.Elements(PIC+"blipFill").Count()!=1 ||
            picture.Elements(PIC+"spPr").Count()!=1)return false;
        var blips=picture.Element(PIC+"blipFill")!.Elements(A+"blip").ToArray();
        if(blips.Length!=1 || string.IsNullOrWhiteSpace((string?)blips[0].Attribute(R+"embed")) || blips[0].Attribute(R+"link") is not null)return false;
        // Do not bridge through textbox/math/other story content, even when it
        // is wrapped in picture-looking XML. Compatibility branches stay closed.
        return !drawing.Descendants().Any(e=>e.Name.Namespace==W || e.Name==A+"t" ||
            e.Name.NamespaceName is "http://schemas.openxmlformats.org/markup-compatibility/2006" or
                "http://schemas.openxmlformats.org/officeDocument/2006/math") &&
            !drawing.DescendantNodes().OfType<XText>().Any(t=>!string.IsNullOrWhiteSpace(t.Value));
    }
    internal static bool IsOnlyImageParagraph(XElement paragraph)
    {
        if(paragraph.Name!=W+"p" || paragraph.Elements().Any(e=>e.Name!=W+"pPr" && e.Name!=W+"r"))return false;
        var runs=paragraph.Elements(W+"r").ToArray();
        if(runs.Any(r=>r.Elements().Any(e=>e.Name!=W+"rPr" && e.Name!=W+"drawing" &&
            !(e.Name==W+"t" && string.IsNullOrWhiteSpace(e.Value)))))return false;
        var drawings=runs.SelectMany(r=>r.Elements(W+"drawing")).ToArray();
        return drawings.Length==1 && IsImage(drawings[0]);
    }
}
