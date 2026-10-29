using System.IO;
using System.Reflection;
using System.Windows.Controls;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class CoverageReportExportChecks
{
    public sealed class Document
    {
        public int Saves,Closes;
        public void Save(){Saves++;throw new Exception("Coverage export must not save Word");}
        public void Close(int changes){if(changes!=0)throw new Exception("Synthetic close must not save");Closes++;}
    }
    private static object? Field(WordSession session,string name)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session);
    private static void Field(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);
    private static void Property(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.SetValue(session,value);

    public static void Run(Action<bool,string> assert)
    {
        const string secret="Private coverage document content 6d9c24";const int nativeStart=192837465;
        var snapshot=DocumentCoverageBuilder.Build(new(DocumentProfile.State,[new(nativeStart,secret)],[],true,true));
        var plain=new DocumentCoverageWindow(snapshot);
        try
        {
            assert(!plain.ExportButton.IsEnabled && plain.ExportStatus.Text.Length==0 && plain.ReportText==snapshot.ToText(),
                "A coverage window without an exporter remains a read-only copyable snapshot and cannot advertise an unbound save action");
        }
        finally{plain.Close();}

        string folder=Path.Combine(Path.GetTempPath(),"ctv-coverage-export-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
        DocumentCoverageWindow? window=null;
        try
        {
            string source=Path.Combine(folder,"source.docx"),undo=Path.Combine(owned,"prior.undo.docx");
            File.WriteAllText(source,"immutable source");File.WriteAllText(session.RecoveryPath,"unverified working bytes");
            File.WriteAllText(undo,"verified prior action");Property(session,"Source",source);Property(session,"Changed",true);
            Field(session,"snapshotCurrent",false);var checkpoints=(Stack<string>)Field(session,"undoCheckpoints")!;checkpoints.Push(undo);
            var document=new Document();Field(session,"document",document);
            int writes=0;string? payload=null;
            window=new(snapshot,(path,text)=>{writes++;payload=text;session.SaveProgressReport(path,text);});
            object rows=window.Table.ItemsSource;string captured=window.ReportText;
            var later=snapshot.WithExternalEvidence("C33",new(CoverageState.Observed,1,1,"Later completed observation"));
            assert(window.ExportButton.IsEnabled && writes==0 && window.ExportStatus.Text.Length==0 &&
                window.Table.IsReadOnly && !window.Table.CanUserAddRows && !window.Table.CanUserDeleteRows,
                "Opening a coverage report exposes TXT export without writing, scanning or changing the read-only table");
            string output=Path.Combine(folder,"pham-vi-kiem-tra.txt");
            var result=window.ExportTo(output);byte[] published=File.ReadAllBytes(output);
            assert(result is null && writes==1 && payload==captured && File.ReadAllText(output)==captured &&
                published.Take(3).SequenceEqual(new byte[]{0xef,0xbb,0xbf}) && window.ExportStatus.Text.Contains(output,StringComparison.Ordinal),
                "The coverage window publishes its exact frozen Vietnamese UTF-8 BOM payload through the real verified report writer and reports the completed destination");
            assert(!captured.Contains(secret,StringComparison.Ordinal) && !captured.Contains(nativeStart.ToString(),StringComparison.Ordinal) &&
                captured.Contains(DocumentCoverageSnapshot.Legend,StringComparison.Ordinal) &&
                later.Rows.Single(row=>row.Id=="C33").State==CoverageState.Observed && window.ReportText==snapshot.ToText() &&
                window.Table.ItemsSource==rows && window.Table.ItemsSource.Cast<DocumentCoverageWindow.DisplayRow>()
                    .Single(row=>row.Id=="C33").State==DocumentCoverageSnapshot.Label(CoverageState.Unavailable),
                "Coverage export retains the original scan snapshot and limits without exporting private text/native positions or borrowing a later completed observation");

            void VerifySession()
            {
                assert(document.Saves==0 && document.Closes==0 && session.IsOpen && session.Changed &&
                    Field(session,"snapshotCurrent") is false && session.Source==source && checkpoints.Count==1 && checkpoints.Peek()==undo &&
                    File.ReadAllText(source)=="immutable source" && File.ReadAllText(session.RecoveryPath)=="unverified working bytes" &&
                    File.ReadAllText(undo)=="verified prior action",
                    "Successful or rejected coverage export never calls Word Save/Close or changes Source, checkpoint trust, dirty state, Undo or source/working bytes");
            }
            VerifySession();
            var privateTarget=Path.Combine(owned,"coverage.txt");File.WriteAllText(privateTarget,"prior private entry");
            foreach(string target in new[]{Path.Combine(folder,"wrong.docx"),privateTarget})
            {
                int before=writes;var failure=window.ExportTo(target);
                assert(failure is InvalidOperationException && writes==before+1 &&
                    window.ExportStatus.Text.StartsWith("Chưa lưu được báo cáo",StringComparison.Ordinal) &&
                    window.Table.ItemsSource==rows && window.ReportText==captured &&
                    (target==privateTarget?File.ReadAllText(privateTarget)=="prior private entry":!File.Exists(target)) &&
                    File.ReadAllBytes(output).SequenceEqual(published),
                    "Invalid extension and disposable-folder report destinations fail without altering the report snapshot, existing publication or protected private entry");
                VerifySession();
            }
            using(var held=new FileStream(output,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                var failure=window.ExportTo(output);
                assert(failure is IOException or UnauthorizedAccessException && File.ReadAllBytes(output).SequenceEqual(published) &&
                    window.ExportStatus.Text.StartsWith("Chưa lưu được báo cáo",StringComparison.Ordinal) &&
                    window.Table.ItemsSource==rows && window.ReportText==captured,
                    "A locked coverage report cannot be truncated or reported as saved, and its visible findings remain intact after publication failure");
                VerifySession();
            }
            assert(window.ExportTo(output) is null && window.ExportStatus.Text.StartsWith("Đã lưu báo cáo",StringComparison.Ordinal) &&
                File.ReadAllBytes(output).SequenceEqual(published) &&
                !Directory.EnumerateFiles(folder,".ChinhTaViet-*.tmp").Any() && !Directory.EnumerateFiles(owned,"*.report.txt").Any(),
                "The same captured report can retry after the lock is removed, with no stale failure message or leftover owned staging files");
            VerifySession();
            session.Dispose();
            assert(document.Closes==1 && File.ReadAllBytes(output).SequenceEqual(published) && Directory.Exists(owned),
                "The exported coverage report survives later session disposal while an unverified working checkpoint stays retained");
        }
        finally
        {
            window?.Close();session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);
            Directory.Delete(folder,true);
        }
        var expected=new IOException("managed coverage writer rejection");int attempts=0;
        var refused=new DocumentCoverageWindow(snapshot,(_,_)=>{attempts++;throw expected;});
        try
        {
            assert(ReferenceEquals(refused.ExportTo("unused.txt"),expected) && attempts==1 && refused.ReportText==snapshot.ToText() &&
                refused.ExportStatus.Text.StartsWith("Chưa lưu được báo cáo",StringComparison.Ordinal),
                "The window retains the exact export failure for its own feedback and performs one attempt without replacing or rescanning the immutable report");
        }
        finally{refused.Close();}
    }
}
