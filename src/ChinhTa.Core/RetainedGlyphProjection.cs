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
            values.RemoveRange(start,edit.Length);values.InsertRange(start,replacement);
            text=text.Remove(start,edit.Length).Insert(start,edit.Replacement);
        }
        return (text,values);
    }
}
