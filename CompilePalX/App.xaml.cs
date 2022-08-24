using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // catch all unhandled exceptions and log them
            AppDomain.CurrentDomain.UnhandledException += (s, err) => { ExceptionHandler.LogException((Exception)err.ExceptionObject, false); };
            DispatcherUnhandledException += (s, err) => { ExceptionHandler.LogException(err.Exception, false); };
            TaskScheduler.UnobservedTaskException += (s, err) => { ExceptionHandler.LogException(err.Exception, false); };

            // set working directory
            Directory.SetCurrentDirectory(Path.GetDirectoryName(AppContext.BaseDirectory));

            // store path in registry
            RegistryManager.Write("Path", AppContext.BaseDirectory);
        }
    }
}
