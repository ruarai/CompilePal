using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
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

		public static readonly DependencyProperty HintProperty =
			DependencyProperty.Register(
				"Hint",
				typeof(string),
				typeof(FileInput),
				new PropertyMetadata("Choose File"));

        public string Hint
        {
            get => (string) GetValue(HintProperty);
            set => SetValue(HintProperty, value);
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
				using (var folderDialog = new CommonOpenFileDialog()
				{
					Title = "Select Folder",
					IsFolderPicker = true,
					InitialDirectory = GameConfigurationManager.GameConfiguration.GameFolder,
				})
				{
					var folderPath = "";
					var result = folderDialog.ShowDialog();
                    if (result == CommonFileDialogResult.Cancel)
                        return;

					folderPath = folderDialog.FileName;

					if (string.IsNullOrWhiteSpace(folderPath))
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
					Title = FileDialogTitle,
                };

				fileDialog.ShowDialog();
				var filePath = fileDialog.FileName;;

				if (string.IsNullOrWhiteSpace(filePath))
					return;

				textBox.Text = filePath;
			}

		}
	}
}
