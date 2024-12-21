using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers
{
	//Process does not actually run, only builds list of custom processes
	class CustomProcess : CompileProcess
	{
		public CustomProcess() : base("CUSTOM") { }

		public List<CustomProgram> Programs = [];

		public List<CustomProgram> BuildProgramList()
		{
			Programs = [];
			foreach (var parameter in PresetDictionary[ConfigurationManager.CurrentPreset])
			{
				string path = parameter.Value;
				string args = parameter.Value2;

				//Set default order to 15
				int order = 15;

				//Use warning to hold custom order
				if (!string.IsNullOrWhiteSpace(parameter.Warning))
					Int32.TryParse(parameter.Warning, out order);

				if (string.IsNullOrWhiteSpace(path))
					continue;

				CustomProgram program = new CustomProgram(path, args, parameter.ReadOutput, parameter.WaitForExit, order);

				Programs.Add(program);
			}

			return Programs;
		}
	}

	class CustomProgram : CompileProcess
	{
		public new Process Process { get; set; }

		public new string Name { get; set; }
		public new string Description { get; set; }

		public string Path { get; set; }

		public ProcessStartInfo StartInfo { get; set; }

		public string Args { get; }

		public bool ReadOutput { get; set; }

		public bool WaitForExit { get; set; }

		public int CustomOrder { get; set; }

		public CustomProgram(string path, string args, bool readOutput, bool waitForExit, int customOrder) : base("CUSTOM")
		{
			Path = path;
			Args = args;
			ReadOutput = readOutput;
			WaitForExit = waitForExit;
			CustomOrder = customOrder;
			Name = path.Replace("\\", "/").Replace("\"", "").Split('/').Last();
			Description = "Run program.";
			Draggable = true;
		}

		//Import FindExecutable to find program associated with filetype
		[DllImport("shell32.dll")]
		static extern int FindExecutable(string lpFile, string lpDirectory, [Out] StringBuilder lpResult);

		public override void Run(CompileContext c, CancellationToken cancellationToken)
		{
            CompileErrors = [];

            if (!CanRun(c)) return;

			CompilePalLogger.LogLine("\nCompilePal - " + Path);

			//Find filepath of program associated with filetype
			//This is similar to using shellexecute, except we can read the output
			StringBuilder programPath = new StringBuilder();
			Path = ParseArgs(Path, c);
			int result = FindExecutable(Path, null, programPath);

			//Result code <= is an error
			if (result <= 32)
			{
				//TODO switch to error logs
				switch (result)
				{
					case 2:
						CompilePalLogger.LogCompileError($"File not found: {Path}\n", new Error($"File not found: {Path}\n", ErrorSeverity.Error));
						break;
					case 3:
						CompilePalLogger.LogCompileError($"Path is invalid: {Path}\n", new Error($"Path is invalid: {Path}\n", ErrorSeverity.Error));
						break;
					case 5:
						CompilePalLogger.LogCompileError($"File could not be accessed: {Path}\n", new Error($"File could not be accessed: {Path}\n", ErrorSeverity.Error));
						break;
					case 31:
						CompilePalLogger.LogCompileError($"There is no program associated with this filetype: {Path}\n", new Error($"There is no program associated with this filetype: {Path}\n", ErrorSeverity.Error));
						break;
				}
				return;
			}

			string parsedArgs = ParseArgs(Args, c);
			// Python files require the filename to be the first arg, otherwise it just opens python
			if (Path.EndsWith(".py"))
				parsedArgs = parsedArgs.Insert(0, Path);

			StartInfo = new ProcessStartInfo
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				FileName = programPath.ToString(),
				Arguments = parsedArgs
			};

			if (ReadOutput)
			{
				StartInfo.RedirectStandardOutput = true;
				StartInfo.RedirectStandardInput = true;
				StartInfo.RedirectStandardError = true;
			}


            // listen for cancellations
            cancellationToken.Register(() =>
            {
                try
                {
                    if (ReadOutput)
                    {
                        Process.OutputDataReceived -= ProcessOnOutputDataReceived;
                        Process.ErrorDataReceived -= ProcessOnErrorDataReceived;
                    }
                    Cancel();
                }
                catch (InvalidOperationException) { }
                catch (Exception e) { ExceptionHandler.LogException(e); }
            });

            try
            {
                Process = new Process()
                {
                    StartInfo = StartInfo
                };

                Process.Start();
                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();

                if (ReadOutput)
                {
                    Process.OutputDataReceived += ProcessOnOutputDataReceived;
                    Process.ErrorDataReceived += ProcessOnErrorDataReceived;
                }

                if (WaitForExit)
                {
                    Process.WaitForExit();
                    Process.Close();
					if (cancellationToken.IsCancellationRequested) return;
                    CompilePalLogger.LogLine("\nProgram completed successfully\n");
                }
                else
                {
                    // run async
                    Task.Run(() =>
                    {
                        Process.WaitForExit();
                        Process.Close();
                        if (cancellationToken.IsCancellationRequested) return;
                        CompilePalLogger.LogLine("\nProgram completed successfully\n");
                    });
                }
            }
            catch (Exception e)
            {
                CompilePalLogger.LogCompileError($"Failed to run {Path}", new Error($"Failed to run {Path}", ErrorSeverity.Error));
				CompilePalLogger.LogCompileError(e.ToString(), new Error(e.ToString(), ErrorSeverity.Error));
            }
		}

		private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
				CompilePalLogger.LogLineColor(e.Data, Error.GetSeverityBrush(4));
		}

		private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
		{
            if (e.Data != null)
                CompilePalLogger.LogLine(e.Data);
		}

		//Parse args for parameters and replace them with their corresponding values
		//Paramaters from https://developer.valvesoftware.com/wiki/Command_Sequences
		private string ParseArgs(string originalArgs, CompileContext c)
		{
			string args = originalArgs.Replace("$file", $"{System.IO.Path.GetFileNameWithoutExtension(c.MapFile)}");
			args = args.Replace("$ext", $"{System.IO.Path.GetExtension(c.MapFile)}");
			args = args.Replace("$path", $"{System.IO.Path.GetDirectoryName(c.MapFile)}");

			args = args.Replace("$exedir", $"{System.IO.Path.GetDirectoryName(c.Configuration.GameEXE)}");
			args = args.Replace("$bspdir", $"{c.Configuration.MapFolder}\\");
			args = args.Replace("$gamedir", $"{c.Configuration.GameFolder}");
			args = args.Replace("$bindir", $"{c.Configuration.BinFolder}");

			args = args.Replace("$bsp_exe", $"{c.Configuration.VBSP}");
			args = args.Replace("$vis_exe", $"{c.Configuration.VVIS}");
			args = args.Replace("$light_exe", $"{c.Configuration.VRAD}");
			args = args.Replace("$game_exe", $"{c.Configuration.GameEXE}");
			CompilePalLogger.LogLine("Args: " + args);
			return args;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			if (obj is CustomProgram program)
				return Equals(program);

			if (obj is ConfigItem config)
				return Equals(config);

			return ReferenceEquals(this, obj);
		}

		protected bool Equals(CustomProgram other)
		{
			if (other == null)
				return false;

			return Equals(Process, other.Process) && string.Equals(Name, other.Name) && string.Equals(Description, other.Description) && string.Equals(Path, other.Path) && Equals(StartInfo, other.StartInfo) && string.Equals(Args, other.Args) && ReadOutput == other.ReadOutput && CustomOrder == other.CustomOrder;
		}

		protected bool Equals(ConfigItem other)
		{
			if (other == null)
				return false;

			return (ReadOutput == other.ReadOutput && string.Equals(Path, other.Value) && string.Equals(CustomOrder.ToString(), other.Warning) && Equals(Args, other.Value2));
		}

		public override string ToString()
		{
			return Name;
		}
	}

	//public static class StringExtension
	//{
	//	//Finds first instance of search character and replaces it
	//	public static string ReplaceFirst(this string str, string searchString, string replaceString)
	//	{
	//		int index = str.IndexOf(searchString);
	//		return str.Remove(index, searchString.Length).Insert(index, replaceString);
	//	}
	//}
}
