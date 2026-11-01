using System.Globalization;
using System.Text;

namespace ChinhTa.Core;

// Candidate generation is data-driven. No source/correction word pairs are
// stored here. An accepted window must have one unambiguous restoration, and
// observed/lexical original windows win over a more frequent alternative.
internal sealed class DiacriticReview
{
    private sealed record Pattern(string[] Words,bool Lexical,long Count);
    private readonly Dictionary<string,List<Pattern>> patterns=new(StringComparer.Ordinal);
    private readonly HashSet<string> phrases=new(StringComparer.Ordinal);
    private readonly IReadOnlyDictionary<(string,string),long> bigrams;
    private readonly IReadOnlyDictionary<(string,string,string),long> trigrams;
    private const int MaxCorpusPatterns=200000;
    private const long MinimumCorpusCount=3;
    internal DiacriticReview(IEnumerable<string> entries,IReadOnlyDictionary<(string,string),long> bi,IReadOnlyDictionary<(string,string,string),long> tri)
    {
        bigrams=bi;trigrams=tri;
        foreach(string entry in entries)
        {
            var parts=entry.Split(' ',StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length is <2 or >4 || parts.Any(w=>!Word(w)))continue;
            phrases.Add(Join(parts));Add(parts,true,0);
        }
        var corpus=bi.Where(p=>p.Value>=MinimumCorpusCount && Word(p.Key.Item1) && Word(p.Key.Item2))
            .Select(p=>(Words:new[]{p.Key.Item1,p.Key.Item2},Count:p.Value))
            .Concat(tri.Where(p=>p.Value>=MinimumCorpusCount && Word(p.Key.Item1) && Word(p.Key.Item2) && Word(p.Key.Item3))
                .Select(p=>(Words:new[]{p.Key.Item1,p.Key.Item2,p.Key.Item3},Count:p.Value)))
            .OrderByDescending(p=>p.Count).ThenBy(p=>Join(p.Words),StringComparer.Ordinal).Take(MaxCorpusPatterns);
        foreach(var p in corpus)Add(p.Words,false,p.Count);
    }
    private static bool Word(string s)=>s.Length is >=1 and <=32 && s.All(c=>char.IsLetter(c) || char.GetUnicodeCategory(c)==UnicodeCategory.NonSpacingMark);
    private static string Join(IEnumerable<string> words)=>string.Join('\u001f',words);
    private void Add(string[] words,bool lexical,long count)
    {
        string key=Join(words.Select(Fold));if(!patterns.TryGetValue(key,out var values))patterns[key]=values=[];
        var before=values.FindIndex(p=>p.Words.SequenceEqual(words));
        if(before<0)values.Add(new(words,lexical,count));
        else values[before]=values[before] with{Lexical=values[before].Lexical || lexical,Count=Math.Max(values[before].Count,count)};
    }
    internal static string Fold(string text)=>new string(VietnameseSpellingModel.Key(text).Replace('đ','d').Normalize(NormalizationForm.FormD)
        .Where(c=>char.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray());
    private static List<(char Letter,string Marks)> Letters(string word)
    {
        var result=new List<(char,string)>();
        foreach(char c in VietnameseSpellingModel.Key(word).Normalize(NormalizationForm.FormD))
            if(char.GetUnicodeCategory(c)==UnicodeCategory.NonSpacingMark && result.Count>0)
            {var previous=result[^1];result[^1]=(previous.Item1,previous.Item2+c);}
            else result.Add((c,""));
        return result;
    }
    internal static bool CanRestore(string source,string candidate)
    {
        if(VietnameseSpellingModel.Key(source)==candidate || Fold(source)!=Fold(candidate))return false;
        var a=Letters(source);var b=Letters(candidate);if(a.Count!=b.Count)return false;
        for(int i=0;i<a.Count;i++)
            if(a[i].Letter!=b[i].Letter && !(a[i].Letter=='d' && b[i].Letter=='đ') || a[i].Marks.Any(c=>!b[i].Marks.Contains(c)))return false;
        return true;
    }
    private bool Observed(string[] words)=>phrases.Contains(Join(words)) || words.Length==2 && bigrams.ContainsKey((words[0],words[1])) ||
        words.Length==3 && trigrams.ContainsKey((words[0],words[1],words[2]));
    private bool OriginalSupport(string[] words,int index)
    {
        if(Observed(words))return true;
        for(int i=Math.Max(0,index-1);i<Math.Min(words.Length-1,index+1);i++)if(Observed([words[i],words[i+1]]))return true;
        return false;
    }
    private static string Case(string original,string target)=>original.All(c=>!char.IsLetter(c) || char.IsUpper(c))?target.ToUpperInvariant():
        char.IsUpper(original[0])?char.ToUpperInvariant(target[0])+target[1..]:target;
    internal IReadOnlyList<SpellingFinding> Check(string source,IReadOnlyList<SpellingToken> tokens,IReadOnlyList<(int Start,int Length)> personal)
    {
        var proposals=new Dictionary<int,HashSet<string>>();var ambiguous=new HashSet<int>();
        bool Name(int index)
        {
            var t=tokens[index];bool upper=char.IsUpper(t.Original[0]);
            return t.Original.Count(char.IsUpper)>=2 || upper &&
                (index>0 && char.IsUpper(tokens[index-1].Original[0]) && Space(tokens[index-1],t) || index+1<tokens.Count && char.IsUpper(tokens[index+1].Original[0]) && Space(t,tokens[index+1]));
        }
        bool Space(SpellingToken a,SpellingToken b)=>source.AsSpan(a.Start+a.Length,b.Start-a.Start-a.Length).IndexOfAnyExcept(' ')<0;
        for(int start=0;start<tokens.Count;start++)for(int count=2;count<=4 && start+count<=tokens.Count;count++)
        {
            if(!Space(tokens[start+count-2],tokens[start+count-1]))break;
            var window=tokens.Skip(start).Take(count).Select(t=>t.Key).ToArray();
            if(window.Any(w=>!Word(w)) || !patterns.TryGetValue(Join(window.Select(Fold)),out var values))continue;
            var matches=values.Where(p=>p.Words.Length==count && p.Words.Where((word,i)=>word!=window[i]).Any() &&
                p.Words.Select((word,i)=>word==window[i] || CanRestore(window[i],word)).All(b=>b)).ToArray();
            if(matches.Length!=1)
            {
                if(matches.Length>1)foreach(int i in Enumerable.Range(0,count).Where(i=>matches.Any(p=>p.Words[i]!=window[i])))ambiguous.Add(start+i);
                continue;
            }
            var selected=matches[0];
            for(int i=0;i<count;i++)
            {
                var token=tokens[start+i];string target=selected.Words[i];
                if(target==window[i] || Name(start+i) || OriginalSupport(window,i) ||
                    personal.Any(s=>token.Start>=s.Start && token.Start+token.Length<=s.Start+s.Length))continue;
                if(!proposals.TryGetValue(start+i,out var choices))proposals[start+i]=choices=new(StringComparer.Ordinal);
                choices.Add(target);
            }
        }
        return proposals.Where(p=>p.Value.Count==1 && !ambiguous.Contains(p.Key)).OrderBy(p=>p.Key).Select(p=>
        {
            var token=tokens[p.Key];return new SpellingFinding(token.Start,token.Length,token.Original,[Case(token.Original,p.Value.Single())],
                "Có ứng viên bổ sung dấu duy nhất trong cụm từ/ngữ cảnh có dữ liệu. Dấu đã gõ được giữ; bạn đối chiếu rồi tự sửa.",SpellingFindingKind.MissingDiacritics);
        }).ToArray();
    }
}
