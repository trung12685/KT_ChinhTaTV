using ChinhTa.Core;

public static class BundledSpellingChecks
{
    public static string SourceDirectory()
    {
        for(var p=new DirectoryInfo(Directory.GetCurrentDirectory());p is not null;p=p.Parent)
        {string candidate=Path.Combine(p.FullName,"assets","spelling");if(File.Exists(Path.Combine(candidate,"manifest.json")))return candidate;}
        throw new InvalidOperationException("Missing source spelling fixture assets.");
    }
    public static void Run(Action<bool,string> assert)
    {
        string source=SourceDirectory();var model=VietnameseSpellingDefaults.Load(source);
        assert(!model.Source.Starter && model.Source.Headwords==72535 && model.Source.Vocabulary==3314 && model.Source.Bigrams==30607 && model.Source.Trigrams==41821,"The actual shipped data contains 72,535 normalized headwords and the pinned licensed contextual counts");
        foreach(string text in new[]{"Cần sử lý hồ sơ.","Cần bổ xung nhân lực.","Giải quyết dứt điễm vụ việc.","Nghành giáo dục.","Nâng cao chât lượng.","Kết quả sáng lạng."})
            assert(model.Check(text).Count==1,"The bundled model offers a reviewable correction on a fresh installation without a user ZIP: "+text);
        foreach(string text in new[]{"Cần xử lý hồ sơ.","Bổ sung nhân lực.","Ngành giáo dục.","Đinh vít trong hộp.","Liên hệ help@example.com để xử lý.","Ông Nguyễn Văn An."})
            assert(model.Check(text).Count==0,"The offline default leaves clean prose, names and technical text unflagged: "+text);
        string temp=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".default-spelling");Directory.CreateDirectory(temp);
        try
        {
            foreach(string file in Directory.GetFiles(source))File.Copy(file,Path.Combine(temp,Path.GetFileName(file)));
            string cache=Path.Combine(temp,"private-import.zip");var reports=new List<string>();
            var fresh=VietnameseSpellingDefaults.Select(cache,temp,(op,_)=>reports.Add(op));
            assert(fresh.Source.Name==model.Source.Name && reports.Count==0 && !File.Exists(cache),"No cache and no network selects the bundled data without a per-user import or copied cache");
            byte[] own=VietnameseSpellingChecks.Zip();File.WriteAllBytes(cache,own);
            var custom=VietnameseSpellingDefaults.Select(cache,temp);
            assert(custom.Source.Headwords==1 && custom.Source.Name.StartsWith("Dữ liệu nạp trên máy",StringComparison.Ordinal) && File.ReadAllBytes(cache).SequenceEqual(own),"An existing valid private import takes precedence without replacement on upgrade");
            File.WriteAllText(cache,"broken private data");
            var recovered=VietnameseSpellingDefaults.Select(cache,temp,(op,_)=>reports.Add(op));
            assert(recovered.Source.Name==model.Source.Name && File.ReadAllText(cache)=="broken private data" && reports.Contains("Spelling.LoadLocal"),"A damaged private cache falls back to the actual bundled data and remains intact for recovery");
            bool Reject(Action action){try{action();return false;}catch(Exception){return true;}}
            string zip=Path.Combine(temp,"default-data.zip");byte[] before=File.ReadAllBytes(zip);before[before.Length/2]^=1;File.WriteAllBytes(zip,before);
            assert(Reject(()=>VietnameseSpellingDefaults.Load(temp)),"A changed data payload is rejected by the pinned manifest before reading model counts");
            assert(VietnameseSpellingDefaults.Select(cache,temp).Source.Starter && File.Exists(cache),"Failure of both resources keeps the application usable with explicit starter scope and preserves the private cache");
            File.Copy(Path.Combine(source,"default-data.zip"),zip,true);File.Delete(Path.Combine(temp,"CC-BY-SA-4.0.txt"));
            assert(Reject(()=>VietnameseSpellingDefaults.Load(temp)),"A missing license notice prevents accepting an incomplete redistributable bundle");
        }
        finally{Directory.Delete(temp,true);}
    }
}
