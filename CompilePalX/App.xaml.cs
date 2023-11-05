using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CompilePalX.Compiling;
using Microsoft.Win32;

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

            // force invariant culture so stack traces are always in english
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            // set working directory
            Directory.SetCurrentDirectory(Path.GetDirectoryName(AppContext.BaseDirectory));

            // store path in registry
            RegistryManager.Write("Path", AppContext.BaseDirectory);
        }
    }
}
