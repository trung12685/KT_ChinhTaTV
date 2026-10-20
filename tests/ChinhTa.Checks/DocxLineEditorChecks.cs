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
                p.Text,p.Page,p.InTable,p.Centered)).ToArray();
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
            XNamespace o="urn:schemas-microsoft-com:office:office";
            XNamespace w10="urn:schemas-microsoft-com:office:word";
            XDocument ReadDoc(string path)
            {
                using var zip=ZipFile.OpenRead(path); using var stream=zip.GetEntry("word/document.xml")!.Open();
                return XDocument.Load(stream,LoadOptions.PreserveWhitespace);
            }
            var result=ReadDoc(edited); var lines=result.Descendants(v+"line").ToArray();
            assert(lines.Length==3 && lines.Select(l=>(string?)l.Attribute("id")).Distinct().Count()==3,
                "Gói DOCX chứa ba Shape Line có ID khác nhau");
            assert(lines.All(l=>(string?)l.Attribute(o+"allowincell")=="f" &&
                (string?)l.Element(w10+"wrap")?.Attribute("type")=="none" &&
                ((string?)l.Attribute("style"))!.Contains("relative:page")),
                "Line neo theo trang, không wrap và không bị giới hạn trong ô");
            assert(lines.All(l=>l.Attribute("from")!.Value.Split(',')[1]==l.Attribute("to")!.Value.Split(',')[1]),
                "Hai điểm đầu cuối đồng mức, không cần COM Rotation/Height");
            var validator=new OpenXmlValidator(FileFormatVersions.Office2019);
            foreach(var node in lines)
            {
                var errors=validator.Validate(new DocumentFormat.OpenXml.Vml.Line(node.ToString())).ToArray();
                if(errors.Length>0)throw new InvalidOperationException(string.Join("\n",errors.Select(e=>e.Description)));
            }
            assert(true, "Ba VML Shape Line hợp lệ theo bộ kiểm chuẩn OpenXML SDK");
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
            assert(ReadDoc(editedAgain).Descendants(v+"line").Count()==3,
                "Sửa lại ba Line có marker không tạo trùng");
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
