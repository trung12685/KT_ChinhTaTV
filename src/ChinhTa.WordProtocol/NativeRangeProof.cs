namespace ChinhTa.WordProtocol;

public sealed class NativeSelection
{
    public int Start {get;}
    public int End {get;}
    internal NativeSelection(int start,int end){Start=start;End=end;}
}

// Word may count cell markers, fields and supplementary characters differently
// from a .NET string. Only the live Word reader supplies native coordinates.
public static class NativeRangeProof
{
    public static NativeSelection Resolve(ParagraphDto paragraph,FindingDto finding,Func<int,int,string> read)
    {
        if(!BridgeContract.CanSelect(paragraph,paragraph,finding,"current","current") ||
            finding.Length>1024 || finding.Original.Any(c=>char.IsControl(c) || char.IsSurrogate(c)))throw Unmapped();
        var prefixes=new Dictionary<int,string>();
        string Prefix(int cp)
        {
            if(!prefixes.TryGetValue(cp,out var value)){value=read(paragraph.Start,cp);prefixes.Add(cp,value);}
            return value;
        }
        int Boundary(int offset)
        {
            string expected=paragraph.Text.Substring(0,offset);
            int guess=(int)Math.Min((long)paragraph.End,(long)paragraph.Start+offset);
            if(Prefix(guess)==expected)return guess;
            long low=paragraph.Start,high=paragraph.End;
            // Length is a search hint, never proof. An unavailable boundary,
            // unusual/non-monotonic Word range or different text fails closed.
            while(low<=high)
            {
                int middle=(int)(low+(high-low)/2);string value=Prefix(middle);
                if(value.Length==offset){if(value==expected)return middle;throw Unmapped();}
                if(value.Length<offset)low=(long)middle+1;else high=(long)middle-1;
            }
            throw Unmapped();
        }
        int start=Boundary(finding.Offset),end=Boundary(finding.Offset+finding.Length);
        if((long)end-start!=finding.Length || read(start,end)!=finding.Original)throw Unmapped();
        // Equal total lengths alone can hide a zero-text CP compensated by a
        // collapsed character elsewhere. Never select an unproven object/field.
        for(int i=0;i<finding.Length;i++)
            if(read(start+i,start+i+1)!=finding.Original.Substring(i,1))throw Unmapped();
        return new NativeSelection(start,end);
    }
    private static InvalidOperationException Unmapped()=>new InvalidOperationException("Chưa đối chiếu chắc vị trí chữ trong Word. Hãy Quét lại hoặc tự sửa tại vị trí đang thấy; Add-in không chọn đoán.");
}
