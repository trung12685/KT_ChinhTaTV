using System.Text;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public static partial class VisibleFormatReader
{
    public static CharacterStyleEvidence ReadStyles(object rangeObject,string expected)
    {
        dynamic range=rangeObject;
        if((string?)range.Text!=expected)throw new InvalidOperationException("Vùng giữ định dạng tên loại đã thay đổi.");
        int start=(int)range.Start,end=(int)range.End,cursor=start;
        if(start<0 || end<=start)throw new InvalidOperationException("Chưa rõ biên vùng giữ định dạng tên loại.");
        dynamic characters=range.Characters;var text=new StringBuilder();var styles=new List<ObservedCharacterStyle?>();
        try
        {
            int count=(int)characters.Count;
            if(count<1 || count>expected.Length+2)throw new InvalidOperationException("Chưa đọc đủ ký tự để giữ định dạng tên loại.");
            for(int i=1;i<=count;i++)
            {
                dynamic child=characters[i];
                try
                {
                    string value=(string?)child.Text??"";int from=(int)child.Start,to=(int)child.End;
                    if(from!=cursor || to<=from || to>end || value.Length==0)
                        throw new InvalidOperationException("Ký tự nằm ngoài vùng giữ định dạng tên loại.");
                    cursor=to;text.Append(value);ObservedCharacterStyle? observed=null;
                    if(!string.IsNullOrWhiteSpace(value))
                    {
                        dynamic font=child.Font;
                        try{observed=new(new((string?)font.Name??"",(float)font.Size,(int)font.Bold,(int)font.Italic,(int)font.Color),(int)font.Underline);}
                        finally{Release(font);}
                    }
                    styles.AddRange(value.Select(c=>char.IsWhiteSpace(c)?null:observed));
                }
                finally{Release(child);}
            }
        }
        finally{Release(characters);}
        if(cursor!=end || text.ToString()!=expected || (string?)range.Text!=expected)
            throw new InvalidOperationException("Chưa đối chiếu đủ định dạng ký tự tên loại.");
        dynamic paragraph=range.ParagraphFormat;
        try{return new(expected,styles,new((int)paragraph.Alignment,(float)paragraph.LeftIndent,(float)paragraph.RightIndent,(float)paragraph.FirstLineIndent));}
        finally{Release(paragraph);}
    }
}
