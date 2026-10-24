using System.Text;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public static partial class VisibleFormatReader
{
    // Use Word's actual character ranges; Word CPs are not UTF-16 indices.
    // Prove the complete sequence and every bound before any size write.
    public static void RepairSizes(object rangeObject,string expected,CharacterFormat style)
    {
        if(!style.Valid || !style.RepairInvalidSizes)
            throw new InvalidOperationException("Chưa có đề xuất sửa riêng cỡ chữ hợp lệ.");
        dynamic range=rangeObject;
        if((string?)range.Text!=expected)throw new InvalidOperationException("Vùng sửa cỡ chữ đã thay đổi.");
        int start=(int)range.Start,end=(int)range.End;
        if(start<0 || end<=start)throw new InvalidOperationException("Biên vùng sửa cỡ chữ chưa rõ.");
        var repairs=new List<object>();var text=new StringBuilder();int cursor=start;
        dynamic characters=range.Characters;
        try
        {
            int count=(int)characters.Count;
            if(count<1 || count>expected.Length+2)throw new InvalidOperationException("Chưa đối chiếu đủ ký tự cần sửa cỡ.");
            for(int i=1;i<=count;i++)
            {
                dynamic child=characters[i];
                try
                {
                    string value=(string?)child.Text??"";
                    int from=(int)child.Start,to=(int)child.End;
                    if(from!=cursor || to<=from || to>end || value.Length==0)
                        throw new InvalidOperationException("Ký tự Word nằm ngoài vùng sửa cỡ đã duyệt.");
                    cursor=to;text.Append(value);
                    if(!string.IsNullOrWhiteSpace(value))
                    {
                        dynamic font=child.Font;float size;
                        try{size=(float)font.Size;}finally{Release(font);}
                        if(!float.IsFinite(size) || size<=0 || size>1638)
                            throw new InvalidOperationException("Chưa đọc chắc cỡ của một ký tự cần sửa.");
                        if(size<style.SizeRange.Minimum || size>style.SizeRange.Maximum)
                        {repairs.Add((object)child);child=null!;}
                    }
                }
                finally{Release(child);}
            }
            if(cursor!=end || text.ToString()!=expected || (string?)range.Text!=expected)
                throw new InvalidOperationException("Chưa đối chiếu đủ vùng sửa riêng cỡ chữ.");
            foreach(dynamic child in repairs)
            {
                dynamic font=child.Font;
                try{font.Size=style.Size;}finally{Release(font);}
            }
        }
        finally
        {
            foreach(var child in repairs)Release(child);
            Release(characters);
        }
    }
}
