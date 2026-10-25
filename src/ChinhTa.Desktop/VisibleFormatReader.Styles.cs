using System.Text;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public static partial class VisibleFormatReader
{
    public static CharacterStyleEvidence ReadStyles(object rangeObject,string expected,bool includeSpacing=false)
    {
        dynamic range=rangeObject;
        if((string?)range.Text!=expected)throw new InvalidOperationException("Vùng giữ định dạng ký tự đã thay đổi.");
        int start=(int)range.Start,end=(int)range.End,cursor=start;
        if(start<0 || end<=start)throw new InvalidOperationException("Chưa rõ biên vùng giữ định dạng ký tự.");
        dynamic characters=range.Characters;var text=new StringBuilder();var styles=new List<ObservedCharacterStyle?>();
        try
        {
            int count=(int)characters.Count;
            if(count<1 || count>expected.Length+2)throw new InvalidOperationException("Chưa đọc đủ ký tự để giữ định dạng ký tự.");
            for(int i=1;i<=count;i++)
            {
                dynamic child=characters[i];
                try
                {
                    string value=(string?)child.Text??"";int from=(int)child.Start,to=(int)child.End;
                    if(from!=cursor || to<=from || to>end || value.Length==0)
                        throw new InvalidOperationException("Ký tự nằm ngoài vùng giữ định dạng ký tự.");
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
            throw new InvalidOperationException("Chưa đối chiếu đủ định dạng ký tự.");
        return new(expected,styles,ReadParagraph(rangeObject,includeSpacing));
    }
    public static ObservedParagraphStyle ReadParagraph(object rangeObject,bool includeSpacing=true)
    {
        dynamic range=rangeObject;dynamic paragraph=range.ParagraphFormat;
        try
        {
            var style=new ObservedParagraphStyle((int)paragraph.Alignment,(float)paragraph.LeftIndent,(float)paragraph.RightIndent,(float)paragraph.FirstLineIndent,
                includeSpacing?new((float)paragraph.SpaceBefore,(float)paragraph.SpaceAfter,(int)paragraph.LineSpacingRule,(float)paragraph.LineSpacing,
                    (int)paragraph.SpaceBeforeAuto,(int)paragraph.SpaceAfterAuto):null);
            if(!style.Known)throw new InvalidOperationException("Chưa đọc chắc căn/thụt và khoảng đoạn.");
            return style;
        }
        finally{Release(paragraph);}
    }
    public static void ApplyParagraph(object rangeObject,string expected,BodyParagraphFix fix,ObservedParagraphStyle? expectedBefore=null)
    {
        dynamic range=rangeObject;
        var before=expectedBefore??fix.Before;
        if(!fix.Valid || (string?)range.Text!=expected || !before.Matches(ReadParagraph(rangeObject)))
            throw new InvalidOperationException("Căn/thụt hoặc chữ đã đổi trước khi sửa; hãy quét lại.");
        dynamic paragraph=range.ParagraphFormat;
        try
        {
            if(fix.Alignment is int alignment)paragraph.Alignment=alignment;
            if(fix.First is float first)paragraph.FirstLineIndent=first;
        }
        finally{Release(paragraph);}
        if((string?)range.Text!=expected || !(before with {Alignment=fix.Alignment??before.Alignment,First=fix.First??before.First}).Matches(ReadParagraph(rangeObject)))
            throw new InvalidOperationException("Căn/thụt sau sửa chưa đúng hoặc khoảng đoạn đã đổi; phải phục hồi.");
    }
    public static void ApplySpacing(object rangeObject,string expected,BodySpacingFix fix,ObservedParagraphStyle? expectedBefore=null)
    {
        dynamic range=rangeObject;var before=expectedBefore??fix.Before;
        if(!fix.Valid || (string?)range.Text!=expected || !before.Matches(ReadParagraph(rangeObject)))
            throw new InvalidOperationException("Khoảng đoạn/giãn dòng hoặc chữ đã đổi; hãy quét lại.");
        dynamic paragraph=range.ParagraphFormat;
        try
        {
            if(fix.After is float after)paragraph.SpaceAfter=after;
            if(fix.Rule is int rule)paragraph.LineSpacingRule=rule;
        }
        finally{Release(paragraph);}
        if((string?)range.Text!=expected || !fix.Apply(before).Matches(ReadParagraph(rangeObject)))
            throw new InvalidOperationException("Khoảng đoạn/giãn dòng sau sửa chưa đạt hoặc phần chưa chọn đã đổi; phải phục hồi.");
    }

    public static void ApplyCharacters(object rangeObject,string expected,CharacterFormat format)
    {
        dynamic range=rangeObject;
        if(!format.Valid || (string?)range.Text!=expected)throw new InvalidOperationException("Vùng phông/cỡ đã đổi hoặc gợi ý chưa hợp lệ; hãy quét lại.");
        dynamic font=range.Font;
        try
        {
            font.Name="Times New Roman";
            if(!format.PreserveSize)font.Size=format.Size;
            font.Color=0;
            if(!format.PreserveEmphasis)
            {
                font.Italic=format.Italic?-1:0;
                if(format.Bold is bool bold)font.Bold=bold?-1:0;
            }
        }
        finally{Release(font);}
        if(format.RepairInvalidSizes)RepairSizes(rangeObject,expected,format);
    }

}
