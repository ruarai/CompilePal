using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for PresetDialog.xaml
    /// </summary>
    public partial class PresetDialog
    {
        public bool Result = false;
        public bool IsMapSpecific { get { return IsMapSpecificCheckbox.IsChecked ?? false; } }
        public PresetDialog(string title, Map? selectedMap, Preset? preset = null)
        {
            InitializeComponent();
            Title = title;

            string? mapRegex = null;
            if (selectedMap is not null)
            {
                mapRegex = $"{selectedMap.MapName}.*";
            }

            if (preset != null)
            {
                DataContext = preset;
                IsMapSpecificCheckbox.IsChecked = preset.Map != null;
            }
            else
            {
                DataContext = new Preset()
                {
                    Name = "",
                    Map = selectedMap?.MapName,
                    MapRegex = mapRegex
                };
            }
        }

        private void OKButton_OnClick(object sender, RoutedEventArgs e)
        {
            // clear map if not map specific
            if (!IsMapSpecific)
            {
                ((Preset)DataContext).MapRegex = null;
                ((Preset)DataContext).Map = null;
            }
            Result = true;
            Close();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
