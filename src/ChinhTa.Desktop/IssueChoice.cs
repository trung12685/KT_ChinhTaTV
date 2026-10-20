using System.ComponentModel;
using ChinhTa.Core;
namespace ChinhTa.Desktop;
// Checked membership is independent of the active item used by hover/highlight.
public sealed class IssueChoice(Issue issue) : INotifyPropertyChanged
{
    public Issue Issue { get; } = issue;
    private bool isChecked;
    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if (isChecked == value) return;
            isChecked = value;
            PropertyChanged?.Invoke(this, new(nameof(IsChecked)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
