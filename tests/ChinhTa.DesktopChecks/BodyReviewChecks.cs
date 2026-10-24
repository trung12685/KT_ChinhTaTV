using ChinhTa.Core;
using ChinhTa.Desktop;

public static class BodyReviewChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string text="Nội dung công tác đã được kiểm tra đầy đủ.";
        Issue Body(int start)=>new(start,text.Length,text,text,"",false,FixKind.BodyFormat,Citation:BodyFormatRules.Citation,
            Title:"Phông/cỡ chữ nội dung chưa đúng",CharacterFormat:new(13,false,false,ComponentRole.Body,true,true));
        var a=Body(100);var b=Body(200);var space=new Issue(500,2,"  "," ","",true);
        var manual=new Issue(600,4,"test","test","",false,FixKind.Advisory,CanFix:false);
        var model=new IssueReviewModel([a,space,manual,b]);
        var first=model.Choices.Single(c=>c.Issue==a);first.Group.IsChecked=true;
        assert(model.All.SelectedCount==2 && first.Group.SelectedCount==2 && model.Choices.Where(c=>c.Issue.Kind==FixKind.BodyFormat).All(c=>
            c.Info.Section==IssueReviewSection.RequiredFormat && c.Tooltip.Split('\n').Length==3 && c.Tooltip.Contains("II.6, điểm e")),
            "Body formatting groups compact required rows and selects both without selecting a spelling suggestion or manual notice");
        model.All.IsChecked=true;
        assert(model.All.SelectedCount==3 && !model.Choices.Single(c=>c.Issue==manual).IsChecked &&
            BatchPlan.Create(model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue)).Count==3,
            "Select all combines body format and an independent text group while keeping manual warnings unselected");
        first.IsChecked=false;
        assert(first.Group.IsChecked is null && model.All.SelectedCount==2,
            "Unchecking one body row keeps the other body and selected text group with correct tri-state counts");
        var refreshed=new IssueReviewModel([space,manual]);
        assert(!refreshed.Choices.Any(c=>c.Info.FamilyKey=="body-font") && refreshed.All.SelectableCount==1,
            "A fresh result list drops repaired body rows entirely instead of retaining disabled stale checkboxes");
    }
}
