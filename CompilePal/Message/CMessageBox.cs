using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilePal.Message
{
    class CMessageBox
    {
        public static void Show(string text)
        {
            GenericMessageBox messageBox = new GenericMessageBox(text);
            messageBox.Show();
        }
        public static void Show(string text,string header)
        {
            GenericMessageBox messageBox = new GenericMessageBox(text);
            messageBox.Content = header;
            messageBox.Show();
        }
    }
}
