using System.IO;
using System.Windows;
using System.Windows.Controls;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal sealed record OfficeFocusedCheckResult(OfficeSuiteReport Report,string Folder)
{
    public string Summary=>OfficeFocusedCheck.Summary(Report);
}

// This is a separate diagnostic entry point. It never upgrades or replaces the
// result of the original full integration check, and always uses the legacy reader.
internal static class OfficeFocusedCheck
{
    internal const string Scope="Chỉ các nhóm đã chọn, bộ đọc cũ; không xác nhận bài Word toàn bộ hoặc so sánh hai bộ đọc.";
    internal static IReadOnlyList<OfficeSuiteCase> Cases {get;}=Array.AsReadOnly(new[]
    {
        new OfficeSuiteCase("handwriting","Chỗ ký tay 80 pt"),
        new OfficeSuiteCase("signature","Phông khối ký"),
        new OfficeSuiteCase("page-layout","Khổ giấy và lề"),
        new OfficeSuiteCase("page-number","Số trang"),
        new OfficeSuiteCase("body-spacing","Khoảng đoạn/giãn dòng và phối hợp"),
        new OfficeSuiteCase("body-format","Phông/căn nội dung và phối hợp")
    });
    private sealed record FocusedSession(WordSession Test,string Seed,string Folder);
    internal static string Summary(OfficeSuiteReport report)=>
        $"{(report.Passed?"ĐẠT CÁC NHÓM ĐÃ CHỌN":"CHƯA ĐẠT CÁC NHÓM ĐÃ CHỌN")}: {report.PassedCount} đạt; {report.FailedCount} lỗi; {report.NotRunCount} chưa chạy.\n"+
        string.Join("\n",report.Results.Select(r=>$"{Token(r.Status)} — {r.Suite.Name}"))+"\n\n"+Scope;
    private static string Token(OfficeSuiteStatus status)=>status switch
    {OfficeSuiteStatus.Pass=>"PASS",OfficeSuiteStatus.Fail=>"FAIL",_=>"NOT_RUN"};
    internal static WordSession NewSession(string folder)=>new(){Profile=DocumentProfile.State,UseOptimizedReading=false,LineCheckArtifactFolder=folder};
    internal static OfficeFocusedCheckResult Run(IReadOnlyList<OfficeSuiteCase> selected)
    {
        if(selected.Count==0 || selected.Any(s=>!Cases.Contains(s)) || selected.Select(s=>s.Id).Distinct().Count()!=selected.Count)
            throw new ArgumentException("Hãy chọn ít nhất một nhóm kiểm Word hợp lệ.",nameof(selected));
        string folder=Path.Combine(Path.GetDirectoryName(Diagnostics.LogPath)!,"OfficeGroups-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);string reportPath=Path.Combine(folder,"office-groups.txt");
        File.WriteAllText(reportPath,$"ChinhTaViet {typeof(OfficeFocusedCheck).Assembly.GetName().Version}\n{DateTimeOffset.Now:O}\nMODE Legacy\n{Scope}\nSELECTED {string.Join(",",selected.Select(s=>s.Id))}\n");
        void Note(string message)=>File.AppendAllText(reportPath,message+Environment.NewLine);
        var report=OfficeSuiteRunner.Run(selected,
            suite=>
            {
                string isolated=Path.Combine(folder,suite.Id);Directory.CreateDirectory(isolated);
                Note($"BEGIN {suite.Id}: {suite.Name}");
                string sample=Path.Combine(AppContext.BaseDirectory,"samples","BaDuongKeThieu.docx");
                if(!File.Exists(sample))throw new FileNotFoundException("Thiếu mẫu giả đi kèm bản giải nén.",sample);
                string modern=OfficeIntegrationCheck.CreateModernSample(sample,isolated);
                var test=NewSession(isolated);
                try{test.Open(modern);Note(suite.Id+" "+test.OfficeDescription);return new FocusedSession(test,modern,isolated);}
                catch(Exception failure)
                {
                    try{test.Dispose();}
                    catch(Exception cleanup){throw new AggregateException("Chưa mở và đóng được phiên Word riêng.",failure,cleanup);}
                    throw;
                }
            },
            (current,suite)=>
            {
                int completed=0;
                void Pass(string detail){completed++;Note($"STEP_PASS {suite.Id}: {detail}");}
                switch(suite.Id)
                {
                    case "handwriting":OfficeSignatureWritingSpaceCheck.Run(current.Test,current.Seed,current.Folder,Pass);break;
                    case "signature":OfficeSignatureFormatCheck.Run(current.Test,current.Seed,current.Folder,Pass);break;
                    case "page-layout":OfficePageLayoutCheck.Run(current.Test,current.Seed,current.Folder,Pass);break;
                    case "page-number":OfficePageNumberCheck.Run(current.Test,current.Seed,current.Folder,Pass);break;
                    case "body-spacing":
                        // Build only the body fixture. An earlier font/Line test
                        // must not stop the spacing assertions from starting.
                        string seed=OfficeBodyFormatCheck.CreateSource(current.Seed,current.Folder);
                        OfficeBodySpacingCheck.Run(current.Test,seed,current.Folder,Pass);break;
                    case "body-format":OfficeBodyFormatCheck.Run(current.Test,current.Seed,current.Folder,Pass);break;
                    default:throw new InvalidOperationException("Nhóm kiểm chưa được nối với bài kiểm thực.");
                }
                RequireCompleted(completed);
            },current=>current.Test.Dispose(),result=>
            {
                // Persist the completed result before opening the next Word
                // instance, so a later crash cannot suppress earlier evidence.
                Note($"{Token(result.Status)} {result.Suite.Id}: {result.Suite.Name}");
                if(result.Error is Exception error)
                {
                    // Exception.ToString preserves aggregate branches, messages and stacks.
                    Note(error.ToString());Diagnostics.Write("OfficeGroups."+result.Suite.Id,error);
                }
            });
        Note(Summary(report));Note(report.Passed?"RESULT SELECTED_PASS":"RESULT SELECTED_FAIL");
        return new(report,folder);
    }
    internal static void RequireCompleted(int completed)
    {if(completed<=0)throw new InvalidOperationException("Nhóm kiểm chưa xác nhận bước nào; không được ghi PASS.");}
    internal static IReadOnlyList<OfficeSuiteCase>? Select(Window owner)
    {
        var dialog=new Window{Title="Kiểm tra từng nhóm Word",Owner=owner,Width=610,SizeToContent=SizeToContent.Height,
            WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize};
        var panel=new StackPanel{Margin=new Thickness(18)};
        panel.Children.Add(new TextBlock{Text="Mỗi nhóm dùng một phiên Word và tài liệu mẫu riêng. Nhóm lỗi không ngăn nhóm tiếp theo chạy.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)});
        var boxes=Cases.Select(s=>new CheckBox{Content=s.Name,Tag=s,IsChecked=true,Margin=new Thickness(0,4,0,4)}).ToArray();
        foreach(var box in boxes)panel.Children.Add(box);
        panel.Children.Add(new TextBlock{Text=Scope+"\nMột số nhóm có cả bước phối hợp với đường kẻ; lỗi tại đó vẫn được báo FAIL.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,12)});
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
        var start=new Button{Content="Bắt đầu kiểm tra",Padding=new Thickness(12,5,12,5),IsDefault=true};
        var cancel=new Button{Content="Đóng",Padding=new Thickness(12,5,12,5),Margin=new Thickness(8,0,0,0),IsCancel=true};
        start.Click+=(_,_)=>
        {
            if(!boxes.Any(b=>b.IsChecked==true)){MessageBox.Show(dialog,"Hãy chọn ít nhất một nhóm.","Chọn nhóm kiểm");return;}
            dialog.DialogResult=true;
        };
        buttons.Children.Add(start);buttons.Children.Add(cancel);panel.Children.Add(buttons);dialog.Content=panel;
        return dialog.ShowDialog()==true?boxes.Where(b=>b.IsChecked==true).Select(b=>(OfficeSuiteCase)b.Tag).ToArray():null;
    }
}
