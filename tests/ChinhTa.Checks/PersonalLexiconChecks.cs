using System.Text;
using ChinhTa.Core;
public static class PersonalLexiconChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string root=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".user-dictionary");Directory.CreateDirectory(root);string path=Path.Combine(root,"terms.json");
        bool Reject(Action action){try{action();return false;}catch(Exception){return true;}}
        try
        {
            var store=new PersonalLexiconStore(path);var added=store.Add("CôngNghệMới");
            assert(added.Revision==1 && new PersonalLexiconStore(path).Load().Terms.Single().Text=="CôngNghệMới","Personal acceptance persists after reopening without changing the shared dictionary");
            var same=store.Add("CôngNghệMới".Normalize(NormalizationForm.FormD));
            assert(same.Revision==1 && same.Terms.Count==1,"NFC/NFD additions share a lookup identity without duplicate entries or a fake revision");
            Parallel.For(0,12,i=>new PersonalLexiconStore(path).Add("ThuậtNgữ"+i));
            assert(store.Load().Terms.Count==13 && store.Load().Revision==13,"Concurrent readers/writers retain every distinct personal addition");
            assert(store.Remove("thuậtngữ0").Terms.Count==12 && store.Remove("thuậtngữ0").Revision==14,"Removing an accepted key is case insensitive and idempotent");
            foreach(string invalid in new[]{""," ","x\ny","x\ty","x\u202Ey","1234","x😀",new string('a',129)})assert(Reject(()=>store.Add(invalid)),"Invalid or structural dictionary input is rejected before changing the good personal data");
            var phrase=new PersonalTerm("An Phú",PersonalLexiconStore.Normalize("An Phú"));
            assert(PersonalLexiconRules.Covers("Đến An Phú.",4,2,[phrase]) && !PersonalLexiconRules.Covers("An ở Phú.",0,2,[phrase]) && !PersonalLexiconRules.Covers("An Phúc",0,2,[phrase]),"Accepting a phrase covers only its complete occurrence and does not whitelist its components or a longer word");
            var model=VietnameseSpellingChecks.Model();var term=new PersonalTerm("hoach","hoach");
            assert(model.Check("kế hoach tốt.").Any(f=>f.Kind==SpellingFindingKind.UnknownLexeme) && model.Check("kế hoach tốt.",[term]).Count==0,"A personal term suppresses only an unknown-lexeme proposal");
            assert(model.Check("Cần bổ xung và sử lý.",[new("xung","xung"),new("sử","sử")]).Count==2 && model.Check("Giải quyết dứt điễm.",[new("điễm","điễm")]).Count==1,"Personal acceptance cannot erase independent reviewed expression/context warnings");
            string mirror=Path.Combine(root,"ChinhTaViet.User.dic");store.Add("An Phú");store.Add("TênMới");WordDictionaryMirror.Write(mirror,store.Load());
            byte[] data=File.ReadAllBytes(mirror);string decoded=Encoding.Unicode.GetString(data,2,data.Length-2);
            assert(data.Take(2).SequenceEqual(new byte[]{255,254}) && decoded.Contains("TênMới\r\n",StringComparison.Ordinal) && !decoded.Contains("An Phú",StringComparison.Ordinal) && !decoded.Contains("ThuậtNgữ1",StringComparison.Ordinal),"The Word mirror is BOM UTF-16LE with complete single words only, never silently split phrases or numeric brand names");
            WordDictionaryMirror.Write(mirror,store.Load());File.AppendAllText(mirror,"external change");byte[] before=File.ReadAllBytes(mirror);
            assert(Reject(()=>WordDictionaryMirror.Write(mirror,store.Load())) && File.ReadAllBytes(mirror).SequenceEqual(before),"Externally modified Word dictionary data is retained instead of overwritten during synchronization");
            File.WriteAllText(path,"broken original");
            assert(Reject(()=>store.Add("TừKhác")) && File.ReadAllText(path)=="broken original","A corrupt personal dictionary is never replaced by an empty or newly guessed one");
        }
        finally{Directory.Delete(root,true);}
    }
}
