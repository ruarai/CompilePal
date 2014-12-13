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
    /// Interaction logic for DialogBox.xaml
    /// </summary>
    public partial class DialogBox
    {
        public string TextReturned = "";
        public DialogBox(string messageText)
        {
            InitializeComponent();
            MessageText.Text = messageText;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TextReturned = InputText.Text;
        }

        private void InputText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Close();
        }
    }
}
