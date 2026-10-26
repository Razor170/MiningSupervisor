using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MiningSupervisor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly string CrashLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiningSupervisor", "crash.log");

    public App()
    {
        // Without these, a startup failure on another machine just closes the app with no hint why.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ReportCrash(e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WriteCrashLog(e.Exception);
            e.SetObserved();
        };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportCrash(e.Exception);
        e.Handled = true;
        Shutdown(1);
    }

    private static void ReportCrash(Exception? exception)
    {
        WriteCrashLog(exception);
        try
        {
            MessageBox.Show(
                $"MiningSupervisor hit an unexpected error and has to close.\n\n{exception?.Message}\n\nDetails were written to:\n{CrashLogPath}",
                "MiningSupervisor", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
            // The UI itself may be what failed; the log file is still there.
        }
    }

    private static void WriteCrashLog(Exception? exception)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);
            File.AppendAllText(CrashLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {Environment.OSVersion} ({System.Runtime.InteropServices.RuntimeInformation.OSArchitecture})\n{exception}\n\n");
        }
        catch
        {
            // ignore
        }
    }
}
