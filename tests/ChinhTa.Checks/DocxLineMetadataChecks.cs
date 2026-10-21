using System.Xml.Linq;
using ChinhTa.Core;

static class DocxLineMetadataChecks
{
    public static void Run(Action<bool, string> assert)
    {
        const string start = "<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main' xmlns:wp='http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing' xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main' xmlns:wps='http://schemas.microsoft.com/office/word/2010/wordprocessingShape'><w:body>";
        string Line(int id, string role, string geom = "line") => $"<w:p><w:r><w:drawing><wp:anchor><wp:docPr id='{id}' descr='ChinhTaViet.ShapeLine.{role}'/><wps:wsp><wps:spPr><a:prstGeom prst='{geom}'/></wps:spPr></wps:wsp></wp:anchor></w:drawing></w:r></w:p>";
        XDocument Doc(string body) => XDocument.Parse(start + body + "</w:body></w:document>");
        var valid = DocxLineMetadata.ReadXml(Doc(Line(3,"Agency")+Line(2,"Motto")+Line(1,"Subject")));
        DocxLineMetadata.Verify(valid, Enum.GetValues<LineRole>());
        assert(valid.Single(m=>m.Role==LineRole.Agency).Id==3, "DOCX marker maps by ID, independent of document order");
        bool Reject(Action action) { try { action(); return false; } catch (InvalidDataException) { return true; } }
        assert(Reject(()=>DocxLineMetadata.ReadXml(Doc(Line(1,"Agency")+Line(2,"Agency")))), "Reject duplicate role");
        assert(Reject(()=>DocxLineMetadata.ReadXml(Doc(Line(1,"Agency")+Line(1,"Motto")))), "Reject duplicate drawing ID");
        assert(Reject(()=>DocxLineMetadata.ReadXml(Doc(Line(1,"Unknown")))), "Reject unknown marker role");
        assert(Reject(()=>DocxLineMetadata.Verify(valid, new[]{(LineRole)99})), "Reject missing marker");
        assert(Reject(()=>DocxLineMetadata.Verify(DocxLineMetadata.ReadXml(Doc(Line(1,"Agency","rect"))),new[]{LineRole.Agency})), "Shape count does not validate a non-line marker");
        assert(RenderedLineVerifier.Find(new(100,100,210),new[]{new LineGeometry(100,145,210)}) is null, "Correct metadata cannot accept a displaced rendered line");
    }
}
