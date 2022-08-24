using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Navigation;

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

        private void ErrorBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (firstLoad)
            {
                firstLoad = false;
                return;
            }
            // cancel navigation to the clicked link in the webBrowser control
            e.Cancel = true;

            var url = e.Uri.ToString();

            if (url.StartsWith("about:forum"))
            {
                url = url.Replace("about:forum", "http://www.interlopers.net/forum");
            }

            if (url.StartsWith("about:tutorials"))
            {
                url = url.Replace("about:forum", "http://www.interlopers.net/tutorials");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = url
            };

            Process.Start(startInfo);
        }
    }
}
