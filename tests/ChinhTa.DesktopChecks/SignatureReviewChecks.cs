using ChinhTa.Core;
using ChinhTa.Desktop;

public static class SignatureReviewChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue Format(int start,string text,ComponentRole role)=>new(start,text.Length,text,text,"",false,FixKind.SignatureFormat,
            Citation:SignatureRules.Citation,CharacterFormat:new(13,false,true,role,PreserveSize:true,RepairInvalidSizes:true));
        var authority=Format(10,"TRƯỞNG PHÒNG",ComponentRole.SignatureAuthority);
        var name=Format(100,"Đại tá Nguyễn Văn An",ComponentRole.SignatureName);
        var space=new Issue(200,2,"  "," ","",false);
        var manual=new Issue(300,4,"test","test","",false,FixKind.Advisory,CanFix:false);
        var model=new IssueReviewModel([authority,space,manual,name]);
        var group=model.Choices.Single(c=>c.Issue==authority).Group;group.IsChecked=true;
        assert(group.SelectedCount==2 && model.All.SelectedCount==2 && model.Choices.Where(c=>c.Issue.Kind==FixKind.SignatureFormat).All(c=>
            c.Info.Section==IssueReviewSection.RequiredFormat && c.Tooltip.Contains("II.7, điểm đ") && c.Tooltip.Split('\n').Length==3),
            "Authority and signer typography share one selectable compact required-format group");
        model.All.IsChecked=true;
        assert(model.All.SelectedCount==3 && !model.Choices.Single(c=>c.Issue==manual).IsChecked &&
            BatchPlan.Create(model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue)).Count==3,
            "Select all combines signature typography and text repairs without selecting manual notices");
        model.Choices.Single(c=>c.Issue==name).IsChecked=false;
        assert(group.IsChecked is null && group.SelectedCount==1 && model.All.SelectedCount==2,
            "A signer row can be excluded while authority and another selected family remain selected");
        var refreshed=new IssueReviewModel([space,manual]);
        assert(!refreshed.Choices.Any(c=>c.Info.FamilyKey=="signature-font") && refreshed.All.SelectableCount==1,
            "Refreshing repaired signatures removes their rows completely without stale disabled checkboxes");
        foreach(var role in new[]{ComponentRole.SignatureAuthority,ComponentRole.SignatureName})
        {
            var style=new CharacterFormat(13,false,true,role,PreserveSize:true,RepairInvalidSizes:true);
            var a=new SelectiveSizeWriterChecks.Range(10,11,"A",new(12));
            var b=new SelectiveSizeWriterChecks.Range(11,12,"B",new(14));
            VisibleFormatReader.RepairSizes(new SelectiveSizeWriterChecks.Range(10,12,"AB",new(9999999),[a,b]),"AB",style);
            assert(a.Font.Size==13 && a.Font.Writes==1 && b.Font.Size==14 && b.Font.Writes==0,
                "The Word selective-size writer supports signing roles and retains each already legal 14pt glyph");
        }
    }
}
