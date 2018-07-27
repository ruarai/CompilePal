using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;

namespace CompilePalX
{
	/// <summary>
	/// Interaction logic for FileInput.xaml
	/// </summary>
	public partial class FileInput : UserControl
	{
		public static readonly DependencyProperty ConfigItemProperty =
			DependencyProperty.Register(
				"Config",
				typeof(ConfigItem),
				typeof(FileInput),
				new PropertyMetadata(default(ConfigItem)));

		public ConfigItem Config
		{
			get => (ConfigItem)GetValue(ConfigItemProperty);
			set => SetValue(ConfigItemProperty, value);
		}

		public static readonly DependencyProperty FileFilterProperty =
			DependencyProperty.Register(
				"FileFilter",
				typeof(string),
				typeof(FileInput),
				new PropertyMetadata(""));

		public string FileFilter
		{
			get => (string)GetValue(FileFilterProperty);
			set => SetValue(FileFilterProperty, value);
		}

		public static readonly DependencyProperty FileDialogTitleProperty =
			DependencyProperty.Register(
				"FileDialogTitle",
				typeof(string),
				typeof(FileInput),
				new PropertyMetadata(""));

		public string FileDialogTitle
		{
			get => (string)GetValue(FileDialogTitleProperty);
			set => SetValue(FileDialogTitleProperty, value);
		}

		public static readonly DependencyProperty IsFolderProperty =
			DependencyProperty.Register(
				"IsFolder",
				typeof(bool),
				typeof(FileInput),
				new PropertyMetadata(false));

		public bool IsFolder
		{
			get => (bool)GetValue(IsFolderProperty);
			set => SetValue(IsFolderProperty, value);
		}

		public FileInput()
		{
			InitializeComponent();
		}

		private void FileBrowse_OnClick(object sender, RoutedEventArgs e)
		{
			if (IsFolder)
			{
				// create new folder dialog
				// TODO look for a Nuget package for a folder browser that doesnt suck
				using (var folderDialog = new FolderBrowserDialog()
				{
					Description = "Select Folder",
					SelectedPath = GameConfigurationManager.GameConfiguration.GameFolder
				})
				{
					var folderPath = "";
					folderDialog.ShowDialog();
					folderPath = folderDialog.SelectedPath;

					if (String.IsNullOrWhiteSpace(folderPath))
						return;

					textBox.Text = folderPath;
				}
			}
			else
			{
				// create new file dialog
				var fileDialog = new OpenFileDialog()
				{
					Multiselect = false,
					Filter = FileFilter,
					CheckFileExists = false,
					Title = FileDialogTitle
				};

				var filePath = "";
				fileDialog.ShowDialog();
				filePath = fileDialog.FileName;

				if (String.IsNullOrWhiteSpace(filePath))
					return;

				textBox.Text = filePath;
			}

		}
	}
}
