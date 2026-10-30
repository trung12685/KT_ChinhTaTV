using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
public partial class MainWindow
{
    private InlineWordEditor? inlineEditor;
    private bool inlineCommitInProgress,revertingSelection;
    private bool TryCloseInline()=>inlineEditor is null || inlineEditor.TryClose(()=>MessageBox.Show(this,
        "Chữ đang nhập chưa được ghi. Bỏ bản nháp để thực hiện thao tác khác?","Sửa tại chữ",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes);
    private void PreviewWordClick(object sender,MouseButtonEventArgs e)
    {
        if(!ManualMode || !session.IsOpen || Preview.Document is not FixedDocumentSequence sequence || xpsPath is null || e.ChangedButton!=MouseButton.Left)return;
        string previewPath=xpsPath;
        if(inlineEditor is not null){inlineEditor.Focus();return;}
        var hit=e.OriginalSource is DependencyObject source?highlighter.HitWord(sequence,source,e.GetPosition):null;
        if(hit is null)return;
        e.Handled=true;hover.Stop();pending=displayed=null;highlighter.Clear();
        InlineWordDraft? draft=null;
        Run(()=>
        {
            try{draft=session.PrepareInlineWord(hit.Anchor,previewPath);}
            catch(InlineEditUnavailableException unavailable){Status.Text=unavailable.Message;}
        });
        if(draft is null)return;
        var spelling=Review.Choices.Select(c=>c.Issue).FirstOrDefault(i=>i.Kind==FixKind.SpellingReview && i.Start==draft.Target.Start+draft.Offset && i.Original==draft.Original);
        OpenInlineEditor(hit,draft,spelling?.SpellingSuggestions);
    }
    private void OpenInlineEditor(PreviewWordHit hit,InlineWordDraft current,IReadOnlyList<string>? suggestions=null)
    {
        inlineEditor=new InlineWordEditor(this,hit,current,replacement=>
        {
            bool applied=false,reviewed=false;string? failure=null;
            inlineCommitInProgress=true;
            try
            {
                Run(()=>EditThenScan.Run(()=>{session.ApplyInlineWord(current,replacement);applied=true;},()=>
                {Scan();reviewed=true;Status.Text="Đã ghi đúng chữ bạn sửa; Hoàn tác một lần để trở lại. "+Status.Text;}),message=>failure=message);
            }
            finally{inlineCommitInProgress=false;}
            return new(applied,reviewed,failure);
        },()=>inlineEditor=null,suggestions);
        inlineEditor.Open();
    }
}
