using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class BodyParagraphWriterChecks
{
    public sealed class Layout
    {
        private int alignment;private float first;
        public int Writes {get;private set;}
        public bool FailFirst {get;set;}
        public bool DriftSpacing {get;set;}
        public int Alignment{get=>alignment;set{Writes++;alignment=value;if(DriftSpacing)SpaceAfter=30;}}
        public float FirstLineIndent{get=>first;set{Writes++;if(FailFirst)throw new COMException("synthetic",unchecked((int)0x80004005));first=value;}}
        public float LeftIndent=>0;
        public float RightIndent=>0;
        public float SpaceBefore {get;set;}=3;
        public float SpaceAfter {get;set;}=8;
        public int LineSpacingRule=>4;
        public float LineSpacing=>18;
        public int SpaceBeforeAuto=>0;
        public int SpaceAfterAuto=>0;
        public Layout(int align=0,float indent=0){alignment=align;first=indent;}
    }
    public sealed class Range(Layout layout)
    {
        public string Text{get;set;}="Nội dung công tác đã được kiểm tra đầy đủ.";
        public Layout ParagraphFormat=>layout;
        public object Font=>throw new InvalidOperationException("Paragraph-only writer must not access Font");
    }
    public static void Run(Action<bool,string> assert)
    {
        foreach(var tuple in new[]{(0,0f),(0,28.35f),(3,0f)})
        {
            var range=new Range(new(tuple.Item1,tuple.Item2));var before=VisibleFormatReader.ReadParagraph(range);
            var fix=new BodyParagraphFix(before,tuple.Item1==3?null:3,BodyParagraphRules.ValidFirst(tuple.Item2)?null:36);
            VisibleFormatReader.ApplyParagraph(range,range.Text,fix);
            var after=VisibleFormatReader.ReadParagraph(range);
            assert(fix.Expected.Matches(after) && range.ParagraphFormat.Writes==(fix.Alignment is null?0:1)+(fix.First is null?0:1),
                "Paragraph-only COM writer sets exactly the requested alignment/first-line properties without accessing Font or writing spacing");
            bool stale=false;try{VisibleFormatReader.ApplyParagraph(range,range.Text,fix);}catch(InvalidOperationException){stale=true;}
            assert(stale,"A paragraph target applied to an already-changed source is rejected rather than replaying an old payload");
        }
        var changed=new Range(new());var payload=new BodyParagraphFix(VisibleFormatReader.ReadParagraph(changed),3,36);
        changed.ParagraphFormat.SpaceAfter=10;bool invalid=false;
        try{VisibleFormatReader.ApplyParagraph(changed,changed.Text,payload);}catch(InvalidOperationException){invalid=true;}
        assert(invalid && changed.ParagraphFormat.Writes==0,"An unselected spacing change invalidates a stale paragraph write before any setter runs");
        var drift=new Range(new(){DriftSpacing=true});var desired=new BodyParagraphFix(VisibleFormatReader.ReadParagraph(drift),3,36);
        bool detected=false;try{VisibleFormatReader.ApplyParagraph(drift,drift.Text,desired);}catch(InvalidOperationException){detected=true;}
        assert(detected,"A Word setter that drifts an unselected spacing property fails immediate verification");
        var failing=new Range(new(){FailFirst=true});var fail=new BodyParagraphFix(VisibleFormatReader.ReadParagraph(failing),3,36);
        bool restored=false;var issue=new Issue(100,failing.Text.Length,failing.Text,failing.Text,"",false,FixKind.BodyParagraph,Citation:BodyParagraphRules.Citation,ParagraphFix:fail);
        try{AtomicTextBatch.Apply([issue],i=>failing.Text,i=>{VisibleFormatReader.ApplyParagraph(failing,i.Original,fail);return failing.Text;},()=>{},()=>restored=true);}catch(COMException){}
        assert(restored,"A COM exception after one paragraph setter reaches the transaction's whole-batch rollback path");
        var model=new IssueReviewModel([issue,issue with {Start=300},new(500,4,"test","test","",false,FixKind.Advisory,CanFix:false)]);
        model.Choices[0].Group.IsChecked=true;
        assert(model.All.SelectedCount==2 && model.Choices.Take(2).All(c=>c.Info.Section==IssueReviewSection.RequiredFormat && c.Tooltip.Split('\n').Length==3),
            "Paragraph groups select both compact repair rows, retain three-part tooltips and leave the manual warning unselected");
    }
}
