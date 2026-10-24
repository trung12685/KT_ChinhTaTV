using System.ComponentModel;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed class IssueChoiceGroup : INotifyPropertyChanged
{
    private int selected;
    private bool expanded=true;
    public bool IsExpanded
    {
        get=>expanded;
        set {if(expanded==value)return;expanded=value;PropertyChanged?.Invoke(this,new(nameof(IsExpanded)));}
    }
    public string Title { get; }
    public string Detail { get; }
    public IReadOnlyList<IssueChoice> Items { get; }
    public int SelectableCount { get; }
    public int SelectedCount=>selected;
    public bool CanSelect=>SelectableCount>0;
    public string CountText=>CanSelect?$"{selected}/{SelectableCount}":Items.Count.ToString();
    public bool? IsChecked
    {
        get=>selected==0?false:selected==SelectableCount?true:null;
        set {foreach(var item in Items)item.IsChecked=value==true;}
    }
    public IssueChoiceGroup(string title,string detail,IReadOnlyList<IssueChoice> items)
    {
        Title=title;Detail=detail;Items=items;
        SelectableCount=items.Count(i=>i.Issue.CanFix);selected=items.Count(i=>i.IsChecked);
        foreach(var item in items)item.PropertyChanged+=ItemChanged;
    }
    private void ItemChanged(object? sender,PropertyChangedEventArgs args)
    {
        if(args.PropertyName!=nameof(IssueChoice.IsChecked) || sender is not IssueChoice item)return;
        selected+=item.IsChecked?1:-1;
        PropertyChanged?.Invoke(this,new(nameof(IsChecked)));
        PropertyChanged?.Invoke(this,new(nameof(CountText)));
        PropertyChanged?.Invoke(this,new(nameof(SelectedCount)));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    internal void RefreshSummary()=>PropertyChanged?.Invoke(this,new(nameof(SelectedCount)));
}

public sealed class IssueReviewModel
{
    private bool updatingDependencies;
    private string? selectionNote;
    private readonly IReadOnlyList<IssueChoice> dependentChoices;
    public IReadOnlyList<IssueChoice> Choices { get; }
    public IReadOnlyList<IssueChoiceGroup> Sections { get; }
    public IReadOnlyList<IssueChoiceGroup> Groups { get; }
    public IssueChoiceGroup All { get; }
    public string Summary=>$"Đã tích {All.SelectedCount}/{All.SelectableCount} · {Choices.Count-All.SelectableCount} thủ công"+
        (selectionNote is null?"":" · "+selectionNote);
    public IssueReviewModel(IEnumerable<Issue> issues,IReadOnlyDictionary<Issue,PreviewAnchor>? anchors=null)
    {
        Choices=issues.Select(i=>new IssueChoice(i,anchors is not null && anchors.TryGetValue(i,out var a)?a:null))
            .OrderBy(c=>c.Info.Section).ThenBy(c=>c.Info.Order).ThenBy(c=>c.Info.FamilyKey,StringComparer.Ordinal)
            .ThenBy(c=>c.Issue.Start).ToArray();
        var sections=new List<IssueChoiceGroup>();var groups=new List<IssueChoiceGroup>();
        foreach(var section in Choices.GroupBy(c=>c.Info.Section))
        {
            var parent=new IssueChoiceGroup(IssueReviewCatalog.SectionTitle(section.Key),IssueReviewCatalog.SectionDetail(section.Key),section.ToArray());
            sections.Add(parent);
            foreach(var family in section.GroupBy(c=>c.Info.FamilyKey))
            {
                var group=new IssueChoiceGroup(family.First().Info.Family,"Tích/bỏ tích các mục sửa được trong nhóm này.",family.ToArray());
                groups.Add(group);
                foreach(var choice in family){choice.Section=parent;choice.Group=group;}
            }
        }
        Sections=sections;Groups=groups;
        All=new("Tất cả","Tích/bỏ tích tất cả mục sửa được, kể cả nhóm đang thu gọn. Cảnh báo thủ công được giữ nguyên.",Choices);
        dependentChoices=Choices.Where(c=>c.Issue.Prerequisites is {Count:>0}).ToArray();
        foreach(var choice in Choices)choice.PropertyChanged+=DependenciesChanged;
    }
    private void DependenciesChanged(object? sender,PropertyChangedEventArgs args)
    {
        if(updatingDependencies || args.PropertyName!=nameof(IssueChoice.IsChecked) || sender is not IssueChoice choice)return;
        updatingDependencies=true;
        try
        {
            selectionNote=null;
            if(choice.IsChecked && choice.Issue.Prerequisites is {Count:>0})
            {
                var required=RepairDependencies.Expand([choice.Issue],Choices.Select(c=>c.Issue).ToArray());int added=0;
                foreach(var other in Choices.Where(c=>!c.IsChecked && required.Any(i=>RepairDependencies.Same(i,c.Issue))))
                {other.IsChecked=true;added++;}
                if(added>0)selectionNote=$"Tích kèm {added} mục cần sửa trước đường kẻ";
            }
            else if(!choice.IsChecked)
            {
                foreach(var other in dependentChoices.Where(c=>c.IsChecked && c.Issue.Prerequisites!.Any(i=>RepairDependencies.Same(i,choice.Issue))))
                {other.IsChecked=false;selectionNote="Đã bỏ chọn đường kẻ phụ thuộc";}
            }
        }
        catch(InvalidOperationException)
        {choice.IsChecked=false;selectionNote="Mục liên quan đã đổi; hãy quét lại";}
        finally{updatingDependencies=false;All.RefreshSummary();}
    }
}
