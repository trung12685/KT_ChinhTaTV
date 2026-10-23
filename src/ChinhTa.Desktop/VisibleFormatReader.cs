using System.Runtime.InteropServices;
using System.Text;

namespace ChinhTa.Desktop;

public sealed record VisibleFont(string Name,float Size,int Bold,int Italic,int Color)
{
    public bool Known=>!string.IsNullOrWhiteSpace(Name) && float.IsFinite(Size) && Size>0 && Size<9999999 &&
        Bold is 0 or -1 && Italic is 0 or -1 && Color is not (9999999 or -9999999);
}
// Read-only fallback for an already XML-proven character-format span. No CP
// arithmetic per character: Word supplies each actual character Range.
public static class VisibleFormatReader
{
    private static void Release(object? value){if(value is not null && Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
    private static VisibleFont Font(dynamic range)
    {
        dynamic font=range.Font;
        try{return new((string?)font.Name??"",(float)font.Size,(int)font.Bold,(int)font.Italic,(int)font.Color);}
        finally{Release(font);}
    }
    public static IReadOnlyList<VisibleFont> Read(object rangeObject,string expected,bool allowCharacterFallback)
    {
        dynamic range=rangeObject;
        if((string?)range.Text!=expected)throw new InvalidOperationException("Vùng định dạng đã thay đổi; hãy quét lại.");
        VisibleFont whole=Font(range);
        if(whole.Known || !allowCharacterFallback)return [whole];
        dynamic characters=range.Characters;
        var text=new StringBuilder();var fonts=new List<VisibleFont>();
        try
        {
            int count=(int)characters.Count;
            if(count<1 || count>expected.Length+2) return [whole];
            for(int i=1;i<=count;i++)
            {
                dynamic character=characters[i];
                try
                {
                    string value=(string?)character.Text??"";text.Append(value);
                    // Empty drawing runs and separator/break styles do not
                    // describe the visible letters. Every visible character counts.
                    if(!string.IsNullOrWhiteSpace(value))fonts.Add(Font(character));
                }
                finally{Release(character);}
            }
        }
        finally{Release(characters);}
        if(text.ToString()!=expected)throw new InvalidOperationException("Chưa đối chiếu được các ký tự Word để kiểm phông.");
        return fonts.Count>0?fonts:[whole];
    }
}
