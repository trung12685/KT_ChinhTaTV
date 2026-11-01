using System.IO;
using System.Security.Cryptography;
namespace ChinhTa.WordAddin;
internal static class WordNativeProofing
{
    internal const int Vietnamese=1066;
    internal const string SourceHash="1417975F8479F3A4009CF6AF120E16CCEA190F89B392FB16889D67ED66B6E769";
    internal static bool Ensure(object application)
    {
        string source=Path.Combine(Path.GetDirectoryName(typeof(Connect).Assembly.Location)!,"proofing","Vietnamese.dic");
        string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ChinhTaViet","WordAddin","proofing");
        string target=Path.Combine(directory,"Vietnamese.dic");
        string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
        if(!File.Exists(source) || Hash(source)!=SourceHash)throw new InvalidOperationException("Từ điển Vietnamese.dic đi kèm thiếu/hỏng; cài lại toàn bộ gói.");
        Directory.CreateDirectory(directory);
        if(!File.Exists(target))File.Copy(source,target,false);
        if(Hash(target)!=SourceHash)throw new InvalidOperationException("Vietnamese.dic đã được sửa bên ngoài; đã giữ nguyên tệp. Chưa đăng ký lại; Add-in vẫn kiểm bằng bộ dữ liệu kèm theo.");
        Register(application,target,true);
        object? languages=null,language=null,dictionary=null;
        try
        {
            dynamic app=application;languages=app.Languages;dynamic ls=languages;language=ls[Vietnamese];dynamic l=language;dictionary=l.ActiveSpellingDictionary;
            return dictionary is not null;
        }
        finally{ComLifetime.Release(dictionary);ComLifetime.Release(language);ComLifetime.Release(languages);}
    }
    internal static void Register(object application,string path,bool vietnamese)
    {
        object? collection=null,active=null,entry=null;
        try
        {
            dynamic app=application;collection=app.CustomDictionaries;dynamic dictionaries=collection;
            if((int)dictionaries.Count>0)active=dictionaries.ActiveCustomDictionary;
            for(int i=1;i<=(int)dictionaries.Count;i++)
            {
                object? candidate=dictionaries[i];
                try{dynamic value=candidate;if(string.Equals(Path.Combine((string)value.Path,(string)value.Name),path,StringComparison.OrdinalIgnoreCase)){entry=candidate;candidate=null;break;}}
                finally{ComLifetime.Release(candidate);}
            }
            if(entry is null)entry=dictionaries.Add(path);
            dynamic e=entry;e.LanguageSpecific=vietnamese;if(vietnamese)e.LanguageID=Vietnamese;
        }
        finally
        {
            try{if(collection is not null && active is not null){dynamic dictionaries=collection;dictionaries.ActiveCustomDictionary=active;}}
            finally{ComLifetime.Release(entry);ComLifetime.Release(active);ComLifetime.Release(collection);}
        }
    }
    internal static void EnableForSelection(object application)
    {
        if(!Ensure(application))throw new InvalidOperationException("Word chưa có bộ kiểm chính tả tiếng Việt. Add-in vẫn gạch riêng khi gõ. Có thể cài Vietnamese Language Accessory Pack đúng Office 32/64-bit từ Microsoft để dùng thêm dấu gạch gốc của Word.");
        object? options=null,selection=null,document=null;
        try
        {
            dynamic app=application;options=app.Options;dynamic o=options;o.CheckSpellingAsYouType=true;
            document=app.ActiveDocument;dynamic d=document;d.ShowSpellingErrors=true;
            selection=app.Selection;dynamic s=selection;s.LanguageID=Vietnamese;s.NoProofing=0;
        }
        finally{ComLifetime.Release(selection);ComLifetime.Release(document);ComLifetime.Release(options);}
    }
}
