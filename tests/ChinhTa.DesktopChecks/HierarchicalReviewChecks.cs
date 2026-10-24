using ChinhTa.Core;
using ChinhTa.Desktop;

public static class HierarchicalReviewChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string[] texts=["1. Mục cha","1.1. Nhánh đầu","1.4. Nhánh sau","2. Mục cha tiếp",
            "2.1. Nhánh đầu","2.4. Nhánh sau","4. Mục cần duyệt","4.1. Nhánh phụ thuộc"];
        int cp=0;var rows=texts.Select(t=>{var p=new Paragraph(cp,t+"\r",Outline:new(OutlineScope.PlainBody));cp+=p.Text.Length;return p;}).ToArray();
        var issues=Checker.Check(rows).Where(i=>i.Kind==FixKind.OutlineOrder).ToArray();var model=new IssueReviewModel(issues);
        assert(issues.Count(i=>i.CanFix)==2 && issues.Count(i=>!i.CanFix)==1 &&
            model.Choices.Where(c=>c.Issue.CanFix).All(c=>c.Info.Section==IssueReviewSection.RequiredFormat),
            "Two independent decimal parents remain fixable while a prefix-dependent parent stays a manual warning");
        var first=model.Choices.First(c=>c.Issue.CanFix);first.Group.IsChecked=true;
        assert(model.All.SelectedCount==2 && !model.Choices.Single(c=>!c.Issue.CanFix).IsChecked,
            "Checking the order group selects both independent decimal corrections and excludes the manual parent cascade");
        var selected=model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue).ToArray();
        string story=string.Concat(rows.Select(p=>p.Text));string fixedText=AtomicTextBatch.ExpectedStory(BatchPlan.Create(selected),story,i=>story[..i.Start]);
        assert(fixedText.Contains("1.2. Nhánh sau",StringComparison.Ordinal) && fixedText.Contains("2.2. Nhánh sau",StringComparison.Ordinal) &&
            fixedText.EndsWith("4. Mục cần duyệt\r4.1. Nhánh phụ thuộc\r",StringComparison.Ordinal),
            "Group selection changes only the last digits in approved scopes and retains the manual parent plus dependent prefix");
        assert(model.Choices.Where(c=>c.Issue.CanFix).All(c=>c.Issue.Title!.Contains(".4 →",StringComparison.Ordinal) && c.Tooltip.Split('\n').Length==3),
            "Compact order rows show complete hierarchical labels while tooltip keeps the three requested parts");
        first.IsChecked=false;
        assert(model.All.SelectedCount==1 && first.Group.IsChecked is null,
            "An individual uncheck leaves the other parent selected and updates group tri-state correctly");
    }
}
