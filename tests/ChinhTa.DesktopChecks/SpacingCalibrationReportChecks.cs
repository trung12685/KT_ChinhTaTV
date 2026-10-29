using ChinhTa.Core;
using ChinhTa.Desktop;

public static class SpacingCalibrationReportChecks
{
    private static IsolatedSpacingCalibrationReport Report(SpacingCalibrationStatus status=SpacingCalibrationStatus.WithinRange,
        SpacingGapCalibrationResult? gap=null)=>new(new(status,"PRIVATE-TEXT-CP123-HASHsecret",
            status==SpacingCalibrationStatus.BelowSingle?[10,11]:status==SpacingCalibrationStatus.AboveOneAndHalf?[19,20]:[13,14],
            [12,12],[18,18]),gap,7,1,6);
    public static void Run(Action<bool,string> assert)
    {
        foreach(var pair in new[]{(SpacingCalibrationStatus.BelowSingle,"thấp hơn mức dòng đơn"),
            (SpacingCalibrationStatus.WithinRange,"nằm trong khoảng dòng đơn"),(SpacingCalibrationStatus.AboveOneAndHalf,"cao hơn mức 1,5 dòng"),
            (SpacingCalibrationStatus.Unproven,"chưa đủ bằng chứng")})
        {
            string text=SpacingCalibrationWindow.BuildReport(Report(pair.Item1));
            assert(text.Contains(pair.Item2) && text.Contains(SpacingCalibrationWindow.Scope) && !text.Contains("PRIVATE") && !text.Contains("HASHsecret") && !text.Contains("CP123"),
                "Spacing report maps diagnostic status without exposing arbitrary reason/document text: "+pair.Item1);
        }
        string measured=SpacingCalibrationWindow.BuildReport(Report(gap:new(SpacingGapCalibrationStatus.BelowSixPoints,"PRIVATE",3,4)));
        assert(measured.Contains("3 pt") && measured.Contains("4 pt") && measured.Contains("nhỏ hơn mốc 6 pt đã đo") && measured.Contains("Khoảng baseline hiện tại") && measured.Contains("Mốc dòng đơn") && measured.Contains("Mốc 1,5 dòng"),
            "Spacing report renders measured baseline vectors and separated gap contribution rather than ink-box distance");
        measured=SpacingCalibrationWindow.BuildReport(Report(gap:new(SpacingGapCalibrationStatus.AtLeastSixPoints,"PRIVATE",4,4)));
        assert(measured.Contains("đạt mốc 6 pt đã đo") && measured.Contains("dung sai đo") && measured.Contains("không phải tổng khoảng trắng hiển thị") && !measured.Contains("PRIVATE"),
            "A measured gap threshold retains tolerance and never claims complete document compliance");
        string unknown=SpacingCalibrationWindow.BuildReport(Report(SpacingCalibrationStatus.Unproven,new(SpacingGapCalibrationStatus.Unproven,"PRIVATE",null,null)));
        assert(!unknown.Contains("Mốc dòng đơn:") && unknown.Contains("chưa tách được tác động") && !unknown.Contains("0 pt") && !unknown.Contains("PRIVATE"),
            "Unproven line/gap results do not fabricate numeric measurements or expose raw reasons");
        foreach(double value in new[]{double.NaN,double.PositiveInfinity,-1})
        {
            string text=SpacingCalibrationWindow.BuildReport(Report(gap:new(SpacingGapCalibrationStatus.AtLeastSixPoints,"",value,4)) with
                {Line=new(SpacingCalibrationStatus.WithinRange,"",[value,14],[12,12],[18,18])});
            assert(text.Contains("chưa đủ bằng chứng") && text.Contains("chưa tách được tác động") && !text.Contains("Khoảng baseline hiện tại:"),
                "Malformed numeric evidence cannot be promoted into a proven spacing result: "+value);
        }
        foreach(var line in new[]{new SpacingCalibrationResult(SpacingCalibrationStatus.WithinRange,"",[25,25],[12,12],[18,18]),
            new(SpacingCalibrationStatus.BelowSingle,"",[13,14],[12,12],[18,18]),
            new(SpacingCalibrationStatus.AboveOneAndHalf,"",[13,14],[12,12],[18,18]),
            new(SpacingCalibrationStatus.WithinRange,"",[10,20],[12,12],[18,18]),
            new(SpacingCalibrationStatus.WithinRange,"",[.06,.06],[.01,.01],[1,1])})
        {
            string text=SpacingCalibrationWindow.BuildReport(Report() with {Line=line});
            assert(text.Contains("chưa đủ bằng chứng") && !text.Contains("Khoảng baseline hiện tại:"),
                "A supplied status inconsistent with its whole measured vector cannot appear as a proven line result");
        }
        foreach(var gap in new[]{new SpacingGapCalibrationResult(SpacingGapCalibrationStatus.AtLeastSixPoints,"",1,4),
            new(SpacingGapCalibrationStatus.BelowSixPoints,"",4,4),new(SpacingGapCalibrationStatus.AtLeastSixPoints,"",.05,.05)})
        {
            string text=SpacingCalibrationWindow.BuildReport(Report(gap:gap));
            assert(text.Contains("chưa tách được tác động") && !text.Contains("Tác động khoảng đoạn so với bản sao bỏ khoảng:"),
                "An inconsistent gap status or indistinct six-point reference cannot be presented as proven spacing");
        }
        string noGap=SpacingCalibrationWindow.BuildReport(Report());
        assert(noGap.Contains("Khoảng đoạn: chưa đo") && !noGap.Contains("Tác động khoảng đoạn so với bản sao bỏ khoảng:"),
            "A paragraph without an eligible neighbor does not fabricate a gap trial result");
        string missingScope=SpacingCalibrationWindow.BuildReport(Report() with {Pages=0});
        assert(missingScope.Contains("chưa đủ bằng chứng") && !missingScope.Contains("Khoảng baseline hiện tại:"),
            "Missing source scope cannot be presented as a proven baseline measurement");
        string noGapScope=SpacingCalibrationWindow.BuildReport(Report() with {Pages=0,Gap=new(SpacingGapCalibrationStatus.AtLeastSixPoints,"",3,3)});
        assert(!noGapScope.Contains("Tác động hiện tại đạt mốc") && noGapScope.Contains("chưa tách được"),
            "Missing scope cannot claim a proven gap result even when standalone differential values appear valid");
        foreach(var code in Enum.GetValues<SpacingDiagnosticFailureCode>())
        {
            string failure=SpacingCalibrationWindow.BuildFailure(new SpacingDiagnosticException(code,new InvalidOperationException("PRIVATE-SOURCE-XML")));
            assert(failure.Contains(new SpacingDiagnosticException(code).Message) && !failure.Contains("PRIVATE-SOURCE"),
                "Spacing failure explains the actual safe recovery category without raw exception content: "+code);
        }
        assert(SpacingCalibrationWindow.BuildFailure(new SpacingDiagnosticException(SpacingDiagnosticFailureCode.ReferenceStale)).Contains("quét lại") &&
            !SpacingCalibrationWindow.BuildFailure(new SpacingDiagnosticException(SpacingDiagnosticFailureCode.UnsupportedEvidence)).Contains("quét lại") &&
            SpacingCalibrationWindow.BuildFailure(new SpacingDiagnosticException(SpacingDiagnosticFailureCode.IntegrityFailed)).Contains("Không dùng bản thử"),
            "Unsupported scope and integrity failure no longer prescribe a misleading rescan-only recovery");
        string recoveryPath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ChinhTa-Isolated-"+Guid.NewGuid().ToString("N"));
        string cleanup=SpacingCalibrationWindow.BuildFailure(new SpacingDiagnosticException(SpacingDiagnosticFailureCode.CleanupFailed,null,recoveryPath));
        assert(cleanup.Contains(recoveryPath) && cleanup.Contains("Bản phục hồi phiên đo:"),"An owned retained copy is visible for recovery after failed Word cleanup");
        assert(!SpacingCalibrationWindow.BuildFailure(new SpacingDiagnosticException(SpacingDiagnosticFailureCode.CleanupFailed,null,"PRIVATE-DOC-PATH")).Contains("PRIVATE-DOC"),
            "An arbitrary recovery path cannot leak source text through a failure report");
        var targets=new[]{new SpacingDiagnosticTarget("Nội dung riêng thứ nhất\vDòng hai",null),new SpacingDiagnosticTarget("Nội dung riêng thứ hai","Đoạn kế")};
        int calls=0;SpacingCalibrationWindow? window=null;
        window=new(targets,target=>
        {
            calls++;assert(window!.IsMeasuring && !window.TargetPicker.IsEnabled && !window.MeasureButton.IsEnabled,
                "The measurement callback executes with selection/run disabled");
            window.MeasureSelected(); // Model a reentrant event while Word pumps STA messages.
            return Report();
        });
        try
        {
            assert(calls==0 && window.ReportText.Length==0 && window.ResultBox.IsReadOnly,
                "Opening the measurement dialog never contacts Word or manufactures an initial result");
            window.MeasureSelected();
            assert(calls==1 && !window.IsMeasuring && window.MeasureButton.IsEnabled && window.ReportText.Contains("nằm trong khoảng") && !window.ReportText.Contains("Nội dung riêng"),
                "A reentrant run is blocked and the completed report contains no selected document text");
            window.TargetPicker.SelectedIndex=1;
            assert(window.ReportText.Length==0 && calls==1,"Changing the target immediately invalidates the previous measurement without running another one");
        }
        finally{window.Close();}
        int attempt=0;var failed=new SpacingCalibrationWindow(targets,_=>++attempt==1?Report():throw new InvalidOperationException("PRIVATE-DOC-XML-CONTENT"));
        try
        {
            failed.MeasureSelected();failed.MeasureSelected();
            assert(failed.ReportText==SpacingCalibrationWindow.Failed && !failed.ReportText.Contains("PRIVATE-DOC") && !failed.ReportText.Contains("nằm trong khoảng") && !failed.IsMeasuring && failed.MeasureButton.IsEnabled,
                "A failed rerun removes stale success, hides raw document-bearing exceptions and restores controls");
        }
        finally{failed.Close();}
        var empty=new SpacingCalibrationWindow([], _=>throw new Exception("Must not measure an empty scope"));
        try{assert(!empty.MeasureButton.IsEnabled && empty.ReportText.Contains("Chưa có đoạn đủ điều kiện"),"An empty eligible scope is explicit and cannot start Word measurement");}
        finally{empty.Close();}
    }
}
