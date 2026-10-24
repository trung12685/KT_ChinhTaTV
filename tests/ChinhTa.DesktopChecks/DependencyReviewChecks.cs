using ChinhTa.Core;
using ChinhTa.Desktop;

public static class DependencyReviewChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var font=new Issue(0,7,"BÁO CÁO","BÁO CÁO","",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var unrelated=new Issue(100,2,"  "," ","",false);var manual=unrelated with {Start=120,CanFix=false};
        var line=new Issue(20,8,"Nội dung","Nội dung","",false,FixKind.ShapeLine,LineFix:new(null,20,40,80,LineRole.Subject),Prerequisites:[font]);
        var model=new IssueReviewModel([font,line,unrelated,manual]);
        IssueChoice Row(Issue i)=>model.Choices.Single(c=>c.Issue==i);
        Row(line).IsChecked=true;
        assert(Row(font).IsChecked && !Row(unrelated).IsChecked && !Row(manual).IsChecked && model.All.SelectedCount==2 && model.Summary.Contains("Tích kèm 1"),
            "Checking a Line visibly checks its prerequisite in another family and updates the shared totals without selecting unrelated/manual rows");
        Row(unrelated).IsChecked=true;Row(font).IsChecked=false;
        assert(!Row(line).IsChecked && Row(unrelated).IsChecked && model.All.SelectedCount==1 && model.Summary.Contains("phụ thuộc"),
            "Unchecking a prerequisite unchecks the dependent Line while preserving an independently selected unrelated repair");
        Row(line).IsChecked=true;Row(line).IsChecked=false;
        assert(Row(font).IsChecked && model.All.SelectedCount==2,"Unchecking a Line leaves its font available as an independently selected repair");
        model.All.IsChecked=false;Row(line).Group.IsChecked=true;
        assert(Row(line).IsChecked && Row(font).IsChecked && model.All.SelectedCount==2,"Checking a Line family includes its required repair across another family");
        Row(font).Group.IsChecked=false;
        assert(!Row(line).IsChecked && !Row(font).IsChecked && model.All.SelectedCount==0,"Clearing a prerequisite family cannot leave a hidden executable Line selected");
        model.All.IsChecked=true;
        assert(model.All.SelectedCount==3 && model.Choices.Where(c=>c.Issue.CanFix).All(c=>c.IsChecked) && !Row(manual).IsChecked,
            "Select all is dependency-complete and still excludes every manual warning");
        model.All.IsChecked=false;assert(model.All.SelectedCount==0,"Global clearing remains consistent with cross-family dependency events");
        var broken=new IssueReviewModel([line]);broken.Choices.Single().IsChecked=true;
        assert(broken.All.SelectedCount==0 && broken.Summary.Contains("quét lại"),"A missing current prerequisite cannot leave a checked Line or throw through the checkbox binding");
        broken=new([line,font with {CanFix=false}]);broken.Choices.Single(c=>c.Issue==line).IsChecked=true;
        assert(broken.All.SelectedCount==0,"A manual prerequisite never gains permission from selecting its dependent Line");
        var sibling=line with {Start=40,LineFix=line.LineFix! with {Role=LineRole.Agency}};
        var shared=new IssueReviewModel([line,sibling,font]);shared.All.IsChecked=true;
        assert(shared.All.SelectedCount==3,"Two Line roles share one selected prerequisite without double-counting");
        var fresh=new IssueReviewModel([unrelated]);
        assert(fresh.Choices.Count==1 && fresh.All.SelectedCount==0 && !fresh.Choices.Any(c=>c.Issue.LineFix is not null),
            "A fresh scan rebuilds independent groups and cannot retain disabled or selected old Line rows");
    }
}
