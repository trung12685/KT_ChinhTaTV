using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Explicit read-only measurement. Paragraph previews stay inside this window;
// neither the report nor an exception handler exports document text.
internal sealed class SpacingCalibrationWindow:Window
{
    private sealed record TargetRow(SpacingDiagnosticTarget Target,string Label);
    private readonly Func<SpacingDiagnosticTarget,IsolatedSpacingCalibrationReport> measure;
    private readonly Button close;
    internal ComboBox TargetPicker {get;}
    internal Button MeasureButton {get;}
    internal TextBox ResultBox {get;}
    internal bool IsMeasuring {get;private set;}
    internal string ReportText=>ResultBox.Text;
    internal const string Scope="Chỉ đo thử trên bản sao; chưa tự sửa hoặc xác nhận toàn bộ thể thức tài liệu.";
    internal static readonly string Failed="Kết quả cũ đã được xóa. "+new SpacingDiagnosticException(SpacingDiagnosticFailureCode.OperationFailed).Message;

    internal SpacingCalibrationWindow(IReadOnlyList<SpacingDiagnosticTarget> targets,Func<SpacingDiagnosticTarget,IsolatedSpacingCalibrationReport> measure)
    {
        this.measure=measure??throw new ArgumentNullException(nameof(measure));
        ArgumentNullException.ThrowIfNull(targets);
        Title="Đo thử giãn dòng và khoảng đoạn";Width=790;Height=610;MinWidth=600;MinHeight=440;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new DockPanel{Margin=new Thickness(18)};
        var introduction=new TextBlock{Text="Chọn một đoạn để so khoảng dòng với mức dòng đơn và 1,5 dòng trên bản sao. Nếu có đoạn liền sau đủ điều kiện, phép đo gồm cả tác động khoảng đoạn.\n"+Scope,
            TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)};
        DockPanel.SetDock(introduction,Dock.Top);root.Children.Add(introduction);
        TargetPicker=new ComboBox{DisplayMemberPath=nameof(TargetRow.Label),Margin=new Thickness(0,0,0,12),
            ItemsSource=targets.Select((t,i)=>new TargetRow(t,$"Đoạn {i+1}: "+Preview(t.Text)+(t.NextText is null?"":" — có cặp liền sau"))).ToArray()};
        DockPanel.SetDock(TargetPicker,Dock.Top);root.Children.Add(TargetPicker);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,12,0,0)};
        MeasureButton=new Button{Content="Bắt đầu đo thử",Padding=new Thickness(14,6,14,6),IsDefault=true};
        close=new Button{Content="Đóng",Padding=new Thickness(14,6,14,6),Margin=new Thickness(8,0,0,0),IsCancel=true};
        MeasureButton.Click+=(_,_)=>MeasureSelected();close.Click+=(_,_)=>Close();buttons.Children.Add(MeasureButton);buttons.Children.Add(close);
        DockPanel.SetDock(buttons,Dock.Bottom);root.Children.Add(buttons);
        ResultBox=new TextBox{IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(10)};
        root.Children.Add(ResultBox);Content=root;
        TargetPicker.SelectionChanged+=(_,_)=>
        {
            ResultBox.Clear();MeasureButton.IsEnabled=!IsMeasuring && TargetPicker.SelectedItem is TargetRow;
        };
        if(targets.Count>0)TargetPicker.SelectedIndex=0;
        else{MeasureButton.IsEnabled=false;ResultBox.Text="Chưa có đoạn đủ điều kiện đo thử. Hãy quét lại hoặc chọn tài liệu có đoạn nội dung nhiều dòng rõ ràng.";}
        Closing+=PreventCloseDuringMeasurement;
    }
    private static string Preview(string text)
    {
        string visible=string.Concat(text.Select(c=>char.IsControl(c)?' ':c));
        return visible.Length<=120?visible:visible[..120]+"…";
    }
    private void PreventCloseDuringMeasurement(object? sender,CancelEventArgs e)
    {if(IsMeasuring)e.Cancel=true;}
    internal void MeasureSelected()
    {
        if(IsMeasuring)return;
        ResultBox.Clear();
        if(TargetPicker.SelectedItem is not TargetRow row){ResultBox.Text="Chọn một đoạn để bắt đầu đo thử.";return;}
        var previousCursor=Mouse.OverrideCursor;
        IsMeasuring=true;TargetPicker.IsEnabled=false;MeasureButton.IsEnabled=false;close.IsEnabled=false;Mouse.OverrideCursor=Cursors.Wait;
        try
        {
            var report=measure(row.Target);
            ResultBox.Text=ReferenceEquals(TargetPicker.SelectedItem,row)?BuildReport(report):"Lựa chọn đã đổi trong lúc đo; kết quả chưa được dùng. Hãy bắt đầu phép đo mới.";
        }
        catch(Exception error){ResultBox.Text=BuildFailure(error);} // Never log/display raw document-bearing exceptions.
        finally
        {
            Mouse.OverrideCursor=previousCursor;IsMeasuring=false;TargetPicker.IsEnabled=true;close.IsEnabled=true;
            MeasureButton.IsEnabled=TargetPicker.SelectedItem is TargetRow;
        }
    }
    internal static string BuildFailure(Exception error)
    {
        if(error is not SpacingDiagnosticException failure)return Failed;
        string text="Kết quả cũ đã được xóa. "+failure.Message;
        if(failure.RecoveryPath is { } path)text+="\nBản phục hồi phiên đo: "+path;
        return text;
    }
    private static string Number(double value)=>value.ToString("0.###",CultureInfo.GetCultureInfo("vi-VN"));
    private static bool KnownVectors(SpacingCalibrationResult result)=>result.SourceStepsPt.Count>=2 &&
        result.SingleStepsPt.Count==result.SourceStepsPt.Count && result.OneAndHalfStepsPt.Count==result.SourceStepsPt.Count &&
        result.SourceStepsPt.Concat(result.SingleStepsPt).Concat(result.OneAndHalfStepsPt).All(v=>double.IsFinite(v) && v>0) &&
        result.SingleStepsPt.All(v=>v>.05) &&
        result.SingleStepsPt.Select((v,i)=>result.OneAndHalfStepsPt[i]-v>.1).All(v=>v);
    private static SpacingCalibrationStatus CoherentLineStatus(SpacingCalibrationResult result,bool scope)
    {
        if(!scope || !KnownVectors(result))return SpacingCalibrationStatus.Unproven;
        const double tolerance=.05;
        bool matches=result.Status switch
        {
            SpacingCalibrationStatus.BelowSingle=>result.SourceStepsPt.Select((v,i)=>v<result.SingleStepsPt[i]-tolerance).All(v=>v),
            SpacingCalibrationStatus.WithinRange=>result.SourceStepsPt.Select((v,i)=>v>=result.SingleStepsPt[i]-tolerance && v<=result.OneAndHalfStepsPt[i]+tolerance).All(v=>v),
            SpacingCalibrationStatus.AboveOneAndHalf=>result.SourceStepsPt.Select((v,i)=>v>result.OneAndHalfStepsPt[i]+tolerance).All(v=>v),
            _=>false
        };
        return matches?result.Status:SpacingCalibrationStatus.Unproven;
    }
    internal static string BuildReport(IsolatedSpacingCalibrationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var text=new StringBuilder();text.AppendLine(Scope);text.AppendLine();
        if(report.Parts>0 && report.Pages>0 && report.GlyphRuns>0)
            text.AppendLine($"Phạm vi bản sao: {report.Pages} trang; {report.GlyphRuns} cụm chữ; {report.Parts} phần dữ liệu.");
        else text.AppendLine("Chưa có phạm vi bản sao đủ rõ để xác nhận phép đo.");
        var result=report.Line;
        bool scope=report.Parts>0 && report.Pages>0 && report.GlyphRuns>0;
        var status=CoherentLineStatus(result,scope);
        bool vectors=status!=SpacingCalibrationStatus.Unproven;
        text.AppendLine("Giãn dòng: "+(status switch
        {
            SpacingCalibrationStatus.BelowSingle=>"thấp hơn mức dòng đơn đã đo.",
            SpacingCalibrationStatus.WithinRange=>"nằm trong khoảng dòng đơn đến 1,5 dòng đã đo (có dung sai đo).",
            SpacingCalibrationStatus.AboveOneAndHalf=>"cao hơn mức 1,5 dòng đã đo.",
            _=>"chưa đủ bằng chứng; giữ kiểm tra thủ công."
        }));
        // Reason is intentionally not echoed: statuses and measured values are
        // enough here, without trusting an arbitrary string to be content-free.
        if(vectors && status is SpacingCalibrationStatus.BelowSingle or SpacingCalibrationStatus.WithinRange or SpacingCalibrationStatus.AboveOneAndHalf)
        {
            string Values(IReadOnlyList<double> values)=>string.Join("; ",values.Select(Number))+" pt";
            text.AppendLine("Khoảng baseline hiện tại: "+Values(result.SourceStepsPt));
            text.AppendLine("Mốc dòng đơn: "+Values(result.SingleStepsPt));
            text.AppendLine("Mốc 1,5 dòng: "+Values(result.OneAndHalfStepsPt));
        }
        if(report.Gap is not { } gap)text.AppendLine("Khoảng đoạn: chưa đo; chưa có cặp đoạn liền nhau đủ điều kiện.");
        else if(report.Parts>0 && report.Pages>0 && report.GlyphRuns>0 &&
            gap.Status is SpacingGapCalibrationStatus.BelowSixPoints or SpacingGapCalibrationStatus.AtLeastSixPoints &&
            gap.SourceEffectPt is double value && double.IsFinite(value) && value>=0 &&
            gap.SixPointReferenceEffectPt is double reference && double.IsFinite(reference) && reference>.1 &&
            (gap.Status==SpacingGapCalibrationStatus.AtLeastSixPoints?value>=reference-.05:value<reference-.05))
        {
            text.AppendLine("Tác động khoảng đoạn so với bản sao bỏ khoảng: "+Number(value)+" pt.");
            text.AppendLine("Tác động mốc đặt khoảng sau 6 pt trong cùng ngữ cảnh: "+Number(reference)+" pt.");
            text.AppendLine(gap.Status==SpacingGapCalibrationStatus.BelowSixPoints?"Tác động hiện tại nhỏ hơn mốc 6 pt đã đo.":"Tác động hiện tại đạt mốc 6 pt đã đo (có dung sai đo).");
            text.AppendLine("Hai giá trị trên là chênh lệch baseline so với bản sao bỏ khoảng, không phải tổng khoảng trắng hiển thị giữa hai đoạn.");
        }
        else text.AppendLine("Khoảng đoạn: chưa tách được tác động đủ chắc chắn; giữ kiểm tra thủ công.");
        text.AppendLine();text.Append("Kết quả này không thay đổi nội dung, không cấp quyền sửa tự động và không chứng nhận tài liệu đã đúng toàn bộ thể thức.");
        return text.ToString();
    }
}
