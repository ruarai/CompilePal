using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CompilePalX.Compiling
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow
    {
        private bool firstLoad = true;
        public ErrorWindow(Error error)
        {
            InitializeComponent();

            ErrorBrowser.Navigating += ErrorBrowser_Navigating;

            // extract values from error message using regex and insert them into the template  
            var html = error.Message;
            var i = 0;
            foreach (Group group in Regex.Match(error.ShortDescription, error.RegexTrigger.ToString()).Groups)
            {
                // first group is always the entire match, ignore it
                if (i == 0)
                {
                    i++;
                    continue;
                }

                html = html.Replace($"[sub:{i}]", group.Value);
                i++;
            }

            ErrorBrowser.NavigateToString(html);
        }

        void ErrorBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (firstLoad)
            {
                firstLoad = false;
                return;
            }
            // cancel navigation to the clicked link in the webBrowser control
            e.Cancel = true;

            string url = e.Uri.ToString();

            if(url.StartsWith("about:forum"))
                url = url.Replace("about:forum", "http://www.interlopers.net/forum");

            if (url.StartsWith("about:tutorials"))
                url = url.Replace("about:forum", "http://www.interlopers.net/tutorials");

            var startInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }

    }
}
