using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public partial class IssueReviewPanel : UserControl
{
    private IssueReviewModel model=null!;
    public bool SelectOnHover {get;set;}=true;
    public static readonly DependencyProperty AutomaticSelectionVisibilityProperty=DependencyProperty.Register(nameof(AutomaticSelectionVisibility),typeof(Visibility),typeof(IssueReviewPanel),new PropertyMetadata(Visibility.Visible,(d,_)=>((IssueReviewPanel)d).RefreshSummary()));
    public Visibility AutomaticSelectionVisibility {get=>(Visibility)GetValue(AutomaticSelectionVisibilityProperty);set=>SetValue(AutomaticSelectionVisibilityProperty,value);}
    public void SelectIssue(Issue issue)
    {var choice=model.Choices.FirstOrDefault(c=>c.Issue==issue);if(choice is not null){IssueList.SelectedItem=choice;IssueList.ScrollIntoView(choice);}}

    public IReadOnlyList<IssueChoice> Choices=>model.Choices;
    public Issue? SelectedIssue=>(IssueList.SelectedItem as IssueChoice)?.Issue;
    public event Action<Issue>? ActiveIssueChanged;
    public IssueReviewPanel(){InitializeComponent();LoadIssues([]);}
    public void LoadIssues(IEnumerable<Issue> issues,IReadOnlyDictionary<Issue,PreviewAnchor>? anchors=null)
    {
        if(model is not null)model.All.PropertyChanged-=SelectionChanged;
        IssueList.SelectedItem=null;IssueList.ItemsSource=null;
        model=new(issues,anchors);DataContext=model;
        var view=new ListCollectionView(model.Choices.ToArray());
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(IssueChoice.Section)));
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(IssueChoice.Group)));
        IssueList.ItemsSource=view;
        IssueList.SelectedItem=null;
        model.All.PropertyChanged+=SelectionChanged;
        RefreshSummary();
        Empty.Visibility=Choices.Count==0?Visibility.Visible:Visibility.Collapsed;
        IssueList.Visibility=Choices.Count==0?Visibility.Collapsed:Visibility.Visible;
        DetailText.Text="Chọn lỗi để xem mô tả, gợi ý và căn cứ.";
    }
    private void RefreshSummary()
    {if(model is not null && Summary is not null)Summary.Text=AutomaticSelectionVisibility==Visibility.Visible?model.Summary:$"{Choices.Count} cảnh báo — chọn mục để tự sửa";}
    private void SelectionChanged(object? sender,PropertyChangedEventArgs args)
    {if(args.PropertyName==nameof(IssueChoiceGroup.SelectedCount))RefreshSummary();}
    private void IssueSelected(object sender,SelectionChangedEventArgs args)
    {
        if(IssueList.SelectedItem is not IssueChoice choice)return;
        DetailText.Text=choice.Detail;ActiveIssueChanged?.Invoke(choice.Issue);
    }
    private void IssueHover(object sender,MouseEventArgs args)
    {
        if(!SelectOnHover)return;
        if(sender is not FrameworkElement {DataContext:IssueChoice choice})return;
        if(IssueList.SelectedItem!=choice)IssueList.SelectedItem=choice;
        else ActiveIssueChanged?.Invoke(choice.Issue);
    }
}
