using System.Xml.Linq;

namespace ChinhTa.Core;

// A name's before-spacing is the only approved XML change. Word revision IDs,
// proofing/pagination hints and package core/app timestamps are not content.
public static class SignatureSpaceXmlProof
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main",
        PKG="http://schemas.microsoft.com/office/2006/xmlPackage";
    public static void Verify(string before,string after,int nameIndex)
    {
        XElement Canonical(string value)
        {
            var xml=XDocument.Parse(value);var p=xml.Descendants(W+"document").Single().Element(W+"body")!.Descendants(W+"p")
                .Where(p=>!p.Ancestors(W+"txbxContent").Any()).ElementAt(nameIndex);
            var properties=p.Element(W+"pPr");var spacing=properties?.Element(W+"spacing");
            spacing?.Attributes().Where(a=>a.Name==W+"before" || a.Name==W+"beforeLines" || a.Name==W+"beforeAutospacing").Remove();
            if(spacing is not null && spacing.Attributes().All(a=>a.IsNamespaceDeclaration) && !spacing.HasElements && string.IsNullOrWhiteSpace(spacing.Value))spacing.Remove();
            if(properties is not null && properties.Attributes().All(a=>a.IsNamespaceDeclaration) && !properties.HasElements && string.IsNullOrWhiteSpace(properties.Value))properties.Remove();
            xml.Descendants().Attributes().Where(a=>a.Name.Namespace==W && a.Name.LocalName.StartsWith("rsid",StringComparison.Ordinal)).Remove();
            xml.Descendants().Where(e=>e.Name==W+"proofErr" || e.Name==W+"lastRenderedPageBreak" || e.Name==W+"rsids").Remove();
            xml.Descendants(PKG+"part").Where(e=>(string?)e.Attribute(PKG+"name") is "/docProps/core.xml" or "/docProps/app.xml").Remove();
            foreach(var data in xml.Descendants(PKG+"binaryData"))data.Value=string.Concat(data.Value.Where(c=>!char.IsWhiteSpace(c)));
            return xml.Root!;
        }
        if(!XNode.DeepEquals(Canonical(before),Canonical(after)))
            throw new InvalidOperationException("Có thay đổi ngoài khoảng trước họ tên; phải phục hồi cả lượt.");
    }
}
