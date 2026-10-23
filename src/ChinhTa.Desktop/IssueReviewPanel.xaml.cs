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
        Summary.Text=model.Summary;
        Empty.Visibility=Choices.Count==0?Visibility.Visible:Visibility.Collapsed;
        IssueList.Visibility=Choices.Count==0?Visibility.Collapsed:Visibility.Visible;
        DetailText.Text="Chọn lỗi để xem mô tả, gợi ý và căn cứ.";
    }
    private void SelectionChanged(object? sender,PropertyChangedEventArgs args)
    {if(args.PropertyName==nameof(IssueChoiceGroup.SelectedCount))Summary.Text=model.Summary;}
    private void IssueSelected(object sender,SelectionChangedEventArgs args)
    {
        if(IssueList.SelectedItem is not IssueChoice choice)return;
        DetailText.Text=choice.Detail;ActiveIssueChanged?.Invoke(choice.Issue);
    }
    private void IssueHover(object sender,MouseEventArgs args)
    {
        if(sender is not FrameworkElement {DataContext:IssueChoice choice})return;
        if(IssueList.SelectedItem!=choice)IssueList.SelectedItem=choice;
        else ActiveIssueChanged?.Invoke(choice.Issue);
    }
}
