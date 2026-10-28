using ChinhTa.Core;

namespace ChinhTa.Desktop;

public static class SignatureSpaceWriter
{
    public static ObservedParagraphStyle SetBefore(object rangeObject,string text,ObservedParagraphStyle before,float value,Action<object?> release)
    {
        dynamic range=rangeObject;
        if(!float.IsFinite(value) || value<0 || value>720 || before.Spacing is not {Known:true} ||
            (string?)range.Text!=text || !before.Matches(VisibleFormatReader.ReadParagraph(rangeObject)))
            throw new InvalidOperationException("Chữ hoặc khoảng đoạn vùng ký đã thay đổi trước khi sửa.");
        dynamic paragraph=range.ParagraphFormat;
        try{if(before.Spacing.BeforeAuto!=0)paragraph.SpaceBeforeAuto=0;paragraph.SpaceBefore=value;}
        finally{release(paragraph);}
        var expected=before with{Spacing=before.Spacing with{Before=value,BeforeAuto=0}};
        if((string?)range.Text!=text || !expected.Matches(VisibleFormatReader.ReadParagraph(rangeObject)))
            throw new InvalidOperationException("Khoảng trước họ tên chưa đúng hoặc thuộc tính khác đã đổi; phải phục hồi.");
        return expected;
    }
}
