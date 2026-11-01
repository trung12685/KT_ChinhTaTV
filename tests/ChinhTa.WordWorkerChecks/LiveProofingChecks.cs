using ChinhTa.Core;
using ChinhTa.WordProtocol;
using ChinhTa.WordWorker;
internal static class LiveProofingChecks
{
    internal static void Run(string folder,string assets,Action<bool,string> assert)
    {
        string dic=Path.Combine(AppContext.BaseDirectory,"proofing","Vietnamese.words.txt");
        if(!File.Exists(dic))dic=Path.GetFullPath("assets/word-proofing/Vietnamese.words.txt");
        var syllables=SyllableDictionary.Load(dic);assert(syllables.Count==6634 && syllables.Contains("sử") && syllables.Contains("xung"),"The supplied syllable dictionary loads 6,634 letter-only entries without accepting its count header or a hyphenated marker");
        var service=new WorkerService(VietnameseSpellingDefaults.Load(assets),new(Path.Combine(folder,"live-terms.json")),Path.Combine(folder,"live-user.dic"),syllables);
        BridgeRequest Scan(string text,string mode="spelling")=>new(){Id=Guid.NewGuid().ToString(),Operation="scan",DocumentId=Guid.NewGuid().ToString(),SnapshotHash=BridgeContract.Hash(text),ReviewMode=mode,StrictSpelling=true,Paragraphs=[new(){Index=1,Start=100,End=100+text.Length,Text=text}]};
        foreach(var sample in new[]{("điễm.","điễm"),("nghành.","nghành"),("Cần sử lý.","sử"),("Cần bổ xung.","xung"),("Tương lai sáng lạng.","sáng lạng")})
        {
            var response=service.Handle(Scan(sample.Item1));
            assert(response.Success && response.Findings.Any(f=>f.Original==sample.Item2 && f.Category=="Spelling"),"Reviewed misspelling is a red spelling finding, including whole-phrase rules: "+sample.Item1);
        }
        var unknown=service.Handle(Scan("chưabiếttừ."));assert(unknown.Success && unknown.Findings.Any(f=>f.Original=="chưabiếttừ" && f.Category=="Spelling"),"An unknown typed word produces a spelling finding even without corpus ranking evidence");
        var add=new BridgeRequest{Id=Guid.NewGuid().ToString(),Operation="add",Term="chưabiếttừ"};assert(service.Handle(add).Success && service.Handle(Scan("chưabiếttừ.")).Findings.Count==0,"Adding a new personal word suppresses its unknown-word underline on the next scan");
        add=new(){Id=Guid.NewGuid().ToString(),Operation="add",Term="điễm"};service.Handle(add);
        assert(service.Handle(Scan("điễm.")).Findings.Any(f=>f.Original=="điễm"),"Accepting a dictionary token does not suppress a reviewed misspelling");
        var malformed=Scan("text");malformed.ReviewMode="assume-correct";assert(!service.Handle(malformed).Success,"Unknown review modes fail closed");
        var request=Scan("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM\r","format");
        var format=new FormatDto{Page=1,LayoutKnown=true,Alignment=1,UnindentedCenter=true,Observed=true,Font="Arial",Size=16,Bold=0,Italic=0,Color=0};
        request.Paragraphs[0].Format=format;
        var reply=service.Handle(request);assert(reply.Success && reply.Findings.Any(f=>f.Category=="Format" && f.Message.Contains("Quốc hiệu")),"Format review reuses the observed national-heading policy without proposing an automatic write");
        request.Paragraphs[0].Format=new(){Page=1};reply=service.Handle(request);
        assert(reply.Success && reply.Findings.All(f=>!f.Message.Contains("Times New Roman, chữ đứng, đậm")),"Unavailable font observations do not become invented format defects");
        request=Scan("Nội dung\r","format");request.Sections=[new(){Number=1,Start=0,End=500,WidthMm=210,HeightMm=297,TopMm=10,BottomMm=10,LeftMm=10,RightMm=10}];
        reply=service.Handle(request);assert(reply.Success && reply.Findings.Any(f=>f.Category=="Format" && f.Message.Contains("lề")),"The format mode checks actual section margins as read-only findings");
        request.Profile="Personal";assert(service.Handle(request).Findings.Count==0,"Personal documents do not inherit State page-margin rules");
        NamedAndLetter(service,Scan,assert);
        var dates=service.Handle(Scan("Hẹn ngày 31/02/2026.","deep"));
        assert(dates.Success && dates.Findings.Any(f=>f.Category=="Context" && f.Original=="ngày 31/02/2026"),"Deep review flags an impossible calendar date as blue contextual advice, not a spelling error");
        dates=service.Handle(Scan("Hẹn ngày 29/02/2024.","deep"));assert(dates.Success && dates.Findings.All(f=>f.Category!="Context"),"The deep calendar rule accepts a real leap-day date");
        dates=service.Handle(Scan("Hẹn thứ hai, ngày 01/11/2026.","deep"));assert(dates.Success && dates.Findings.Any(f=>f.Category=="Context" && f.Message.Contains("chủ nhật")),"Deep review distinguishes a day-of-week/date inconsistency without choosing what to rewrite");
        assert(service.Handle(Scan("Ngày ٣١/٠٢/٢٠٢٦.","deep")).Success,"Unrecognized numeral scripts cannot crash calendar checking through Int32 parsing");
    }
    private static void NamedAndLetter(WorkerService service,Func<string,string,BridgeRequest> scan,Action<bool,string> assert)
    {
        ParagraphDto Row(int index,int start,string text,float size=13,int bold=0,bool table=false,int cell=-1,int cellEnd=-1)=>new(){Index=index,Start=start,End=start+text.Length+1,Text=text+"\r",InTable=table,Format=new(){Page=1,LayoutKnown=true,Alignment=1,UnindentedCenter=true,Observed=true,Font="Times New Roman",Size=size,Bold=bold,Italic=0,Color=0,CellStart=cell,CellEnd=cellEnd,TableStart=table?0:-1}};
        var request=scan("","format");request.Paragraphs=[Row(1,0,Checker.Heading,12,-1),Row(2,100,HeadingRules.Motto,13,-1),Row(3,150,"Số: 01/BC-AB"),Row(4,180,"Báo cáo",12),Row(5,200,"Về việc kiểm tra",13),Row(6,250,"Kính gửi: Cơ quan")];
        request.Paragraphs[^1].Format!.Alignment=0;
        var result=service.Handle(request);
        assert(result.Success && result.Findings.Any(f=>f.ParagraphIndex==4 && f.Category=="Format") && result.Findings.Any(f=>f.ParagraphIndex==5 && f.Category=="Format"),"Named documents distinguish their title and outside-table subject and reuse the State heading policy");
        request.Paragraphs=[Row(1,0,"Số: 01/AB-VP",13,0,true,0,90),Row(2,20,"V/v kiểm tra",14,-1,true,0,90),Row(3,100,Checker.Heading,12,-1,true,100,190),Row(4,150,HeadingRules.Motto,13,-1,true,100,190),Row(5,220,"Kính gửi: Cơ quan")];
        result=service.Handle(request);
        assert(result.Success && result.Findings.Any(f=>f.ParagraphIndex==2 && f.Message.Contains("Không thêm đường kẻ")),"Official-letter V/v uses its separate left-header policy and never requests a subject underline");
    }
}
