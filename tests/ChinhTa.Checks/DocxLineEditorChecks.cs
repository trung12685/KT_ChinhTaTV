using ChinhTa.Core;
using System.IO.Compression;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

public static class DocxLineEditorChecks
{
    public static void Run(Action<bool,string> assert, string? privateFixture)
    {
        string folder = Path.Combine(Path.GetTempPath(), "ChinhTaPackageChecks-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            string source = privateFixture ?? Path.GetFullPath("samples/BaDuongKeThieu.docx");
            var layout = DocxLineFixture.Read(source);
            var live = layout.Select(p=>new WordParagraphSnapshot(p.Start,
                p.Start+p.Text.Length+(p.InTable && p.CellEnd==p.Start+p.Text.Length+1?1:0),
                p.Text,p.Page,p.InTable,p.Centered,p.ParagraphCentered)).ToArray();
            var targets = ShapeLineRules.Identify(DocxLineFixture.ReadThroughXmlMap(source),DocumentProfile.State);
            var issues = targets.Select(t=>t.Probe with { CanFix=true,
                LineFix=ShapeLineRules.Evaluate(t,new(100,100,210),null,null,LinePolicy.Decree30).Fix }).ToArray();
            byte[] original = File.ReadAllBytes(source);
            string edited = Path.Combine(folder,"edited.docx");
            DocxLineEditor.Write(source,edited,live,targets,issues);
            assert(File.ReadAllBytes(source).SequenceEqual(original), "Ghi ba Line vào candidate, nguồn giữ nguyên từng byte");
            string active=Path.Combine(folder,"active-in-word.docx"),before=Path.Combine(folder,"before-unlocked.docx");
            File.Copy(source,active); File.Copy(active,before);
            using(var locked=new FileStream(active,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            {
                string lockedCandidate=Path.Combine(folder,"from-unlocked-backup.docx");
                DocxLineEditor.Write(before,lockedCandidate,live,targets,issues);
                assert(File.Exists(lockedCandidate) && locked.Length==original.Length,
                    "Tệp làm việc đang khóa: ghi từ checkpoint riêng, không mở ZIP trên tệp đang dùng");
            }
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XNamespace v="urn:schemas-microsoft-com:vml";
            XNamespace wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
            XNamespace drawing="http://schemas.openxmlformats.org/drawingml/2006/main";
            XDocument ReadDoc(string path)
            {
                using var zip=ZipFile.OpenRead(path); using var stream=zip.GetEntry("word/document.xml")!.Open();
                return XDocument.Load(stream,LoadOptions.PreserveWhitespace);
            }
            var result=ReadDoc(edited); var lines=result.Descendants(w+"drawing").ToArray();
            assert(lines.Length==3 && lines.Descendants(wp+"docPr").Select(l=>(string?)l.Attribute("id")).Distinct().Count()==3,
                "Gói DOCX chứa ba DrawingML Line có ID khác nhau");
            assert(lines.All(l=>l.Element(wp+"anchor")?.Attribute("layoutInCell")?.Value=="0" &&
                l.Descendants(wp+"wrapNone").Any() && l.Descendants(wp+"positionH").Single().Attribute("relativeFrom")?.Value=="page" &&
                l.Descendants(wp+"positionV").Single().Attribute("relativeFrom")?.Value=="page"),
                "DrawingML neo theo trang, không wrap và không bị giới hạn trong ô");
            assert(lines.All(l=>l.Descendants(drawing+"prstGeom").Single().Attribute("prst")?.Value=="line" &&
                l.Descendants(wp+"extent").Single().Attribute("cy")?.Value=="0"),
                "Line ngang, không cần COM Rotation/Height");
            assert(lines.Select((l,i)=>(l,i)).All(pair=>pair.l.Descendants(drawing+"off").Single().Attribute("x")?.Value=="0" &&
                pair.l.Descendants(drawing+"off").Single().Attribute("y")?.Value=="0" &&
                pair.l.Descendants(wp+"posOffset").Select(x=>long.Parse(x.Value)).SequenceEqual(new[]{(long)Math.Round(issues[pair.i].LineFix!.Left*12700),(long)Math.Round(issues[pair.i].LineFix!.Top*12700)})),
                "Tọa độ trang bằng EMU và transform cục bộ gốc 0");
            var validator=new OpenXmlValidator(FileFormatVersions.Office2019);
            foreach(var node in lines)
            {
                var errors=validator.Validate(new DocumentFormat.OpenXml.Wordprocessing.Drawing(node.ToString())).ToArray();
                if(errors.Length>0)throw new InvalidOperationException(string.Join("\n",errors.Select(e=>e.Description)));
            }
            assert(true, "Ba DrawingML Line hợp lệ theo bộ kiểm chuẩn OpenXML SDK");
            string compensated=Path.Combine(folder,"compensated.docx");
            var offsets=issues.Select(i=>i with {LineFix=i.LineFix! with {Left=i.LineFix!.Left-30,Top=i.LineFix!.Top-20}}).ToArray();
            DocxLineEditor.Write(source,compensated,live,targets,offsets,issues);
            var intentLines=DocxLineInventory.Read(compensated,live).Lines;
            assert(intentLines.Count==3 && intentLines.All(l=>issues.Any(i=>l.Geometry==new LineGeometry(i.LineFix!.Left,i.LineFix!.Top,i.LineFix!.Width))),
                "Rescan uses original intended page geometry on measured-correction Lines");
            foreach(var node in ReadDoc(compensated).Descendants(w+"drawing"))
                if(validator.Validate(new DocumentFormat.OpenXml.Wordprocessing.Drawing(node.ToString())).Any())
                    throw new InvalidOperationException("Compensated drawing violates schema");
            assert(true,"Measured-correction intent uses schema-valid drawing metadata");
            var originalDoc=ReadDoc(source);
            foreach(var line in lines)line.Ancestors(w+"r").First().Remove();
            assert(XNode.DeepEquals(originalDoc.Root,result.Root), "Nội dung chữ, paragraph/table formatting và XML cũ giữ nguyên");
            using(var a=ZipFile.OpenRead(source))
            using(var b=ZipFile.OpenRead(edited))
            {
                bool same=a.Entries.Count==b.Entries.Count;
                foreach(var entry in a.Entries.Where(e=>e.FullName!="word/document.xml"))
                {
                    using var x=entry.Open(); using var y=b.GetEntry(entry.FullName)!.Open();
                    using var xb=new MemoryStream(); using var yb=new MemoryStream(); x.CopyTo(xb); y.CopyTo(yb);
                    same &= xb.ToArray().SequenceEqual(yb.ToArray());
                }
                assert(same, "Mọi part ngoài document.xml giữ nguyên, gồm style/ảnh/quan hệ");
            }
            string[] SchemaIssues(string path)
            {
                using var doc=WordprocessingDocument.Open(path,false);
                return validator.Validate(doc).Select(e=>e.Id+"|"+e.Path?.XPath+"|"+e.Description).ToArray();
            }
            var beforeErrors=SchemaIssues(source).ToHashSet();
            var afterErrors=SchemaIssues(edited);
            var addedErrors=afterErrors.Where(e=>!beforeErrors.Contains(e)).ToArray();
            if(addedErrors.Length>0)throw new InvalidOperationException(string.Join("\n",addedErrors));
            assert(true,"Thêm Line không tạo lỗi chuẩn mới cho toàn bộ gói DOCX");
            string editedAgain=Path.Combine(folder,"edited-again.docx");
            DocxLineEditor.Write(edited,editedAgain,live,targets,issues.Select(i=>i with {LineFix=i.LineFix! with {ShapeId=42,Width=80}}).ToArray());
            assert(ReadDoc(editedAgain).Descendants(w+"drawing").Count()==3 &&
                !ReadDoc(editedAgain).Descendants(w+"pict").Any(),
                "Sửa lại ba DrawingML Line không tạo trùng hoặc để lại pict rỗng");
            bool rejected=false;
            try { DocxLineEditor.Write(source,Path.Combine(folder,"foreign.docx"),live,targets,
                [issues[0] with {LineFix=issues[0].LineFix! with {ShapeId=42}}]); }
            catch(InvalidOperationException) {rejected=true;}
            assert(rejected && File.ReadAllBytes(source).SequenceEqual(original),
                "Không sửa Shape có sẵn không rõ marker, không thay nguồn khi từ chối");
            rejected=false;
            try { DocxLineEditor.Write(source,source,live,targets,issues); }
            catch(InvalidOperationException) {rejected=true;}
            assert(rejected,"Không cho ghi Line đè trực tiếp nguồn");
            CheckSwap(folder,assert);
        }
        finally {Directory.Delete(folder,true);}
    }
    public static void CheckSavedRepair(Action<bool,string> assert,string saved)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTaLegacyRepair-"+Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XNamespace v="urn:schemas-microsoft-com:vml";
            XDocument Read(string path)
            {
                using var zip=ZipFile.OpenRead(path);using var stream=zip.GetEntry("word/document.xml")!.Open();
                return XDocument.Load(stream,LoadOptions.PreserveWhitespace);
            }
            var old=Read(saved); byte[] unchanged=File.ReadAllBytes(saved);
            var legacy=old.Descendants(v+"line").Where(l=>((string?)l.Attribute("alt"))?.StartsWith("ChinhTaViet.ShapeLine.")==true).ToArray();
            assert(legacy.Length==2,"Bản sau sửa được cung cấp có hai Line, thiếu Line trích yếu");
            var layout=DocxLineFixture.Read(saved);
            var live=layout.Select(p=>new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length+
                (p.InTable && p.CellEnd==p.Start+p.Text.Length+1?1:0),p.Text,p.Page,p.InTable,p.Centered,p.ParagraphCentered)).ToArray();
            var targets=ShapeLineRules.Identify(DocxLineFixture.ReadThroughXmlMap(saved),DocumentProfile.State);
            assert(targets.Count==3,"Bản sau sửa vẫn nhận diện đủ ba mốc kể cả dòng đầu trích yếu có khoảng trắng cuối");
            var plan=targets.Select(t=>
            {
                var existing=legacy.SingleOrDefault(l=>(string?)l.Attribute("alt")=="ChinhTaViet.ShapeLine."+t.Role);
                ShapeLineFix fix;
                if(existing is not null)
                {
                    double[] Pair(string value)=>value.Split(',').Select(s=>double.Parse(s.Replace("pt",""),System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    var from=Pair(existing.Attribute("from")!.Value);var to=Pair(existing.Attribute("to")!.Value);
                    fix=new(42,from[0],from[1],to[0]-from[0],t.Role);
                }
                else fix=new(null,200,230,100,t.Role); // Synthetic reference: not a claim about real rendered placement.
                return t.Probe with {CanFix=true,LineFix=fix};
            }).ToArray();
            string candidate=Path.Combine(folder,"repaired.docx");
            DocxLineEditor.Write(saved,candidate,live,targets,plan);
            XNamespace wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
            var result=Read(candidate);var lines=result.Descendants(w+"drawing").ToArray();
            assert(lines.Length==3 && !result.Descendants(w+"pict").Any() && !result.Descendants(v+"line").Any(),
                "Chuyển hai VML Line cũ và thêm Line trích yếu thành ba DrawingML, không tạo rác pict");
            foreach(var issue in plan)
            {
                var fix=issue.LineFix!;
                var line=lines.Single(l=>(string?)l.Descendants(wp+"docPr").Single().Attribute("descr")=="ChinhTaViet.ShapeLine."+fix.Role);
                double Offset(string position)=>long.Parse(line.Descendants(wp+position).Single().Element(wp+"posOffset")!.Value)/12700d;
                double width=long.Parse(line.Descendants(wp+"extent").Single().Attribute("cx")!.Value)/12700d;
                assert(Math.Abs(Offset("positionH")-fix.Left)<.0001 && Math.Abs(Offset("positionV")-fix.Top)<.0001 &&
                    Math.Abs(width-fix.Width)<.0001,
                    "Bản sửa giữ tọa độ đề xuất bằng vị trí trang EMU: "+fix.Role);
            }
            assert(old.Descendants(w+"t").Select(t=>t.Value).SequenceEqual(result.Descendants(w+"t").Select(t=>t.Value)) &&
                File.ReadAllBytes(saved).SequenceEqual(unchanged),"Sửa legacy không đổi chữ và không ghi đè tệp người dùng");
            var validator=new OpenXmlValidator(FileFormatVersions.Office2019);
            using var before=WordprocessingDocument.Open(saved,false);
            var baseline=validator.Validate(before).Select(e=>e.Id+"|"+e.Description).ToHashSet();
            using var after=WordprocessingDocument.Open(candidate,false);
            var errors=validator.Validate(after).Where(e=>!baseline.Contains(e.Id+"|"+e.Description)).ToArray();
            if(errors.Length>0)throw new InvalidOperationException(string.Join("\n",errors.Select(e=>e.Description)));
            assert(true,"Bản phục hồi từ file đặt sai không tạo lỗi chuẩn OpenXML mới");
        }
        finally {Directory.Delete(folder,true);}
    }
    private static void CheckSwap(string folder,Action<bool,string> assert)
    {
        string current=Path.Combine(folder,"working.docx"),candidate=Path.Combine(folder,"candidate.docx");
        File.WriteAllText(current,"before"); File.WriteAllText(candidate,"after");
        int opens=0,closes=0;
        CheckpointSwap.Apply(current,candidate,()=>closes++,()=>{opens++;if(File.ReadAllText(current)!="after")throw new InvalidOperationException();});
        assert(opens==1 && closes==1 && File.ReadAllText(current)=="after","Thay checkpoint sau khi đóng và mở lại đúng candidate");
        File.WriteAllText(current,"before"); opens=closes=0;
        bool failed=false;
        try {CheckpointSwap.Apply(current,candidate,()=>closes++,()=>{if(++opens==1)throw new IOException("Open failed");});}
        catch(IOException){failed=true;}
        assert(failed && opens==2 && closes==2 && File.ReadAllText(current)=="before",
            "Word mở candidate thất bại: tự khôi phục và mở lại checkpoint trước");
        failed=false; opens=0;
        try {CheckpointSwap.Apply(current,candidate,()=>throw new IOException("Close failed"),()=>opens++);}
        catch(IOException){failed=true;}
        assert(failed && opens==0 && File.ReadAllText(current)=="before","Đóng Word thất bại thì không thay file đang dùng");
        failed=false;
        try {CheckpointSwap.Apply(current,candidate,()=>{},()=>throw new IOException("Always fail"));}
        catch(AggregateException){failed=true;}
        assert(failed && Directory.GetFiles(folder,"*.rollback.docx").Length==1,
            "Nếu khôi phục cũng không mở được, giữ rollback file để phục hồi thủ công");
    }
}

