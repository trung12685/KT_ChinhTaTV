using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ChinhTa.Core;

public sealed record PageFooterSource(string Font,float Size,bool Italic,string Color,bool Bold=false)
{
    public bool Valid=>!string.IsNullOrWhiteSpace(Font) && !Font.Any(char.IsControl) && float.IsFinite(Size) && Size is >0 and <=1638 &&
        Regex.IsMatch(Color??"",@"^(?:[0-9A-F]{6}|auto)$",RegexOptions.CultureInvariant);
}
public sealed record PageFooterMigration(PageFooterSource? Default,PageFooterSource? First,bool DifferentFirst,int Pages)
{
    public bool Valid=>(Default is not null || First is not null) && (Default is null || Default.Valid) && (First is null || First.Valid) && Pages is >=1 and <=9;
    public PageFooterSource? Source(int page)=>page==1 && DifferentFirst?First:Default;
    public bool Removes(string type)=>type=="default"?Default is not null:type=="first" && First is not null;
}
public sealed record PageNumberFix(string SourceKey,PageFooterMigration? FooterMigration=null)
{
    public bool Valid=>SourceKey.Length==64 && SourceKey.All(Uri.IsHexDigit) && (FooterMigration is null || FooterMigration.Valid);
    public string Suggestion=>FooterMigration is null?Action:
        "Chuyển riêng trường PAGE đã kiểm từ cuối trang lên giữa lề trên; đánh từ 1, ẩn trang đầu, Times New Roman đen cỡ 13–14 đứng. Giữ nguyên nội dung và phần footer khác.";
    public const string Action="Đánh số tự động, Times New Roman đen, cỡ 13–14, đứng, giữa lề trên; đánh từ 1 và ẩn trang đầu. Giữ cỡ đúng, nội dung và footer.";
}
internal sealed record NumberFont(string? Name,float? Size,bool? Italic,string? Color)
{
    public bool Known=>Name is not null && Size is >0 and <=1638 && Italic is not null && Color is not null;
    public bool Correct=>Known && Name!.Equals("Times New Roman",StringComparison.OrdinalIgnoreCase) && Size is >=13 and <=14 && Italic==false && Color is "000000" or "auto";
}
internal sealed record NumberHeader(XElement Paragraph,XElement Result,string Code,NumberFont Font,bool? Centered,bool SafeLayout);
internal sealed record NumberAnalysis(bool Covered,string Key,bool Repairable,IReadOnlyList<string> Errors,SectionLayout? Layout,NumberHeader? Header,bool Create,bool CopyFooter,PageFooterMigration? Migration=null,NumberHeader? FooterSource=null);

