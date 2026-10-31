using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;
public sealed record PersonalTerm(string Text,string Key);
public sealed record PersonalLexiconData(int Version,long Revision,IReadOnlyList<PersonalTerm> Terms);
public sealed class PersonalLexiconStore(string path)
{
    public const int Limit=20000;
    private static readonly Regex Allowed=new(@"^[\p{L}\p{M}\p{N}]+(?:[ '_-][\p{L}\p{M}\p{N}]+)*$",RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    public static string Normalize(string term)
    {
        if(term.Length>128 || term.Length==0 || term.Any(c=>char.IsControl(c) || char.IsSurrogate(c)) || !Allowed.IsMatch(term) || !term.Any(char.IsLetter))
            throw new InvalidOperationException("Nhập từ/cụm tối đa 128 ký tự, có chữ; không chứa ngắt dòng hoặc ký tự điều khiển.");
        return VietnameseSpellingModel.Key(term);
    }
    private PersonalLexiconData Read()
    {
        if(!File.Exists(path))return new(1,0,Array.Empty<PersonalTerm>());
        if(new FileInfo(path).Length>4*1024*1024)throw new InvalidOperationException("Từ điển cá nhân vượt giới hạn; giữ tệp để kiểm tra.");
        var bytes=File.ReadAllBytes(path);if(bytes.Length>4*1024*1024)throw new InvalidOperationException("Từ điển cá nhân vượt giới hạn; giữ tệp để kiểm tra.");
        var data=JsonSerializer.Deserialize<PersonalLexiconData>(bytes)??throw new InvalidOperationException("Từ điển cá nhân chưa hợp lệ; giữ tệp để kiểm tra.");
        if(data.Version!=1 || data.Revision<0 || data.Terms is null || data.Terms.Count>Limit || data.Terms.Any(t=>t is null || Normalize(t.Text)!=t.Key) || data.Terms.Select(t=>t.Key).Distinct(StringComparer.Ordinal).Count()!=data.Terms.Count)
            throw new InvalidOperationException("Từ điển cá nhân không đúng phiên bản hoặc nội dung; chưa ghi đè.");
        return data;
    }
    internal static FileStream Acquire(string lockPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(lockPath))!);var until=DateTime.UtcNow.AddSeconds(5);
        while(true)try{return new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
            catch(IOException) when(DateTime.UtcNow<until){Thread.Sleep(25);}
    }
    public PersonalLexiconData Load(){using var gate=Acquire(path+".lock");return Read();}
    public PersonalLexiconData Add(string term)=>Change(term,false);
    public PersonalLexiconData Remove(string term)=>Change(term,true);
    private PersonalLexiconData Change(string term,bool remove)
    {
        string key=Normalize(term);using var gate=Acquire(path+".lock");var before=Read();var terms=before.Terms.ToDictionary(t=>t.Key,t=>t,StringComparer.Ordinal);
        if(remove?!terms.Remove(key):terms.ContainsKey(key))return before;
        if(!remove){if(terms.Count>=Limit)throw new InvalidOperationException("Từ điển cá nhân đạt giới hạn số mục.");terms.Add(key,new(term.Normalize(NormalizationForm.FormC),key));}
        var result=new PersonalLexiconData(1,checked(before.Revision+1),terms.Values.OrderBy(t=>t.Key,StringComparer.Ordinal).ToArray());
        byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(result);if(bytes.Length>4*1024*1024)throw new InvalidOperationException("Từ điển cá nhân đạt giới hạn dung lượng; giữ nguyên bản đã lưu.");
        AtomicWrite(path,bytes);return result;
    }
    internal static void AtomicWrite(string path,byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);string stage=path+"."+Guid.NewGuid()+".tmp";
        try{using(var file=new FileStream(stage,FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(bytes);file.Flush(true);}File.Move(stage,path,true);}
        finally{if(File.Exists(stage))File.Delete(stage);}
    }
}
public static class PersonalLexiconRules
{
    public static IReadOnlyList<(int Start,int Length)> Spans(string text,IReadOnlyList<PersonalTerm> terms)
    {
        if(terms.Count==0)return Array.Empty<(int,int)>();
        var tokens=VietnameseSpellingModel.Tokenize(text);var result=new List<(int,int)>();
        var indexed=terms.Select(t=>(Term:t,Parts:VietnameseSpellingModel.Tokenize(t.Text))).Where(t=>t.Parts.Count>0).ToLookup(t=>t.Parts[0].Key,StringComparer.Ordinal);
        for(int i=0;i<tokens.Count;i++)foreach(var entry in indexed[tokens[i].Key])
        {
            if(i+entry.Parts.Count>tokens.Count)continue;
            int a=tokens[i].Start,b=tokens[i+entry.Parts.Count-1].Start+tokens[i+entry.Parts.Count-1].Length;
            if(VietnameseSpellingModel.Key(text[a..b])==entry.Term.Key)result.Add((a,b-a));
        }
        return result;
    }
    public static bool Covers(string text,int start,int length,IReadOnlyList<PersonalTerm> terms)=>Spans(text,terms).Any(s=>start>=s.Start && (long)start+length<=(long)s.Start+s.Length);
}
public static class WordDictionaryMirror
{
    public static void Write(string path,PersonalLexiconData data)
    {
        // A private mirror only; never overwrite an externally edited Word file.
        using var gate=PersonalLexiconStore.Acquire(path+".mirror.lock");
        string marker=path+".sha256";
        if(File.Exists(path))
        {
            if(new FileInfo(path).Length>4*1024*1024)throw new InvalidOperationException("Tệp từ điển Word vượt giới hạn; giữ nguyên để kiểm tra.");
            string current=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
            if(!File.Exists(marker) || new FileInfo(marker).Length!=64 || File.ReadAllText(marker)!=current)throw new InvalidOperationException("Tệp từ điển Word đã được thay đổi bên ngoài. Đã giữ nguyên; hãy kiểm tra trước khi đồng bộ.");
        }
        var words=data.Terms.Select(t=>t.Text).Where(t=>t.All(c=>char.IsLetter(c) || char.GetUnicodeCategory(c)==System.Globalization.UnicodeCategory.NonSpacingMark)).Order(StringComparer.Ordinal).ToArray();
        var body=Encoding.Unicode.GetBytes(string.Join("\r\n",words)+(words.Length>0?"\r\n":""));byte[] bytes=[..Encoding.Unicode.GetPreamble(),..body];
        byte[]? previous=File.Exists(path)?File.ReadAllBytes(path):null;
        try{PersonalLexiconStore.AtomicWrite(path,bytes);PersonalLexiconStore.AtomicWrite(marker,Encoding.UTF8.GetBytes(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))));}
        catch{if(previous is not null)PersonalLexiconStore.AtomicWrite(path,previous);else if(File.Exists(path))File.Delete(path);throw;}
    }
}
