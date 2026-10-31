using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;
public sealed record SpellingToken(int Start,int Length,string Original,string Key);
public enum SpellingFindingKind { UnknownLexeme,ContextConfusion,ReviewedExpression }
public sealed record SpellingFinding(int Start,int Length,string Original,IReadOnlyList<string> Suggestions,string Reason,SpellingFindingKind Kind=SpellingFindingKind.ReviewedExpression);
public sealed record SpellingSource(string Name,string Fingerprint,int Headwords,int Vocabulary,int Bigrams,int Trigrams,bool Starter=false);

// Resource-backed, local suggestions only. Counts rank candidates; they are not
// calibrated probabilities and never authorize a Word write.
public sealed class VietnameseSpellingModel
{
    public static readonly Regex Tokens=new(@"[\p{L}\p{N}_][\p{L}\p{M}\p{N}_]*",RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    private static readonly HashSet<char> Tones=['\u0300','\u0301','\u0309','\u0303','\u0323'];
    // Real-word changes need an explicit, reviewed confusion pair as well as
    // corpus evidence. Frequency alone otherwise turns good prose into jargon.
    private static readonly Dictionary<string,string[]> Confusions=new(StringComparer.Ordinal)
    { ["sử"]=["xử"],["xung"]=["sung"],["đinh"]=["định"],["đo"]=["đò"],["sữa"]=["sửa"],["sửa"]=["sữa"] };
    private readonly HashSet<string> lexicon;
    private readonly Dictionary<string,long> uni;
    private readonly Dictionary<(string,string),long> bi;
    private readonly Dictionary<(string,string,string),long> tri;
    private readonly Dictionary<string,HashSet<string>> index=new(StringComparer.Ordinal);
    public SpellingSource Source {get;}
    public static VietnameseSpellingModel Starter {get;}=new(["sáng","lạng","xán","lạn","dứt","điễm","điểm","ngành","nghành","giáo","dục"],[],[],[],new("Bộ mẫu nhỏ của dự án — chưa nạp dữ liệu lớn","starter-v1",11,0,0,0,true));
    public VietnameseSpellingModel(IEnumerable<string> words,IEnumerable<KeyValuePair<string,long>> unigrams,
        IEnumerable<KeyValuePair<(string,string),long>> bigrams,IEnumerable<KeyValuePair<(string,string,string),long>> trigrams,SpellingSource source)
    {
        Source=source;lexicon=words.SelectMany(w=>Tokens.Matches(w).Cast<Match>().Select(m=>Key(m.Value)))
            .Where(w=>w.Length is >=2 and <=32 && w.All(c=>char.IsLetter(c) || char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark)).ToHashSet(StringComparer.Ordinal);
        uni=unigrams.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);bi=bigrams.ToDictionary(p=>p.Key,p=>p.Value);tri=trigrams.ToDictionary(p=>p.Key,p=>p.Value);
        foreach(string word in lexicon.Order(StringComparer.Ordinal))foreach(string signature in Signatures(Base(word)))
        {if(!index.TryGetValue(signature,out var values))index[signature]=values=new(StringComparer.Ordinal);values.Add(word);}
    }
    public static string Key(string text)=>text.Normalize(NormalizationForm.FormC).ToLowerInvariant();
    public static string WithoutTone(string word)=>new string(Key(word).Normalize(NormalizationForm.FormD).Where(c=>!Tones.Contains(c)).ToArray()).Normalize(NormalizationForm.FormC);
    public static string Base(string word)=>new string(Key(word).Normalize(NormalizationForm.FormD).Where(c=>char.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray());
    private static IEnumerable<string> Signatures(string text)
    {yield return text;for(int i=0;i<text.Length;i++)yield return text.Remove(i,1);}
    private static bool OrthographicVariant(string a,string b)
    {
        string Variant(string w)
        {
            string key=WithoutTone(w);if(key is "ki" or "ky" or "li" or "ly" or "ti" or "ty" or "hi" or "hy")key=key[..1]+"i";
            if(key=="qui")key="quy";
            var marks=Key(w).Normalize(NormalizationForm.FormD).Where(Tones.Contains).Order().ToArray();
            return key+":"+new string(marks);
        }
        return Variant(a)==Variant(b);
    }
    public IReadOnlyList<string> Candidates(string word)
    {
        word=Key(word);if(word.Length is <2 or >32)return [];
        var found=new HashSet<string>(StringComparer.Ordinal);string original=Base(word);
        foreach(string sig in Signatures(original))if(index.TryGetValue(sig,out var values))foreach(string candidate in values)
            if(candidate!=word && !OrthographicVariant(word,candidate) && Distance(original,Base(candidate))<=2 && Distance(word,candidate)<=2)found.Add(candidate);
        return found.Order(StringComparer.Ordinal).ToArray();
    }
    private static int Distance(string a,string b)
    {
        if(Math.Abs(a.Length-b.Length)>2)return 3;
        int[] previous=Enumerable.Range(0,b.Length+1).ToArray();
        for(int i=0;i<a.Length;i++){var current=new int[b.Length+1];current[0]=i+1;for(int j=0;j<b.Length;j++)current[j+1]=Math.Min(Math.Min(current[j]+1,previous[j+1]+1),previous[j]+(a[i]==b[j]?0:1));previous=current;}
        return previous[b.Length];
    }
    public static IReadOnlyList<SpellingToken> Tokenize(string source)=>Tokens.Matches(source).Cast<Match>().Select(m=>new SpellingToken(m.Index,m.Length,m.Value,Key(m.Value))).ToArray();
    private long Bi(string a,string b)=>bi.GetValueOrDefault((a,b));
    private long Tri(string a,string b,string c)=>tri.GetValueOrDefault((a,b,c));
    private static string MatchCase(string source,string suggestion)=>source.All(c=>!char.IsLetter(c) || char.IsUpper(c))?suggestion.ToUpperInvariant():
        char.IsUpper(source[0])?char.ToUpperInvariant(suggestion[0])+suggestion[1..]:suggestion;
    private static bool OnlySpace(string source,int start,int end)=>end>=start && source.AsSpan(start,end-start).IndexOfAnyExcept(' ','\t')<0;
    private static bool IsName(SpellingToken token)=>token.Original.Length>0 && char.IsUpper(token.Original[0]);
    public IReadOnlyList<SpellingFinding> Check(string text,IReadOnlyList<PersonalTerm>? personalTerms=null)
    {
        if(text.Length>64000 || TechnicalTextContext.IsTechnical(text))return [];
        var personalSpans=personalTerms is null?Array.Empty<(int Start,int Length)>():PersonalLexiconRules.Spans(text,personalTerms);
        var tokens=Tokenize(text);var protectedTokens=new HashSet<int>();
        for(int i=0;i<tokens.Count;i++)
        {
            var t=tokens[i];bool digits=t.Original.Any(c=>char.IsDigit(c) || c=='_');
            bool capitals=t.Original.Count(char.IsUpper)>=2;
            bool named=IsName(t) && (i>0 && IsName(tokens[i-1]) && OnlySpace(text,tokens[i-1].Start+tokens[i-1].Length,t.Start) ||
                i+1<tokens.Count && IsName(tokens[i+1]) && OnlySpace(text,t.Start+t.Length,tokens[i+1].Start));
            bool unknownAsciiName=IsName(t) && t.Original.All(c=>c<128) && !lexicon.Contains(t.Key);
            if(digits || capitals || named || unknownAsciiName)protectedTokens.Add(i);
        }
        var result=new List<SpellingFinding>();var owned=new HashSet<int>();
        // A small reviewed phrase layer precedes model proposals, so weaker
        // single-token alternatives cannot steal an overlapping phrase.
        foreach(var (wrong,right) in new[]{("sáng lạng","xán lạn"),("cọ sát","cọ xát"),("thăm quan","tham quan")})
        {
            var parts=wrong.Split(' ');
            for(int i=0;i+1<tokens.Count;i++)if(!protectedTokens.Contains(i) && !protectedTokens.Contains(i+1) && tokens[i].Key==parts[0] && tokens[i+1].Key==parts[1] && OnlySpace(text,tokens[i].Start+tokens[i].Length,tokens[i+1].Start))
            {
                int start=tokens[i].Start,end=tokens[i+1].Start+tokens[i+1].Length;string original=text[start..end];
                result.Add(new(start,end-start,original,Array.AsReadOnly(new[]{MatchCase(original,right)}),"Cụm cần đối chiếu cách dùng; chỉ là gợi ý để người dùng duyệt."));owned.Add(i);owned.Add(i+1);
            }
        }
        for(int i=0;i<tokens.Count;i++)
        {
            var token=tokens[i];string word=token.Key;
            if(owned.Contains(i) || protectedTokens.Contains(i) || word.Length is <2 or >32)continue;
            string prev=i>0 && !protectedTokens.Contains(i-1) && OnlySpace(text,tokens[i-1].Start+tokens[i-1].Length,token.Start)?tokens[i-1].Key:"";
            string next=i+1<tokens.Count && !protectedTokens.Contains(i+1) && OnlySpace(text,token.Start+token.Length,tokens[i+1].Start)?tokens[i+1].Key:"";
            // Reviewed local expressions also work when a small licensed corpus
            // has too few counts. These rules do not fabricate LM frequencies.
            string? correction=(word,prev,next) switch
            {
                ("sử",_,"lý")=>"xử",("xung","bổ",_)=>"sung",
                ("điễm","dứt",_)=>"điểm",("nghành",_,"giáo")=>"ngành",
                ("chât",_,"lượng")=>"chất",("hiễu",_,"nội")=>"hiểu",
                ("đo","bến","sang")=>"đò",_=>null
            };
            if(correction is not null)
            {
                result.Add(new(token.Start,token.Length,token.Original,Array.AsReadOnly(new[]{MatchCase(token.Original,correction)}),"Cặp chữ trong ngữ cảnh đã duyệt; vẫn cần người dùng đối chiếu, không phải kết luận từ tần suất."));
                continue;
            }
            if(Source.Starter)continue;
            long ol=Bi(prev,word),orr=Bi(word,next),os=ol+orr,ot=Tri(prev,word,next);bool known=lexicon.Contains(word);
            if(!known && personalSpans.Any(s=>token.Start>=s.Start && token.Start+token.Length<=s.Start+s.Length))continue;
            IEnumerable<string> candidates=known?Confusions.GetValueOrDefault(word)??[]:Candidates(word);
            var ranked=candidates.Where(c=>!OrthographicVariant(word,c)).Select(candidate=>
            {
                long l=Bi(prev,candidate),r=Bi(candidate,next),t=Tri(prev,candidate,next);long support=l+r;
                double score=2*Math.Log(1+t)+Math.Log(1+l)+Math.Log(1+r)+.05*Math.Log(1+uni.GetValueOrDefault(candidate))-.25*Distance(word,candidate);
                bool evidence=support>=4 && support>=4*Math.Max(1,os) && l>=ol && r>=orr && t>=ot &&
                    (l>0 && r>0 || Math.Max(l,r)>=30);
                return (candidate,score,evidence);
            }).Where(x=>x.evidence).OrderByDescending(x=>x.score).ThenBy(x=>x.candidate,StringComparer.Ordinal).Take(3).ToArray();
            if(ranked.Length==0)continue;
            result.Add(new(token.Start,token.Length,token.Original,Array.AsReadOnly(ranked.Select(x=>MatchCase(token.Original,x.candidate)).ToArray()),
                (known?"Từ có thể hợp lệ nhưng cần đối chiếu ngữ cảnh.":"Chưa xác nhận mục từ; có ứng viên được ngữ cảnh ủng hộ.")+" Tần suất chỉ hỗ trợ xếp hạng, không chứng minh câu sai.",known?SpellingFindingKind.ContextConfusion:SpellingFindingKind.UnknownLexeme));
        }
        return result.OrderBy(x=>x.Start).ToArray();
    }
    public IReadOnlyList<Issue> CheckParagraphs(IReadOnlyList<Paragraph> paragraphs)=>paragraphs.Where(p=>p.TextEditable && !p.InTable && !p.Text.Contains('\t')).SelectMany(p=>Check(p.Text.TrimEnd('\r','\a','\n')).Select(f=>
        new Issue(p.Start+f.Start,f.Length,f.Original,f.Original,f.Reason,false,FixKind.SpellingReview,CanFix:false,Title:"Nghi vấn chính tả: "+f.Original,
            SpellingSuggestions:f.Suggestions,SpellingSource:Source.Name))).ToArray();
}
