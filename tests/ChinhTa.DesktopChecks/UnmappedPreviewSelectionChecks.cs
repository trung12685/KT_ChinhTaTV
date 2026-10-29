using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using Rectangle = System.Windows.Shapes.Rectangle;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class UnmappedPreviewSelectionChecks
{
    private static FieldInfo Field(string name)=>typeof(MainWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)
        ??throw new InvalidOperationException("Missing main-window field: "+name);
    public static void Run(Action<bool,string> assert)
    {
        const string text="Tôi đi con đo.";
        var page=new FixedPage{Width=600,Height=800};
        page.Children.Add(new Glyphs
        {
            FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf")),
            FontRenderingEmSize=18,OriginX=50,OriginY=80,UnicodeString=text,Fill=Brushes.Black
        });
        var pageContent=new PageContent();((IAddChild)pageContent).AddChild(page);
        var document=new FixedDocument();document.Pages.Add(pageContent);
        var reference=new DocumentReference();reference.SetDocument(document);
        var sequence=new FixedDocumentSequence();sequence.References.Add(reference);
        var mapped=new Issue(11,2,"đo","đò","",false);
        var unmapped=new Issue(80,4,"khác","khác","",false,FixKind.Advisory,CanFix:false);
        var window=new MainWindow();
        try
        {
            var viewer=(DocumentViewer)window.FindName("Preview");viewer.Document=sequence;
            var anchors=(Dictionary<Issue,PreviewAnchor>)Field("anchors").GetValue(window)!;
            anchors[mapped]=new(1,text,text.IndexOf("đo",StringComparison.Ordinal),2);
            var show=typeof(MainWindow).GetMethod("ShowPreview",BindingFlags.Instance|BindingFlags.NonPublic)
                ??throw new InvalidOperationException("Missing preview-selection method.");
            var highlighter=(PreviewHighlighter)Field("highlighter").GetValue(window)!;
            var status=(TextBlock)window.FindName("Status");
            show.Invoke(window,[mapped]);
            assert(page.Children.OfType<Rectangle>().Any() && Equals(Field("displayed").GetValue(window),mapped),
                "A mapped selection displays an actual WPF highlight before the missing-anchor regression is exercised");
            string priorStatus=status.Text;
            Field("pending").SetValue(window,unmapped);show.Invoke(window,[unmapped]);
            assert(!page.Children.OfType<Rectangle>().Any() && Field("displayed").GetValue(window) is null &&
                Field("pending").GetValue(window) is null && ReferenceEquals(viewer.Document,sequence),
                "Selecting an unmapped issue removes the previous issue's highlight and pending/displayed identity while preserving the current preview document");
            assert(status.Text!=priorStatus && status.Text.Contains("Chưa ánh xạ chắc chắn",StringComparison.Ordinal) &&
                status.Text.Contains("không đánh dấu",StringComparison.Ordinal) && !status.Text.Contains("Trang 1",StringComparison.Ordinal),
                "The unmapped selection replaces stale status with an explicit no-location message without guessing its page");
            show.Invoke(window,[mapped]);
            assert(page.Children.OfType<Rectangle>().Any() && Equals(Field("displayed").GetValue(window),mapped),
                "Returning to the previously mapped issue can highlight it again after the unmapped selection cleared displayed identity");
            int marks=page.Children.OfType<Rectangle>().Count();string shownStatus=status.Text;
            show.Invoke(window,[mapped]);
            assert(page.Children.OfType<Rectangle>().Count()==marks && status.Text==shownStatus,
                "The existing same-selection hover guard preserves a stable highlight without adding duplicate marks");
            highlighter.Clear();
            assert(!page.Children.OfType<Rectangle>().Any() && anchors.Count==1 && anchors.ContainsKey(mapped) && !anchors.ContainsKey(unmapped),
                "Selection feedback cannot invent an anchor or mutate the source mapping to make an unmapped issue appear located");
        }
        finally
        {
            window.InvalidateReview();window.Close();
        }
    }
}
