using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChinhTa.Core;

if(args.Length is not (3 or 4) || args[0] is not ("prepare" or "manifest" or "verify"))
    throw new ArgumentException("Usage: PreviewMetadata prepare <folder> <version> <preview-guide> | manifest|verify <folder> <version>");
string command=args[0],folder=Path.GetFullPath(args[1]),version=args[2];
if(!Regex.IsMatch(version,@"\A0\.[0-9]+\.[0-9]+\z",RegexOptions.CultureInvariant))throw new InvalidOperationException("Invalid application version.");
var evidence=ReportEvidence.Parse(File.ReadAllText(Path.Combine(folder,"build-checks.json")),version)
    ??throw new InvalidOperationException("The package needs successful, current-version Core/WPF evidence from its exact source commit.");
string[] required=["ChinhTa.Desktop.exe","ChinhTa.Desktop.dll","ChinhTa.Core.dll","ChinhTa.Desktop.deps.json",
    "ChinhTa.Desktop.runtimeconfig.json","hostfxr.dll","hostpolicy.dll","coreclr.dll","PresentationFramework.dll","WindowsBase.dll"];
foreach(string name in required)
    if(!File.Exists(Path.Combine(folder,name)) || new FileInfo(Path.Combine(folder,name)).Length==0)
        throw new InvalidOperationException("Missing self-contained Windows package file: "+name);
using(var executable=File.OpenRead(Path.Combine(folder,"ChinhTa.Desktop.exe")))
using(var reader=new BinaryReader(executable))
{
    if(executable.Length<64 || reader.ReadByte()!='M' || reader.ReadByte()!='Z')throw new InvalidOperationException("The package entry point is not a Windows executable.");
    executable.Position=0x3c;int header=reader.ReadInt32();
    if(header<64 || header>executable.Length-6)throw new InvalidOperationException("Invalid executable PE header.");
    executable.Position=header;
    if(reader.ReadUInt32()!=0x00004550 || reader.ReadUInt16()!=0x8664)throw new InvalidOperationException("The preview entry point must target Windows x64.");
}
using(var config=JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,"ChinhTa.Desktop.runtimeconfig.json"))))
{
    var options=config.RootElement.GetProperty("runtimeOptions");
    if(options.TryGetProperty("framework",out _) || options.TryGetProperty("frameworks",out _) ||
        !options.TryGetProperty("includedFrameworks",out var frameworks) ||
        !frameworks.EnumerateArray().Any(f=>f.GetProperty("name").GetString()=="Microsoft.WindowsDesktop.App"))
        throw new InvalidOperationException("The preview must include its own WindowsDesktop runtime.");
}
if(!Directory.Exists(Path.Combine(folder,"samples")) || !Directory.EnumerateFiles(Path.Combine(folder,"samples"),"*.docx").Any())
    throw new InvalidOperationException("Missing synthetic DOCX samples.");
