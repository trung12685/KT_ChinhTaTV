using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class ProgressReportDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
        int exports=0;
        var window=new ProgressReportWindow((_,_)=>exports++);
        try
        {
            var body=(TextBox)window.FindName("ReportBody");var summary=(TextBlock)window.FindName("Summary");
            assert(body.IsReadOnly && body.Text.Contains("C13 —") && body.Text.Contains("CÁC BƯỚC TIẾP THEO") && body.Text.Contains("hôm       nay là thứ 2") && body.Text.Contains("P05 —") && exports==0,
                "The report window opens read-only with catalog, examples and next steps, without exporting or contacting Word");
            assert(summary.Text.Contains(DevelopmentReport.Count(CapabilityMode.Reviewable)+" nhóm có đề xuất sửa") &&
                summary.Text.Contains(DevelopmentReport.Count(CapabilityMode.Manual)+" nhóm cảnh báo") &&
                ((Button)window.FindName("ExportButton")).IsEnabled,
                "The visible summary computes counts from the catalog and exposes export before a DOCX is opened");
        }
        finally{window.Close();}
        string folder=Path.Combine(Path.GetTempPath(),"ctv-report-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            using var session=new WordSession();
            string text=DevelopmentReport.Build("0.1.59",new DateTimeOffset(2026,10,23,10,0,0,TimeSpan.FromHours(7)));
            string saved=Path.Combine(folder,"báo-cáo.txt");session.SaveProgressReport(saved,text);
            byte[] bytes=File.ReadAllBytes(saved);
            assert(File.ReadAllText(saved)==text && bytes.Take(3).SequenceEqual(new byte[]{0xef,0xbb,0xbf}) &&
                !session.IsOpen && !session.Changed && !File.Exists(session.RecoveryPath),
                "A Vietnamese UTF-8 BOM report exports correctly without Word, checkpoint creation or dirty-state changes");
            session.SaveProgressReport(saved,text+"\nBản xuất tiếp.");
            assert(File.ReadAllText(saved)==text+"\nBản xuất tiếp." && !session.Changed,
                "Replacing an existing report publishes the complete new text without becoming a document edit");
            byte[] previous=File.ReadAllBytes(saved);bool locked=false;
            using(var held=new FileStream(saved,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                try{session.SaveProgressReport(saved,"must not publish");}
                catch(Exception error) when(error is IOException or UnauthorizedAccessException){locked=true;}
                assert(locked && File.ReadAllBytes(saved).SequenceEqual(previous) && !session.Changed && !session.IsOpen,
                    "A locked report destination preserves existing bytes and all document state when publication fails");
            }
            string protectedAlias=Path.Combine(folder,"working-alias.txt");byte[] secret=Encoding.UTF8.GetBytes("private checkpoint sentinel");
            File.WriteAllBytes(session.RecoveryPath,secret);
            if(!CreateHardLink(protectedAlias,session.RecoveryPath,IntPtr.Zero))throw new Win32Exception(Marshal.GetLastWin32Error());
            bool aliasRejected=false;
            try{session.SaveProgressReport(protectedAlias,text);}
            catch(InvalidOperationException){aliasRejected=true;}
            assert(aliasRejected && File.ReadAllBytes(session.RecoveryPath).SequenceEqual(secret) && File.ReadAllBytes(protectedAlias).SequenceEqual(secret),
                "A TXT alias cannot redirect report export onto the protected working checkpoint");
            string wrongExtension=Path.Combine(folder,"report.docx");bool extensionRejected=false;
            try{session.SaveProgressReport(wrongExtension,text);}catch(InvalidOperationException){extensionRejected=true;}
            assert(extensionRejected && !File.Exists(wrongExtension),"Report export requires TXT and cannot masquerade as a DOCX save");
            session.Undo();
            assert(File.ReadAllBytes(session.RecoveryPath).SequenceEqual(secret) && !session.IsOpen && !session.Changed,
                "Report export adds no Undo action and does not open Word when Undo is empty");
            assert(Directory.GetFiles(folder,".ChinhTaViet-*.tmp").Length==0 &&
                Directory.GetFiles(Path.GetDirectoryName(session.RecoveryPath)!,"*.report.txt").Length==0,
                "Successful and rejected report exports clean their owned staging files");
        }
        finally{Directory.Delete(folder,true);}
    }
    public static void VerifyPackage(string folder,Action<bool,string> assert)
    {
        string version=typeof(ProgressReportWindow).Assembly.GetName().Version!.ToString(3);
        var evidence=ReportEvidence.Parse(File.ReadAllText(Path.Combine(folder,"build-checks.json")),version);
        string text=DevelopmentReport.Build(version,DateTimeOffset.Now,evidence);
        assert(evidence is not null && text.Contains($"{evidence.CoreChecks} Core/OpenXML và {evidence.WpfChecks} WPF đạt.") &&
            File.Exists(Path.Combine(folder,"ChinhTa.Desktop.exe")),
            "The actual Windows package includes current-version build evidence that the report parser and renderer accept");
        string checklist=File.ReadAllText(Path.Combine(folder,$"SUPPORTED-FIXES-{version}.md"));
        static string MarkdownCell(string value)=>value.Replace("|","\\|").Replace("\r\n","<br>").Replace("\n","<br>");
        assert(DevelopmentReport.Capabilities.All(c=>checklist.Contains($"| {c.Id} | {c.Name} |",StringComparison.Ordinal) &&
            checklist.Contains(MarkdownCell(c.Example.Before),StringComparison.Ordinal) &&
            checklist.Contains(MarkdownCell(c.Example.Outcome),StringComparison.Ordinal)) &&
            DevelopmentReport.PlannedWork.All(p=>checklist.Contains($"| {p.Id} | {p.Name} |",StringComparison.Ordinal) &&
                checklist.Contains(MarkdownCell(p.Example.Before),StringComparison.Ordinal) && checklist.Contains(MarkdownCell(p.Example.Outcome),StringComparison.Ordinal)),
            "The packaged checklist includes every current capability and planned-work ID, name and before/after example");
        assert(File.Exists(Path.Combine(folder,$"OFFICE-CHECK-{version}.md")) &&
            Enumerable.Range(1,4).All(n=>File.Exists(Path.Combine(folder,$"FORMAT-BATCH-{n:00}.md"))),
            "The package includes the Office validation guide and all four formatting-batch notes");
        string exported=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(folder))!,$"ChinhTaViet-{version}-tien-do.txt");
        File.WriteAllText(exported,text,new UTF8Encoding(true));
    }
    [DllImport("kernel32.dll",EntryPoint="CreateHardLinkW",CharSet=CharSet.Unicode,SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string file,string existing,IntPtr attributes);
}
