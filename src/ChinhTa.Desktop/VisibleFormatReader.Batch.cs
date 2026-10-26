using System.Runtime.InteropServices;
using ChinhTa.Core;
using Microsoft.CSharp.RuntimeBinder;

namespace ChinhTa.Desktop;

public sealed record NativeFormatTarget(WordParagraphSnapshot Row,bool ReadParagraph,string? ParagraphKey);
public sealed record NativeFormatHint(WordParagraphSnapshot Row,VisibleFont? Font,ObservedParagraphStyle? Paragraph)
{
    public bool Matches(int start,int end,string text)=>Row.Start==start && Row.End==end && Row.Text==text;
}
public sealed class FormatBatchMeasurements
{
    public int Ranges { get; internal set; }
    public int FontParagraphs { get; internal set; }
    public int StyleParagraphs { get; internal set; }
    public int Splits { get; internal set; }
    public int Fallbacks { get; internal set; }
}

public static partial class VisibleFormatReader
{
    // Only current-scan, XML-authorized targets are supplied. Real Word aggregate
    // properties must be known; XML signatures merely narrow candidate groups.
    public static IReadOnlyDictionary<int,NativeFormatHint> ReadBatches(IReadOnlyList<NativeFormatTarget> targets,
        Func<int,int,object> openRange,FormatBatchMeasurements measurements)
    {
        var hints=new Dictionary<int,NativeFormatHint>();var group=new List<NativeFormatTarget>();int chars=0;
        void Observe(NativeFormatTarget[] rows,int depth)
        {
            if(rows.Length<4)return;
            string text=string.Concat(rows.Select(r=>r.Row.Text));
            int start=rows[0].Row.Start,end=rows[^1].Row.End;
            VisibleFont? font=null;ObservedParagraphStyle? style=null;bool matched=false;
            try
            {
                dynamic range=openRange(start,end);
                try
                {
                    measurements.Ranges++;
                    if((int)range.Start!=start || (int)range.End!=end || (string?)range.Text!=text)
                    {measurements.Fallbacks++;return;}
                    VisibleFont aggregate=Font(range);if(aggregate.Known)font=aggregate;
                    if(rows.All(r=>r.ReadParagraph && r.ParagraphKey is not null && r.ParagraphKey==rows[0].ParagraphKey))
                    {
                        // Word can include an adjacent paragraph at a range boundary.
                        // Do not share its ParagraphFormat unless cardinality agrees.
                        dynamic paragraphs=range.Paragraphs;
                        try
                        {
                            if((int)paragraphs.Count==rows.Length)
                            {
                                try{style=ReadParagraph((object)range);}
                                catch(Exception error) when(OptionalReadFailure(error)){style=null;}
                            }
                        }
                        finally{Release(paragraphs);}
                    }
                    matched=(int)range.Start==start && (int)range.End==end && (string?)range.Text==text;
                }
                finally{Release(range);}
            }
            catch(Exception error) when(OptionalReadFailure(error))
            {measurements.Fallbacks++;return;}
            if(!matched){measurements.Fallbacks++;return;}
            if(font is not null || style is not null)
            {
                foreach(var row in rows)hints[row.Row.Start]=new(row.Row,font,style);
                if(font is not null)measurements.FontParagraphs+=rows.Length;
                if(style is not null)measurements.StyleParagraphs+=rows.Length;
                return;
            }
            // Bound extra COM work on heavily mixed documents: only one split.
            if(depth==0 && rows.Length>=8)
            {
                measurements.Splits++;int half=rows.Length/2;
                Observe(rows[..half],1);Observe(rows[half..],1);
            }
            else measurements.Fallbacks++;
        }
        void Flush(){if(group.Count>0){Observe(group.ToArray(),0);group.Clear();chars=0;}}
        foreach(var target in targets)
        {
            var row=target.Row;
            if(row.Start<0 || row.End<=row.Start || string.IsNullOrWhiteSpace(row.Text.TrimEnd('\r','\a','\n')))
            {Flush();continue;}
            if(group.Count>0 && (row.Start!=group[^1].Row.End || target.ReadParagraph!=group[0].ReadParagraph || group.Count>=16 || chars+row.Text.Length>12000))Flush();
            group.Add(target);chars+=row.Text.Length;
        }
        Flush();return hints;
    }
    private static bool OptionalReadFailure(Exception error)=>error is InvalidOperationException or RuntimeBinderException ||
        error is COMException && !WordConnectionPolicy.IsDisconnected(error);

    public static bool MatchesHint(object rangeObject,NativeFormatHint hint)
    {
        dynamic range=rangeObject;
        return (hint.Font is null || hint.Font.Known) && (hint.Paragraph is null || hint.Paragraph.Known) &&
            (int)range.Start==hint.Row.Start && (int)range.End==hint.Row.End && (string?)range.Text==hint.Row.Text;
    }
}
