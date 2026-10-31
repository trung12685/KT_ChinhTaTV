using System.Text;
using System.Text.Json;
using ChinhTa.Core;
using ChinhTa.WordWorker;
using ChinhTa.WordProtocol;

bool isolated=args.Length==2 && args[0]=="--test-stdio";
if(!isolated && (args.Length!=1 || args[0]!="--stdio"))return 2;
Console.InputEncoding=new UTF8Encoding(false,true);Console.OutputEncoding=new UTF8Encoding(false);
// Process.StandardInput may emit one UTF-8 BOM on Windows. Console.In does not
// consistently consume it after InputEncoding is assigned; consume it once at the stream start.
Console.SetIn(new StreamReader(Console.OpenStandardInput(),new UTF8Encoding(false,true),detectEncodingFromByteOrderMarks:true));
string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ChinhTaViet","WordAddin");
if(isolated)folder=Path.GetFullPath(args[1]);
string cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ChinhTaViet","Spelling","local-import.zip");
var model=isolated?VietnameseSpellingDefaults.Load(VietnameseSpellingDefaults.DirectoryPath):VietnameseSpellingDefaults.Select(cache,VietnameseSpellingDefaults.DirectoryPath);
var service=new WorkerService(model,new(Path.Combine(folder,"personal-terms.json")),Path.Combine(folder,"ChinhTaViet.User.dic"));
var options=new JsonSerializerOptions{MaxDepth=16};
while(true)
{
    var builder=new StringBuilder();int ch;
    while((ch=Console.In.Read())!=-1 && ch!='\n')
    {if(builder.Length>=BridgeContract.MaxFrame)return 3;builder.Append((char)ch);}
    if(ch==-1 && builder.Length==0)return 0;
    BridgeResponse response;
    try
    {
        var request=JsonSerializer.Deserialize<BridgeRequest>(builder.ToString(),options)??throw new InvalidOperationException("Yêu cầu rỗng.");
        response=service.Handle(request);
    }
    catch(Exception error){response=new(){Error=error.Message};}
    string output=JsonSerializer.Serialize(response,options);
    if(output.Length>BridgeContract.MaxFrame)return 4;
    Console.Out.WriteLine(output);Console.Out.Flush();
}
