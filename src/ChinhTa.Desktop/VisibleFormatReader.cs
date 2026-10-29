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
public sealed class FormatReadMeasurements
{
    public int AggregateReads { get; internal set; }
    public int PartitionReads { get; internal set; }
    public int PartitionSuccesses { get; internal set; }
    public int PartitionFallbacks { get; internal set; }
    public int WordReads { get; internal set; }
    public int CharacterReads { get; internal set; }
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
        int start=(int)range.Start,end=(int)range.End,cursor=start;
        if(start<0 || end<=start)throw new InvalidOperationException("Chưa rõ biên vùng giữ cỡ chữ.");
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
                    string value=(string?)child.Text??"";int from=(int)child.Start,to=(int)child.End;
                    if(from!=cursor || to<=from || to>end || value.Length==0 ||
                        text.Length>expected.Length-value.Length || !expected.AsSpan(text.Length,value.Length).SequenceEqual(value))
                        throw new InvalidOperationException("Ký tự nằm ngoài vùng giữ cỡ chữ hoặc không khớp nguồn.");
                    cursor=to;text.Append(value);float? size=null;
                    if(!string.IsNullOrWhiteSpace(value))
                    {dynamic font=child.Font;try{size=(float)font.Size;}finally{Release(font);}}
                    sizes.AddRange(value.Select(c=>char.IsWhiteSpace(c)?(float?)null:size));
                }
                finally{Release(child);}
            }
        }
        finally{Release(characters);}
        if(cursor!=end || text.ToString()!=expected || (int)range.Start!=start || (int)range.End!=end || (string?)range.Text!=expected)
            throw new InvalidOperationException("Chưa đối chiếu đủ ký tự để giữ cỡ chữ.");
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
    public static IReadOnlyList<VisibleFont> Read(object rangeObject,string expected,bool allowCharacterFallback,
        WordFormatRunPlan? runPlan=null,Func<int,int,object>? readRange=null,FormatReadMeasurements? measurements=null)
    {
        dynamic range=rangeObject;
        if((string?)range.Text!=expected)throw new InvalidOperationException("Vùng định dạng đã thay đổi; hãy quét lại.");
        if(measurements is not null)measurements.AggregateReads++;
        VisibleFont whole=Font(range);
        if(whole.Known || !allowCharacterFallback)return [whole];
        if(runPlan is not null && readRange is not null)
        {
            var partitioned=ReadPartitions(rangeObject,runPlan,expected,readRange,measurements);
            if(partitioned is not null)
            {
                // Recheck the containing live text after reading every run.
                if((string?)range.Text!=expected)throw new InvalidOperationException("Vùng định dạng đã thay đổi; hãy quét lại.");
                if(measurements is not null)measurements.PartitionSuccesses++;
                return partitioned;
            }
            if(measurements is not null)measurements.PartitionFallbacks++;
        }
        try
        {
            dynamic words=range.Words;
            try{return Children((object)words,expected,whole,allowCharacters:true,measurements:measurements);}
            finally{Release(words);}
        }
        catch(Exception error) when(error is RuntimeBinderException or InvalidOperationException ||
            error is COMException && !WordConnectionPolicy.IsDisconnected(error))
        { /* Word may return a last word outside the requested range; verify Characters instead. */ }
        dynamic characters=range.Characters;
        try{return Children((object)characters,expected,whole,allowCharacters:false,measurements:measurements);}
        finally{Release(characters);}
    }
    private static IReadOnlyList<VisibleFont>? ReadPartitions(object parentObject,WordFormatRunPlan plan,string expected,
        Func<int,int,object> readRange,FormatReadMeasurements? measurements)
    {
        if(plan.Text!=expected)return null;
        var fonts=new List<VisibleFont>();
        try
        {
            dynamic parent=parentObject;
            if((int)parent.Start!=plan.Start || (long)(int)parent.End!=(long)plan.Start+expected.Length)return null;
            foreach(var part in plan.Runs)
            {
                dynamic child=readRange(part.Start,part.End);
                try
                {
                    if(measurements is not null)measurements.PartitionReads++;
                    if((int)child.Start!=part.Start || (int)child.End!=part.End || (string?)child.Text!=part.Text)return null;
                    if(string.IsNullOrWhiteSpace(part.Text))continue;
                    var font=Font(child);
                    if(!font.Known)return null;
                    fonts.Add(font);
                }
                finally{Release(child);}
            }
            return fonts.Count>0?fonts:null;
        }
        catch(Exception error) when(error is RuntimeBinderException or InvalidOperationException ||
            error is COMException && !WordConnectionPolicy.IsDisconnected(error))
        {return null;}
    }
    private static IReadOnlyList<VisibleFont> Children(object childrenObject,string expected,VisibleFont whole,bool allowCharacters,FormatReadMeasurements? measurements=null)
    {
        dynamic children=childrenObject;
        var text=new StringBuilder();var fonts=new List<VisibleFont>();
        int count=(int)children.Count;
        if(count<1 || count>expected.Length+2)throw new InvalidOperationException("Chưa đối chiếu được số vùng Word để kiểm phông.");
        for(int i=1;i<=count;i++)
        {
            if(measurements is not null)
            {if(allowCharacters)measurements.WordReads++;else measurements.CharacterReads++;}
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
                        try{fonts.AddRange(Children((object)characters,value,observed,allowCharacters:false,measurements:measurements));}
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
