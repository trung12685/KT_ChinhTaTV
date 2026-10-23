using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class IssueReviewPanelChecks
{
    private sealed class BindingTrace : TraceListener
    {
        public List<string> Errors { get; }=[];
        public override void Write(string? message) {if(!string.IsNullOrWhiteSpace(message))Errors.Add(message);}
        public override void WriteLine(string? message)=>Write(message);
    }
    private static IEnumerable<T> Visuals<T>(DependencyObject root) where T:DependencyObject
    {
        if(root is T found)yield return found;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
            foreach(var child in Visuals<T>(VisualTreeHelper.GetChild(root,i)))yield return child;
    }
    private static void Layout(FrameworkElement element)
    {element.Measure(new Size(470,840));element.Arrange(new Rect(0,0,470,840));element.UpdateLayout();element.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);}
    public static void Run(Action<bool,string> assert)
    {
        IssueReviewModelChecks.Run(assert);
        var trace=new BindingTrace();PresentationTraceSources.DataBindingSource.Listeners.Add(trace);
        var previousLevel=PresentationTraceSources.DataBindingSource.Switch.Level;
        PresentationTraceSources.DataBindingSource.Switch.Level=SourceLevels.Error;
        try
        {
            const string source="Hôm nay   là ngày thử, nội dung chỉ để minh họa.";
            var space=new Issue(7,3,"   "," ","Ba dấu cách liên tiếp giữa chữ; cần duyệt bố cục.",false);
            var repeat=new Issue(50,9," kế hoạch","","Giữ cụm lần đầu, xóa phần lặp đã chọn.",false,FixKind.RepeatedText);
            Issue Line(LineRole role,int start)=>new(start,3,"ABC","ABC","Thiếu Shape Line. Dòng tham chiếu 210 pt; đề xuất 90 pt, cách dưới 6 pt.",false,FixKind.ShapeLine,
                Citation:"NĐ30, Phụ lục I.",Title:"Thiếu đường kẻ dưới "+(role==LineRole.Agency?"cơ quan ban hành":role==LineRole.Motto?"tiêu ngữ":"trích yếu"),LineFix:new(null,100,106,90,role));
            var missing=Line(LineRole.Agency,100);
            var manual=new Issue(200,3,"ABC","ABC","Có đối tượng chưa ghép chắc chắn; không tạo thêm đường chồng.",false,FixKind.ShapeLine,CanFix:false,Title:"Kiểm tra đường kẻ dưới tiêu ngữ");
            var warnings=new[]{space,space with {Start=30},repeat,missing,Line(LineRole.Motto,110),Line(LineRole.Subject,120),manual,
                new Issue(300,3,"ABC","ABC","Kiểm tra lề trái theo quy định và ngoại lệ.",false,FixKind.PageLayout,CanFix:false,Title:"Lề trái chưa phù hợp"),
                new Issue(350,1,"4","3","Vị trí thứ ba trong cùng mục cha; giữ nội dung, cần duyệt dẫn chiếu.",false,FixKind.OutlineOrder,Title:"Thứ tự mục: 4 → 3"),
                new Issue(380,2,"+ ","","Chỉ có một dấu + trong mục cha; giữ nội dung và thụt đầu dòng.",false,FixKind.OutlineMarker,Title:"Dấu + đứng một mình trong mục")};
            var anchors=warnings.ToDictionary(i=>i,i=>new PreviewAnchor(i.Start<100?2:1,source,Math.Min(i.Start,source.Length),Math.Min(i.Length,source.Length-Math.Min(i.Start,source.Length))));
            var panel=new IssueReviewPanel();panel.LoadIssues(warnings,anchors);Layout(panel);
            var list=(ListBox)panel.FindName("IssueList");var all=(CheckBox)panel.FindName("AllCheck");var model=(IssueReviewModel)panel.DataContext;
            assert(list.Items.Count==warnings.Length && model.Sections.Count==3 && list.ItemsSource is ListCollectionView {GroupDescriptions.Count:2},
                "The real WPF panel groups every issue into section and family without losing rows");
            assert(Visuals<TextBlock>(panel).Any(t=>t.Text=="BẮT BUỘC SỬA") &&
                Visuals<TextBlock>(panel).Any(t=>t.Text=="ĐỀ XUẤT SỬA — CẦN DUYỆT") &&
                Visuals<TextBlock>(panel).Any(t=>t.Text=="CHƯA TỰ SỬA — CHỈ CẢNH BÁO"),
                "All three section headers render through the actual grouped templates");
            var rows=Visuals<ListBoxItem>(list).ToArray();
            assert(rows.Length>0 && rows.All(r=>r.ActualHeight<=29 && r.ActualHeight>=26),"Rendered issue rows occupy one compact 28-pixel line");
            assert(!Visuals<CheckBox>(panel).Any(c=>c.Content is string text && text.Contains("Chọn để sửa hàng loạt")),
                "The old bulk-selection sentence is absent from visible checkboxes");
            var selected=panel.Choices.Single(c=>c.Issue==space);
            var row=rows.Single(r=>ReferenceEquals(r.DataContext,selected));
            var rowCheck=Visuals<CheckBox>(row).Single();rowCheck.SetCurrentValue(ToggleButton.IsCheckedProperty,true);
            assert(selected.IsChecked && model.All.SelectedCount==1 && all.IsChecked is null,"The actual row checkbox updates its family, section and global partial selection");
            Issue? active=null;panel.ActiveIssueChanged+=issue=>active=issue;
            list.SelectedItem=selected;
            assert(ReferenceEquals(panel.SelectedIssue,space) && ReferenceEquals(active,space) && model.All.SelectedCount==1,
                "Keyboard or pointer selection highlights the original Issue independently of checked membership");
            assert(((TextBlock)panel.FindName("DetailText")).Text.Contains(space.Message) && selected.Detail.Contains("Hôm nay") &&
                Visuals<FrameworkElement>(row).Any(e=>e.ToolTip is ToolTip),"Selected details and hover tooltip retain the full message and local context outside the compact row");
            model.Groups.Single(g=>g.Items.Contains(selected)).IsChecked=true;
            assert(model.All.SelectedCount==2 && model.All.IsChecked is null,"A family checkbox uses the same real row choices");
            var lineGroup=model.Groups.Single(g=>g.Items.Any(c=>c.Issue==missing));lineGroup.IsExpanded=false;Layout(panel);
            all.SetCurrentValue(ToggleButton.IsCheckedProperty,true);
            assert(model.All.SelectedCount==8 && lineGroup.SelectedCount==3 && panel.Choices.Where(c=>!c.Issue.CanFix).All(c=>!c.IsChecked),
                "Select all reaches collapsed groups and leaves manual warnings unchecked");
            all.SetCurrentValue(ToggleButton.IsCheckedProperty,false);
            assert(panel.Choices.All(c=>!c.IsChecked),"Global clearing resets every checked row through actual WPF binding");
            lineGroup.IsExpanded=true;Layout(panel);
            var manualRow=Visuals<ListBoxItem>(list).Single(r=>((IssueChoice)r.DataContext).Issue==manual);
            assert(!Visuals<CheckBox>(manualRow).Single().IsEnabled,"Manual row checkboxes remain disabled in the actual template");
            var old=selected;old.IsChecked=true;panel.LoadIssues([repeat]);Layout(panel);old.IsChecked=false;
            assert(panel.SelectedIssue is null && panel.Choices.Count==1 && panel.Choices.All(c=>!c.IsChecked) &&
                ((TextBlock)panel.FindName("Summary")).Text.Contains("0/1") && !((TextBlock)panel.FindName("DetailText")).Text.Contains(space.Message),
                "Rescan discards old rows, checks, selection and details instead of retaining stale repaired entries: selected="+panel.SelectedIssue?.Kind+", checked="+panel.Choices.Count(c=>c.IsChecked)+", summary="+((TextBlock)panel.FindName("Summary")).Text);
            panel.LoadIssues([]);Layout(panel);
            assert(!all.IsEnabled && list.Visibility==Visibility.Collapsed && ((TextBlock)panel.FindName("Empty")).Visibility==Visibility.Visible,
                "Clearing after failure or empty scan leaves no enabled stale row or select-all checkbox");
            panel.LoadIssues(warnings,anchors);Layout(panel);
            var screenshot=new RenderTargetBitmap(470,840,96,96,PixelFormats.Pbgra32);screenshot.Render(panel);
            string folder=Path.Combine(Environment.CurrentDirectory,"artifacts");Directory.CreateDirectory(folder);
            var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(screenshot));
            using(var output=File.Create(Path.Combine(folder,"issue-list-preview.png")))png.Save(output);
            assert(new FileInfo(Path.Combine(folder,"issue-list-preview.png")).Length>1000,"The Windows preview image renders from synthetic issues without opening Word or private documents");
            assert(trace.Errors.Count==0,"Grouped row and header bindings have no WPF binding errors: "+string.Join(" | ",trace.Errors.Take(3)));
        }
        finally
        {
            PresentationTraceSources.DataBindingSource.Switch.Level=previousLevel;
            PresentationTraceSources.DataBindingSource.Listeners.Remove(trace);
        }
    }
}
