using System.Windows;
namespace ChinhTa.Desktop;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if(e.Args.Length>0)
        {
            // An explicit command has no interactive document window. Invalid
            // arguments are rejected before a Word session or sample is opened.
            ShutdownMode=ShutdownMode.OnExplicitShutdown;
            var result=WordCheckCommand.Execute(e.Args);
            Environment.ExitCode=result.ExitCode;
            try{Console.WriteLine(result.Output);}catch(System.IO.IOException){ }
            Shutdown(result.ExitCode);
            return;
        }
        MainWindow=new MainWindow();MainWindow.Show();
    }
}
