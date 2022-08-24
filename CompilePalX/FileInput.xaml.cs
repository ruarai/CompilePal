using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

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

        public static readonly DependencyProperty FileFilterProperty =
            DependencyProperty.Register(
                "FileFilter",
                typeof(string),
                typeof(FileInput),
                new PropertyMetadata(""));

        public static readonly DependencyProperty FileDialogTitleProperty =
            DependencyProperty.Register(
                "FileDialogTitle",
                typeof(string),
                typeof(FileInput),
                new PropertyMetadata(""));

        public static readonly DependencyProperty IsFolderProperty =
            DependencyProperty.Register(
                "IsFolder",
                typeof(bool),
                typeof(FileInput),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(
                "Hint",
                typeof(string),
                typeof(FileInput),
                new PropertyMetadata("Choose File"));

        public FileInput()
        {
            InitializeComponent();
        }

        public ConfigItem Config
        {
            get => (ConfigItem)GetValue(ConfigItemProperty);
            set => SetValue(ConfigItemProperty, value);
        }

        public string FileFilter
        {
            get => (string)GetValue(FileFilterProperty);
            set => SetValue(FileFilterProperty, value);
        }

        public string FileDialogTitle
        {
            get => (string)GetValue(FileDialogTitleProperty);
            set => SetValue(FileDialogTitleProperty, value);
        }

        public bool IsFolder
        {
            get => (bool)GetValue(IsFolderProperty);
            set => SetValue(IsFolderProperty, value);
        }

        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        private void FileBrowse_OnClick(object sender, RoutedEventArgs e)
        {
            if (IsFolder)
            {
                // create new folder dialog
                using (var folderDialog = new CommonOpenFileDialog
                {
                    Title = "Select Folder", IsFolderPicker = true, InitialDirectory = GameConfigurationManager.GameConfiguration.GameFolder
                })
                {
                    var folderPath = "";
                    var result = folderDialog.ShowDialog();
                    if (result == CommonFileDialogResult.Cancel)
                    {
                        return;
                    }

                    folderPath = folderDialog.FileName;

                    if (string.IsNullOrWhiteSpace(folderPath))
                    {
                        return;
                    }

                    textBox.Text = folderPath;
                }
            }
            else
            {
                // create new file dialog
                var fileDialog = new OpenFileDialog
                {
                    Multiselect = false, Filter = FileFilter, CheckFileExists = false, Title = FileDialogTitle
                };

                var filePath = "";
                fileDialog.ShowDialog();
                filePath = fileDialog.FileName;

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return;
                }

                textBox.Text = filePath;
            }

        }
    }
}
