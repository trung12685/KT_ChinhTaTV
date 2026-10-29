using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ChinhTa.Core;

public enum IsolatedSpacingTrialKind { Single, OneAndHalf, ZeroParagraphGap, SixPointParagraphGap }
public sealed record IsolatedSpacingAnchor(int BodyIndex,string Text,string SourceHash);
public sealed record IsolatedSpacingTrialResult(string FlatXml,IsolatedSpacingTrialKind Kind,
    IsolatedSpacingAnchor Anchor,IsolatedSpacingAnchor? Next=null);

// Diagnostic candidates only. An explicit source-bound anchor is not permission
// to edit the live Word document, infer Single from point size, or repair C28/C29.
public static class IsolatedSpacingTrial
{
    private static readonly XNamespace Pkg="http://schemas.microsoft.com/office/2006/xmlPackage";
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static SpacingDiagnosticException Unknown()=>new(SpacingDiagnosticFailureCode.UnsupportedEvidence);
    private static string Hash(string xml)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(xml)));
    private static bool IsGap(IsolatedSpacingTrialKind kind)=>kind is IsolatedSpacingTrialKind.ZeroParagraphGap or IsolatedSpacingTrialKind.SixPointParagraphGap;
    private static XDocument Parse(string xml)
    {
        // The package parser supplies bounds, DTD protection, relationships,
        // binary retention and passive-opening checks before editable XML exists.
        try{IsolatedWordPackage.FromFlat(xml).RequirePassiveCalibration();}
        catch(InvalidOperationException error) when(error is not SpacingDiagnosticException)
        {throw new SpacingDiagnosticException(SpacingDiagnosticFailureCode.UnsupportedEvidence,error);}
        catch(XmlException error){throw new SpacingDiagnosticException(SpacingDiagnosticFailureCode.UnsupportedEvidence,error);}
        using var reader=XmlReader.Create(new StringReader(xml.TrimStart('\uFEFF')),new XmlReaderSettings
        {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=64*1024*1024});
        return XDocument.Load(reader,LoadOptions.PreserveWhitespace);
    }
    private static XElement Body(XDocument xml)
    {
        var part=xml.Root!.Elements(Pkg+"part").Single(p=>(string?)p.Attribute(Pkg+"name")=="/word/document.xml");
        return part.Element(Pkg+"xmlData")!.Element(W+"document")!.Element(W+"body")!;
    }
    private static XElement Row(XDocument xml,int index)
    {
        var rows=Body(xml).Elements().ToArray();
        if(index<0 || index>=rows.Length || rows[index].Name!=W+"p")throw Unknown();
        return rows[index];
    }
    private static IEnumerable<XElement> StyleSources(XDocument xml,XElement row)
    {
        if(row.Element(W+"pPr") is { } properties)yield return properties;
        var styles=Part(xml,"/word/styles.xml")?.Element(Pkg+"xmlData")?.Element(W+"styles");
        string? id=(string?)row.Element(W+"pPr")?.Element(W+"pStyle")?.Attribute(W+"val");
        var defaultsForParagraph=styles?.Elements(W+"style").Where(s=>(string?)s.Attribute(W+"type")=="paragraph" &&
            (string?)s.Attribute(W+"default") is "1" or "true").ToArray()??[];
        if(defaultsForParagraph.Length>1)throw Unknown();
        id??=(string?)defaultsForParagraph.SingleOrDefault()?.Attribute(W+"styleId");
        var seen=new HashSet<string>(StringComparer.Ordinal);
        while(id is not null)
        {
            if(!seen.Add(id))throw Unknown();
            var style=styles?.Elements(W+"style").Where(s=>(string?)s.Attribute(W+"styleId")==id).ToArray();
            if(style is not {Length:1} || (string?)style[0].Attribute(W+"type")!="paragraph")throw Unknown();
            if(style[0].Element(W+"pPr") is { } inherited)yield return inherited;
            id=(string?)style[0].Element(W+"basedOn")?.Attribute(W+"val");
        }
        if(styles?.Element(W+"docDefaults")?.Element(W+"pPrDefault")?.Element(W+"pPr") is { } defaults)yield return defaults;
    }
    private static XElement? Part(XDocument xml,string name)=>xml.Root!.Elements(Pkg+"part").SingleOrDefault(p=>(string?)p.Attribute(Pkg+"name")==name);
    private static XDocument StyleEvidence(XDocument xml)
    {
        XNamespace rel="http://schemas.openxmlformats.org/package/2006/relationships";
        var relationships=Part(xml,"/word/_rels/document.xml.rels")?.Descendants(rel+"Relationship").ToArray()??[];
        XElement? Validate(string path,string contentType,string relationType,string relativeTarget)
        {
            var part=Part(xml,path);var matches=relationships.Where(r=>(string?)r.Attribute("Type")==relationType).ToArray();
            if(part is null){if(matches.Length!=0)throw Unknown();return null;}
            var root=part.Element(Pkg+"xmlData")?.Elements().SingleOrDefault();
            if((string?)part.Attribute(Pkg+"contentType")!=contentType || root?.Name!=W+"styles" || matches.Length!=1 ||
                (string?)matches[0].Attribute("Target") is not string target || target!=relativeTarget && target!=path ||
                (string?)matches[0].Attribute("TargetMode") is not (null or "Internal"))throw Unknown();
            return root;
        }
        var primary=Validate("/word/styles.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml",
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles","styles.xml");
        var effects=Validate("/word/stylesWithEffects.xml","application/vnd.ms-word.stylesWithEffects+xml",
            "http://schemas.microsoft.com/office/2007/relationships/stylesWithEffects","stylesWithEffects.xml");
        if(effects is not null && primary is null || xml.Descendants(W+"styles").Any(s=>!ReferenceEquals(s,primary) && !ReferenceEquals(s,effects)))throw Unknown();
        if(effects is null)return xml;
        // Word keeps a legitimate effects-specific style tree alongside the
        // primary styles. Existing generic outline evidence scans globally; use
        // a private projection that excludes ONLY that validated alternate part.
        // The complete source/trial package and its relationships are unchanged.
        var projection=new XDocument(xml);Part(projection,"/word/stylesWithEffects.xml")!.Remove();return projection;
    }
    private static void RequireOrdinary(XDocument xml,XElement row)
    {
        var projected=StyleEvidence(xml);
        var body=Body(xml);var sections=body.Descendants(W+"sectPr").ToArray();
        if(sections.Length!=1 || sections[0].Parent!=body || body.Elements().Last()!=sections[0])throw Unknown();
        var section=sections[0];
        if(section.Element(W+"cols") is { } columns && (columns.HasElements || (string?)columns.Attribute(W+"num") is not (null or "1")) ||
            section.Element(W+"pgSz")?.Attribute(W+"orient") is {Value:not "portrait"} ||
            xml.Descendants(W+"mirrorMargins").Any(e=>(string?)e.Attribute(W+"val") is not ("0" or "false" or "off")))throw Unknown();
        if(section.Elements(W+"textDirection").Any(e=>(string?)e.Attribute(W+"val")!="lrTb") ||
            new[]{"bidi","rtlGutter"}.Any(name=>section.Elements(W+name).Any(e=>(string?)e.Attribute(W+"val") is not ("0" or "false" or "off"))))throw Unknown();
        if(section.Element(W+"pgSz") is { } size)
        {
            if(!int.TryParse((string?)size.Attribute(W+"w"),NumberStyles.None,CultureInfo.InvariantCulture,out int width) ||
                !int.TryParse((string?)size.Attribute(W+"h"),NumberStyles.None,CultureInfo.InvariantCulture,out int height) ||
                width<=0 || height<=0 || width>height)throw Unknown();
        }
        if(!WordXmlTableMap.IsPlainText(row,allowTextBreaks:true) || row.Elements().Any(e=>e.Name!=W+"pPr" && e.Name!=W+"r") ||
            row.Elements(W+"r").Any(r=>r.Elements().Any(e=>e.Name!=W+"rPr" && e.Name!=W+"t" && e.Name!=W+"br" && e.Name!=W+"cr")))throw Unknown();
        string text=WordXmlTableMap.TextOf(row);
        if(string.IsNullOrWhiteSpace(text) || text.Any(c=>char.IsControl(c) && c!='\v'))throw Unknown();
        int rowIndex=Array.IndexOf(body.Elements().ToArray(),row);
        var evidence=new WordOutlineEvidence(projected).Describe(Row(projected,rowIndex),true);
        if(evidence is not {Scope:OutlineScope.PlainBody,HeadingLevel:null,Alignment:0 or 3,
            SpacingContext:{Supported:true,Contextual:false,Grid:false,PageBreakBefore:false,KeepNext:false},
            BodyIndent:{Left:0,Right:0,Hanging:0,First:>=0 and <=2880}})throw Unknown();
        // Resolve auto-spacing with the same style precedence. A point override
        // cannot establish that inherited auto-spacing is disabled.
        var sources=StyleSources(xml,row).ToArray();
        foreach(string name in new[]{"beforeAutospacing","afterAutospacing"})
        {
            string? effective=sources.Select(p=>(string?)p.Element(W+"spacing")?.Attribute(W+name)).FirstOrDefault(v=>v is not null);
            if(effective is not (null or "0" or "false" or "off"))throw Unknown();
        }
        if(section.Element(W+"pgMar")?.Attribute(W+"gutter") is { } gutter &&
            (!int.TryParse(gutter.Value,NumberStyles.None,CultureInfo.InvariantCulture,out int gutterWidth) || gutterWidth!=0))throw Unknown();
    }
    public static IsolatedSpacingAnchor Bind(string fullFlatXml,int bodyIndex)
    {
        var xml=Parse(fullFlatXml);var row=Row(xml,bodyIndex);RequireOrdinary(xml,row);
        return new(bodyIndex,WordXmlTableMap.TextOf(row),Hash(fullFlatXml));
    }
    public static IsolatedSpacingAnchor Find(string fullFlatXml,string exactText)
    {
        var xml=Parse(fullFlatXml);var matches=Body(xml).Elements().Select((row,index)=>(row,index))
            .Where(p=>p.row.Name==W+"p" && WordXmlTableMap.TextOf(p.row)==exactText).ToArray();
        if(matches.Length!=1)throw Unknown();
        RequireOrdinary(xml,matches[0].row);return new(matches[0].index,exactText,Hash(fullFlatXml));
    }
    private static void RequireAnchor(string source,XDocument xml,IsolatedSpacingAnchor anchor)
    {
        if(anchor.SourceHash!=Hash(source))throw Unknown();
        var row=Row(xml,anchor.BodyIndex);RequireOrdinary(xml,row);
        if(WordXmlTableMap.TextOf(row)!=anchor.Text)throw Unknown();
    }
    private static XElement Spacing(XElement row)
    {
        var properties=row.Element(W+"pPr");
        if(properties is null){properties=new(W+"pPr");row.AddFirst(properties);}
        var spacing=properties.Element(W+"spacing");
        if(spacing is null)
        {
            spacing=new(W+"spacing");
            // Insert before every schema successor, leaving existing property
            // order and all unrelated attributes/content untouched.
            var successors=new HashSet<string>("ind contextualSpacing mirrorIndents suppressOverlap jc textDirection textAlignment textboxTightWrap outlineLvl divId cnfStyle rPr sectPr pPrChange".Split(' '));
            var next=properties.Elements().FirstOrDefault(e=>e.Name.Namespace==W && successors.Contains(e.Name.LocalName));
            if(next is null)properties.Add(spacing);else next.AddBeforeSelf(spacing);
        }
        return spacing;
    }
    public static IsolatedSpacingTrialResult Create(string fullFlatXml,IsolatedSpacingAnchor anchor,
        IsolatedSpacingTrialKind kind,IsolatedSpacingAnchor? next=null)
    {
        var xml=Parse(fullFlatXml);RequireAnchor(fullFlatXml,xml,anchor);
        if(!Enum.IsDefined(kind) || IsGap(kind) != (next is not null))throw Unknown();
        var spacing=Spacing(Row(xml,anchor.BodyIndex));
        if(next is not null)
        {
            RequireAnchor(fullFlatXml,xml,next);
            if(next.BodyIndex!=anchor.BodyIndex+1)throw Unknown();
            spacing.SetAttributeValue(W+"after",kind==IsolatedSpacingTrialKind.SixPointParagraphGap?120:0);
            Spacing(Row(xml,next.BodyIndex)).SetAttributeValue(W+"before",0);
        }
        else
        {
            spacing.SetAttributeValue(W+"line",kind==IsolatedSpacingTrialKind.Single?240:360);
            spacing.SetAttributeValue(W+"lineRule","auto");
        }
        var result=new IsolatedSpacingTrialResult(xml.ToString(SaveOptions.DisableFormatting),kind,anchor,next);
        try{Verify(fullFlatXml,result);}
        catch(InvalidOperationException error){throw new SpacingDiagnosticException(SpacingDiagnosticFailureCode.IntegrityFailed,error);}
        return result;
    }
    public static void Verify(string sourceFlatXml,IsolatedSpacingTrialResult trial)
    {
        var source=Parse(sourceFlatXml);
        try{VerifyAudit(sourceFlatXml,source,trial);}
        catch(InvalidOperationException error){throw new SpacingDiagnosticException(SpacingDiagnosticFailureCode.IntegrityFailed,error);}
    }
    private static void VerifyAudit(string sourceFlatXml,XDocument source,IsolatedSpacingTrialResult trial)
    {
        var actual=Parse(trial.FlatXml);
        RequireAnchor(sourceFlatXml,source,trial.Anchor);
        if(!Enum.IsDefined(trial.Kind) || IsGap(trial.Kind) != (trial.Next is not null))throw Unknown();
        void Strip(int index,params string[] attributes)
        {
            var before=Row(source,index);var after=Row(actual,index);
            bool hadProperties=before.Element(W+"pPr") is not null,hadSpacing=before.Element(W+"pPr")?.Element(W+"spacing") is not null;
            foreach(var row in new[]{before,after})foreach(string name in attributes)row.Element(W+"pPr")?.Element(W+"spacing")?.Attribute(W+name)?.Remove();
            if(!hadSpacing && after.Element(W+"pPr")?.Element(W+"spacing") is { } added)
            {if(added.Attributes().Any(a=>!a.IsNamespaceDeclaration || a.Value!=W.NamespaceName) || added.Nodes().Any())throw Unknown();added.Remove();}
            if(!hadProperties && after.Element(W+"pPr") is { } properties)
            {if(properties.Attributes().Any(a=>!a.IsNamespaceDeclaration || a.Value!=W.NamespaceName) || properties.Nodes().Any())throw Unknown();properties.Remove();}
        }
        string? Read(int index,string name)=>(string?)Row(actual,index).Element(W+"pPr")?.Element(W+"spacing")?.Attribute(W+name);
        if(trial.Next is { } next)
        {
            RequireAnchor(sourceFlatXml,source,next);
            if(next.BodyIndex!=trial.Anchor.BodyIndex+1 || Read(trial.Anchor.BodyIndex,"after")!=(trial.Kind==IsolatedSpacingTrialKind.SixPointParagraphGap?"120":"0") || Read(next.BodyIndex,"before")!="0")throw Unknown();
            Strip(trial.Anchor.BodyIndex,"after");Strip(next.BodyIndex,"before");
        }
        else
        {
            if(Read(trial.Anchor.BodyIndex,"line")!=(trial.Kind==IsolatedSpacingTrialKind.Single?"240":"360") || Read(trial.Anchor.BodyIndex,"lineRule")!="auto")throw Unknown();
            Strip(trial.Anchor.BodyIndex,"line","lineRule");
        }
        if(!XNode.DeepEquals(source,actual))throw Unknown();
    }
}
