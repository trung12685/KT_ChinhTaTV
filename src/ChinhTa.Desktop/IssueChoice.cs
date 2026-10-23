using System.ComponentModel;
using ChinhTa.Core;
namespace ChinhTa.Desktop;
// Checked membership is independent of the active item used by hover/highlight.
public sealed class IssueChoice(Issue issue,PreviewAnchor? anchor=null) : INotifyPropertyChanged
{
    public Issue Issue { get; } = issue;
    public IssueReviewInfo Info { get; }=IssueReviewCatalog.Describe(issue);
    public IssueChoiceGroup Section { get; internal set; }=null!;
    public IssueChoiceGroup Group { get; internal set; }=null!;
    public string Label=>Info.Label;
    public string Location=>anchor is null?"":"T."+anchor.Page;
    public string SelectionLabel=>"Tích lỗi: "+Label;
    public string Context { get; }=anchor is not null && anchor.Length>=0 && anchor.Offset>=0 && anchor.Offset<=anchor.PageText.Length
        ?IssueReviewCatalog.Brief(anchor.PageText.Substring(Math.Max(0,anchor.Offset-35),
            Math.Min(anchor.PageText.Length-Math.Max(0,anchor.Offset-35),Math.Min(anchor.Length,100)+70)),170)
        :IssueReviewCatalog.Brief(issue.Original,170);
    public string Detail=>Label+"\n"+(Location.Length>0?"Trang "+anchor!.Page+"\n":"")+
        (Context.Length>0?"Ngữ cảnh: "+Context+"\n\n":"")+Issue.Message+"\n\nGợi ý: "+Issue.Suggestion+
        (string.IsNullOrWhiteSpace(Issue.Citation)?"":"\n\nCăn cứ: "+Issue.Citation);
    private bool isChecked;
    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if (value && !Issue.CanFix) return;
            if (isChecked == value) return;
            isChecked = value;
            PropertyChanged?.Invoke(this, new(nameof(IsChecked)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

