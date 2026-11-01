using ChinhTa.Core;
public static class DiacriticReviewChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string[] words=["hôm qua","hom","hôm","qua","hòm","hom giống","thực hiện","thuc","thực","hiện","nghiêm túc","nghiêm","túc","tuc","tôi","đã","da","áo","công tác","công tắc","tác","tắc","tac","ngay","ngày","mai","ngày mai"];
        var bi=new Dictionary<(string,string),long>{[("hôm","qua")]=5,[("thực","hiện")]=16,[("tôi","đã")]=19,[("ngay","hôm")]=1,[("ngày","hôm")]=20};
        var model=new VietnameseSpellingModel(words,[],bi,[],new("Independent algorithm fixture","accent-fixture",words.Length,0,bi.Count,0));
        IReadOnlyList<SpellingFinding> Review(string text,IReadOnlyList<PersonalTerm>? personal=null)=>model.Check(text,personal,true);
        foreach(var example in new[]{("Hom qua đã gửi thư.","Hom","Hôm"),("Thuc hiện công việc.","Thuc","Thực"),("Phải nghiêm tuc.","tuc","túc"),("Tôi da gửi thư.","da","đã"),("Ngay mai đã rõ.","Ngay","Ngày")})
        {
            var hits=Review(example.Item1).Where(f=>f.Original==example.Item2 && f.Kind==SpellingFindingKind.MissingDiacritics).ToArray();
            assert(hits.Length==1 && hits[0].Suggestions.Single()==example.Item3,"Generic data-driven diacritic restoration applies beyond a word-pair exception: "+example.Item1);
        }
        assert(model.Check("Hom qua đã gửi thư.").Count==0 && model.Check("Hom qua đã gửi thư.",null,false).Count==0,"The quality comparator can reproduce the previous known-word miss without retraining data");
        foreach(string text in new[]{"Hom giống đã chuẩn bị.","Cái hòm đã được đóng.","Ngay hôm nay đã rõ.","Công tác kiểm tra.","Công tắc đã bật.","Nguyễn Hom đã đến.","API HTTP CPU","Xem https://example.com/Hom/qua"})
            assert(Review(text).All(f=>f.Kind!=SpellingFindingKind.MissingDiacritics),"Valid original forms, ambiguity and names are not overwritten by a higher-frequency alternative: "+text);
        assert(Review("Công tac.").All(f=>f.Kind!=SpellingFindingKind.MissingDiacritics),"Two lexical restorations with different meanings require abstention rather than an invented single answer");
        assert(Review("Hom qua đã gửi thư.",[new PersonalTerm("Hom","hom")]).All(f=>f.Kind!=SpellingFindingKind.MissingDiacritics),"A user-accepted term protects even a known dictionary syllable from uncertain generic restoration");
        string nfd="😀 Thuc hiện công việc.".Normalize(System.Text.NormalizationForm.FormD);
        var finding=Review(nfd).Single(f=>f.Original=="Thuc" && f.Kind==SpellingFindingKind.MissingDiacritics);
        assert(finding.Start==nfd.IndexOf("Thuc",StringComparison.Ordinal) && nfd.Substring(finding.Start,finding.Length)=="Thuc","Diacritic lookup preserves original UTF-16 offsets with emoji and decomposed neighboring marks");
    }
}
