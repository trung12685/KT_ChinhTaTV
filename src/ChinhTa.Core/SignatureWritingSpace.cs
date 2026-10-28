using System.Xml.Linq;

namespace ChinhTa.Core;

public sealed record SignatureWritingSpaceTarget(IReadOnlyList<WordParagraphSnapshot> Authorities,WordParagraphSnapshot Name,int NameXmlIndex)
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    public static string Text(WordParagraphSnapshot row)=>row.Text.TrimEnd('\r','\a','\n');
    public static Issue Probe(WordParagraphSnapshot row)=>new(row.Start,Text(row).Length,Text(row),Text(row),"Mốc vùng ký tay.",false,FixKind.SignatureFormat,CanFix:false);
    public static SignatureWritingSpaceTarget Read(string currentXml,IReadOnlyList<WordParagraphSnapshot> live,DocumentProfile profile)
    {
        if(profile!=DocumentProfile.State)throw new InvalidOperationException("Lệnh chừa chỗ ký tay hiện áp dụng khối ký Nhà nước trong bảng cuối đã nhận diện.");
        var snapshot=WordXmlSnapshot.Parse(currentXml);var outline=new Dictionary<int,OutlineParagraphInfo>();
        var formats=new HashSet<int>();var indices=new Dictionary<int,int>();
        var editable=WordTextProof.Read(snapshot,live,_=>{},(s,o)=>outline[s]=o,(s,can)=>{if(can)formats.Add(s);},(s,i)=>indices[s]=i);
        var rows=WordTextProof.Apply(live.Select(p=>new Paragraph(p.Start,p.Text)),editable,outline,formats);
        var roles=SignatureRules.ObservationTargets(rows,profile);
        var name=roles.SingleOrDefault(p=>p.Outline!.SignatureRole==ComponentRole.SignatureName)
            ??throw new InvalidOperationException("Chưa nhận diện đủ chức vụ và họ tên để chừa chỗ ký tay.");
        var authority=roles.Where(p=>p.Outline!.SignatureRole==ComponentRole.SignatureAuthority).OrderBy(p=>p.Start).ToArray();
        if(authority.Length==0 || authority[^1].Start>=name.Start)throw new InvalidOperationException("Thứ tự khối ký chưa rõ.");
        var paragraphs=snapshot.Paragraphs;int first=indices[authority[^1].Start],last=indices[name.Start];
        var cell=paragraphs[last].Parent;
        if(cell?.Name!=W+"tc" || paragraphs[first].Parent!=cell || cell.Descendants().Any(e=>e.Name==W+"drawing" || e.Name==W+"pict" || e.Name==W+"object") ||
            cell.Ancestors(W+"tr").First().Element(W+"trPr")?.Elements(W+"trHeight").Any(e=>(string?)e.Attribute(W+"hRule")=="exact")==true)
            throw new InvalidOperationException("Vùng ký có đối tượng hoặc chiều cao cố định; giữ nguyên để duyệt bố cục thủ công.");
        var byIndex=indices.ToDictionary(p=>p.Value,p=>live.Single(v=>v.Start==p.Key));
        for(int i=first+1;i<last;i++)
        {
            if(paragraphs[i].Parent!=cell || !byIndex.TryGetValue(i,out var row) || !editable.Contains(row.Start) ||
                row.Text.Any(c=>c is not (' ' or '\r' or '\a')) || !Ordinary(paragraphs[i],snapshot.Document))
                throw new InvalidOperationException("Giữa chức vụ và họ tên có nội dung hoặc cấu trúc chưa rõ; chưa tự thay đổi vùng ký.");
        }
        if(!Ordinary(paragraphs[first],snapshot.Document) || !Ordinary(paragraphs[last],snapshot.Document))
            throw new InvalidOperationException("Định dạng đoạn khối ký chưa đủ chắc chắn để chừa khoảng trống.");
        return new(authority.Select(p=>live.Single(v=>v.Start==p.Start)).ToArray(),live.Single(v=>v.Start==name.Start),last);
    }
    private static bool Ordinary(XElement p,XDocument xml)
    {
        var styles=xml.Descendants(W+"styles").FirstOrDefault();var sources=new List<XElement?>{p.Element(W+"pPr")};
        string? id=(string?)sources[0]?.Element(W+"pStyle")?.Attribute(W+"val");
        id??=(string?)styles?.Elements(W+"style").FirstOrDefault(s=>(string?)s.Attribute(W+"type")=="paragraph" && (string?)s.Attribute(W+"default") is "1" or "true")?.Attribute(W+"styleId");
        var seen=new HashSet<string>();
        while(id is not null)
        {
            var found=styles?.Elements(W+"style").Where(s=>(string?)s.Attribute(W+"styleId")==id).ToArray();
            if(!seen.Add(id) || found is not {Length:1} || (string?)found[0].Attribute(W+"type")!="paragraph")return false;
            sources.Add(found[0].Element(W+"pPr"));id=(string?)found[0].Element(W+"basedOn")?.Attribute(W+"val");
        }
        sources.Add(styles?.Element(W+"docDefaults")?.Element(W+"pPrDefault")?.Element(W+"pPr"));
        return !sources.Where(s=>s is not null).SelectMany(s=>s!.Elements()).Any(e=>e.Name==W+"numPr" || e.Name==W+"framePr" || e.Name==W+"sectPr" ||
            e.Name==W+"pageBreakBefore" && (string?)e.Attribute(W+"val") is not ("0" or "false" or "off"));
    }
}

