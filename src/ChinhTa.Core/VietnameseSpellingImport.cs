using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChinhTa.Core;
public static class VietnameseSpellingImport
{
    public const long MaximumArchive=64L*1024*1024;
    private static byte[] ReadEntry(ZipArchiveEntry entry,int limit)
    {
        if(entry.Length>limit)throw new InvalidOperationException("Tệp ngôn ngữ vượt giới hạn dung lượng.");
        using var stream=entry.Open();using var output=new MemoryStream();var buffer=new byte[65536];int n;
        while((n=stream.Read(buffer))>0){if(output.Length+n>limit)throw new InvalidOperationException("Dữ liệu giải nén vượt giới hạn.");output.Write(buffer,0,n);}
        return output.ToArray();
    }
    private static byte[] Find(ZipArchive zip,string suffix,int limit)
    {
        var entries=zip.Entries.Where(e=>e.FullName.EndsWith(suffix,StringComparison.Ordinal)).ToArray();
        if(entries.Length is <1 or >4)throw new InvalidOperationException("Không tìm được duy nhất nguồn "+suffix+" trong ZIP.");
        byte[]? result=null;
        foreach(var entry in entries)
        {
            var names=entry.FullName.Split('/');
            if(entry.FullName.Contains('\\') || entry.FullName.StartsWith('/') || names.Any(p=>p is ".." or "."))throw new InvalidOperationException("Đường dẫn ZIP chưa hợp lệ.");
            byte[] content=ReadEntry(entry,limit);
            // Uploaded packages may contain mirrors. They count once only if
            // byte-identical; conflicting duplicates require a clean source.
            if(result is not null && !result.AsSpan().SequenceEqual(content))throw new InvalidOperationException("ZIP có nhiều bản dữ liệu khác nhau; chưa chọn đoán một bản.");
            result=content;
        }
        return result!;
    }
    // Validate a private snapshot before replacing the last successful local import.
    // No archive entry is extracted or executed, including Python files.
    public static VietnameseSpellingModel ImportAndStore(Stream source,string cache)
    {
        string directory=Path.GetDirectoryName(Path.GetFullPath(cache))!;Directory.CreateDirectory(directory);
        string staged=Path.Combine(directory,Guid.NewGuid()+".tmp");
        try
        {
            VietnameseSpellingModel result;
            using(var file=new FileStream(staged,FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None))
            {
                byte[] buffer=new byte[65536];int n;
                while((n=source.Read(buffer))>0){if(file.Length+n>MaximumArchive)throw new InvalidOperationException("ZIP ngôn ngữ vượt 64 MiB.");file.Write(buffer,0,n);}
                file.Position=0;result=LoadZip(file);
            }
            File.Move(staged,cache,true);return result;
        }
        finally{if(File.Exists(staged))File.Delete(staged);}
    }
    public static VietnameseSpellingModel LoadZip(Stream source)
    {
        if(!source.CanSeek || source.Length>MaximumArchive)throw new InvalidOperationException("Chọn ZIP ngôn ngữ không quá 64 MiB.");
        using var zip=new ZipArchive(source,ZipArchiveMode.Read,true);
        if(zip.Entries.Count>10000)throw new InvalidOperationException("ZIP có quá nhiều mục.");
        byte[] dictionary=Find(zip,"dictionary/words.txt",16*1024*1024),model=Find(zip,"app/lm.json",48*1024*1024);
        return Parse(dictionary,model);
    }
    public static VietnameseSpellingModel Parse(byte[] dictionary,byte[] model)
    {
        if(dictionary.Length>16*1024*1024 || model.Length>48*1024*1024)throw new InvalidOperationException("Dữ liệu ngôn ngữ vượt giới hạn.");
        var utf8=new UTF8Encoding(false,true);string data=utf8.GetString(dictionary);
        var words=new HashSet<string>(StringComparer.Ordinal);var sources=new HashSet<string>(StringComparer.Ordinal);int lines=0;
        void Credit(string? value)
        {if(value is null || value.Length>128 || value.Any(char.IsControl) || sources.Count>=32 && !sources.Contains(value))throw new InvalidOperationException("Nguồn từ điển vượt giới hạn.");sources.Add(value);}
        using(var reader=new StringReader(data))while(reader.ReadLine() is { } line)
        {
            if(string.IsNullOrWhiteSpace(line))continue;
            if(++lines>200000 || line.Length>4096)throw new InvalidOperationException("Từ điển vượt giới hạn số mục hoặc độ dài.");
            using var doc=JsonDocument.Parse(line,new(){MaxDepth=8});var root=doc.RootElement;
            string? text=root.GetProperty("text").GetString();if(string.IsNullOrWhiteSpace(text) || text.Length>256)throw new InvalidOperationException("Mục từ điển chưa hợp lệ.");
            words.Add(VietnameseSpellingModel.Key(text));
            if(root.TryGetProperty("source",out var credit))
            {
                if(credit.ValueKind==JsonValueKind.String)Credit(credit.GetString());
                else if(credit.ValueKind==JsonValueKind.Array)foreach(var item in credit.EnumerateArray())Credit(item.GetString());
                else throw new InvalidOperationException("Nguồn từ điển chưa hợp lệ.");
            }
        }
        if(words.Count<1)throw new InvalidOperationException("Từ điển rỗng; giữ bộ đang dùng.");
        using var parsed=JsonDocument.Parse(model,new(){MaxDepth=8});var main=parsed.RootElement;
        var uni=new Dictionary<string,long>(StringComparer.Ordinal);var bi=new Dictionary<(string,string),long>();var tri=new Dictionary<(string,string,string),long>();
        string Token(string? value)
        {if(string.IsNullOrEmpty(value) || value.Length>128 || value.Any(char.IsControl))throw new InvalidOperationException("Token mô hình chưa hợp lệ.");return VietnameseSpellingModel.Key(value);}
        long Count(JsonElement value){long n=value.GetInt64();if(n<=0 || n>1000000000)throw new InvalidOperationException("Tần suất mô hình chưa hợp lệ.");return n;}
        foreach(var property in main.GetProperty("uni").EnumerateObject())
        {if(uni.Count>=200000)throw new InvalidOperationException("Mô hình vượt giới hạn từ vựng.");string t=Token(property.Name);uni[t]=checked(uni.GetValueOrDefault(t)+Count(property.Value));}
        foreach(var row in main.GetProperty("bi").EnumerateArray())
        {
            if(bi.Count>=600000 || row.GetArrayLength()!=3)throw new InvalidOperationException("Bigram vượt giới hạn hoặc sai cấu trúc.");
            var key=(Token(row[0].GetString()),Token(row[1].GetString()));bi[key]=checked(bi.GetValueOrDefault(key)+Count(row[2]));
        }
        foreach(var row in main.GetProperty("tri").EnumerateArray())
        {
            if(tri.Count>=1000000 || row.GetArrayLength()!=4)throw new InvalidOperationException("Trigram vượt giới hạn hoặc sai cấu trúc.");
            var key=(Token(row[0].GetString()),Token(row[1].GetString()),Token(row[2].GetString()));tri[key]=checked(tri.GetValueOrDefault(key)+Count(row[3]));
        }
        if(uni.Count==0 || uni.Values.Sum()!=main.GetProperty("total").GetInt64())throw new InvalidOperationException("Tổng token mô hình không khớp; giữ bộ đang dùng.");
        byte[] content=[..dictionary,..model];string fingerprint=Convert.ToHexString(SHA256.HashData(content));
        string creditText=string.Join(", ",sources.Where(s=>!string.IsNullOrWhiteSpace(s)).Order(StringComparer.Ordinal).Take(10));
        return new(words,uni,bi,tri,new("Dữ liệu nạp trên máy"+(creditText.Length==0?"":" — "+creditText),fingerprint,words.Count,uni.Count,bi.Count,tri.Count));
    }
}
