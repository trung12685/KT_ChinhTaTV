using ChinhTa.Core;
using ChinhTa.WordProtocol;
using ChinhTa.WordWorker;
internal static class ExperimentalQualityChecks
{
    internal static void Run(string root,string assets,Action<bool,string> assert)
    {
        string dic=Path.Combine(AppContext.BaseDirectory,"proofing","Vietnamese.words.txt");
        if(!File.Exists(dic))dic=Path.GetFullPath("assets/word-proofing/Vietnamese.words.txt");
        var model=VietnameseSpellingDefaults.Load(assets);var words=SyllableDictionary.Load(dic);
        WorkerService Service(bool experimental)=>new(model,new(Path.Combine(root,experimental?"quality-terms.json":"baseline-terms.json")),Path.Combine(root,"quality.dic"),words,experimental);
        var baseline=Service(false);var candidate=Service(true);
        BridgeRequest Request(string text)=>new(){Id=Guid.NewGuid().ToString(),Operation="scan",ReviewMode="spelling",StrictSpelling=true,DocumentId=Guid.NewGuid().ToString(),SnapshotHash=BridgeContract.Hash(text),Paragraphs=[new(){Index=1,Start=50,End=50+text.Length,Text=text}]};
        var hom=Request("Hom qua tôi đã gửi báo cáo.");
        assert(baseline.Handle(hom).Findings.All(f=>f.Original!="Hom"),"The measurement baseline reproduces the actual 0.1.91 Hom qua omission");
        var reply=candidate.Handle(hom);
        assert(reply.Success && reply.Findings.Any(f=>f.Original=="Hom" && f.Category=="Spelling" && f.Suggestions.Contains("Hôm")),"The experimental full pipeline detects missing marks in a valid dictionary syllable using general data-derived candidates");
        foreach(string text in new[]{"Hom giống đã được chuẩn bị.","Nguyễn Văn Nam đã đến.","Chấp hành đúng qui định.","Hom\tqua"})
            assert(candidate.Handle(Request(text)).Findings.All(f=>f.Category!="Spelling"),"Candidate lookup and name/boundary policies preserve a valid control: "+text);
        foreach(string text in new[]{"điễm.","nghành.","Cần sử lý.","Cần bổ xung.","Tương lai sáng lạng."})
            assert(candidate.Handle(Request(text)).Findings.Any(f=>f.Category=="Spelling"),"Experimental quality changes preserve reviewed red spelling errors: "+text);
        var personal=new BridgeRequest{Id=Guid.NewGuid().ToString(),Operation="add",Term="Hom"};candidate.Handle(personal);
        assert(candidate.Handle(hom).Findings.All(f=>f.Original!="Hom"),"An accepted personal label is respected by candidate restoration even when the original syllable exists in the dictionary");
        assert(Service(false).Handle(Request("Hom qua")).Findings.All(f=>f.Original!="Hom"),"The experimental stage remains disabled in the application baseline");
    }
}
