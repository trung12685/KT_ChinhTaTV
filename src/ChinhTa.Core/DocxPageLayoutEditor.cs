using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace ChinhTa.Core;

// Writes only pgSz/pgMar attributes in a disposable package. No text/table or
// other ZIP part is rewritten. Candidate verification precedes the Word swap.
public static class DocxPageLayoutEditor
{
    private static readonly XNamespace W=PageLayoutGuard.W;
    private static XDocument Read(ZipArchive zip,string part)
    {using var stream=zip.GetEntry(part)?.Open()??throw new InvalidOperationException("Thiếu phần DOCX: "+part);return XDocument.Load(stream,LoadOptions.PreserveWhitespace);}
    private static XDocument Evidence(ZipArchive zip,XDocument document)
    {
        var root=new XElement("package",new XElement(document.Root!));
        if(zip.GetEntry("word/settings.xml") is not null)root.Add(new XElement(Read(zip,"word/settings.xml").Root!));
        return new(root);
    }
    private static void Patch(XDocument document,PageLayoutFix fix)
    {
        var section=PageLayoutGuard.Body(document)!.Element(W+"sectPr")!;
        void Set(string parent,string attr,double before,double after)
        {
            if(before==after)return;
            section.Element(W+parent)!.SetAttributeValue(W+attr,Math.Round(after/PageLayoutGuard.MmPerTwip,MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture));
        }
        Set("pgSz","w",fix.Before.WidthMm,fix.After.WidthMm);Set("pgSz","h",fix.Before.HeightMm,fix.After.HeightMm);
        if(fix.Before.WidthMm!=fix.After.WidthMm || fix.Before.HeightMm!=fix.After.HeightMm)section.Element(W+"pgSz")!.Attribute(W+"code")?.Remove();
        Set("pgMar","top",fix.Before.TopMm,fix.After.TopMm);Set("pgMar","bottom",fix.Before.BottomMm,fix.After.BottomMm);
        Set("pgMar","left",fix.Before.LeftMm,fix.After.LeftMm);Set("pgMar","right",fix.Before.RightMm,fix.After.RightMm);
    }
    public static void Write(string source,string candidate,PageLayoutFix fix)
    {
        if(!fix.Valid || Path.GetFullPath(source)==Path.GetFullPath(candidate) || File.Exists(candidate))throw new InvalidOperationException("Bản trang dự kiến chưa hợp lệ hoặc không tách khỏi nguồn.");
        XDocument doc;
        using(var zip=ZipFile.OpenRead(source))
        {
            if(zip.Entries.Select(e=>e.FullName).Distinct(StringComparer.Ordinal).Count()!=zip.Entries.Count)throw new InvalidOperationException("DOCX có phần trùng tên.");
            doc=Read(zip,"word/document.xml");
            if(!PageLayoutGuard.Eligible(Evidence(zip,doc),fix.Before,PageNumberPackage.FromFile(source)))throw new InvalidOperationException("Bố cục không còn đủ điều kiện sửa trang.");
        }
        Patch(doc,fix);
        File.Copy(source,candidate);
        try
        {
            using(var zip=ZipFile.Open(candidate,ZipArchiveMode.Update))
            {zip.GetEntry("word/document.xml")!.Delete();using var output=zip.CreateEntry("word/document.xml").Open();doc.Save(output,SaveOptions.DisableFormatting);}
            Verify(source,candidate,fix);
        }
        catch{File.Delete(candidate);throw;}
    }
    public static void Verify(string source,string candidate,PageLayoutFix fix)
    {
        if(!fix.Valid)throw new InvalidOperationException("Không có đề xuất trang hợp lệ để hậu kiểm.");
        using var a=ZipFile.OpenRead(source);using var b=ZipFile.OpenRead(candidate);
        if(!a.Entries.Select(e=>e.FullName).Order().SequenceEqual(b.Entries.Select(e=>e.FullName).Order()))throw new InvalidOperationException("Danh sách phần DOCX thay đổi khi sửa trang.");
        var expected=Read(a,"word/document.xml");Patch(expected,fix);
        if(!XNode.DeepEquals(expected,Read(b,"word/document.xml")))throw new InvalidOperationException("Candidate thay đổi chữ/bảng/thuộc tính ngoài thiết lập trang đã duyệt.");
        foreach(var part in a.Entries.Where(e=>e.FullName!="word/document.xml"))
        {
            using var x=part.Open();using var y=b.GetEntry(part.FullName)!.Open();using var xx=new MemoryStream();using var yy=new MemoryStream();x.CopyTo(xx);y.CopyTo(yy);
            if(!xx.ToArray().SequenceEqual(yy.ToArray()))throw new InvalidOperationException("Phần DOCX khác bị đổi khi sửa trang.");
        }
    }
    public static void VerifyBody(string beforeXml,string afterXml,PageLayoutFix fix)
    {
        if(!fix.Valid)throw new InvalidOperationException("Thiếu đề xuất trang đã duyệt.");
        if(EmptyHeaderFooterScope.HasReferences(XDocument.Parse(beforeXml,LoadOptions.PreserveWhitespace)) &&
            !PageLayoutGuard.Eligible(XDocument.Parse(afterXml,LoadOptions.PreserveWhitespace),fix.After))
            throw new InvalidOperationException("Đầu/cuối trang trống hoặc phạm vi bố cục chưa còn khớp sau mở lại; phải phục hồi.");
        XElement Canonical(string xml,bool patch)
        {
            var doc=XDocument.Parse(xml,LoadOptions.PreserveWhitespace);if(patch)Patch(doc,fix);
            var body=new XElement(PageLayoutGuard.Body(doc)??throw new InvalidOperationException("Không có phần thân để đối chiếu."));
            body.Descendants(W+"lastRenderedPageBreak").Remove();
            body.DescendantsAndSelf().Attributes().Where(a=>a.IsNamespaceDeclaration).Remove();
            foreach(var space in body.DescendantNodes().OfType<XText>().Where(t=>t.Parent?.Name!=W+"t" && string.IsNullOrWhiteSpace(t.Value)).ToArray())space.Remove();
            foreach(var size in body.Descendants(W+"pgSz"))
            {
                if((string?)size.Attribute(W+"orient")=="portrait")size.Attribute(W+"orient")!.Remove();
                if(fix.Before.WidthMm!=fix.After.WidthMm || fix.Before.HeightMm!=fix.After.HeightMm)size.Attribute(W+"code")?.Remove();
            }
            foreach(var section in body.Descendants(W+"sectPr"))
                section.Attributes().Where(a=>a.Name.Namespace==W && a.Name.LocalName.StartsWith("rsid",StringComparison.Ordinal)).Remove();
            return body;
        }
        if(!XNode.DeepEquals(Canonical(beforeXml,true),Canonical(afterXml,false)))throw new InvalidOperationException("Chữ/bảng/kiểu trong phần thân thay đổi ngoài thiết lập trang; phải phục hồi.");
    }
}
