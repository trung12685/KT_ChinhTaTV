namespace ChinhTa.WordWorker;

internal sealed record TextSegment(int Offset,string Text,bool ParagraphEnd);
internal static class TextSegments
{
    internal static bool Supported(string text)=>!text.Any(c=>c=='\ufffc' || char.IsControl(c) && !Separator(c));
    private static bool Separator(char c)=>c is '\t' or '\r' or '\n' or '\v' or '\f' or '\a' or '\u000e';
    internal static IEnumerable<TextSegment> Read(string text)
    {
        int start=0;
        for(int i=0;i<=text.Length;i++)
        {
            if(i<text.Length && !Separator(text[i]))continue;
            if(i>start && !string.IsNullOrWhiteSpace(text.Substring(start,i-start)))
                yield return new(start,text.Substring(start,i-start),i==text.Length || text[i] is '\r' or '\a');
            start=i+1;
        }
    }
}