string reportName=$"ChinhTaViet-{version}-tien-do.txt",guideName=$"PREVIEW-{version}.md";
string checklistName=$"SUPPORTED-FIXES-{version}.md",officeName=$"OFFICE-CHECK-{version}.md";
const string manifestName="FILES.sha256",previewManifestName="preview-manifest.json";
static string Cell(string value)=>value.Replace("|","\\|",StringComparison.Ordinal).Replace("\r\n","<br>",StringComparison.Ordinal).Replace("\n","<br>",StringComparison.Ordinal);
void WriteNew(string name,string value)
{
    using var output=new FileStream(Path.Combine(folder,name),FileMode.CreateNew,FileAccess.Write,FileShare.None);
    using var writer=new StreamWriter(output,new UTF8Encoding(true));writer.Write(value);
}
string[] Files()
{
    var pending=new Stack<DirectoryInfo>();pending.Push(new(folder));var files=new List<string>();
    while(pending.Count>0)
    {
        var directory=pending.Pop();
        if((directory.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Package symlinks/reparse points are unsupported.");
        foreach(var entry in directory.EnumerateFileSystemInfos())
        {
            if((entry.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Package symlinks/reparse points are unsupported.");
            if(entry is DirectoryInfo child)pending.Push(child);
            else files.Add(Path.GetRelativePath(folder,entry.FullName).Replace('\\','/'));
        }
    }
    return files.Order(StringComparer.Ordinal).ToArray();
}
string Hash(string name)
{
    using var input=File.OpenRead(Path.Combine(folder,name.Replace('/',Path.DirectorySeparatorChar)));
    return Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
}
void VerifyMetadata()
{
    foreach(string name in new[]{guideName,checklistName,officeName,reportName,"KIEM-TRA-WORD.cmd"})
        if(!File.Exists(Path.Combine(folder,name)))throw new InvalidOperationException("Missing preview validation metadata: "+name);
    string checklist=File.ReadAllText(Path.Combine(folder,checklistName));
    if(!DevelopmentReport.Capabilities.All(c=>checklist.Contains($"| {c.Id} | {c.Name} |",StringComparison.Ordinal) && checklist.Contains(Cell(c.Example.Before),StringComparison.Ordinal) && checklist.Contains(Cell(c.Example.Outcome),StringComparison.Ordinal)) ||
        !DevelopmentReport.PlannedWork.All(p=>checklist.Contains($"| {p.Id} | {p.Name} |",StringComparison.Ordinal) && checklist.Contains(Cell(p.Example.Before),StringComparison.Ordinal) && checklist.Contains(Cell(p.Example.Outcome),StringComparison.Ordinal)))
        throw new InvalidOperationException("The packaged capability checklist does not match current code.");
    string report=File.ReadAllText(Path.Combine(folder,reportName)).Replace("\r\n","\n",StringComparison.Ordinal);
    var createdLines=report.Split('\n').Where(line=>line.StartsWith("Tạo lúc: ",StringComparison.Ordinal)).ToArray();
    if(createdLines.Length!=1 || !DateTimeOffset.TryParseExact(createdLines[0]["Tạo lúc: ".Length..],"yyyy-MM-dd HH:mm zzz",CultureInfo.InvariantCulture,DateTimeStyles.None,out var created) ||
        report!=DevelopmentReport.Build(version,created,evidence).Replace("\r\n","\n",StringComparison.Ordinal))
        throw new InvalidOperationException("The packaged progress report does not match current code and build evidence.");
    using var metadata=JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,previewManifestName)));
    var root=metadata.RootElement;
    if(root.GetProperty("Version").GetString()!=evidence.Version || root.GetProperty("Commit").GetString()!=evidence.Commit ||
        root.GetProperty("CoreChecks").GetInt32()!=evidence.CoreChecks || root.GetProperty("WpfChecks").GetInt32()!=evidence.WpfChecks ||
        root.GetProperty("RunUrl").GetString()!=evidence.RunUrl || root.GetProperty("Channel").GetString()!="preview" ||
        root.GetProperty("WordRuntime").GetString()!="NOT_RUN" || root.GetProperty("AutomaticallyRepairsExactlyAtLeast").GetBoolean())
        throw new InvalidOperationException("Preview metadata is stale or incorrectly claims Word-runtime/spacing support.");
}
if(command=="prepare")
{
    if(args.Length!=4 || Path.GetFileName(args[3])!=guideName || !File.Exists(args[3]))throw new InvalidOperationException("Prepare the version-matching preview validation guide first.");
    File.Copy(args[3],Path.Combine(folder,guideName),false);
    var text=new StringBuilder($"# Phạm vi bản thử {version}\n\nGói preview để kiểm Word, chưa phải xác nhận tài liệu đã đạt thể thức hoặc chính tả đầy đủ.\n\n");
    text.AppendLine("| ID | Nhóm | Trạng thái | Phạm vi | Ví dụ trước | Kết quả/giới hạn |\n| --- | --- | --- | --- | --- | --- |");
    foreach(var item in DevelopmentReport.Capabilities)
    {
        string state=item.Mode switch {CapabilityMode.Reviewable=>"Đề xuất khi đủ điều kiện",CapabilityMode.Manual=>"Kiểm tra thủ công",_=>"Minh họa, cần duyệt"};
        text.AppendLine($"| {item.Id} | {item.Name} | {state} | {Cell(item.Scope)} | {Cell(item.Example.Before)} | {Cell(item.Example.Outcome)} |");
    }
    text.AppendLine("\n## Dự kiến — chưa triển khai\n\n| ID | Nhóm | Ví dụ trước | Kết quả/giới hạn |\n| --- | --- | --- | --- |");
    foreach(var item in DevelopmentReport.PlannedWork)
        text.AppendLine($"| {item.Id} | {item.Name} | {Cell(item.Example.Before)} | {Cell(item.Example.Outcome)} |");
    WriteNew(checklistName,text.ToString());
    WriteNew(officeName,$"# Kiểm Word cho preview {version}\n\nĐọc [{guideName}]({guideName}) và chạy các nhóm được chỉ định trên Microsoft Word desktop đã kích hoạt. CI chỉ chạy Core/OpenXML và WPF; Word runtime: **NOT_RUN**.\n\nGiữ nguyên tài liệu gốc; bắt đầu bằng mẫu giả đi kèm. Không bật tự sửa Exactly/AtLeast từ kết quả đo chưa được chứng minh.\n");
    WriteNew(reportName,DevelopmentReport.Build(version,DateTimeOffset.Now,evidence));
    WriteNew(previewManifestName,JsonSerializer.Serialize(new
    {
        Channel="preview",Version=evidence.Version,evidence.Commit,evidence.CoreChecks,evidence.WpfChecks,evidence.RunUrl,
        WordRuntime="NOT_RUN",AutomaticallyRepairsExactlyAtLeast=false,RequiresActivatedWordDesktop=true,SelfContainedWindowsX64=true
    },new JsonSerializerOptions{WriteIndented=true})+"\n");
    Console.WriteLine("Prepared current-code capability metadata and explicit Word validation scope.");
}
else if(command=="manifest")
{
    if(args.Length!=3)throw new ArgumentException("manifest accepts exactly folder and version.");
    VerifyMetadata();string[] files=Files();
    if(files.Contains(manifestName,StringComparer.Ordinal))throw new InvalidOperationException("Do not replace an existing package hash manifest.");
    if(files.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=files.Length)throw new InvalidOperationException("Ambiguous case-aliased package file names.");
    WriteNew(manifestName,string.Join("\n",files.Select(name=>Hash(name)+"  "+name))+"\n");
    Console.WriteLine($"Recorded SHA256 for {files.Length} packaged files.");
}
else
{
    if(args.Length!=3)throw new ArgumentException("verify accepts exactly folder and version.");
    VerifyMetadata();var entries=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    foreach(string line in File.ReadAllLines(Path.Combine(folder,manifestName)))
    {
        if(line.Length<67 || !Regex.IsMatch(line[..64],@"\A[0-9a-f]{64}\z",RegexOptions.CultureInvariant) || line[64..66]!="  ")throw new InvalidOperationException("Invalid package hash manifest.");
        string name=line[66..];
        if(name.StartsWith('/') || name.Contains('\\') || name.Contains(':') || name.Any(char.IsControl) ||
            name.Split('/').Any(p=>p is "" or "." or "..") || !entries.TryAdd(name,line[..64]))throw new InvalidOperationException("Unsafe or duplicate manifest file name.");
    }
    var actual=Files().Where(p=>p!=manifestName).ToArray();
    if(actual.Length!=entries.Count || actual.Any(p=>!entries.TryGetValue(p,out string? expected) || Hash(p)!=expected))
        throw new InvalidOperationException("The package contains changed, missing or unlisted files.");
    Console.WriteLine($"Verified {entries.Count} package files and exact preview evidence.");
}
