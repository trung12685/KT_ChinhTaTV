using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Runs only as part of the synthetic technical-text Word group. Building this
// fixture or checking its XML does not establish that Microsoft Word passed it.
internal static class OfficeAnnotatedTextCheck
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    internal const string First="Nội dung tự do đã  duyệt.";
    internal const string FixedFirst="Nội dung tự do đã duyệt.";
    internal const string Marked="Nội dung  kế hoạch kế hoạch đã duyệt.";
    internal const string CrossFirst="Phần tham chiếu đầu đã  duyệt.";
    internal const string CrossLast="Phần tham chiếu cuối đã  duyệt.";
    internal const string Last="Văn bản độc lập đã  duyệt.";
    internal const string FixedLast="Văn bản độc lập đã duyệt.";
    internal static IReadOnlyList<string> Rows {get;}=Array.AsReadOnly(new[]{First,Marked,CrossFirst,CrossLast,Last});
    internal static IReadOnlyList<string> Names {get;}=Array.AsReadOnly(new[]{"CTV_KEEP_SAME","CTV_KEEP_CROSS"});
    private static XElement Run(string text)=>new(W+"r",new XElement(W+"rPr",
        new XElement(W+"rFonts",new XAttribute(W+"ascii","Times New Roman"),new XAttribute(W+"hAnsi","Times New Roman")),
        new XElement(W+"sz",new XAttribute(W+"val",26))),
        new XElement(W+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
    private static XElement Row(string text)=>new(W+"p",Run(text));
    private static XElement Start(int id,string name)=>new(W+"bookmarkStart",new XAttribute(W+"id",id),new XAttribute(W+"name",name));
    private static XElement End(int id)=>new(W+"bookmarkEnd",new XAttribute(W+"id",id));

    internal static string Create(string seed,string folder)
    {
        string path=Path.Combine(folder,"annotated-text-input.docx");File.Copy(seed,path,false);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        var body=xml.Root!.Element(W+"body")!;var section=body.Elements(W+"sectPr").SingleOrDefault();
        if(section is null || body.Descendants(W+"sectPr").Count()!=1)
            throw new InvalidOperationException("Mẫu bookmark cần một section rõ ràng.");
        body.ReplaceNodes(Row(First),new XElement(W+"p",Start(7,Names[0]),Run(Marked),End(7)),
            new XElement(W+"p",Start(8,Names[1]),Run(CrossFirst)),new XElement(W+"p",Run(CrossLast),End(8)),
            Row(Last),new XElement(section));
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        return path;
    }

    internal static IReadOnlyDictionary<string,string> ReadAnnotations(string path)
    {
        // The checkpoint may still have a Word handle. Evidence reads never
        // call Save or set Saved and release this handle before the next action.
        using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        using var zip=new ZipArchive(input,ZipArchiveMode.Read);
        using var main=zip.GetEntry("word/document.xml")!.Open();
        return ReadAnnotations(XDocument.Load(main));
    }
    internal static IReadOnlyDictionary<string,string> ReadAnnotations(XDocument xml)
    {
        var body=xml.Root?.Element(W+"body")??throw new InvalidOperationException("Mẫu thiếu body.");
        var starts=body.Descendants(W+"bookmarkStart").Where(e=>Names.Contains((string?)e.Attribute(W+"name")??"")).ToArray();
        if(starts.Length!=Names.Count || Names.Any(name=>starts.Count(e=>(string?)e.Attribute(W+"name")==name)!=1))
            throw new InvalidOperationException("Tên bookmark bị thiếu hoặc trùng.");
        var byId=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var start in starts)
        {
            string id=(string?)start.Attribute(W+"id")??throw new InvalidOperationException("Bookmark thiếu ID.");
            if(!byId.TryAdd(id,(string)start.Attribute(W+"name")!) ||
                body.Descendants(W+"bookmarkStart").Count(e=>(string?)e.Attribute(W+"id")==id)!=1 ||
                body.Descendants(W+"bookmarkEnd").Count(e=>(string?)e.Attribute(W+"id")==id)!=1)
                throw new InvalidOperationException("Mốc bookmark không duy nhất.");
        }
        var active=new HashSet<string>(StringComparer.Ordinal);var seen=new HashSet<string>(StringComparer.Ordinal);
        var ended=new HashSet<string>(StringComparer.Ordinal);var text=Names.ToDictionary(name=>name,_=>new StringBuilder(),StringComparer.Ordinal);
        void Visit(XElement element)
        {
            string? id=(string?)element.Attribute(W+"id");
            if(element.Name==W+"bookmarkStart" && id is not null && byId.TryGetValue(id,out string? name))
            {
                if(!seen.Add(name) || !active.Add(name))throw new InvalidOperationException("Mốc đầu bookmark bị lặp.");
            }
            else if(element.Name==W+"bookmarkEnd" && id is not null && byId.TryGetValue(id,out name))
            {
                if(!active.Remove(name) || !ended.Add(name))throw new InvalidOperationException("Mốc cuối bookmark đảo hoặc lặp.");
            }
            else if(element.Name==W+"t")foreach(string current in active)text[current].Append(element.Value);
            else if(element.Name==W+"tab")foreach(string current in active)text[current].Append('\t');
            else if(element.Name==W+"br" || element.Name==W+"cr")foreach(string current in active)text[current].Append('\n');
            else foreach(var child in element.Elements())Visit(child);
            if(element.Name==W+"p")foreach(string current in active)text[current].Append('\r');
        }
        Visit(body);
        if(active.Count!=0 || ended.Count!=Names.Count)throw new InvalidOperationException("Bookmark chưa khép kín.");
        return text.ToDictionary(pair=>pair.Key,pair=>pair.Value.ToString(),StringComparer.Ordinal);
    }

    internal static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        string source=Create(seed,folder);byte[] digest=SHA256.HashData(File.ReadAllBytes(source));
        var annotations=ReadAnnotations(source);
        test.Profile=DocumentProfile.Personal;test.Open(source);string original=test.ReadCheckText();test.Check();
        var protectedStyles=new[]{Marked,CrossFirst,CrossLast}.Select(test.ReadCheckCurrentStyles).ToArray();
        bool Mechanical(Issue issue)=>issue.CanFix && !issue.IsStyleOnly;
        void VerifyKept()
        {
            var current=test.Check();var actual=ReadAnnotations(test.RecoveryPath);
            if(annotations.Count!=actual.Count || annotations.Any(pair=>!actual.TryGetValue(pair.Key,out string? value) || value!=pair.Value) ||
                !new[]{Marked,CrossFirst,CrossLast}.All(row=>current.Any(i=>!i.CanFix && i.Original==row)) ||
                !protectedStyles.All(style=>style.Matches(test.ReadCheckCurrentStyles(style.Text))))
                throw new InvalidOperationException("Bookmark/tên/vùng chữ hoặc kiểu của phần chưa chọn đã bị đổi.");
        }
        Issue[] Select()
        {
            var selected=test.Check().Where(Mechanical).OrderBy(i=>i.Start).ToArray();
            if(selected.Length!=2 || selected.Any(i=>i.Kind!=FixKind.Text || i.Original!="  " || i.Replacement!=" "))
                throw new InvalidOperationException("Mẫu bookmark chưa giữ thủ công vùng đánh dấu và chỉ chọn hai khoảng văn xuôi độc lập.");
            return selected;
        }
        void VerifyBaseline()
        {
            if(test.ReadCheckText()!=original || test.CheckUndoCount!=0)
                throw new InvalidOperationException("Mẫu bookmark chưa phục hồi nguyên văn và một Undo.");
            VerifyKept();
        }
        VerifyKept();Select();
        if(test.Changed || test.CheckUndoCount!=0 || !Rows.All(row=>original.Contains(row+"\r",StringComparison.Ordinal)))
            throw new InvalidOperationException("Quét ban đầu đã đổi trạng thái mẫu bookmark.");
        test.Fix(Select()[0]);
        if(test.ReadCheckText()!=original.Replace(First,FixedFirst,StringComparison.Ordinal) || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Sửa riêng đã đổi phần ngoài khoảng văn xuôi đầu tiên.");
        VerifyKept();test.Undo();VerifyBaseline();
        var selected=Select();bool restored=false,previousChanged=test.Changed;
        try{test.FixTextFailureProbe(selected,selected.Length);}
        catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){restored=true;}
        if(!restored || test.Changed!=previousChanged)throw new InvalidOperationException("Lỗi cuối lượt mẫu bookmark chưa phục hồi.");
        VerifyBaseline();test.FixMany(Select());
        string final=original.Replace(First,FixedFirst,StringComparison.Ordinal).Replace(Last,FixedLast,StringComparison.Ordinal);
        void VerifyFixed()
        {
            if(test.ReadCheckText()!=final || test.Check().Any(Mechanical))
                throw new InvalidOperationException("Sửa/lưu-mở mẫu bookmark chưa giữ đúng chữ và quyền sửa.");
            VerifyKept();
        }
        VerifyFixed();
        if(test.CheckUndoCount!=1)throw new InvalidOperationException("Sửa nhóm mẫu bookmark chưa tạo đúng một Undo.");
        string saved=Path.Combine(folder,"annotated-text-fixed.docx");test.SaveCopy(saved);
        var savedAnnotations=ReadAnnotations(saved);
        if(annotations.Any(pair=>!savedAnnotations.TryGetValue(pair.Key,out string? value) || value!=pair.Value))
            throw new InvalidOperationException("Bản sao đã đổi tên/vùng chữ bookmark.");
        test.Undo();VerifyBaseline();test.Open(saved);VerifyFixed();
        if(!digest.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))
            throw new InvalidOperationException("Nguồn mẫu bookmark đã bị đổi.");
        pass("Bookmark cùng/qua đoạn có lỗi chữ: giữ thủ công; văn xuôi riêng sửa đơn/nhóm, lỗi cuối lượt/rollback, một Undo, lưu-mở giữ tên/vùng chữ/kiểu và nguồn bất biến");
    }
}
