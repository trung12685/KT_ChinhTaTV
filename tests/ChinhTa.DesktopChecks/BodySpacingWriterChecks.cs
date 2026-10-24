using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class BodySpacingWriterChecks
{
    public sealed class Layout
    {
        private float after;private int rule=2;private int alignment;private float first;
        public int AfterWrites {get;private set;}
        public int RuleWrites {get;private set;}
        public bool DriftBefore {get;set;}
        public bool FailAfter {get;set;}
        public float SpaceAfter{get=>after;set{AfterWrites++;if(FailAfter)throw new COMException("synthetic",unchecked((int)0x80004005));after=value;}}
        public int LineSpacingRule{get=>rule;set{RuleWrites++;rule=value;}}
        public float SpaceBefore=>DriftBefore && RuleWrites>0?10:0;
        public float LineSpacing=>rule<=2 && rule!=2?12:24;
        public int SpaceBeforeAuto=>0;
        public int SpaceAfterAuto=>0;
        public float LeftIndent=>0;
        public float RightIndent=>0;
        public int Alignment{get=>alignment;set=>alignment=value;}
        public float FirstLineIndent{get=>first;set=>first=value;}
    }
    public sealed class Range(Layout layout)
    {
        public string Text{get;set;}="Nội dung công tác đã được kiểm tra đầy đủ.";
        public Layout ParagraphFormat=>layout;
        public object Font=>throw new InvalidOperationException("Spacing-only writer must not access Font");
    }
    private static (Issue Gap,Issue Line,Issue Layout) Issues(Range range)
    {
        var style=VisibleFormatReader.ReadParagraph(range);var context=new BodySpacingContext();var flow=new BodyFlow(1,1);
        var neighbor=new BodyGapNeighbor(500,"Nội dung đoạn tiếp đã được kiểm tra đầy đủ.",style,context,flow);
        Issue Make(FixKind kind,BodySpacingFix fix)=>new(100,range.Text.Length,range.Text,range.Text,"",false,kind,
            Citation:kind==FixKind.BodyGap?BodySpacingRules.GapCitation:BodySpacingRules.LineCitation,SpacingFix:fix);
        return (Make(FixKind.BodyGap,new(style,context,flow,neighbor,After:6)),Make(FixKind.BodyLineSpacing,new(style,context,flow,Rule:0)),
            new(100,range.Text.Length,range.Text,range.Text,"",false,FixKind.BodyParagraph,Citation:BodyParagraphRules.Citation,ParagraphFix:new(style,3,36)));
    }
    public static void Run(Action<bool,string> assert)
    {
        foreach(bool gapOnly in new[]{true,false})
        {
            var range=new Range(new());var issues=Issues(range);var item=gapOnly?issues.Gap:issues.Line;
            VisibleFormatReader.ApplySpacing(range,range.Text,item.SpacingFix!);
            var final=VisibleFormatReader.ReadParagraph(range);
            assert(range.ParagraphFormat.AfterWrites==(gapOnly?1:0) && range.ParagraphFormat.RuleWrites==(gapOnly?0:1) &&
                (gapOnly?final.Spacing is {After:6,Rule:2}:final.Spacing is {After:0,Rule:0}) && final.Alignment==0 && final.First==0,
                "Spacing writer sets only SpaceAfter or LineSpacingRule, never Font, before-spacing, point height, alignment or indent");
            bool stale=false;try{VisibleFormatReader.ApplySpacing(range,range.Text,item.SpacingFix!);}catch(InvalidOperationException){stale=true;}
            assert(stale,"A stale spacing payload is rejected instead of replayed after the first repair");
        }
        foreach(int order in new[]{0,1,2})
        {
            var range=new Range(new());var items=Issues(range);
            var plan=BatchPlan.Create(order==0?[items.Gap,items.Line,items.Layout]:order==1?[items.Layout,items.Line,items.Gap]:[items.Line,items.Gap,items.Layout]);
            foreach(var item in plan)
            {
                var before=BodyStylePatchPlan.Before(item,plan);
                if(item.ParagraphFix is { } layout)VisibleFormatReader.ApplyParagraph(range,range.Text,layout,before);
                else VisibleFormatReader.ApplySpacing(range,range.Text,item.SpacingFix!,before);
            }
            var final=VisibleFormatReader.ReadParagraph(range);
            assert(final is {Alignment:3,First:36,Spacing:{After:6,Rule:0}} && range.ParagraphFormat.AfterWrites==1 && range.ParagraphFormat.RuleWrites==1,
                "Real paragraph setters compose in every selection order while preserving another patch's approved settings and derived Single height");
        }
        var drifting=new Range(new(){DriftBefore=true});var target=Issues(drifting).Line;bool rejected=false;
        try{VisibleFormatReader.ApplySpacing(drifting,drifting.Text,target.SpacingFix!);}catch(InvalidOperationException){rejected=true;}
        assert(rejected,"An unexpected change to unselected before-spacing fails the line writer's immediate postcondition");
        var failing=new Range(new(){FailAfter=true});var failure=Issues(failing).Gap;bool restored=false;
        try{AtomicTextBatch.Apply([failure],i=>failing.Text,i=>{VisibleFormatReader.ApplySpacing(failing,i.Original,i.SpacingFix!);return failing.Text;},()=>{},()=>restored=true);}catch(COMException){}
        assert(restored,"A COM spacing setter failure reaches whole-batch rollback");
        var modelItems=Issues(new(new()));var model=new IssueReviewModel([modelItems.Gap,modelItems.Line,modelItems.Layout]);
        model.Choices.Single(c=>c.Issue.Kind==FixKind.BodyGap).Group.IsChecked=true;
        assert(model.All.SelectedCount==1 && model.Groups.Count==3 && model.Choices.Single(c=>c.IsChecked).Issue.Kind==FixKind.BodyGap,
            "Gap, line and indent remain independently selectable compact groups");
        model.All.IsChecked=true;assert(model.All.SelectedCount==3 && model.Choices.All(c=>c.Info.Section==IssueReviewSection.RequiredFormat && c.Tooltip.Split('\n').Length==3),
            "Select all combines the three groups with three-part legal tooltips");
        var refreshed=new IssueReviewModel([modelItems.Line]);assert(refreshed.All.SelectableCount==1 && !refreshed.Choices.Any(c=>c.Issue.Kind==FixKind.BodyGap),
            "A corrected gap disappears from a fresh list while an unselected line defect remains actionable");
    }
}
