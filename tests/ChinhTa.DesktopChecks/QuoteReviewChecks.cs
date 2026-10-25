using ChinhTa.Core;
using ChinhTa.Desktop;

public static class QuoteReviewChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string text="Nội dung  “ nội dung báo cáo ” đã duyệt.";
        var issues=Checker.Check([new(0,text)]);var model=new IssueReviewModel(issues);
        var quotes=model.Choices.Where(c=>c.Issue.Kind==FixKind.QuoteSpace).ToArray();
        quotes[0].Group.IsChecked=true;
        assert(quotes.Length==2 && model.All.SelectedCount==2 && quotes.All(c=>c.IsChecked && c.Info.Section==IssueReviewSection.ReviewSuggestion &&
            c.Tooltip.Split('\n').Length==3) && model.Choices.Where(c=>c.Issue.Kind!=FixKind.QuoteSpace).All(c=>!c.IsChecked),
            "The quote checkbox group selects both inner edges without selecting independent spacing defects");
        model.All.IsChecked=true;
        var plan=BatchPlan.Create(model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue));
        assert(AtomicTextBatch.ExpectedStory(plan,text,i=>text[..i.Start])=="Nội dung “nội dung báo cáo” đã duyệt.",
            "Select all safely combines quote and ordinary spacing groups");
        quotes[0].IsChecked=false;
        assert(quotes[0].Group.IsChecked is null && quotes[1].IsChecked,
            "Unchecking one quote edge retains the other edge and the group's partial selection state");
        var refreshed=new IssueReviewModel(Checker.Check([new(0,"Nội dung “nội dung báo cáo” đã duyệt.")]));
        assert(!refreshed.Choices.Any(c=>c.Issue.Kind==FixKind.QuoteSpace),"Fresh results remove repaired quote rows entirely");
        var manual=new IssueReviewModel(Checker.Check([new(0,"“ nội dung ”",TextEditable:false)]));manual.All.IsChecked=true;
        assert(manual.All.SelectedCount==0 && manual.Choices.All(c=>!c.IsChecked),"A manual quote/XML warning remains unselectable through Select all");
    }
}
