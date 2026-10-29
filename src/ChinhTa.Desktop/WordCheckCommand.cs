using System.IO;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal sealed record WordCheckCommandResult(int ExitCode,string Output,string? Folder=null);

// Explicit synthetic suites only. Argument validation cannot open Word, read a
// supplied document, change the reader mode or silently run every family.
internal static class WordCheckCommand
{
    internal const int Success=0,Failed=1,InvalidArguments=2;
    internal static string Usage=>"Dùng: ChinhTa.Desktop.exe --word-check isolated-spacing,spacing-calibration,technical-text\n"+
        "Chọn rõ mã nhóm, phân cách bằng dấu phẩy; không nhận đường dẫn tài liệu.\n"+
        "Mã nhóm: "+string.Join(",",OfficeFocusedCheck.Cases.Select(s=>s.Id));
    internal static bool TryParse(IReadOnlyList<string> args,out IReadOnlyList<OfficeSuiteCase> selected)
    {
        selected=Array.Empty<OfficeSuiteCase>();
        if(args is null || args.Count!=2 || args[0]!="--word-check" || string.IsNullOrEmpty(args[1]) || args[1].Length>512 ||
            args[1].Any(char.IsWhiteSpace))return false;
        var ids=args[1].Split(',');
        if(ids.Length>OfficeFocusedCheck.Cases.Count || ids.Any(id=>id.Length==0) || ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length)return false;
        var known=OfficeFocusedCheck.Cases.ToDictionary(s=>s.Id,StringComparer.Ordinal);
        if(ids.Any(id=>!known.ContainsKey(id)))return false;
        selected=Array.AsReadOnly(ids.Select(id=>known[id]).ToArray());return true;
    }
    internal static WordCheckCommandResult Execute(IReadOnlyList<string> args,
        Func<IReadOnlyList<OfficeSuiteCase>,OfficeFocusedCheckResult>? run=null)
    {
        if(!TryParse(args,out var selected))return new(InvalidArguments,Usage);
        try
        {
            // Production always uses the existing legacy-reader, separately
            // opened Word sessions and public synthetic samples. Injection is
            // used by regression checks, never selected through command text.
            var completed=(run??OfficeFocusedCheck.Run)(selected);
            if(completed?.Report?.Results is not { } rows)
                return Incomplete();
            var results=rows.ToArray();
            if(results.Length!=selected.Count || results.Where((row,i)=>row is null || row.Suite!=selected[i] ||
                !Enum.IsDefined(row.Status) || row.Status==OfficeSuiteStatus.Pass && row.Error is not null).Any())
                return Incomplete();
            string? folder=SafeFolder(completed.Folder);
            if(folder is null)return Incomplete();
            var report=new OfficeSuiteReport(Array.AsReadOnly(results));
            string output=$"ChinhTaViet {typeof(WordCheckCommand).Assembly.GetName().Version}\nMODE Legacy\n"+
                "SELECTED "+string.Join(",",selected.Select(s=>s.Id))+"\n"+OfficeFocusedCheck.Summary(report)+"\n"+
                (report.Passed?"RESULT SELECTED_PASS":"RESULT SELECTED_FAIL")+"\nBáo cáo phiên kiểm: "+Path.Combine(folder,"office-groups.txt");
            return new(report.Passed?Success:Failed,output,folder);
        }
        catch(Exception)
        {
            // A report-write/initialization error is not PASS. Do not echo raw
            // exception text, command input, document data or process state.
            return new(Failed,"RESULT OPERATION_FAILED\nPhiên kiểm Word chưa hoàn tất hoặc chưa ghi được báo cáo. Không xác nhận nhóm nào đã đạt.\n"+
                OfficeFocusedCheck.Scope);
        }
    }
    private static WordCheckCommandResult Incomplete()=>new(Failed,
        "RESULT INCOMPLETE\nKết quả hoặc thư mục báo cáo chưa khớp đủ các nhóm đã yêu cầu. Không xác nhận nhóm nào đã đạt.\n"+OfficeFocusedCheck.Scope);
    private static string? SafeFolder(string? value)
    {
        if(string.IsNullOrWhiteSpace(value) || value.Length>4096 || value.Any(char.IsControl))return null;
        try
        {
            if(!Path.IsPathFullyQualified(value))return null;
            string full=Path.GetFullPath(value),name=Path.GetFileName(full);
            const string prefix="OfficeGroups-";
            if(!name.StartsWith(prefix,StringComparison.Ordinal) || !Guid.TryParseExact(name[prefix.Length..],"N",out _))return null;
            var comparison=OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal;
            return string.Equals(Path.GetDirectoryName(full),Path.GetDirectoryName(Path.GetFullPath(Diagnostics.LogPath)),comparison)?full:null;
        }
        catch(Exception error) when(error is ArgumentException or IOException or NotSupportedException or System.Security.SecurityException)
        {return null;}
    }
}
