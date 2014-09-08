using System.Diagnostics;
using System.Windows.Navigation;

namespace CompilePal
{
    /// <summary>
    /// Interaction logic for UpdateNeededWindow.xaml
    /// </summary>
    public partial class UpdateNeededWindow
    {
        public UpdateNeededWindow(string newVersion,string oldVersion)
        {
            InitializeComponent();

            NewLabel.Content = "Newest Version: " + newVersion;
            CurrentLabel.Content = "Current Version: " + oldVersion;
        }

        private void GitHubLink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
        }
    }
}