public sealed record SignatureSpaceMeasurement(int AuthorityPage,int NamePage,int PageCount,double? Gap)
{
    public bool Verified=>AuthorityPage>0 && NamePage>0 && PageCount>=Math.Max(AuthorityPage,NamePage) &&
        (AuthorityPage==NamePage?Gap is double g && double.IsFinite(g):Gap is null);
}
public sealed record SignatureSpaceResult(bool Changed,double Gap,float Before,int Attempts);

public static class SignatureWritingSpace
{
    // User preference for handwriting, not a statutory spacing requirement.
    public const double Target=80,Tolerance=3;
    private static float Q(double value)=>(float)(Math.Round(value*20)/20);
    public static SignatureSpaceResult Adjust(float before,SignatureSpaceMeasurement initial,Func<float,SignatureSpaceMeasurement> setAndMeasure)
    {
        if(!float.IsFinite(before) || before<0 || before>720 || !initial.Verified || initial.AuthorityPage!=initial.NamePage)
            throw new InvalidOperationException("Chưa đo được vùng ký trên cùng một trang.");
        if(initial.Gap>=Target-Tolerance)return new(false,initial.Gap.Value,before,0);
        float low=Q(before),high=Q(Math.Min(720,low+Math.Max(80,2*(Target-initial.Gap!.Value))));bool upper=false;
        for(int attempt=1;attempt<=12;attempt++)
        {
            float value=upper?Q((low+high)/2):high;
            if(value<=low || value>720)break;
            var measured=setAndMeasure(value);
            if(!measured.Verified)throw new InvalidOperationException("Mất số đo khối ký sau thay đổi; phải phục hồi.");
            bool fits=measured.AuthorityPage==initial.AuthorityPage && measured.NamePage==initial.NamePage && measured.PageCount==initial.PageCount;
            if(fits && measured.Gap is double gap && Math.Abs(gap-Target)<=Tolerance)return new(true,gap,value,attempt);
            if(!fits || measured.Gap>Target+Tolerance){high=value;upper=true;}
            else{low=value;if(!upper)high=Q(Math.Min(720,Math.Max(low+80,low*2+20)));}
        }
        throw new InvalidOperationException("Bố cục hiện tại chưa chừa được khoảng 80 pt trên trang này; phải phục hồi.");
    }
    public static SignatureSpaceResult Apply(float before,SignatureSpaceMeasurement initial,Func<float,SignatureSpaceMeasurement> setAndMeasure,
        Action<SignatureSpaceResult> verify,Action commit,Action restore)
    {
        bool started=false;
        try
        {
            var result=Adjust(before,initial,value=>{started=true;return setAndMeasure(value);});
            if(result.Changed){verify(result);commit();}return result;
        }
        catch(Exception failure)
        {
            if(started)try{restore();}catch(Exception recovery){throw new AggregateException("Chưa phục hồi được vùng ký.",failure,recovery);}
            throw;
        }
    }
}
