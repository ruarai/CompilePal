using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers.BSPPack
{
    /// <summary>
    /// Interaction logic for ConflictWindow.xaml
    /// </summary>
    //TODO save settings for showing advanced info so user doesnt have to open it every time
    public partial class ConflictWindow
    {
        private List<ParticleConflict> pc;

        private FileInfo file1;
        private FileInfo file2;

        private int index = 0;
        private int oldHeight = 625;

        public List<PCF> selectedPCFS;

        public ConflictWindow(List<ParticleConflict> particleConflict)
        {
            pc = particleConflict;
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

            fileLeft.Document.Blocks.Clear();
            fileRight.Document.Blocks.Clear();

            particlesLeft.Document.Blocks.Clear();
            particlesRight.Document.Blocks.Clear();

            //Add files to file selection box
            fileBox.Items.Add(pc[index].conflictingFiles.Item1);
            fileBox.Items.Add(pc[index].conflictingFiles.Item2);

            //Get filename by splitting string by / and taking the last value
            string[] temp = pc[index].conflictingFiles.Item1.Split('/');
            string conflictFileName1 = temp[temp.Length - 1].Replace(".pcf", "");

            temp = pc[index].conflictingFiles.Item2.Split('/');
            string conflictFileName2 = temp[temp.Length - 1].Replace(".pcf", "");


            //Fill in filenames above particle name list
            Paragraph fileLeftPath = new Paragraph(new Run(conflictFileName1 + " particles"));
            fileLeftPath.TextAlignment = TextAlignment.Center;
            fileLeft.Document.Blocks.Add(fileLeftPath);

            Paragraph fileRightPath = new Paragraph(new Run(conflictFileName2 + " particles"));
            fileRightPath.TextAlignment = TextAlignment.Center;
            fileRight.Document.Blocks.Add(fileRightPath);

            //Get file info
            try
            {
                file1 = new FileInfo(pc[index].conflictingFiles.Item1);
                file2 = new FileInfo(pc[index].conflictingFiles.Item2);
            }
            catch (FileNotFoundException fileNotFoundException)
            {
                CompilePalLogger.LogLine(fileNotFoundException.Message);
                throw;
            }

            //Fill in file sizes
            Paragraph fileLeftLength = new Paragraph(new Run(file1.Length / 1000 + " KB"))
            {
                TextAlignment = TextAlignment.Center
            };
            fileLeft.Document.Blocks.Add(fileLeftLength);

            Paragraph fileRightLength = new Paragraph(new Run(file2.Length / 1000 + " KB"))
            {
                TextAlignment = TextAlignment.Center
            };
            fileRight.Document.Blocks.Add(fileRightLength);

            //Add particle names from pcf into comparision list. Highlight conflicting names in red
            foreach (string particleName in pc[index].pcfs.Item1.ParticleNames)
            {
                if (pc[index].conflictingNames.Contains(particleName))
                {
                    Paragraph conflictParagraph = new Paragraph(new Run(particleName));
                    conflictParagraph.Foreground = Brushes.Crimson;

                    particlesLeft.Document.Blocks.Add(conflictParagraph);
                }
                else
                {
                    Paragraph normalParagraph = new Paragraph(new Run(particleName));
                    particlesLeft.Document.Blocks.Add(normalParagraph);
                }
            }

            foreach (string particleName in pc[index].pcfs.Item2.ParticleNames)
            {
                //Highlight conflicting particles in red text
                if (pc[index].conflictingNames.Contains(particleName))
                {
                    Paragraph conflictParagraph = new Paragraph(new Run(particleName));
                    conflictParagraph.Foreground = Brushes.Crimson;

                    particlesRight.Document.Blocks.Add(conflictParagraph);
                }
                else
                {
                    Paragraph normalParagraph = new Paragraph(new Run(particleName));
                    particlesRight.Document.Blocks.Add(normalParagraph);
                }
            }
        }

        //Toggle between showing advanced info and hiding it
        private void ShowInfo(object sender, RoutedEventArgs e)
        {
            //Show advanced info and expand window
            if (advancedButton.IsChecked != null && (bool) advancedButton.IsChecked)
            {
                advancedInfo.Visibility = Visibility.Visible;
                advancedInfoTip.Visibility = Visibility.Visible;
                advancedButton.Content = "Hide Advanced Information";

                Height = oldHeight;
                MinHeight = 625;
                ResizeMode = ResizeMode.CanResize;
            }
            else //Hide advanced info and shrink winodw
            {
                advancedInfo.Visibility = Visibility.Collapsed;
                advancedInfoTip.Visibility = Visibility.Collapsed;
                advancedButton.Content = "Show Advanced Information";

                //Store previous height
                oldHeight = (int)Height;

                MinHeight = 250;
                Height = 250;
                ResizeMode = ResizeMode.CanMinimize;
            }
        }

        //Store selection and move to next conflict
        private void SelectClicked(object sender, RoutedEventArgs e)
        {
            if (fileBox.SelectedItems.Count == 0)
                return;

            PCF removedConflictPCF = new PCF();
            List<ParticleConflict> newPc = new List<ParticleConflict>();

            if (fileBox.SelectedItems.Count != 2)
            {
                foreach (var selectedItem in fileBox.SelectedItems)
                {
                    string itemPath = selectedItem as string;

                    //Determine which pcf corresponds to the selected filepath
                    if (pc[index].pcfs.Item1.FilePath == itemPath)
                    {
                        selectedPCFS.Add(pc[index].pcfs.Item1);
                        pc.RemoveAt(0);
                        removedConflictPCF = pc[index].pcfs.Item2;
                    }
                    else if (pc[index].pcfs.Item2.FilePath == itemPath)
                    {
                        selectedPCFS.Add(pc[index].pcfs.Item2);
                        pc.RemoveAt(0);
                        removedConflictPCF = pc[index].pcfs.Item1;
                    }

                }
            }
            else
            {
                selectedPCFS.Add(pc[index].pcfs.Item1);
                selectedPCFS.Add(pc[index].pcfs.Item2);
                pc.RemoveAt(0);
            }

            //pc = newPc;
            Console.WriteLine();
            foreach (ParticleConflict conflict in pc)
            {
                Console.WriteLine("Conflict: " + conflict.conflictingFiles.Item1 + " and " + conflict.conflictingFiles.Item2);
            }

            newPc = new List<ParticleConflict>();

            //Remove all conflicts that contain the conflict that was not selected
            foreach (ParticleConflict conflict in pc)
            {
                if (conflict.pcfs.Item1 != removedConflictPCF && conflict.pcfs.Item2 != removedConflictPCF)
                {
                    newPc.Add(conflict);
                    //if ()
                }
            }

            

            pc = newPc;

            if (index >= pc.Count - 1)
            {
                Close();
                return;
            }
                        
            //Increment index and refresh values
            //index += 1;
            Setup();
        }
    }
}
