using System.IO;

namespace ChinhTa.Desktop;
public static class Diagnostics
{
    public static string LogPath => Path.Combine(Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData), "ChinhTaViet", "diagnostics.log");
    public static void Write(string operation, Exception? error, string? measurements = null)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            // No document text, names, paths or credentials in diagnostic entries.
            var version = typeof(Diagnostics).Assembly.GetName().Version;
            string entry = $"{DateTimeOffset.Now:O} v{version} {operation} " +
                (error is null ? "event" : $"{error.GetType().Name} HRESULT=0x{error.HResult:X8}\n{error.StackTrace}");
            if (error?.Data["ChinhTa.LineVerification"] is string details) entry += "\n" + details;
            if(measurements is not null)entry+="\n"+measurements;
            File.AppendAllText(LogPath, entry + Environment.NewLine);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

