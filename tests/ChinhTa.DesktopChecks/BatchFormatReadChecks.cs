using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class BatchFormatReadChecks
{
    public sealed class Counters { public int FontProperties,ParagraphProperties,Opened; }
    public sealed class Font(VisibleFont value,Counters counts)
    {
        public string Name{get{counts.FontProperties++;return value.Name;}}
        public float Size{get{counts.FontProperties++;return value.Size;}}
        public int Bold{get{counts.FontProperties++;return value.Bold;}}
        public int Italic{get{counts.FontProperties++;return value.Italic;}}
        public int Color{get{counts.FontProperties++;return value.Color;}}
    }
    public sealed class Layout(ObservedParagraphStyle[] values,Counters counts)
    {
        private float Number(Func<ObservedParagraphStyle,float> get)
        {counts.ParagraphProperties++;var all=values.Select(get).Distinct().ToArray();return all.Length==1?all[0]:9999999;}
        public int Alignment=>(int)Number(v=>v.Alignment);
        public float LeftIndent=>Number(v=>v.Left);
        public float RightIndent=>Number(v=>v.Right);
        public float FirstLineIndent=>Number(v=>v.First);
        public float SpaceBefore=>Number(v=>v.Spacing!.Before);
        public float SpaceAfter=>Number(v=>v.Spacing!.After);
        public int LineSpacingRule=>(int)Number(v=>v.Spacing!.Rule);
        public float LineSpacing=>Number(v=>v.Spacing!.Line);
        public int SpaceBeforeAuto=>(int)Number(v=>v.Spacing!.BeforeAuto);
        public int SpaceAfterAuto=>(int)Number(v=>v.Spacing!.AfterAuto);
    }
    public sealed class Counted(int count){public int Count=>count;}
    public sealed class Range(int start,int end,string text,Font font,Layout layout,int paragraphs)
    {
        public int Start{get;set;}=start;
        public int End{get;set;}=end;
        public string Text{get;set;}=text;
        public Font Font=>font;
        public Action? OnAttributesRead{get;set;}
        public int ParagraphCount{get;set;}=paragraphs;
        public Layout ParagraphFormat{get{OnAttributesRead?.Invoke();return layout;}}
        public Counted Paragraphs=>new(ParagraphCount);
    }
    public static void Run(Action<bool,string> assert)
    {
        var font=new VisibleFont("Times New Roman",13,0,0,0);
        var style=new ObservedParagraphStyle(3,0,0,36,new(0,6,0,12,0,0));
        int cp=71;
        NativeFormatTarget[] Targets(int count)=>Enumerable.Range(0,count).Select(i=>
        {string text=$"Nội dung đoạn {i} cần kiểm tra.\r";var row=new WordParagraphSnapshot(cp,cp+text.Length,text,0,false,false);cp=row.End;return new NativeFormatTarget(row,true,"same");}).ToArray();
        var targets=Targets(16);var fonts=Enumerable.Repeat(font,16).ToArray();var styles=Enumerable.Repeat(style,16).ToArray();
        object Factory(int from,int to,Counters counter,Action<Range>? mutate=null)
        {
            counter.Opened++;var indices=Enumerable.Range(0,targets.Length).Where(i=>targets[i].Row.Start>=from && targets[i].Row.End<=to).ToArray();
            var selected=indices.Select(i=>targets[i]).ToArray();var allFonts=indices.Select(i=>fonts[i]).Distinct().ToArray();
            var range=new Range(from,to,string.Concat(selected.Select(t=>t.Row.Text)),
                new(allFonts.Length==1?allFonts[0]:new("",9999999,9999999,9999999,9999999),counter),
                new(indices.Select(i=>styles[i]).ToArray(),counter),indices.Length);
            mutate?.Invoke(range);return range;
        }
        var oldCounts=new Counters();
        foreach(var t in targets)
        {
            object range=Factory(t.Row.Start,t.Row.End,oldCounts);
            assert(VisibleFormatReader.Read(range,t.Row.Text,true).Single()==font && VisibleFormatReader.ReadParagraph(range)==style,
                "Legacy per-paragraph evidence is established before comparing batch reads");
        }
        var counts=new Counters();var measurements=new FormatBatchMeasurements();
        var hints=VisibleFormatReader.ReadBatches(targets,(a,b)=>Factory(a,b,counts),measurements);
        assert(hints.Count==16 && hints.Values.All(h=>h.Font==font && h.Paragraph==style) && measurements.Ranges==1,
            "A uniform group returns the same native font/paragraph values for all sixteen bound paragraphs");
        assert(oldCounts.FontProperties==80 && counts.FontProperties==5 && oldCounts.ParagraphProperties==160 && counts.ParagraphProperties==10,
            "The sixteen-paragraph model reduces font getters 80 to 5 and paragraph getters 160 to 10 without changing observations");
        assert(hints.Values.All(h=>VisibleFormatReader.MatchesHint(Factory(h.Row.Start,h.Row.End,new()),h)),
            "Every shared hint can be rebound to its exact current live paragraph before use");
        var stale=hints.Values.First();var wrong=(Range)Factory(stale.Row.Start,stale.Row.End,new());wrong.Text="Khác";
        assert(!VisibleFormatReader.MatchesHint(wrong,stale) && !stale.Matches(stale.Row.Start+1,stale.Row.End+1,stale.Row.Text),
            "Stale text or a repeated paragraph at another CP cannot consume a shared hint");
        foreach(string defect in new[]{"text","start","end"})
        {
            counts=new();measurements=new();
            hints=VisibleFormatReader.ReadBatches(targets,(a,b)=>Factory(a,b,counts,r=>{if(defect=="text")r.Text+="X";else if(defect=="start")r.Start++;else r.End++;}),measurements);
            assert(hints.Count==0 && counts.FontProperties==0 && measurements.Fallbacks==1,"An incorrect batch range discards all hints before reading attributes: "+defect);
        }
        foreach(var mixed in new[]{style with {Alignment=0},style with {Left=1},style with {Right=1},style with {First=0},
            style with {Spacing=style.Spacing! with {Before=1}},style with {Spacing=style.Spacing! with {After=0}},
            style with {Spacing=style.Spacing! with {Rule=1}},style with {Spacing=style.Spacing! with {Line=18}},
            style with {Spacing=style.Spacing! with {BeforeAuto=-1}},style with {Spacing=style.Spacing! with {AfterAuto=-1}}})
        {
            styles[7]=mixed;counts=new();hints=VisibleFormatReader.ReadBatches(targets,(a,b)=>Factory(a,b,counts),new());
            assert(hints.Count==16 && hints.Values.All(h=>h.Font==font && h.Paragraph is null),
                "An XML key never hides a mixed native alignment/indent/spacing/auto-spacing property");
        }
        styles[7]=style;
        hints=VisibleFormatReader.ReadBatches(targets,(a,b)=>Factory(a,b,new(),r=>r.ParagraphCount++),new());
        assert(hints.Count==16 && hints.Values.All(h=>h.Font==font && h.Paragraph is null),
            "A native range containing an extra paragraph cannot share its ParagraphFormat with the target rows");
        hints=VisibleFormatReader.ReadBatches(targets,(a,b)=>Factory(a,b,new(),r=>r.OnAttributesRead=()=>r.Text+="X"),new());
        assert(hints.Count==0,"Text drift during aggregate property reads discards both font and paragraph hints before they can be consumed");
        styles[7]=style;fonts[7]=font with {Color=255};counts=new();
        hints=VisibleFormatReader.ReadBatches(targets,(a,b)=>Factory(a,b,counts),new());
        assert(hints.Count==16 && hints.Values.All(h=>h.Font is null && h.Paragraph==style),
            "Mixed actual fonts remain per-paragraph while independently uniform paragraph attributes can be reused");
        fonts[7]=font;
        var differentKeys=targets.Select((t,i)=>t with {ParagraphKey=i%2==0?"a":"b"}).ToArray();
        hints=VisibleFormatReader.ReadBatches(differentKeys,(a,b)=>Factory(a,b,new()),new());
        assert(hints.Values.All(h=>h.Paragraph is null),"Different XML paragraph signatures retain native per-paragraph style reads");
        var tableTargets=targets.Select(t=>t with {ParagraphKey=null}).ToArray();
        hints=VisibleFormatReader.ReadBatches(tableTargets,(a,b)=>Factory(a,b,new()),new());
        assert(hints.Count==16 && hints.Values.All(h=>h.Paragraph is null),"Table/complex targets never gain shared paragraph-format evidence from a missing XML key");
        counts=new();var split=targets.Select((t,i)=>i==8?t with {Row=t.Row with {Start=t.Row.Start+1}}:t).ToArray();
        hints=VisibleFormatReader.ReadBatches(split,(a,b)=>Factory(a,b,counts),new());
        assert(!hints.ContainsKey(split[8].Row.Start),"Native CP gaps split groups instead of concatenating across unobserved content");
        bool disconnected=false;
        try{VisibleFormatReader.ReadBatches(targets,(_,_)=>throw new COMException("RPC",unchecked((int)0x800706BE)),new());}catch(COMException){disconnected=true;}
        assert(disconnected,"Disconnected batch reads propagate to the established Word recovery path");
        hints=VisibleFormatReader.ReadBatches(targets,(_,_)=>throw new COMException("Unavailable",unchecked((int)0x80004005)),new());
        assert(hints.Count==0,"An optional COM aggregate failure leaves every paragraph on the original reader");
        var bounded=targets.Select((t,i)=>t with {ParagraphKey=null}).ToArray();
        for(int i=0;i<fonts.Length;i++)fonts[i]=font with {Size=i%2==0?13:14};measurements=new();
        hints=VisibleFormatReader.ReadBatches(bounded,(a,b)=>Factory(a,b,new()),measurements);
        assert(hints.Count==0 && measurements.Ranges==3 && measurements.Splits==1,
            "Heavily mixed groups use at most one split before reverting to all original per-paragraph reads");
    }
}
