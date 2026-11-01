using System.Globalization;
using System.Text;
using ChinhTa.Core;
namespace ChinhTa.WordWorker;
public sealed class SyllableDictionary
{
    private readonly HashSet<string> words;
    public int Count=>words.Count;
    public SyllableDictionary(IEnumerable<string> entries)
    {
        words=entries.Where(w=>w.Length>0 && w.Length<=64 && w.All(c=>char.IsLetter(c) || CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark))
            .Select(VietnameseSpellingModel.Key).ToHashSet(StringComparer.Ordinal);
        if(words.Count<100)throw new InvalidOperationException("Từ điển tiếng Việt kèm theo chưa đủ dữ liệu.");
    }
    public static SyllableDictionary Load(string path)=>new(File.ReadAllLines(path,new UTF8Encoding(false,true)));
    public bool Contains(string word)=>words.Contains(VietnameseSpellingModel.Key(word));
    public IEnumerable<SpellingFinding> Check(string text,IReadOnlyList<PersonalTerm> personal)
    {
        if(TechnicalTextContext.IsTechnical(text))yield break;
        var accepted=PersonalLexiconRules.Spans(text,personal);
        foreach(var token in VietnameseSpellingModel.Tokenize(text))
        {
            if(token.Original.Any(c=>char.IsDigit(c) || c=='_') || token.Original.Count(char.IsUpper)>=2 || token.Original.Length>32)continue;
            string? correction=token.Key switch {"điễm"=>"điểm","nghành"=>"ngành",_=>null};
            if(correction is not null){yield return new(token.Start,token.Length,token.Original,[correction],"Lỗi chính tả trong nhóm đã đối chiếu; bạn duyệt rồi sửa trong Word.");continue;}
            if(words.Contains(token.Key) || accepted.Any(s=>token.Start>=s.Start && token.Start+token.Length<=s.Start+s.Length))continue;
            if(char.IsUpper(token.Original[0]) && token.Original.All(c=>c<128))continue;
            yield return new(token.Start,token.Length,token.Original,[],"Chữ chưa có trong từ điển tiếng Việt/từ cá nhân; có thể là lỗi gõ hoặc tên riêng cần bổ sung.",SpellingFindingKind.UnknownLexeme);
        }
    }
}
