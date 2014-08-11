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

namespace CompilePal
{
    /// <summary>
    /// Interaction logic for GenericMessageBox.xaml
    /// </summary>
    public partial class GenericMessageBox
    {
        public GenericMessageBox(string text)
        {
            InitializeComponent();
            Label.Text = text;
        }
    }
}
