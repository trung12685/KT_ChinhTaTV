using System.IO;
using System.Windows;
using ChinhTa.Core;
using Microsoft.Win32;

namespace ChinhTa.Desktop;

public partial class ProgressReportWindow : Window
{
    private readonly Action<string,string> export;
    private readonly string version;
    public ProgressReportWindow(Action<string,string> export)
    {
        InitializeComponent();this.export=export;
        version=typeof(ProgressReportWindow).Assembly.GetName().Version?.ToString(3)??"chưa rõ";
        Summary.Text=$"Bản {version} • {DevelopmentReport.Count(CapabilityMode.Reviewable)} nhóm có đề xuất sửa • {DevelopmentReport.Count(CapabilityMode.Manual)} nhóm cảnh báo • {DevelopmentReport.Count(CapabilityMode.Demonstration)} minh họa";
        ReportBody.Text=DevelopmentReport.Build(version,DateTimeOffset.Now,ReadEvidence(version));
    }
    private static ReportEvidence? ReadEvidence(string version)
    {
        try{return ReportEvidence.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"build-checks.json")),version);}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException)
        {return null;}
    }
    private void ExportClick(object sender,RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog {Filter="Báo cáo tiếng Việt (*.txt)|*.txt",DefaultExt=".txt",AddExtension=true,
            FileName=$"ChinhTaViet-{version}-tien-do.txt",OverwritePrompt=true};
        if(dialog.ShowDialog(this)!=true)return;
        try
        {
            export(dialog.FileName,ReportBody.Text);
            ExportStatus.Text="Đã xuất báo cáo: "+dialog.FileName;
        }
        catch(Exception error)
        {
            Diagnostics.Write("UI.ProgressReport.Export",error);
            ExportStatus.Text="Chưa xuất được báo cáo; bạn có thể chọn nơi lưu khác và thử lại.";
            MessageBox.Show(this,"Chưa xuất được báo cáo. "+error.Message,"Xuất báo cáo",MessageBoxButton.OK,MessageBoxImage.Warning);
        }
    }
    private void CloseClick(object sender,RoutedEventArgs e)=>Close();
}
