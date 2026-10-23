using ChinhTa.Core;

namespace ChinhTa.Desktop;
public sealed partial class WordSession
{
    private Paragraph ObserveComponent(Paragraph row)
    {
        if(!row.TextEditable && !row.FormatEditable)return row;
        string text=row.Text.TrimEnd('\r','\a','\n');
        var observations=new List<(string Name,float Size,int Bold,int Italic,int Color)>();int offset=0;
        // A discretionary line break can carry a different run style from its
        // visible words. Observe each nonblank line; never remove the break.
        foreach(string part in text.Split('\v'))
        {
            if(!string.IsNullOrWhiteSpace(part))
            {
                dynamic range=document!.Range(row.Start+offset,row.Start+offset+part.Length);dynamic? font=null;
                try
                {
                    if((string)range.Text!=part)throw new InvalidOperationException("Vùng định dạng đã thay đổi; hãy quét lại.");
                    font=range.Font;
                    observations.Add(((string?)font.Name??"",(float)font.Size,(int)font.Bold,(int)font.Italic,(int)font.Color));
                }
                finally{Release(font);Release(range);}
            }
            offset+=part.Length+1;
        }
        if(observations.Count==0)return row;
        var first=observations[0];
        return row with {Font=first.Name,Size=first.Size,Bold=first.Bold==-1,Italic=first.Italic!=0,Color=first.Color,FormatObserved=true,
            FormatMixed=observations.Any(o=>o!=first) || string.IsNullOrWhiteSpace(first.Name) || !float.IsFinite(first.Size) ||
                first.Size<=0 || first.Size>=9999999 || first.Italic is not (0 or -1) || first.Color is 9999999 or -9999999,
            BoldMixed=observations.Any(o=>o.Bold is not (0 or -1))};
    }
}
