using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class ManualModeChecks
{
    private const string Text="Sai nội dung.";
    internal static ManualTextTarget Target()
    {
        XNamespace p="http://schemas.microsoft.com/office/2006/xmlPackage",w="http://schemas.openxmlformats.org/wordprocessingml/2006/main",r="http://schemas.openxmlformats.org/package/2006/relationships";
        XElement Part(string name,string type,XElement root)=>new(p+"part",new XAttribute(p+"name","/"+name),new XAttribute(p+"contentType",type),new XElement(p+"xmlData",root));
        string flat=new XElement(p+"package",Part("_rels/.rels","application/vnd.openxmlformats-package.relationships+xml",new XElement(r+"Relationships",new XElement(r+"Relationship",new XAttribute("Id","main"),new XAttribute("Type","http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),new XAttribute("Target","word/document.xml")))),
            Part("word/document.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",new XElement(w+"document",new XElement(w+"body",new XElement(w+"p",new XElement(w+"r",new XElement(w+"t",Text))),new XElement(w+"sectPr"))))).ToString(SaveOptions.DisableFormatting);
        return ManualTextTarget.Prepare(flat,[new(0,Text.Length+1,Text+"\r",1,false,false)],0,3,"Sai");
    }
    private static FixedDocumentSequence Page(out FixedPage page)
    {
        page=new FixedPage{Width=794,Height=1123};page.Children.Add(new Glyphs{FontUri=new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"times.ttf")),
            FontRenderingEmSize=18,UnicodeString=Text,Fill=Brushes.Black,OriginX=100,OriginY=120});
        var content=new PageContent();((IAddChild)content).AddChild(page);var doc=new FixedDocument();doc.Pages.Add(content);var reference=new DocumentReference();reference.SetDocument(doc);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);return sequence;
    }
    public static void Run(Action<bool,string> assert)
    {
        var window=new MainWindow();
        try
        {
            var modes=(ComboBox)window.FindName("EditModePicker");var review=(IssueReviewPanel)window.FindName("Review");
            var automatic=(WrapPanel)window.FindName("AutomaticButtons");var manual=(Button)window.FindName("ManualEditButton");
            assert(modes.SelectedIndex==0 && window.ManualMode && automatic.Visibility==Visibility.Collapsed && manual.Visibility==Visibility.Visible,
                "A fresh application starts in manual mode and exposes the user-authored action instead of machine repairs");
            assert(!review.SelectOnHover && review.AutomaticSelectionVisibility==Visibility.Collapsed,"Manual browsing cannot change the active issue merely by hovering or expose automatic checkboxes");
            modes.SelectedIndex=1;
            assert(!window.ManualMode && automatic.Visibility==Visibility.Visible && manual.Visibility==Visibility.Collapsed && review.SelectOnHover && review.AutomaticSelectionVisibility==Visibility.Visible,
                "Machine repair buttons and selections require an explicit mode switch");
            modes.SelectedIndex=0;
            assert(window.ManualMode && automatic.Visibility==Visibility.Collapsed,"Switching back restores the primary manual flow without opening Word");
            var issue=new Issue(0,3,"Sai","Đúng","Tự xem xét.",false,CanFix:false);review.LoadIssues([issue]);review.SelectIssue(issue);
            assert(review.SelectedIssue==issue,"A manual warning remains selectable for human review even though automatic CanFix is false");
            assert(((TextBlock)review.FindName("Summary")).Text.Contains("tự sửa",StringComparison.Ordinal),"Manual summaries describe human review rather than checked automatic repairs");
            review.Measure(new Size(470,840));review.Arrange(new Rect(0,0,470,840));review.UpdateLayout();
            assert(((CheckBox)review.FindName("AllCheck")).Visibility==Visibility.Collapsed,"The automatic Select All checkbox is actually hidden in manual mode");
        }
        finally{window.Close();}
        var target=Target();int writes=0;
        var editor=new ManualTextEditWindow(target,_=>{writes++;return new(true,true,null);});
        try
        {
            assert(editor.Editor.Text==Text && !editor.Editor.IsReadOnly && !editor.ApplyButton.IsEnabled && !editor.ApplyButton.IsDefault,"The manual editor starts with exact paragraph text and never submits merely because Enter was pressed");
            editor.Apply();assert(writes==0,"An unchanged manual draft cannot create a write or Undo");
            editor.Editor.Text=Text+"\n";editor.Apply();assert(writes==0 && editor.Editor.Text.EndsWith('\n'),"Structural edits are rejected locally while the user's draft remains available");
            editor.Editor.Text=new string('a',16001);editor.Apply();assert(writes==0 && editor.Editor.Text.Length==16001,"An over-limit paste is rejected without silently truncating the user's draft");
            editor.Editor.Text="Đúng nội dung.";assert(editor.ApplyButton.IsEnabled,"A user-authored draft requires an explicit enabled Apply action");
            editor.Apply();editor.Apply();assert(writes==1 && editor.Editor.IsReadOnly && !editor.ApplyButton.IsEnabled,"A committed manual action is not replayed by a second Apply click");
        }
        finally{editor.Close();}
        var failed=new ManualTextEditWindow(target,_=>{writes++;return new(false,false,"Không khớp nguồn.");});
        try
        {
            int before=writes;failed.Editor.Text="Bản nháp cần giữ.";failed.Apply();failed.Apply();
            assert(writes==before+1 && failed.Editor.Text=="Bản nháp cần giữ." && !failed.Editor.IsReadOnly && !failed.ApplyButton.IsEnabled && failed.Feedback.Text.Contains("Bản nháp",StringComparison.Ordinal),
                "A failed manual action preserves the editable draft and blocks stale retries");
        }
        finally{failed.Editor.Text=target.Original;failed.Close();}
        var afterScanError=new ManualTextEditWindow(target,_=>new(true,false,"Lỗi quét sau sửa."));
        try
        {
            afterScanError.Editor.Text="Đúng nội dung.";afterScanError.Apply();
            assert(afterScanError.Editor.IsReadOnly && !afterScanError.ApplyButton.IsEnabled && afterScanError.Feedback.Text.Contains("Lỗi quét",StringComparison.Ordinal),
                "A committed edit with failed rescan remains committed, displays its real failure and cannot replay the write");
        }
        finally{afterScanError.Close();}
        string folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ChinhTa-manual-fixture-"+Guid.NewGuid());System.IO.Directory.CreateDirectory(folder);
        try
        {
            string source=OfficeManualTextCheck.Create(folder);
            using var package=DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(source,false);
            var errors=new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(package).ToArray();
            assert(errors.Length==0,"The independent manual Word source is schema-valid before starting Word: "+string.Join("; ",errors.Take(3).Select(e=>e.Description)));
        }
        finally{System.IO.Directory.Delete(folder,true);}
        var required=new Issue(0,3,"Sai","Đúng","Mẫu thể thức.",false,FixKind.NationalHeading,Citation:"NĐ30");
        var warning=new Issue(4,3,"nội","nội","Cần người xem.",false,CanFix:false);
        var unknown=warning with {Start=20};var anchors=new Dictionary<Issue,PreviewAnchor>{{required,new(1,Text,0,3)},{warning,new(1,Text,4,3)}};
        var sequence=Page(out var fixedPage);var highlighter=new PreviewHighlighter();var layer=new IssueUnderlineLayer();Issue? selected=null;
        int mapped=layer.Show(sequence,highlighter,[required,warning,unknown],anchors,i=>selected=i);
        var marks=fixedPage.Children.OfType<System.Windows.Shapes.Rectangle>().ToArray();
        assert(mapped==2 && layer.Count==2 && marks.Any(m=>m.Fill==Brushes.Firebrick) && marks.Any(m=>m.Fill==Brushes.DarkGoldenrod),
            "Mapped required issues are red, review/manual issues yellow, and unmapped issues are never placed by guesswork");
        var red=marks.Single(m=>Equals(m.Tag,required));red.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonDownEvent});
        assert(selected==required,"Clicking a preview underline selects the exact corresponding issue for manual review");
        highlighter.Show(sequence,anchors[required]);highlighter.Clear();
        assert(layer.Count==2 && fixedPage.Children.OfType<System.Windows.Shapes.Rectangle>().Count()==2,"Selection highlighting can clear without erasing persistent manual issue underlines");
        layer.Clear();assert(layer.Count==0 && fixedPage.Children.OfType<Glyphs>().Single().UnicodeString==Text && !fixedPage.Children.OfType<System.Windows.Shapes.Rectangle>().Any(),
            "Removing manual decorations leaves original XPS glyphs and content unchanged");
        highlighter.Reset();
    }
}
