using System.Windows;
using SimpleWPFGame.Logging;

namespace SimpleWPFGame;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Logger.Initialize("SimpleWPFGame.log");
        Logger.Log("Application starting up", LogLevel.Info);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Log("Application shutting down", LogLevel.Info);
        Logger.Shutdown();
        base.OnExit(e);
    }
}
