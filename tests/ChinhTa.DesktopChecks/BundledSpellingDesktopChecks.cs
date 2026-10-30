using System.IO;
using System.Windows.Controls;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class BundledSpellingDesktopChecks
{
    internal static void VerifyPackage(string folder,Action<bool,string> assert)
    {
        var model=VietnameseSpellingDefaults.Load(Path.Combine(folder,"spelling"));
        assert(model.Source.Headwords==72535 && !model.Source.Starter,"Published or extracted application carries the complete verified offline default data and license notices");
        assert(model.Check("Giải quyết dứt điễm vụ việc.").Single().Suggestions.Single()=="điểm","The packaged data actually produces a Vietnamese proposal without importing any ZIP or opening Word");
    }
    public static void Run(Action<bool,string> assert)
    {
        VerifyPackage(AppContext.BaseDirectory,assert);
        var window=new MainWindow();
        try
        {
            var info=(TextBlock)window.FindName("SpellingInfo");
            assert(info.Text.Contains("Bộ kèm ứng dụng v1",StringComparison.Ordinal) && !info.Text.Contains("Chỉ vài nhóm",StringComparison.Ordinal),"The actual fresh UI loads and identifies the bundled dictionary instead of requiring a first-run import");
            assert(window.ManualMode,"Bundling data keeps the default human-authored editing mode");
        }
        finally{window.Close();}
    }
}
