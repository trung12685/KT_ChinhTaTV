using ChinhTa.Core;
using ChinhTa.Desktop;

public static class QuoteAfterReviewChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string text="Nội dung  “ nội dung báo cáo ”đã duyệt.";
        var model=new IssueReviewModel(Checker.Check([new(0,text)]));
        var quotes=model.Choices.Where(c=>c.Issue.Kind==FixKind.QuoteSpace).ToArray();var outside=quotes.Single(c=>c.Issue.AddsSpaceAfterClosingQuote);
        outside.IsChecked=true;
        assert(quotes.Length==3 && model.All.SelectedCount==1 && outside.Group.IsChecked is null && outside.Tooltip.Contains("Thêm một dấu cách") &&
            quotes.Where(c=>c!=outside).All(c=>!c.IsChecked),"One missing-gap checkbox does not select either inner quote edge");
        outside.Group.IsChecked=true;
        assert(model.All.SelectedCount==3 && quotes.All(c=>c.IsChecked) && model.Choices.Where(c=>c.Issue.Kind!=FixKind.QuoteSpace).All(c=>!c.IsChecked),
            "The quote group selects both inner edges and the outside insertion together, independently of other groups");
        model.All.IsChecked=true;var plan=BatchPlan.Create(model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue));
        assert(AtomicTextBatch.ExpectedStory(plan,text,i=>text[..i.Start])=="Nội dung “nội dung báo cáo” đã duyệt.",
            "Select all commits mixed quote deletions/insertion and old spaces without overlapping spans");
        var fresh=new IssueReviewModel(Checker.Check([new(0,"Nội dung “nội dung báo cáo”đã duyệt.")]));
        assert(fresh.Choices.Count(c=>c.Issue.Kind==FixKind.QuoteSpace)==1 && fresh.Choices.Single(c=>c.Issue.Kind==FixKind.QuoteSpace).Issue.AddsSpaceAfterClosingQuote,
            "Fresh results keep only the still-actionable outside gap after inner-only repair");
        var done=new IssueReviewModel(Checker.Check([new(0,"Nội dung “nội dung báo cáo” đã duyệt.")]));
        assert(!done.Choices.Any(c=>c.Issue.Kind==FixKind.QuoteSpace),"All corrected quote rows disappear after rescanning");
    }
}
