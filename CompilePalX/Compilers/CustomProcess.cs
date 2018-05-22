using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers
{
	//Process does not actually run, only builds list of custom processes
	class CustomProcess : CompileProcess
	{
		public CustomProcess() : base("CUSTOM") { }

		public List<CustomProgram> Programs = new List<CustomProgram>();

		public List<CustomProgram> BuildProgramList()
		{
			Programs = new List<CustomProgram>();
			foreach (var parameter in PresetDictionary[ConfigurationManager.CurrentPreset])
			{
				string path = parameter.Value;
				string args = parameter.Value2;

				//Set default order to 15
				int order = 15;

				//Use parameter to hold custom order
				if (!string.IsNullOrWhiteSpace(parameter.Parameter))
					Int32.TryParse(parameter.Parameter, out order);

				if (string.IsNullOrWhiteSpace(path))
					continue;

				CustomProgram program = new CustomProgram(path, args, parameter.ReadOutput, order);

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

		public int CustomOrder { get; set; }

		public CustomProgram(string path, string args, bool readOutput, int customOrder) : base("CUSTOM")
		{
			Path = path;
			Args = args;
			ReadOutput = readOutput;
			CustomOrder = customOrder;
			Name = path.Replace("\\", "/").Replace("\"", "").Split('/').Last();
			Description = "Run program.";
		}

		//Import FindExecutable to find program associated with filetype
		[DllImport("shell32.dll")]
		static extern int FindExecutable(string lpFile, string lpDirectory, [Out] StringBuilder lpResult);

		public override void Run(CompileContext c)
		{
			CompilePalLogger.LogLine("\nCompilePal - " + Path);

			//Find filepath of program associated with filetype
			//This is similar to using shellexecute, except we can read the output
			StringBuilder programPath = new StringBuilder();
			int result = FindExecutable(Path, null, programPath);

			//Result code <= is an error
			if (result <= 32)
			{
				//TODO switch to error logs
				switch (result)
				{
					case 2:
						CompilePalLogger.LogLine("ERROR! The specified file not found: \n" + Path);
						break;
					case 3:
						CompilePalLogger.LogLine("ERROR! The specified path is invalid: \n" + Path);
						break;
					case 5:
						CompilePalLogger.LogLine("ERROR! The specified file cannot be accessed: \n" + Path);
						break;
					case 31:
						CompilePalLogger.LogLine("ERROR! There is no program asscociated with this filetype: \n" + Path);
						break;
				}
				return;
			}

			StartInfo = new ProcessStartInfo
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				FileName = programPath.ToString(),
				Arguments = Path + " " + Args
			};

			if (ReadOutput)
			{
				StartInfo.RedirectStandardOutput = true;
				StartInfo.RedirectStandardInput = true;
				StartInfo.RedirectStandardError = true;
				StartInfo.UseShellExecute = false;
				ReadOutput = true;

			}

			Process = new Process()
			{
				StartInfo = StartInfo
			};

			Process.Start();

			if (ReadOutput)
				readOutput();

			//TODO maybe add limit to how long programs can run for programs that dont exit on their own
			Process.WaitForExit();
			CompilePalLogger.LogLine("Program completed sucesfully\n");

		}

		private void readOutput()
		{
			char[] buffer = new char[256];
			Task<int> read = null;
			while (true)
			{
				if (read == null)
					read = Process.StandardOutput.ReadAsync(buffer, 0, buffer.Length);

				read.Wait(100); // an arbitray timeout

				if (read.IsCompleted)
				{
					if (read.Result > 0)
					{
						string text = new string(buffer, 0, read.Result);

						CompilePalLogger.ProgressiveLog(text);

						read = null; // ok, this task completed so we need to create a new one
						continue;
					}

					// got -1, process ended
					break;
				}
			}
			Process.WaitForExit();
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

			return (ReadOutput == other.ReadOutput && string.Equals(Path, other.Value) && string.Equals(CustomOrder.ToString(), other.Parameter) && Equals(Args, other.Value2));
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (Process != null ? Process.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Path != null ? Path.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (StartInfo != null ? StartInfo.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Args != null ? Args.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ ReadOutput.GetHashCode();
				hashCode = (hashCode * 397) ^ CustomOrder;
				return hashCode;
			}
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
