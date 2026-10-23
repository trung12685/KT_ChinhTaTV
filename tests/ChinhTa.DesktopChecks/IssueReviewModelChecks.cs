using ChinhTa.Core;
using ChinhTa.Desktop;

public static class IssueReviewModelChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var a=new Issue(40,2,"  "," ","long detail",false);
        var b=a with {Start=10};var manual=a with {Start=20,CanFix=false};
        var line=new Issue(90,3,"ABC","ABC","Line detail",false,FixKind.ShapeLine,Citation:"NĐ30",Title:"Thiếu đường kẻ dưới cơ quan ban hành",LineFix:new(null,100,100,90,LineRole.Agency));
        var model=new IssueReviewModel([a,manual,line,b]);
        assert(model.Choices.Count==4 && model.Sections.Count==3 && model.Choices[0].Issue==line &&
            model.Choices[1].Issue==b && model.Choices[2].Issue==a && model.Choices[3].Issue==manual,
            "Review items sort by section, family then position while retaining every original Issue payload");
        var group=model.Groups.Single(g=>g.Items.Count==2);
        assert(group.SelectedCount==0 && group.IsChecked==false && model.All.SelectedCount==0,"Fresh groups and global selection begin unchecked");
        group.Items[0].IsChecked=true;
        assert(group.IsChecked is null && model.All.IsChecked is null && group.SelectedCount==1 && model.All.SelectedCount==1,
            "An individual check produces accurate partial group and all states");
        group.IsChecked=true;
        assert(group.IsChecked==true && model.All.SelectedCount==2 && !model.Choices.Single(c=>c.Issue==line).IsChecked,
            "Selecting one family checks its eligible members without touching another section");
        var manualGroup=model.Groups.Single(g=>g.Items.Any(c=>c.Issue==manual));manualGroup.IsChecked=true;
        assert(!manualGroup.CanSelect && manualGroup.SelectedCount==0 && !manualGroup.Items[0].IsChecked,"Manual-only groups cannot be selected programmatically or through headers");
        model.All.IsChecked=true;
        assert(model.All.IsChecked==true && model.All.SelectedCount==3 && model.Choices.Count(c=>c.IsChecked)==3 &&
            model.Choices.Where(c=>!c.Issue.CanFix).All(c=>!c.IsChecked),"Select all covers all eligible groups and excludes every manual warning");
        group.IsChecked=false;
        assert(model.All.SelectedCount==1 && model.All.IsChecked is null && model.Summary.Contains("1/3") && model.Summary.Contains("1 thủ công"),
            "Clearing a family updates global counters without clearing another checked family");
        model.Sections.Single(s=>s.Items.Any(c=>c.Issue==line)).IsChecked=false;
        assert(model.All.SelectedCount==0 && model.All.IsChecked==false,"Section selection is shared with the same underlying row choices");
        model.All.IsChecked=true;model.All.IsChecked=false;
        assert(model.Choices.All(c=>!c.IsChecked),"Global clearing reaches every selected row");
        var anchor=new PreviewAnchor(2,"Hôm nay  là ngày thử.",7,2);
        var anchored=new IssueChoice(a,anchor);
        assert(anchored.Location=="T.2" && anchored.Detail.Contains("long detail") && anchored.Detail.Contains("Hôm nay") && anchored.SelectionLabel.Contains(anchored.Label),
            "Compact rows retain page, context, details and an accessible selection name outside the visible label");
        assert(new IssueChoice(line).Detail.Contains("NĐ30") && new IssueChoice(a,new(1,"abc",99,2)).Context==a.Original.Trim(),
            "Citation details are retained and invalid context bounds fall back safely");
        assert(anchored.Tooltip==IssueTooltipCatalog.Describe(a).Text && !anchored.Tooltip.Contains("long detail") &&
            !anchored.Tooltip.Contains("Hôm nay") && anchored.Detail.Contains("long detail"),
            "Compact tooltip uses the summary while the full selected detail retains diagnostics and context");
        var empty=new IssueReviewModel([]);
        assert(!empty.All.CanSelect && empty.All.IsChecked==false && empty.Groups.Count==0,"An empty scan has no selectable phantom group");
        var many=new IssueReviewModel(Enumerable.Range(0,1200).Select(i=>a with {Start=i*3}));
        many.All.IsChecked=true;
        assert(many.All.SelectedCount==1200 && many.Groups.Single().SelectedCount==1200,"Large group selection maintains exact shared counters without relying on visible row containers");
        many.Choices[600].IsChecked=false;
        assert(many.All.SelectedCount==1199 && many.All.IsChecked is null && many.Groups.Single().IsChecked is null,"An offscreen row updates partial selection correctly");
        var fresh=new IssueReviewModel([b]);fresh.All.IsChecked=true;model.All.IsChecked=true;
        assert(fresh.All.SelectedCount==1 && fresh.Choices.Single().Issue==b && !ReferenceEquals(fresh.Choices[0],model.Choices.First(c=>c.Issue==b)),
            "A new scan has independent choices and never reuses stale checked membership");
    }
}
