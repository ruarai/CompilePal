using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CompilePal
{
    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text,Brush brush)
        {
            var tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd) {Text = text};
            tr.ApplyPropertyValue(TextElement.ForegroundProperty,brush);
        }
    }
}
