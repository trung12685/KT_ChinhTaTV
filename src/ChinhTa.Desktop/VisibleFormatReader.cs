using System.Runtime.InteropServices;
using System.Text;
using ChinhTa.Core;
using Microsoft.CSharp.RuntimeBinder;

namespace ChinhTa.Desktop;

public sealed record VisibleFont(string Name,float Size,int Bold,int Italic,int Color)
{
    public bool Known=>!string.IsNullOrWhiteSpace(Name) && float.IsFinite(Size) && Size>0 && Size<9999999 &&
        Bold is 0 or -1 && Italic is 0 or -1 && Color is not (9999999 or -9999999);
}
// Read-only fallback for an XML-proven span: uniform aggregate, then real Word
// word ranges, then characters only inside unresolved words (or the whole span
// if Words does not cover it exactly). Never infer child ranges from text indices.
public static partial class VisibleFormatReader
{
    public static FontSizeEvidence ReadSizes(object rangeObject,string expected)
    {
        dynamic range=rangeObject;
        if((string?)range.Text!=expected)throw new InvalidOperationException("Vùng giữ cỡ chữ đã thay đổi.");
        dynamic characters=range.Characters;var text=new StringBuilder();var sizes=new List<float?>();
        try
        {
            int count=(int)characters.Count;
            if(count<1 || count>expected.Length+2)throw new InvalidOperationException("Chưa đối chiếu được cỡ chữ Word.");
            for(int i=1;i<=count;i++)
            {
                dynamic child=characters[i];
                try
                {
                    string value=(string?)child.Text??"";text.Append(value);float? size=null;
                    if(!string.IsNullOrWhiteSpace(value))
                    {dynamic font=child.Font;try{size=(float)font.Size;}finally{Release(font);}}
                    sizes.AddRange(value.Select(c=>char.IsWhiteSpace(c)?(float?)null:size));
                }
                finally{Release(child);}
            }
        }
        finally{Release(characters);}
        if(text.ToString()!=expected)throw new InvalidOperationException("Chưa đối chiếu đủ ký tự để giữ cỡ chữ.");
        return new(expected,sizes);
    }
    public static bool Matches(object range,string expected,CharacterFormat? character,HeadingFormat? heading,int? alignment=null)=>
        Read(range,expected,allowCharacterFallback:true).All(f=>character is not null?
            character.Matches(f.Name,f.Size,f.Italic,f.Color,f.Bold):
            heading is not null && heading.Matches(f.Name,f.Size,f.Bold,f.Italic,f.Color,alignment));
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
        try
        {
            dynamic words=range.Words;
            try{return Children((object)words,expected,whole,allowCharacters:true);}
            finally{Release(words);}
        }
        catch(Exception error) when(error is RuntimeBinderException or InvalidOperationException ||
            error is COMException && !WordConnectionPolicy.IsDisconnected(error))
        { /* Word may return a last word outside the requested range; verify Characters instead. */ }
        dynamic characters=range.Characters;
        try{return Children((object)characters,expected,whole,allowCharacters:false);}
        finally{Release(characters);}
    }
    private static IReadOnlyList<VisibleFont> Children(object childrenObject,string expected,VisibleFont whole,bool allowCharacters)
    {
        dynamic children=childrenObject;
        var text=new StringBuilder();var fonts=new List<VisibleFont>();
        int count=(int)children.Count;
        if(count<1 || count>expected.Length+2)throw new InvalidOperationException("Chưa đối chiếu được số vùng Word để kiểm phông.");
        for(int i=1;i<=count;i++)
        {
            dynamic child=children[i];
            try
            {
                string value=(string?)child.Text??"";text.Append(value);
                if(!string.IsNullOrWhiteSpace(value))
                {
                    var observed=Font(child);
                    if(observed.Known || !allowCharacters)fonts.Add(observed);
                    else
                    {
                        dynamic characters=child.Characters;
                        try{fonts.AddRange(Children((object)characters,value,observed,allowCharacters:false));}
                        finally{Release(characters);}
                    }
                }
            }
            finally{Release(child);}
        }
        if(text.ToString()!=expected)throw new InvalidOperationException("Chưa đối chiếu được các ký tự Word để kiểm phông.");
        return fonts.Count>0?fonts:[whole];
    }
}
