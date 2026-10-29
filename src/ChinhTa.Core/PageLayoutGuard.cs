using System.Globalization;
using System.Xml.Linq;

namespace ChinhTa.Core;

public static class PageLayoutGuard
{
    internal static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    internal const double MmPerTwip=25.4/1440;
    internal static int? Number(XElement? e,string name)=>int.TryParse((string?)e?.Attribute(W+name),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out int n)?n:null;
    internal static XElement? Body(XDocument xml)
    {var docs=xml.Descendants(W+"document").ToArray();return docs.Length==1?docs[0].Element(W+"body"):null;}
    public static SectionLayout? Read(XDocument xml,SectionLayout anchor)
    {
        var sections=Body(xml)?.Descendants(W+"sectPr").ToArray();
        if(sections is not {Length:1})return null;
        var size=sections[0].Element(W+"pgSz");var margins=sections[0].Element(W+"pgMar");
        int? width=Number(size,"w"),height=Number(size,"h"),top=Number(margins,"top"),bottom=Number(margins,"bottom"),left=Number(margins,"left"),right=Number(margins,"right");
        if(new[]{width,height,top,bottom,left,right}.Any(n=>n is null))return null;
        return anchor with {WidthMm=width!.Value*MmPerTwip,HeightMm=height!.Value*MmPerTwip,TopMm=top!.Value*MmPerTwip,
            BottomMm=bottom!.Value*MmPerTwip,LeftMm=left!.Value*MmPerTwip,RightMm=right!.Value*MmPerTwip};
    }
    private static bool Optional(XElement? e,string name,params int[] allowed)=>e?.Attribute(W+name) is null || Number(e,name) is int n && allowed.Contains(n);
    private static bool On(XElement e)=>(string?)e.Attribute(W+"val") is not ("0" or "false" or "off");
    public static bool Eligible(XDocument xml,SectionLayout live,PageNumberPackage? package=null)
    {
        var body=Body(xml);var sections=body?.Descendants(W+"sectPr").ToArray();
        if(body is null || sections is not {Length:1} || sections[0].Parent!=body || sections[0]!=body.Elements().LastOrDefault() || live.Number!=1 || live.MirrorMargins ||
            live.WidthMm>live.HeightMm || !PageLayoutFix.Known(live))return false;
        if(sections[0].Elements(W+"pgSz").Count()!=1 || sections[0].Elements(W+"pgMar").Count()!=1)return false;
        var source=Read(xml,live);if(source is null || !PageLayoutFix.Matches(source,live))return false;
        var section=sections[0];
        string[] unsafeNames=["drawing","pict","object","sdt","altChunk","fldChar","instrText","fldSimple","ins","del","moveFrom","moveTo",
            "footnoteReference","endnoteReference","tab","tblpPr","gridSpan","vMerge","hMerge","txbxContent","pgBorders","textDirection","framePr","formProt"];
        if(body.Descendants().Any(e=>e.Name.Namespace==W && unsafeNames.Contains(e.Name.LocalName) || e.Name.NamespaceName=="http://schemas.openxmlformats.org/officeDocument/2006/math") ||
            body.Descendants(W+"tbl").Any(t=>t.Ancestors(W+"tbl").Any()) ||
            xml.Descendants().Any(e=>e.Name.Namespace==W && e.Name.LocalName is "mirrorMargins" or "gutterAtTop" or "bookFoldPrinting" or "bookFoldRevPrinting" && On(e)))return false;
        if(!EmptyHeaderFooterScope.Proven(xml,package))return false;
        if((string?)section.Element(W+"pgSz")?.Attribute(W+"orient") is not (null or "portrait") ||
            !Optional(section.Element(W+"pgMar"),"gutter",0) ||
            (string?)section.Element(W+"vAlign")?.Attribute(W+"val") is not (null or "top") ||
            (string?)section.Element(W+"docGrid")?.Attribute(W+"type") is not (null or "default"))return false;
        var columns=section.Element(W+"cols");
        if(!Optional(columns,"num",1) || columns?.Elements().Any()==true)return false;
        var proposed=PageLayoutFix.Propose(live with {RepairEligible=true})?.After??live;
        double available=(proposed.WidthMm-proposed.LeftMm-proposed.RightMm)/MmPerTwip;
        foreach(var table in body.Descendants(W+"tbl"))
        {
            var grid=table.Element(W+"tblGrid")?.Elements(W+"gridCol").Select(e=>Number(e,"w")).ToArray();
            var props=table.Element(W+"tblPr");var width=props?.Element(W+"tblW");
            if(grid is null || grid.Length==0 || grid.Any(n=>n is null or <=0) || grid.Sum(n=>(long)n!.Value)>available+2 ||
                !Optional(props?.Element(W+"tblInd"),"w",0) ||
                (string?)width?.Attribute(W+"type") is not (null or "auto" or "dxa") ||
                (string?)width?.Attribute(W+"type")=="dxa" && (Number(width,"w") is not int n || n<0 || n>available+2))return false;
        }
        return true;
    }
}
