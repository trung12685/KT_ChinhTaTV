using ChinhTa.Core;
using ChinhTa.Desktop;

public static class DelimiterClusterReviewChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string text="Theo mẫu\" nội dung \"đã  duyệt.";
        var model=new IssueReviewModel(Checker.Check([new(0,text)]));var quotes=model.Choices.Where(c=>c.Issue.Kind==FixKind.QuoteSpace).ToArray();
        quotes[0].Group.IsChecked=true;
        assert(quotes.Length==4 && model.All.SelectedCount==4 && quotes.All(c=>c.IsChecked && c.Tooltip.Split('\n').Length==3),
            "One quote group selects before/inside/after corrections without requiring four separate repair rounds");
        model.All.IsChecked=true;var plan=BatchPlan.Create(model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue));
        assert(AtomicTextBatch.ExpectedStory(plan,text,i=>text[..i.Start])=="Theo mẫu \"nội dung\" đã duyệt.","Select all combines the complete delimiter cluster and independent word spacing");
        var before=quotes.Single(c=>c.Issue.AddsSpaceBeforeOpeningQuote);before.IsChecked=false;
        assert(before.Group.IsChecked is null && quotes.Where(c=>c!=before).All(c=>c.IsChecked),"Each edge remains independently selectable within a partially selected group");
        var fresh=new IssueReviewModel(Checker.Check([new(0,"Theo mẫu\"nội dung\"đã duyệt.")]));
        assert(fresh.Choices.Count(c=>c.Issue.Kind==FixKind.QuoteSpace)==2 && fresh.Choices.All(c=>c.Issue.AddsSpaceBeforeOpeningQuote || c.Issue.AddsSpaceAfterClosingQuote),
            "Inner-only repair leaves exactly the two exterior gaps as actionable rows");
        var done=new IssueReviewModel(Checker.Check([new(0,"Theo mẫu \"nội dung\" đã duyệt.")]));
        assert(!done.Choices.Any(c=>c.Issue.Kind==FixKind.QuoteSpace),"The complete repaired cluster disappears on a fresh scan");
    }
}
