using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChinhTa.Core;
public static class VietnameseSpellingDefaults
{
    public const string Version="vi-offline-v1";
    public const string ManifestHash="806CAF047850F9AC32C4CC2E51E4984B036292F2C56DA222DA6D7180206E3929";
    public static string DirectoryPath=>Path.Combine(AppContext.BaseDirectory,"spelling");
    public static VietnameseSpellingModel Load(string directory)
    {
        byte[] Read(string name,int maximum)
        {
            string path=Path.Combine(directory,name);
            using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
            if(input.Length>maximum)throw new InvalidOperationException("Dữ liệu chính tả kèm ứng dụng vượt giới hạn.");
            return ReadBounded(input,maximum);
        }
        var manifest=Read("manifest.json",16384);
        if(Convert.ToHexString(SHA256.HashData(manifest))!=ManifestHash)throw new InvalidOperationException("Thông tin bộ chính tả kèm ứng dụng không khớp phiên bản; hãy giải nén lại toàn bộ gói.");
        using var info=JsonDocument.Parse(manifest);var root=info.RootElement;
        if(root.GetProperty("id").GetString()!=Version)throw new InvalidOperationException("Sai phiên bản bộ chính tả kèm.");
        byte[] container=Read("default-data.zip",8*1024*1024);
        if(!Convert.ToHexString(SHA256.HashData(container)).Equals(root.GetProperty("containerSha256").GetString(),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Gói dữ liệu chính tả kèm không còn nguyên vẹn; hãy giải nén lại toàn bộ ứng dụng.");
        using var zip=new ZipArchive(new MemoryStream(container),ZipArchiveMode.Read);
        if(zip.Entries.Count!=2 || !zip.Entries.Select(e=>e.FullName).Order(StringComparer.Ordinal).SequenceEqual(new[]{"lm.json","words.txt"}))throw new InvalidOperationException("Cấu trúc gói chính tả kèm chưa hợp lệ.");
        var files=new Dictionary<string,byte[]>(StringComparer.Ordinal);
        foreach(var file in root.GetProperty("files").EnumerateObject())
        {
            if(Path.GetFileName(file.Name)!=file.Name)throw new InvalidOperationException("Đường dẫn dữ liệu kèm chưa hợp lệ.");
            byte[] bytes;
            if(file.Name is "words.txt" or "lm.json")
            {using var input=zip.GetEntry(file.Name)!.Open();bytes=ReadBounded(input,16*1024*1024);}
            else bytes=Read(file.Name,256*1024);
            if(!Convert.ToHexString(SHA256.HashData(bytes)).Equals(file.Value.GetString(),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Tệp chính tả kèm bị thiếu hoặc thay đổi: "+file.Name+". Hãy giải nén lại toàn bộ gói.");
            files.Add(file.Name,bytes);
        }
        var utf8=new UTF8Encoding(false,true);var dictionary=new StringBuilder();
        foreach(string line in utf8.GetString(files["words.txt"]).Split('\n'))
        {
            string word=line.Trim();if(word.Length==0)continue;
            dictionary.Append(JsonSerializer.Serialize(new{text=word,source="Hồ Ngọc Đức / Viet74K (GPL-2.0)"})).Append('\n');
        }
        var result=VietnameseSpellingImport.Parse(utf8.GetBytes(dictionary.ToString()),files["lm.json"],"Bộ kèm ứng dụng v1 — Viet74K + UD Vietnamese-VTB");
        if(result.Source.Vocabulary!=root.GetProperty("unigrams").GetInt32() || result.Source.Bigrams!=root.GetProperty("bigrams").GetInt32() || result.Source.Trigrams!=root.GetProperty("trigrams").GetInt32())throw new InvalidOperationException("Thống kê bộ dữ liệu kèm chưa khớp.");
        return result;
    }
    private static byte[] ReadBounded(Stream input,int maximum)
    {
        using var output=new MemoryStream();byte[] buffer=new byte[65536];int n;
        while((n=input.Read(buffer))>0){if(output.Length+n>maximum)throw new InvalidOperationException("Dữ liệu chính tả kèm vượt giới hạn.");output.Write(buffer,0,n);}
        return output.ToArray();
    }
    // A valid existing user import wins. Failure never overwrites or deletes it.
    public static VietnameseSpellingModel Select(string cache,string bundledDirectory,Action<string,Exception>? report=null)
    {
        if(File.Exists(cache))try{using var source=File.OpenRead(cache);return VietnameseSpellingImport.LoadZip(source);}
            catch(Exception error){report?.Invoke("Spelling.LoadLocal",error);}
        try{return Load(bundledDirectory);}
        catch(Exception error){report?.Invoke("Spelling.LoadBundled",error);return VietnameseSpellingModel.Starter;}
    }
}
