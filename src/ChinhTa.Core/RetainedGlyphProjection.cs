namespace ChinhTa.Core;

// Project observations by verified Word prefixes, retaining only unchanged or
// case-equivalent source glyphs. New glyphs never gain invented source evidence.
internal static class RetainedGlyphProjection
{
    public static (string Text,IReadOnlyList<T> Values) Project<T>(string original,IReadOnlyList<T> observations,
        IReadOnlyList<Issue> edits,Func<Issue,string> readPrefix,T unknown)
    {
        string text=original;var values=observations.ToList();
        if(values.Count!=text.Length)throw new InvalidOperationException("Bằng chứng định dạng chưa phủ đủ chữ.");
        foreach(var edit in edits.OrderByDescending(i=>i.Start))
        {
            int start=readPrefix(edit).Length;
            if(start<0 || start>text.Length-edit.Length || text.Substring(start,edit.Length)!=edit.Original)
                throw new InvalidOperationException("Chưa đối chiếu được định dạng với nội dung sửa.");
            var replacement=Enumerable.Repeat(unknown,edit.Replacement.Length).ToArray();int prefix=0;
            while(prefix<Math.Min(edit.Original.Length,edit.Replacement.Length) && edit.Original[prefix]==edit.Replacement[prefix])
            {replacement[prefix]=values[start+prefix];prefix++;}
            int oldEnd=edit.Original.Length,newEnd=edit.Replacement.Length;
            while(oldEnd>prefix && newEnd>prefix && edit.Original[oldEnd-1]==edit.Replacement[newEnd-1])
            {replacement[--newEnd]=values[start+--oldEnd];}
            if(oldEnd-prefix==newEnd-prefix)
                for(int k=prefix;k<oldEnd;k++)
                    if(char.ToUpperInvariant(edit.Original[k])==char.ToUpperInvariant(edit.Replacement[k]))replacement[k]=values[start+k];
            // A combined spacing repair can move an existing comma/semicolon
            // past its old edge spaces. It is still the same source glyph even
            // when neither common-edge comparison can map it. Only one exact
            // punctuation glyph surrounded by ASCII spaces is unambiguous;
            // never guess a correspondence for multiple marks or changed text.
            if(SinglePunctuation(edit.Original,out int originalMark) && SinglePunctuation(edit.Replacement,out int replacementMark) &&
                edit.Original[originalMark]==edit.Replacement[replacementMark])
                replacement[replacementMark]=values[start+originalMark];
            values.RemoveRange(start,edit.Length);values.InsertRange(start,replacement);
            text=text.Remove(start,edit.Length).Insert(start,edit.Replacement);
        }
        return (text,values);
    }
    private static bool SinglePunctuation(string text,out int position)
    {
        position=-1;
        for(int i=0;i<text.Length;i++)
        {
            if(text[i]==' ')continue;
            if(position>=0 || !char.IsPunctuation(text[i]))return false;
            position=i;
        }
        return position>=0;
    }
}
