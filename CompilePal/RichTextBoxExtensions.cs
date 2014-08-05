using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CompilePal
{
    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text,Brush brush)
        {
            TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
            tr.Text = text;
            tr.ApplyPropertyValue(TextElement.ForegroundProperty,brush);
        }
    }
}
