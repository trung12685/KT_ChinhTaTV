using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ChinhTa.Desktop;

public static class PreviewLifecycleChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var previewFailure=new IOException("owned preview is locked");
        var wordFailure=new InvalidOperationException("owned Word cleanup failed");
        foreach(int mask in new[]{0,1,2,3})
        {
            var order=new List<string>();
            var failure=MainWindow.CloseOwnedResources(
                ()=>{order.Add("preview");if((mask&1)!=0)throw previewFailure;},
                ()=>{order.Add("Word");if((mask&2)!=0)throw wordFailure;});
            assert(order.SequenceEqual(new[]{"preview","Word"}) && (mask switch
            {
                0=>failure is null,
                1=>ReferenceEquals(failure,previewFailure),
                2=>ReferenceEquals(failure,wordFailure),
                _=>failure is AggregateException all && all.InnerExceptions.Count==2 &&
                    ReferenceEquals(all.InnerExceptions[0],previewFailure) && ReferenceEquals(all.InnerExceptions[1],wordFailure)
            }),"Main-window closing attempts owned Word cleanup after a preview failure and retains all errors for combination "+mask);
        }

        var window=new MainWindow();
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-preview-lifecycle-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);string path=Path.Combine(folder,"owned-preview.xps");
        var pathField=typeof(MainWindow).GetField("xpsPath",BindingFlags.Instance|BindingFlags.NonPublic)
            ??throw new InvalidOperationException("Missing owned-preview path.");
        try
        {
            var viewer=(DocumentViewer)window.FindName("Preview");
            var coverage=(TextBlock)window.FindName("CoverageInfo");
            viewer.Document=new FixedDocumentSequence();coverage.Visibility=Visibility.Visible;
            assert(window.InvalidateReview() is null && viewer.Document is null && coverage.Visibility==Visibility.Collapsed,
                "Invalidating a failed scan detaches the actual WPF document and hides stale coverage even before a new preview exists");

            File.WriteAllText(path,"owned synthetic preview");pathField.SetValue(window,path);
            viewer.Document=new FixedDocumentSequence();coverage.Visibility=Visibility.Visible;
            using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                var failure=window.InvalidateReview();
                assert(failure is IOException && viewer.Document is null && coverage.Visibility==Visibility.Collapsed &&
                    (string?)pathField.GetValue(window)==path && File.Exists(path),
                    "A real Windows file-sharing denial still detaches the old WPF preview and retains its undeleted path for recovery");
            }
            assert(window.InvalidateReview() is null && !File.Exists(path) && pathField.GetValue(window) is null,
                "A later cleanup retries the retained owned preview after its sharing lock is released");
        }
        finally
        {
            window.InvalidateReview();window.Close();Directory.Delete(folder,true);
        }
    }
}
