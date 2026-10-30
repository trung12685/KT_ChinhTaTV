using System.Windows;
using System.Windows.Controls;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class SpellingDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var page=new System.Windows.Documents.FixedPage{Width=600,Height=800};var hit=new PreviewWordHit(new(1,"Sai nội dung.",4,3),"nội",new(50,50,50,20),page);
        var owner=new Window{Content=page};int writes=0;var draft=new InlineWordDraft(ManualModeChecks.Target(),4,"nội");
        var editor=new InlineWordEditor(owner,hit,draft,value=>{writes++;return new(true,true,null);},()=>{},["mới","khác","mới"]);
        try
        {
            owner.Show();editor.Open();
            assert(editor.SuggestionButtons.Count==2 && writes==0 && !editor.Dirty,"Suggestions appear once each without selecting or writing a replacement");
            editor.SuggestionButtons[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            assert(editor.Editor.Text=="mới" && editor.Dirty && writes==0 && editor.ApplyButton.IsEnabled,"Choosing a spelling proposal fills only the draft; explicit Ghi is still required");
            editor.Apply();editor.Apply();
            assert(writes==1 && !editor.IsOpen,"A human-confirmed spelling proposal uses the same single-commit manual editor");
        }
        finally{editor.ForceClose();owner.Close();}
        var issue=new Issue(0,3,"sai","sai","Cần duyệt",false,FixKind.SpellingReview,CanFix:false,SpellingSuggestions:["đúng"]);
        var choice=new IssueChoice(issue);choice.IsChecked=true;
        assert(!choice.IsChecked && IssueReviewCatalog.Describe(issue).Section==IssueReviewSection.ManualWarning,"The spelling warning cannot become an automatically selected repair");
        var suite=OfficeFocusedCheck.Cases.Single(c=>c.Id=="vietnamese-spelling");
        assert(OfficeFocusedCheck.Scenario(suite.Id).Method.DeclaringType==typeof(OfficeSpellingCheck) && WordCheckCommand.TryParse(["--word-check",suite.Id],out var selected) && selected.Single()==suite,"The new real Word scenario is independently callable without the user's dictionary ZIP");
    }
}
