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
        var live = new[] {new WordParagraphSnapshot(0,1,"\r",1,false,true),
            new WordParagraphSnapshot(1,2,"\r",1,false,true), new WordParagraphSnapshot(2,3,"\r",1,false,true)};
        string positioned = (Line(3,"Agency")+Line(2,"Motto")+Line(1,"Subject")).Replace("<wp:anchor>",
            "<wp:anchor simplePos='0'><wp:positionH relativeFrom='page'><wp:posOffset>1270000</wp:posOffset></wp:positionH><wp:positionV relativeFrom='page'><wp:posOffset>2540000</wp:posOffset></wp:positionV><wp:extent cx='2667000' cy='0'/>");
        var inventory = DocxLineInventory.ReadXml(Doc(positioned), live);
        assert(inventory.Lines.Count==3 && !inventory.HasUnmappedShapes && inventory.Lines[0].Id==3 &&
            inventory.Lines[0].AnchorStart==0 && inventory.Lines[2].AnchorStart==2,
            "Package inventory uses verified paragraph offsets, no COM Shape IDs or order");
        assert(inventory.Lines.All(l=>l.AppOwned && l.Geometry==new LineGeometry(100,200,210)),
            "Package declared geometry is retained for comparison with actual XPS strokes");
        var unmapped = DocxLineInventory.ReadXml(Doc(positioned), live.Select(p=>p with {Text="changed\r"}).ToArray());
        assert(unmapped.HasUnmappedShapes && unmapped.Lines.Count==0, "Unmapped anchors block automatic Line creation");
        var foreign = DocxLineInventory.ReadXml(Doc(positioned.Replace("ChinhTaViet.ShapeLine.","User.")),live);
        assert(foreign.Lines.Count==3 && foreign.Lines.All(l=>!l.AppOwned && l.Geometry is null),
            "Foreign lines remain in inventory and cannot be updated as application lines");
        var relative = DocxLineInventory.ReadXml(Doc(positioned.Replace("relativeFrom='page'","relativeFrom='column'")),live);
        assert(relative.Lines.All(l=>l.Geometry is null), "Column-relative geometry cannot be treated as page coordinates");
        string temporary = Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());
        Directory.CreateDirectory(temporary);
        try
        {
            string current=Path.Combine(temporary,"current.docx"), candidate=Path.Combine(temporary,"candidate.docx");
            File.WriteAllText(current,"before");File.WriteAllText(candidate,"candidate");
            int opens=0;
            bool rejected=Reject(()=>CheckpointSwap.Apply(current,candidate,()=>{},()=>
            {
                if(++opens==1)
                {
                    DocxLineMetadata.Verify(valid,Enum.GetValues<LineRole>());
                    if(RenderedLineVerifier.Find(new(100,100,210),new[]{new LineGeometry(100,145,210)}) is null)
                        throw new InvalidDataException("Wrong rendered position");
                }
            }));
            assert(rejected && opens==2 && File.ReadAllText(current)=="before",
                "Valid DOCX identities plus displaced XPS stroke cause rollback, never success");
        }
        finally {Directory.Delete(temporary,true);}
        assert(RenderedLineVerifier.Find(new(100,100,210),new[]{new LineGeometry(100,145,210)}) is null, "Correct metadata cannot accept a displaced rendered line");
    }
}
