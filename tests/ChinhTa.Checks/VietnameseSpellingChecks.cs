using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ChinhTa.Core;

public static class VietnameseSpellingChecks
{
    public static VietnameseSpellingModel Model(bool reversed=false)
    {
        string[] words=["dứt","điểm","hiểu","nội","dung","ngành","giáo","chất","lượng","cao","hoạch","kế","quyệt","quyết","đúng"];
        var bi=new Dictionary<(string,string),long>{[("dứt","điểm")]=100,[("điểm","đúng")]=100,[("hiểu","nội")]=80,[("ngành","giáo")]=100,[("chất","lượng")]=100,[("kế","hoạch")]=100};
        return new(reversed?words.Reverse():words,[],bi,[],new("Fixture riêng của dự án","fixture",words.Length,0,bi.Count,0));
    }
    public static byte[] Zip(string dictionary="{\"text\":\"điểm\",\"source\":[\"fixture\"]}",string? model=null,bool mirror=false,bool conflict=false,string prefix="data/")
    {
        model??=JsonSerializer.Serialize(new{total=1,uni=new Dictionary<string,int>{{"điểm",1}},bi=Array.Empty<object>(),tri=Array.Empty<object>()});
        using var buffer=new MemoryStream();using(var zip=new ZipArchive(buffer,ZipArchiveMode.Create,true))
        {
            void Add(string path,string text){using var writer=new StreamWriter(zip.CreateEntry(path).Open(),new UTF8Encoding(false));writer.Write(text);}
            Add(prefix+"dictionary/words.txt",dictionary);Add(prefix+"app/lm.json",model);
            if(mirror || conflict)Add("mirror/dictionary/words.txt",conflict?"{\"text\":\"khác\"}":dictionary);
            Add("do-not-run.py","raise Exception('must never execute')");
        }
        return buffer.ToArray();
    }
    public static void Run(Action<bool,string> assert)
    {
        var model=Model();
        foreach(var pair in new[]{("điễm","điểm"),("hiễu","hiểu"),("nghành","ngành"),("chât","chất"),("quyết","quyệt")})
            assert(model.Candidates(pair.Item1).Contains(pair.Item2),"Candidate lookup reaches the actual accented vowel or onset: "+pair.Item1);
        assert(model.Candidates("điễm").SequenceEqual(Model(true).Candidates("điễm")),"Candidate order is ordinal and independent of dictionary insertion order");
        foreach(string text in new[]{"Phải dứt điễm đúng.","Hãy hiễu nội dung.","nghành giáo dục.","chât lượng cao.","kế hoach tốt."})
            assert(model.Check(text).Count>0 && model.Check(text).All(f=>text.Substring(f.Start,f.Length)==f.Original),"Context-supported proposals retain original UTF-16 spans: "+text);
        string source="😀 Phải dứt điễm đúng.",nfd=source.Normalize(NormalizationForm.FormD);
        var a=model.Check(source).Single(f=>f.Original=="điễm");var b=model.Check(nfd).Single(f=>VietnameseSpellingModel.Key(f.Original)=="điễm");
        assert(a.Start==source.IndexOf("điễm",StringComparison.Ordinal) && nfd.Substring(b.Start,b.Length)==b.Original && a.Suggestions.SequenceEqual(b.Suggestions),"Emoji and decomposed marks keep source offsets while lookup keys normalize consistently");
        foreach(string text in new[]{"Đây là https://example.com/dứt/điễm", "Gửi tới a@b.vn dứt điễm", "ma_diem = điễm", "Nguyễn Điễm", "API HTTP CPU", "x123 abc_unknown", "foobar", "kĩ thuật hoà bình"})
            assert(model.Check(text).Count==0,"Names, technical content, variants and unsupported unknowns are not declared spelling errors: "+text);
        var conditioned=new VietnameseSpellingModel(["hoạch","hoách"],[],new Dictionary<(string,string),long>{[("lập","hoạch")]=20,[("hoạch","tốt")]=20,[("lập","hoách")]=20,[("hoách","tốt")]=20},new Dictionary<(string,string,string),long>{[("lập","hoạch","tốt")]=10},new("Trigram fixture","tri",2,0,4,1));
        assert(conditioned.Check("lập hoach tốt").Single().Suggestions[0]=="hoạch","Actual three-token context breaks a bigram tie outside the reviewed expression rules; sentence markers are not invented around an interior window");
        foreach(string text in new[]{"Đinh vít được đóng trong hộp.","Cần giải quyết xung đột lợi ích.","Chấp hành đúng qui định.","Đây là người có năng lực và phẩm chất tốt.","Tổ công tác phối hợp với Công an phường."})
            assert(model.Check(text).Count==0,"Ordinary valid prose cannot be replaced solely by a more frequent corpus word: "+text);
        var phrase=model.Check("Tương lai sáng lạng.");
        assert(phrase.Count==1 && phrase[0].Original=="sáng lạng" && phrase[0].Suggestions.Single()=="xán lạn","Reviewed phrase owns its full span before any single-token context alternative");
        var issues=model.CheckParagraphs([new(10,"Phải dứt điễm đúng."),new(100,"nghành giáo dục.",InTable:true),new(200,"nghành giáo dục.",TextEditable:false)]);
        assert(issues.Count>0 && issues.All(i=>i.Start<100 && !i.CanFix && i.Kind==FixKind.SpellingReview && i.Replacement==i.Original),"Spelling proposals are manual warnings in proved plain paragraphs only");
        bool Reject(Action action){try{action();return false;}catch(Exception){return true;}}
        assert(Reject(()=>BatchPlan.Create([issues[0] with {CanFix=true,Replacement="x"}])),"Even a forged CanFix cannot send a spelling proposal into an automatic batch");
        assert(IssueTooltipCatalog.Describe(issues[0]).Basis.Contains("thử nghiệm",StringComparison.Ordinal) && IssueReviewCatalog.Describe(issues[0]).Section==IssueReviewSection.ManualWarning,"Spelling has a separate manual group and a language-data basis");
        var bytes=Zip(mirror:true);using var stream=new MemoryStream(bytes);var imported=VietnameseSpellingImport.LoadZip(stream);
        assert(imported.Source.Headwords==1 && imported.Source.Fingerprint.Length==64 && !imported.Source.Starter,"Byte-identical dictionary mirrors count once with a stable SHA256 provenance");
        foreach(byte[] invalid in new[]{Zip(conflict:true),Zip(dictionary:"not JSON"),Zip(prefix:"../"),Zip(model:"{}"),Zip(model:"{\"total\":2,\"uni\":{\"x\":1},\"bi\":[],\"tri\":[]}"),Zip(model:"{\"total\":-1,\"uni\":{\"x\":-1},\"bi\":[],\"tri\":[]}")})
            assert(Reject(()=>VietnameseSpellingImport.LoadZip(new MemoryStream(invalid))),"Malformed, conflicting, traversing or inconsistent import is rejected before use");
        string dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".spelling");Directory.CreateDirectory(dir);string cache=Path.Combine(dir,"local.zip");
        try
        {
            VietnameseSpellingImport.ImportAndStore(new MemoryStream(bytes),cache);byte[] before=File.ReadAllBytes(cache);
            assert(Reject(()=>VietnameseSpellingImport.ImportAndStore(new MemoryStream(Zip(conflict:true)),cache)) && File.ReadAllBytes(cache).SequenceEqual(before) && Directory.GetFiles(dir).Length==1,"Failed resource import leaves the previous cache intact and removes its private staging file");
        }
        finally{Directory.Delete(dir,true);}
    }
}