public static class PageNumberRules
{
    public const string Citation="NĐ30, PL I, Phần I.I.4 và I.7 (PDF 21): Times New Roman đen, số Ả Rập từ 1, cỡ 13–14, đứng, giữa lề trên, không hiện trang đầu.";
    private static readonly XNamespace W=PageNumberPackage.W;
    internal static bool On(XElement? e)=>e is not null && (string?)e.Attribute(W+"val") is not ("0" or "false" or "off");
    internal static bool Empty(XElement root)=>root.Elements().All(p=>p.Name==W+"p" && p.Elements().All(e=>e.Name==W+"pPr" ||
        e.Name==W+"r" && e.Elements().All(t=>t.Name==W+"rPr" || t.Name==W+"t" && string.IsNullOrWhiteSpace(t.Value))));
    internal static bool HasPage(XElement root)=>Regex.IsMatch(string.Concat(root.Descendants(W+"instrText").Select(e=>e.Value)),@"\bPAGE\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant) ||
        root.Descendants(W+"fldSimple").Any(e=>Regex.IsMatch((string?)e.Attribute(W+"instr")??"",@"\bPAGE\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant));
    private static List<XElement?>? ParagraphProperties(PageNumberPackage package,XElement p)
    {
        var styles=package.Parts.GetValueOrDefault("word/styles.xml")?.Root;var values=new List<XElement?>{p.Element(W+"pPr")};
        string? id=(string?)p.Element(W+"pPr")?.Element(W+"pStyle")?.Attribute(W+"val");
        id??=(string?)styles?.Elements(W+"style").FirstOrDefault(s=>(string?)s.Attribute(W+"type")=="paragraph" && (string?)s.Attribute(W+"default") is "1" or "true")?.Attribute(W+"styleId");
        var seen=new HashSet<string>();
        while(id is not null)
        {
            var matches=styles?.Elements(W+"style").Where(e=>(string?)e.Attribute(W+"styleId")==id).ToArray();
            if(!seen.Add(id) || matches is not {Length:1})return null;
            values.Add(matches[0].Element(W+"pPr"));id=(string?)matches[0].Element(W+"basedOn")?.Attribute(W+"val");
        }
        values.Add(styles?.Element(W+"docDefaults")?.Element(W+"pPrDefault")?.Element(W+"pPr"));return values;
    }
    private static List<XElement?> Properties(PageNumberPackage package,XElement p,XElement run)
    {
        var result=new List<XElement?>{run.Element(W+"rPr")};var styles=package.Parts.GetValueOrDefault("word/styles.xml")?.Root;
        bool Style(string? id)
        {
            var seen=new HashSet<string>();
            while(id is not null)
            {
                var found=styles?.Elements(W+"style").Where(s=>(string?)s.Attribute(W+"styleId")==id).ToArray();
                if(!seen.Add(id) || found is not {Length:1})return false;
                result.Add(found[0].Element(W+"rPr"));id=(string?)found[0].Element(W+"basedOn")?.Attribute(W+"val");
            }
            return true;
        }
        if(!Style((string?)run.Element(W+"rPr")?.Element(W+"rStyle")?.Attribute(W+"val")))return [];
        string? paragraph=(string?)p.Element(W+"pPr")?.Element(W+"pStyle")?.Attribute(W+"val");
        paragraph??=(string?)styles?.Elements(W+"style").FirstOrDefault(s=>(string?)s.Attribute(W+"type")=="paragraph" && OnDefault(s))?.Attribute(W+"styleId");
        if(!Style(paragraph))return [];
        result.Add(styles?.Element(W+"docDefaults")?.Element(W+"rPrDefault")?.Element(W+"rPr"));return result;
        static bool OnDefault(XElement s)=>(string?)s.Attribute(W+"default") is "1" or "true";
    }
    internal static NumberHeader? Header(PageNumberPackage package,XElement root)
    {
        var paragraphs=root.Elements().ToArray();if(paragraphs.Length!=1 || paragraphs[0].Name!=W+"p")return null;
        var p=paragraphs[0];
        if(p.Descendants().Any(e=>e.Name==W+"drawing" || e.Name==W+"pict" || e.Name==W+"object" || e.Name==W+"sdt" || e.Name==W+"tab" || e.Name==W+"hyperlink" || e.Name==W+"bookmarkStart" || e.Name==W+"bookmarkEnd") ||
            p.Element(W+"pPr")?.Elements().Any(e=>e.Name==W+"framePr" || e.Name==W+"numPr" || e.Name==W+"sectPr" || e.Name==W+"textDirection" || e.Name==W+"bidi")==true)return null;
        bool Cache(string value,string instruction)=>Regex.IsMatch(value,@"^[0-9]*$") ||
            (Regex.IsMatch(instruction,@"\\\*\s+ROMAN\b",RegexOptions.IgnoreCase) || (string?)package.Section?.Element(W+"pgNumType")?.Attribute(W+"fmt") is "upperRoman" or "lowerRoman") &&
            Regex.IsMatch(value,@"^[IVXLCDMivxlcdm]+$");
        string code;XElement? result;
        var simple=p.Elements(W+"fldSimple").ToArray();
        if(simple.Length==1 && p.Elements().All(e=>e.Name==W+"pPr" || e==simple[0]))
        {
            if(OnFlag(simple[0],"fldLock"))return null;
            code=(string?)simple[0].Attribute(W+"instr")??"";
            var runs=simple[0].Elements(W+"r").ToArray();if(runs.Length!=1 || simple[0].Elements().Count()!=1)return null;result=runs[0];
        }
        else
        {
            if(simple.Length>0 || p.Elements().Any(e=>e.Name!=W+"pPr" && e.Name!=W+"r"))return null;
            var runs=p.Elements(W+"r").ToArray();int phase=0;var instructions=new StringBuilder();result=null;
            foreach(var run in runs)
            foreach(var e in run.Elements().Where(e=>e.Name!=W+"rPr"))
            {
                if(e.Name==W+"fldChar")
                {
                    if(OnFlag(e,"fldLock"))return null;
                    string? type=(string?)e.Attribute(W+"fldCharType");
                    if(phase==0 && type=="begin")phase=1;else if(phase==1 && type=="separate")phase=2;else if(phase==2 && type=="end")phase=3;else return null;
                }
                else if(e.Name==W+"instrText" && phase==1)instructions.Append(e.Value);
                else if(e.Name==W+"t" && phase==2 && Cache(e.Value,instructions.ToString()))
                {if(result is not null && result!=run)return null;result=run;}
                else return null;
            }
            if(phase!=3)return null;code=instructions.ToString();
        }
        if(result is null || result.Elements().Any(e=>e.Name!=W+"rPr" && e.Name!=W+"t") || result.Elements(W+"t").Count()!=1 || !Cache(result.Elements(W+"t").Single().Value,code) ||
            !Regex.IsMatch(code.Trim(),@"^PAGE(?:\s+\\\*\s+(?:MERGEFORMAT|ARABIC|ROMAN|alphabetic))*$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant))return null;
        var props=Properties(package,p,result);
        string? Attr(string element,string attr)=>props.Select(r=>(string?)r?.Element(W+element)?.Attribute(W+attr)).FirstOrDefault(v=>v is not null);
        var families=props.Select(r=>r?.Element(W+"rFonts")).FirstOrDefault(f=>f?.Attributes().Any(a=>a.Name==W+"ascii" || a.Name==W+"hAnsi" || a.Name==W+"asciiTheme" || a.Name==W+"hAnsiTheme")==true);
        string? family=(string?)families?.Attribute(W+"ascii")??(string?)families?.Attribute(W+"hAnsi");
        string? theme=(string?)families?.Attribute(W+"asciiTheme")??(string?)families?.Attribute(W+"hAnsiTheme");
        if(theme is not null)
        {
            XNamespace a="http://schemas.openxmlformats.org/drawingml/2006/main";
            var themes=package.Parts.Values.Select(d=>d.Root).Where(e=>e?.Name==a+"theme").ToArray();
            string? group=theme.StartsWith("minor",StringComparison.Ordinal)?"minorFont":theme.StartsWith("major",StringComparison.Ordinal)?"majorFont":null;
            family=themes.Length==1 && group is not null?(string?)themes[0]?.Element(a+"themeElements")?.Element(a+"fontScheme")?.Element(a+group)?.Element(a+"latin")?.Attribute("typeface"):null;
        }
        float? size=int.TryParse(Attr("sz","val"),NumberStyles.None,CultureInfo.InvariantCulture,out int half)?half/2f:null;
        var italic=props.Select(r=>r?.Element(W+"i")).FirstOrDefault(e=>e is not null);
        bool? italicValue=italic is null?false:(string?)italic.Attribute(W+"val") switch {null or "1" or "true" or "on"=>true,"0" or "false" or "off"=>false,_=>null};
        var colorNode=props.Select(r=>r?.Element(W+"color")).FirstOrDefault(e=>e is not null);
        string? color=(string?)colorNode?.Attribute(W+"val")??"auto";if(colorNode?.Attributes().Any(a=>a.Name==W+"themeColor" || a.Name==W+"themeTint" || a.Name==W+"themeShade")==true)color=null;
        if(result.Element(W+"rPr")?.Element(W+"i") is null && props.Count(r=>r?.Element(W+"i") is not null)>1)italicValue=null;
        var paragraphsProps=ParagraphProperties(package,p);
        string? ParaAttr(string el,string attr)=>paragraphsProps?.Select(r=>(string?)r?.Element(W+el)?.Attribute(W+attr)).FirstOrDefault(v=>v is not null);
        bool? centered=paragraphsProps is null?null: (ParaAttr("jc","val")??"left")=="center" &&
            new[]{"left","right","start","end","firstLine","hanging"}.All(n=>ParaAttr("ind",n) is null or "0");
        bool safeLayout=paragraphsProps is not null && !paragraphsProps.Any(pr=>pr?.Elements().Any(e=>e.Name==W+"framePr" || e.Name==W+"numPr" ||
            e.Name==W+"pBdr" || e.Name==W+"shd" || e.Name==W+"sectPr" || e.Name==W+"textDirection" || e.Name==W+"bidi" && On(e))==true);
        return new(p,result,code,new(family,size,italicValue,color?.ToUpperInvariant()=="AUTO"?"auto":color?.ToUpperInvariant()),centered,safeLayout);
        static bool OnFlag(XElement e,string name)=>(string?)e.Attribute(W+name) is "1" or "true" or "on";
    }
    internal static string Canonical(XElement root,bool pageCache=false,bool ignoreParagraphIds=false)
    {
        string Node(XElement e)=>"<"+e.Name+" "+string.Join(";",e.Attributes().Where(a=>!a.IsNamespaceDeclaration &&
            !(ignoreParagraphIds && a.Name.NamespaceName=="http://schemas.microsoft.com/office/word/2010/wordml" && a.Name.LocalName is "paraId" or "textId") &&
            !(a.Name.Namespace==W && (a.Name.LocalName.StartsWith("rsid",StringComparison.Ordinal) || a.Name.LocalName=="dirty")))
            .OrderBy(a=>a.Name.ToString(),StringComparer.Ordinal).Select(a=>a.Name+"="+a.Value))+">"+
            (pageCache && e.Name==W+"t"?"#PAGE#":string.Concat(e.Nodes().Select(n=>n is XElement child?Node(child):
                n is XText t && (e.Name==W+"t" || e.Name==W+"instrText" || !string.IsNullOrWhiteSpace(t.Value))?t.Value:"")))+"</"+e.Name+">";
        return Node(root);
    }
    internal static NumberAnalysis Analyze(PageNumberPackage package,int pages)
    {
        NumberAnalysis Unknown()=>new(false,"",false,[],null,null,false,false);
        var body=package.Body;var section=package.Section;
        if(body is null || section is null || body.Descendants(W+"sectPr").Count()!=1 || pages<1 || pages>10000)return Unknown();
        var refs=section.Elements().Where(e=>e.Name==W+"headerReference" || e.Name==W+"footerReference").ToArray();
        if(refs.Any(e=>(string?)e.Attribute(W+"type") is not ("default" or "first" or "even")) ||
            refs.GroupBy(e=>(e.Name,(string?)e.Attribute(W+"type"))).Any(g=>g.Count()!=1))return Unknown();
        var resolved=new Dictionary<string,(string Path,XElement Root)>();
        foreach(var reference in refs)
        {
            var part=package.Resolve(reference);if(part is null)return Unknown();
            resolved.Add(reference.Name.LocalName+":"+(string)reference.Attribute(W+"type")!,part.Value);
        }
        var primary=resolved.GetValueOrDefault("headerReference:default").Root;
        var first=resolved.GetValueOrDefault("headerReference:first").Root;
        bool firstDifferent=On(section.Element(W+"titlePg"));
        var header=primary is null || Empty(primary)?null:Header(package,primary);
        bool create=primary is null || Empty(primary);
        bool firstPage=(firstDifferent?first is not null && HasPage(first):header is not null || primary is not null && HasPage(primary));
        bool footerPage=resolved.Where(p=>p.Key=="footerReference:default" && (pages>1 || !firstDifferent) || p.Key=="footerReference:first" && firstDifferent).Any(p=>HasPage(p.Value.Root));
        var errors=new List<string>();
        if(firstPage)errors.Add("Số trang đang hiện ở trang đầu");
        if(footerPage)errors.Add("Số trang đang đặt ở cuối trang");
        if(pages>1 && create && !footerPage)errors.Add("Thiếu số trang từ trang 2");
        bool active=pages>1 || !firstDifferent;
        var num=section.Element(W+"pgNumType");
        if(active && header is not null)
        {
            if((string?)num?.Attribute(W+"fmt") is not (null or "decimal") || Regex.IsMatch(header.Code,@"\\\*\s+(?:ROMAN|alphabetic)\b",RegexOptions.IgnoreCase))errors.Add("Số trang chưa dùng số Ả Rập");
            if(num?.Attribute(W+"start") is not null && PageLayoutGuard.Number(num,"start")!=1)errors.Add("Số trang chưa bắt đầu từ 1");
            if(header.Centered==false)errors.Add("Số trang chưa căn giữa");
            if(header.Font.Known && !header.Font.Correct)errors.Add("Phông/cỡ/kiểu số trang chưa đúng");
        }
        var geometry=PageLayoutGuard.Read(new XDocument(new XElement(package.Parts["word/document.xml"].Root!)),new(1,0,"A",0,0,0,0,0,0));
        bool legalPage=geometry is not null && PageLayoutFix.Known(geometry) && geometry.WidthMm<=geometry.HeightMm && geometry.TopMm is >=19.75 and <=25.25;
        bool BlankSafe(XElement? root)=>root is null || Empty(root) && root.Elements().Count()<=1 && root.Elements(W+"p").All(p=>
            ParagraphProperties(package,p) is { } pr && !pr.Any(props=>props?.Elements().Any(e=>e.Name==W+"framePr" || e.Name==W+"pBdr" || e.Name==W+"shd" || e.Name==W+"numPr")==true));
        double headerMm=(PageLayoutGuard.Number(section.Element(W+"pgMar"),"header")??720)*PageLayoutGuard.MmPerTwip;
        bool headerFits=create || header is not null && geometry is not null && headerMm>=0 && headerMm+(header.Font.Size??100)*1.25*25.4/72<=geometry.TopMm+.25;
        if(active && header is not null && geometry is not null && (headerMm<0 || headerMm>=geometry.TopMm))errors.Add("Số trang nằm ngoài lề trên");
        bool firstSafe=BlankSafe(first) || first is not null && Header(package,first) is {SafeLayout:true};
        bool primarySafe=create?BlankSafe(primary):header is {Font.Known:true,SafeLayout:true};
        bool settingsSafe=package.Parts.GetValueOrDefault("word/settings.xml")?.Descendants().Any(e=>e.Name.Namespace==W &&
            e.Name.LocalName is "evenAndOddHeaders" or "mirrorMargins" or "bookFoldPrinting" or "bookFoldRevPrinting" && On(e))!=true;
        // Enabling different-first must retain the footer previously visible there.
        var defaultFooter=resolved.GetValueOrDefault("footerReference:default").Root;
        var firstFooter=resolved.GetValueOrDefault("footerReference:first").Root;
        bool footerSafe=firstDifferent || firstFooter is null || (defaultFooter is null?Empty(firstFooter):Canonical(firstFooter,ignoreParagraphIds:true)==Canonical(defaultFooter,ignoreParagraphIds:true));
        NumberHeader? Footer(XElement? root)=>root is null || !HasPage(root)?null:Header(package,root);
        var defaultNumber=Footer(defaultFooter);var firstNumber=Footer(firstFooter);
        // Underlines, borders, shading and other decoration can render as extra
        // paths independently of the PAGE glyph. They need their own binding;
        // this initial migration slice only detaches an undecorated PAGE result.
        bool PlainFooterResult(NumberHeader value)
        {
            string[] allowed=["rStyle","rFonts","b","bCs","i","iCs","color","sz","szCs","lang","noProof","snapToGrid"];
            return Properties(package,value.Paragraph,value.Result).All(p=>p is null ||
                p.Elements().All(e=>e.Name.Namespace==W && allowed.Contains(e.Name.LocalName)) &&
                !p.Elements().GroupBy(e=>e.Name).Any(g=>g.Count()>1));
        }
        bool? FooterBold(NumberHeader value)
        {
            var properties=Properties(package,value.Paragraph,value.Result);
            var direct=value.Result.Element(W+"rPr")?.Element(W+"b");
            var inherited=properties.Skip(1).Select(p=>p?.Element(W+"b")).Where(e=>e is not null).ToArray();
            // Style bold is a toggle. Without a direct override, multiple
            // inherited toggles require a separate effective-style proof.
            if(direct is null && inherited.Length>1)return null;
            var bold=direct??inherited.SingleOrDefault();
            return bold is null?false:(string?)bold.Attribute(W+"val") switch
            {null or "1" or "true" or "on"=>true,"0" or "false" or "off"=>false,_=>null};
        }
        PageFooterSource? Source(NumberHeader? value)=>value is {SafeLayout:true,Font.Known:true} && PlainFooterResult(value) && FooterBold(value) is { } bold &&
            !Regex.IsMatch(value.Code,@"\\\*\s+(?:ROMAN|alphabetic)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant) &&
            Regex.IsMatch(value.Font.Color??"",@"^(?:[0-9A-F]{6}|auto)$",RegexOptions.CultureInvariant)?
            new(value.Font.Name!,value.Font.Size!.Value,value.Font.Italic!.Value,value.Font.Color!,bold):null;
        var defaultSource=Source(defaultNumber);var firstSource=Source(firstNumber);
        bool pureFooters=(!HasPage(defaultFooter??new XElement(W+"ftr")) || defaultSource is not null) &&
            (!HasPage(firstFooter??new XElement(W+"ftr")) || firstSource is not null);
        bool decimalSource=(string?)num?.Attribute(W+"fmt") is null or "decimal" &&
            (num?.Attribute(W+"start") is null || PageLayoutGuard.Number(num,"start")==1);
        PageFooterMigration? migration=footerPage && pages is >=1 and <=9 && pureFooters && decimalSource && geometry is {BottomMm:>0}?
            new(defaultSource,firstSource,firstDifferent,pages):null;
        // Both PAGE-only references can be removed without exposing a previously
        // hidden custom first footer. Mixed footers retain the original safeguard.
        if(migration is not null && defaultSource is not null && (firstFooter is null || firstSource is not null))footerSafe=true;
        bool copyFooter=!firstDifferent && firstFooter is null && defaultFooter is not null && migration?.Default is null;
        bool otherSafe=(string?)package.Parts["word/document.xml"].Root?.Element(W+"background")?.Attribute(W+"color") is null or "FFFFFF" &&
            (string?)section.Element(W+"pgMar")?.Attribute(W+"gutter") is null or "0" &&
            (string?)section.Element(W+"pgSz")?.Attribute(W+"orient") is null or "portrait" &&
            (string?)section.Element(W+"cols")?.Attribute(W+"num") is null or "1" &&
            section.Element(W+"textDirection") is null &&
            !refs.Any(e=>(string?)e.Attribute(W+"type")=="even") && !HasPage(body) &&
            section.Element(W+"pgNumType")?.Attributes().Any(a=>!a.IsNamespaceDeclaration && a.Name!=W+"fmt" && a.Name!=W+"start")!=true &&
            section.Element(W+"docGrid")?.Attribute(W+"type")?.Value is null or "default";
        var parts=resolved.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+"="+Canonical(p.Value.Root,p.Key.StartsWith("headerReference:",StringComparison.Ordinal) && Header(package,p.Value.Root) is not null || p.Key.StartsWith("footerReference:",StringComparison.Ordinal) && Source(Footer(p.Value.Root)) is not null,true));
        string configuration=firstDifferent+"|"+Canonical(num??new XElement(W+"pgNumType"))+"|"+(string?)section.Element(W+"pgMar")?.Attribute(W+"header")+"|"+
            string.Join("|",parts)+"|"+header?.Font+"|"+migration;
        string key=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(configuration)));
        bool safe=legalPage && headerFits && firstSafe && settingsSafe && footerSafe && otherSafe && (!footerPage || migration is not null) && primarySafe;
        return new(create || header is not null || firstPage || footerPage,key,safe,errors,geometry,header,create,copyFooter,migration,defaultNumber??firstNumber);
    }
    public static void VerifyFooterMigration(PageNumberPackage package,int pages,PageNumberFix fix)
    {
        var source=Analyze(package,pages);
        if(fix.FooterMigration is null || !fix.Valid || !source.Repairable || source.Key!=fix.SourceKey || source.Migration!=fix.FooterMigration)
            throw new InvalidOperationException("Vùng PAGE cuối trang không còn khớp phần đã duyệt; hãy quét lại.");
    }
    public static string ConfigurationKey(PageNumberPackage package,int pages)=>Analyze(package,pages).Key;
    public static bool Satisfied(PageNumberPackage package,int pages,PageNumberFix fix)
    {var a=Analyze(package,pages);return fix.Valid && a.Covered && a.Key==fix.SourceKey && a.Errors.Count==0;}
    public static bool ProvenStandard(PageNumberPackage package,int pages)
    {
        var a=Analyze(package,pages);var section=package.Section;
        return a.Covered && a.Repairable && a.Errors.Count==0 && a.Header is {Centered:true,SafeLayout:true,Font.Correct:true} &&
            On(section?.Element(W+"titlePg")) && section!.Elements(W+"headerReference").All(e=>(string?)e.Attribute(W+"type")!="first" || package.Resolve(e) is { } first && Empty(first.Root)) &&
            (string?)section.Element(W+"pgNumType")?.Attribute(W+"fmt") is null or "decimal" && (section.Element(W+"pgNumType")?.Attribute(W+"start") is null || PageLayoutGuard.Number(section.Element(W+"pgNumType"),"start")==1);
    }
    public static IReadOnlyList<Issue> Check(PageNumberPackage package,int pages,int start,string anchor,DocumentProfile profile)
    {
        if(profile!=DocumentProfile.State || start<0 || anchor.Length!=1)return [];
        var analysis=Analyze(package,pages);if(!analysis.Covered || analysis.Errors.Count==0)return [];
        bool repair=analysis.Repairable && !char.IsControl(anchor[0]) && !char.IsWhiteSpace(anchor[0]);
        return [new(start,1,anchor,anchor,string.Join("; ",analysis.Errors)+(repair?".":". Header/footer hoặc cấu trúc chưa đủ điều kiện tự sửa; giữ nguyên và đối chiếu trong Word."),false,
            FixKind.PageNumber,CanFix:repair,Citation:Citation,Title:analysis.Errors.Count==1?analysis.Errors[0]:"Số trang chưa đúng thể thức",NumberFix:repair?new(analysis.Key,analysis.Migration):null)];
    }
}
