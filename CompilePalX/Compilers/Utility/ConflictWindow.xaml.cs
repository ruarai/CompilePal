using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers.UtilityProcess
{
    /// <summary>
    /// Interaction logic for ConflictWindow.xaml
    /// </summary>
    //TODO save settings for showing advanced info so user doesnt have to open it every time
    public partial class ConflictWindow
    {

        private FileInfo file1;
        private FileInfo file2;

        private int oldHeight = 625;
        private readonly List<PCF> pc;
        private readonly Dictionary<string, PCF> pcfDict;

        public List<PCF> selectedPCFS;
        private readonly List<string> targetParticles;

        public ConflictWindow(List<PCF> particleConflict, List<string> _targetParticles)
        {
            pc = particleConflict;
            targetParticles = _targetParticles;

            // Correlate pcfs to their filepaths in a dictionary
            pcfDict = pc.Select(p =>
                {
                    // Normalize filepaths
                    p.FilePath = p.FilePath.Replace('/', '\\');
                    return p;
                })
                // Remove duplicates
                .GroupBy(p => p.FilePath)
                .Select(group => group.First())
                // Convert to dict
                .ToDictionary(k => k.FilePath, v => v);
            selectedPCFS = new List<PCF>();
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Setup();
        }

        //Setup file for each
        private void Setup()
        {
            //cleanup all previous entries
            fileBox.Items.Clear();

            //Add files to file selection box
            foreach (var pcf in pc)
            {
                var headerSize = 25;
                var paragraphSize = 13;

                //Add list of files to filebox
                //Fix seperators to match first part of filepath
                fileBox.Items.Add(pcf.FilePath.Replace('/', '\\'));

                //Get filename by splitting string by / and taking the last value
                var fileNameSplit = pcf.FilePath.Split('/');
                var fileName = fileNameSplit[^1].Replace(".pcf", "");

                //Add advanced info to expanders
                var stack = new Grid();

                var expander = new Expander();
                expander.Header = fileName;
                expander.Content = stack;
                AdvancedSP.Children.Add(expander);

                //Get file info
                FileInfo file;
                try
                {
                    file = new FileInfo(pcf.FilePath);
                }
                catch (FileNotFoundException fileNotFoundException)
                {
                    CompilePalLogger.LogLine(fileNotFoundException.Message);
                    //If file cant be opened skip it
                    continue;
                }

                var fileInfo = new RichTextBox();
                fileInfo.BorderThickness = new Thickness(0);
                fileInfo.Margin = new Thickness(10, 0, 10, 10);
                fileInfo.IsReadOnly = true;
                fileInfo.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                fileInfo.Document.Blocks.Clear();
                stack.Children.Add(fileInfo);

                var header = new Paragraph(new Run("File Information"));
                header.FontSize = headerSize;
                header.Margin = new Thickness(0);
                fileInfo.Document.Blocks.Add(header);

                var fileSizeInfo = new Paragraph(new Run("Filesize: " + file.Length / 1000 + " KB"));
                fileSizeInfo.Margin = new Thickness(0);
                fileSizeInfo.FontSize = paragraphSize;
                fileInfo.Document.Blocks.Add(fileSizeInfo);

                var lastDateModified = File.GetLastWriteTime(pcf.FilePath);

                var fileDateInfo = new Paragraph(new Run("Last Modified: " + lastDateModified));
                fileDateInfo.Margin = new Thickness(0);
                fileDateInfo.FontSize = paragraphSize;
                fileInfo.Document.Blocks.Add(fileDateInfo);

                //Add particle names
                var particleHeader = new Paragraph(new Run("Particles"));
                particleHeader.Margin = new Thickness(0);
                particleHeader.FontSize = headerSize;
                fileInfo.Document.Blocks.Add(particleHeader);

                foreach (var name in pcf.ParticleNames)
                {
                    var particleName = new Paragraph(new Run(name));
                    particleName.Margin = new Thickness(0, 0, 0, 0);
                    particleName.FontSize = paragraphSize;
                    if (targetParticles.Contains(name))
                    {
                        particleName.Foreground = TryFindResource("MahApps.Brushes.Accent") as SolidColorBrush;
                    }

                    fileInfo.Document.Blocks.Add(particleName);
                }

                //Add footer
                var footer = new Paragraph(new Run("Used particles are highlighted"));
                footer.Margin = new Thickness(0);
                footer.FontSize = paragraphSize;
                footer.TextAlignment = TextAlignment.Center;
                fileInfo.Document.Blocks.Add(footer);
            }
        }

        //Toggle between showing advanced info and hiding it
        private void ShowInfo(object sender, RoutedEventArgs e)
        {
            //Show advanced info and expand window
            if (advancedButton.IsChecked != null && (bool)advancedButton.IsChecked)
            {
                advancedInfo.Visibility = Visibility.Visible;
                advancedButton.Content = "Hide Advanced Information";

                Height = oldHeight;
                MinHeight = 625;
            }
            else //Hide advanced info and shrink winodw
            {
                advancedInfo.Visibility = Visibility.Collapsed;
                advancedButton.Content = "Show Advanced Information";

                //Store previous height
                oldHeight = (int)Height;

                MinHeight = 450;
                Height = 450;
            }
        }

        //Store selection and exit window
        private void SelectClicked(object sender, RoutedEventArgs e)
        {
            if (fileBox.SelectedItems.Count == 0)
            {
                return;
            }

            foreach (var selectedItem in fileBox.SelectedItems)
            {
                var itemPath = selectedItem as string;
                selectedPCFS.Add(pcfDict[itemPath]);
            }

            //Close window
            Close();
        }

        //Option to select none
        private void NoneClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
