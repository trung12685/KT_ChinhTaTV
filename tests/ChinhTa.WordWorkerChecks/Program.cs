using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ChinhTa.Core;
using ChinhTa.WordProtocol;
using ChinhTa.WordWorker;

int checks=0;void Assert(bool value,string name){if(!value)throw new Exception("FAIL: "+name);checks++;Console.WriteLine("PASS "+name);}
string root=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".worker-checks");Directory.CreateDirectory(root);
try
{
    string assets=Path.Combine(AppContext.BaseDirectory,"spelling");
    if(!Directory.Exists(assets))assets=Path.GetFullPath("assets/spelling");
    var service=new WorkerService(VietnameseSpellingDefaults.Load(assets),new(Path.Combine(root,"terms.json")),Path.Combine(root,"ChinhTaViet.User.dic"));
    BridgeRequest Request(string op)=>new(){Id=Guid.NewGuid().ToString(),Operation=op};
    var request=Request("scan");request.DocumentId=Guid.NewGuid().ToString();request.SnapshotHash=BridgeContract.Hash("source");
    string text="Cần sử lý và bổ xung nhân lực.";var paragraph=new ParagraphDto{Index=1,Start=0,End=text.Length+1,Text=text+"\r"};request.Paragraphs.Add(paragraph);
    var response=service.Handle(request);
    Assert(response.Success && response.Id==request.Id && response.SnapshotHash==request.SnapshotHash && response.Findings.Count==2,"The real bundled engine returns contextual findings bound to the same request/document snapshot");
    var f=response.Findings.First();Assert(BridgeContract.CanSelect(paragraph,paragraph,f,request.SnapshotHash,request.SnapshotHash),"A matching native range can be selected without a write");
    Assert(!BridgeContract.CanSelect(paragraph,paragraph,f,request.SnapshotHash,BridgeContract.Hash("changed")),"Any edit after scan invalidates selection, even when the old word still appears");
    Assert(!BridgeContract.CanSelect(paragraph,new(){Index=1,Start=1,End=paragraph.End+1,Text=paragraph.Text},f,request.SnapshotHash,request.SnapshotHash),"Shifted native coordinates cannot select a guessed word");
    var add=Request("add");add.Term="xung";Assert(service.Handle(add).Success && service.Handle(request).Findings.Count==2,"Accepting an individual word cannot suppress the independent bổ xung/context warning");
    add=Request("add");add.Term="VNeID";var saved=service.Handle(add);Assert(saved.Success && saved.Terms.Contains("VNeID") && saved.DictionaryRevision==2,"A new personal term is persisted with its preferred spelling and revision");
    var mirror=service.Handle(Request("mirror"));Assert(mirror.Success && File.Exists(mirror.MirrorPath),"A separate UTF-16LE Word dictionary can be exported without changing Word options");
    var bad=Request("unknown");Assert(!service.Handle(bad).Success,"Unknown operations fail closed");
    request.Version=99;Assert(!service.Handle(request).Success,"Incompatible bridge versions are rejected before analysis");request.Version=1;
    paragraph.End--;response=service.Handle(request);Assert(response.Success && response.Findings.Count==0 && response.SkippedParagraphs==1,"Nonlinear native/string mapping is reported and skipped rather than guessed");paragraph.End++;
    paragraph.InTable=true;Assert(service.Handle(request).SkippedParagraphs==1,"The first prototype explicitly reports table paragraphs outside its scan scope");paragraph.InTable=false;
    request.Paragraphs.Add(paragraph);Assert(!service.Handle(request).Success,"Duplicate paragraph identities cannot silently merge findings");request.Paragraphs.RemoveAt(1);
    // In CI verify the actual worker stdio executable as well as its service.
    if(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")=="true")
    {
        string worker=typeof(WorkerService).Assembly.Location;string isolated=Path.Combine(root,"ipc");
        using var process=new Process{StartInfo=new(){FileName="dotnet",ArgumentList={"exec","--runtimeconfig",Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location,"runtimeconfig.json"),worker,"--test-stdio",isolated},UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardInputEncoding=Encoding.UTF8}};
        process.Start();var error=process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request));await process.StandardInput.FlushAsync();
        var read=process.StandardOutput.ReadLineAsync();if(await Task.WhenAny(read,Task.Delay(30000))!=read)throw new Exception("Worker IPC timeout");
        var actual=JsonSerializer.Deserialize<BridgeResponse>((await read)!);
        Assert(actual is {Success:true} && actual.Findings.Count==2 && actual.Id==request.Id,"A real child worker exchanges framed UTF-8 JSON and produces the expected Vietnamese findings");
        await process.StandardInput.WriteLineAsync("not JSON");await process.StandardInput.FlushAsync();
        var malformed=JsonSerializer.Deserialize<BridgeResponse>((await process.StandardOutput.ReadLineAsync())!);Assert(malformed is {Success:false},"Malformed IPC input returns an error instead of acting on a stale request");
        process.StandardInput.Close();if(!process.WaitForExit(10000))process.Kill(true);Assert(process.ExitCode==0,"The worker exits when its owner closes stdin and needs no Word or network process");await error;
    }
}
finally{Directory.Delete(root,true);}
Console.WriteLine($"{checks} worker checks passed.");
