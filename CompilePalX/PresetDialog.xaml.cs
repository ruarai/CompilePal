using System;
using System.Collections.Generic;
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
        public PresetDialog(string title, bool mapSpecificEnabled = false)
        {
            InitializeComponent();
            Title = title;

            IsMapSpecificCheckbox.IsEnabled = mapSpecificEnabled;
            IsMapSpecificCheckbox.IsHitTestVisible = mapSpecificEnabled;
        }

        public bool Result = false;
        public string Text { get { return InputTextBox.Text; } }
        public bool IsMapSpecific { get { return IsMapSpecificCheckbox.IsChecked ?? false; } }

        private void OKButton_OnClick(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Result = true;
                Close();
            }
        }
    }
}
