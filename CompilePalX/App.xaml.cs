using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CompilePalX.Compiling;

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		// catch all unhandled exceptions and log them
	    protected override void OnStartup(StartupEventArgs e)
	    {
		    AppDomain.CurrentDomain.UnhandledException += (s, err) => { ExceptionHandler.LogException((Exception)err.ExceptionObject, false); };

		    DispatcherUnhandledException += (s, err) => { ExceptionHandler.LogException(err.Exception, false); };

		    TaskScheduler.UnobservedTaskException += (s, err) => { ExceptionHandler.LogException(err.Exception, false); };

            // set working directory
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
	    }
    }
}
