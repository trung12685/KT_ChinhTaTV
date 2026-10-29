using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class SaveDestinationReviewChecks
{
    private static FieldInfo Field(string name)=>typeof(MainWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!;
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-save-review-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            foreach(string outcome in new[]{"rejected-source","rejected-private","rejected-extension","success","save-failure","late-directory"})
            {
                var window=new MainWindow();var session=(WordSession)Field("session").GetValue(window)!;
                string owned=Path.GetDirectoryName(session.RecoveryPath)!;
                try
                {
                    string source=Path.Combine(folder,Guid.NewGuid()+".source.docx"),destination=Path.Combine(folder,Guid.NewGuid()+".docx");
                    File.WriteAllText(source,"immutable source");File.WriteAllText(session.RecoveryPath,"before Save");
                    SaveDestinationPreflightChecks.Property(session,"Source",source);SaveDestinationPreflightChecks.Property(session,"Changed",true);
                    var primary=new InvalidOperationException("managed Save failed: thư mục bản làm việc tạm");
                    var document=new SaveDestinationPreflightChecks.Document(session.RecoveryPath,outcome=="save-failure"?primary:null,
                        outcome=="late-directory"?()=>Directory.CreateDirectory(destination):null);
                    SaveDestinationPreflightChecks.Field(session,"document",document);SaveDestinationPreflightChecks.Field(session,"app",new SaveDestinationPreflightChecks.Application());
                    var issue=new Issue(17,2,"  "," ","review sentinel",false);var anchor=new PreviewAnchor(1,"Nội dung  báo cáo",8,2);
                    var review=(IssueReviewPanel)window.FindName("Review");review.LoadIssues([issue]);review.Choices[0].IsChecked=true;
                    var anchors=(Dictionary<Issue,PreviewAnchor>)Field("anchors").GetValue(window)!;anchors[issue]=anchor;
                    Field("pending").SetValue(window,issue);Field("displayed").SetValue(window,issue);
                    var coverage=DocumentCoverageBuilder.Build(new(DocumentProfile.Personal,[new(0,"Nội dung  báo cáo")],[],true,true));
                    Field("lastCoverage").SetValue(window,coverage);var coverageInfo=(TextBlock)window.FindName("CoverageInfo");coverageInfo.Visibility=Visibility.Visible;
                    var page=new FixedPage{Width=600,Height=800};var content=new PageContent();((IAddChild)content).AddChild(page);
                    var fixedDocument=new FixedDocument();fixedDocument.Pages.Add(content);var reference=new DocumentReference();reference.SetDocument(fixedDocument);
                    var preview=new FixedDocumentSequence();preview.References.Add(reference);var viewer=(DocumentViewer)window.FindName("Preview");viewer.Document=preview;
                    string target=outcome switch{"rejected-source"=>source,"rejected-private"=>Path.Combine(owned,"new.docx"),
                        "rejected-extension"=>Path.Combine(folder,"wrong.txt"),_=>destination};
                    int executions=0,rejections=0;var failures=new List<string>();
                    window.SaveCopyTo(target,action=>{executions++;window.Run(action,failures.Add);},message=>
                    {rejections++;assert(message.Contains("Danh sách lỗi và bản xem trước được giữ",StringComparison.Ordinal),
                        "Destination-only feedback asks for a different target without claiming the current scan is stale");});
                    bool rejected=outcome.StartsWith("rejected-",StringComparison.Ordinal);
                    if(rejected || outcome=="success")
                        assert(review.Choices.Count==1 && review.Choices[0].IsChecked && anchors.Count==1 && anchors[issue]==anchor &&
                            ReferenceEquals(viewer.Document,preview) && ReferenceEquals(Field("lastCoverage").GetValue(window),coverage) &&
                            coverageInfo.Visibility==Visibility.Visible && Equals(Field("pending").GetValue(window),issue) && Equals(Field("displayed").GetValue(window),issue),
                            "Preflight rejection and successful Save preserve checked review choices, mapping, pending/displayed selection, coverage and the exact preview document");
                    if(rejected)
                        assert(executions==0 && rejections==1 && failures.Count==0 && document.Saves==0 && session.Changed &&
                            SaveDestinationPreflightChecks.Field(session,"snapshotCurrent") is true && File.ReadAllText(session.RecoveryPath)=="before Save" &&
                            !((TextBlock)window.FindName("Status")).Text.Contains("Quét lại",StringComparison.Ordinal),
                            "Rejected targets never enter the general operation runner, Word Save, checkpoint or stale-review guidance");
                    else if(outcome=="success")
                        assert(executions==1 && rejections==0 && failures.Count==0 && document.Saves==1 && !session.Changed &&
                            File.ReadAllText(destination)=="complete current checkpoint",
                            "A valid UI Save still runs once and publishes complete bytes without clearing a valid review");
                    else
                        assert(executions==1 && rejections==0 && failures.Count==1 && document.Saves==1 && session.Changed &&
                            review.Choices.Count==0 && anchors.Count==0 && viewer.Document is null && Field("lastCoverage").GetValue(window) is null &&
                            coverageInfo.Visibility==Visibility.Collapsed && ((TextBlock)window.FindName("Status")).Text.Contains("Quét lại",StringComparison.Ordinal),
                            "A Save failure or destination change discovered after preflight follows the actual general failure path and invalidates review instead of masquerading as harmless destination validation");
                    assert(File.ReadAllText(source)=="immutable source","UI save feedback and destination validation preserve the source bytes");
                }
                finally
                {
                    SaveDestinationPreflightChecks.Property(session,"Changed",false);window.InvalidateReview();window.Close();session.Dispose();
                    if(Directory.Exists(owned))Directory.Delete(owned,true);
                }
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
