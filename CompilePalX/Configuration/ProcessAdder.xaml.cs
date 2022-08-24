using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for ProcessAdder.xaml
    /// </summary>
    public partial class ProcessAdder
    {
        public ProcessAdder()
        {
            InitializeComponent();

            var processView = CollectionViewSource.GetDefaultView(ConfigurationManager.CompileProcesses);
            using (processView.DeferRefresh())
            {
                processView.GroupDescriptions.Clear();
                processView.GroupDescriptions.Add(new IsCompatiblePropertyGroup());
            }
            ProcessDataGrid.ItemsSource = processView;
        }

        private void ConfigDataGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // walk up dependency tree to make sure click source was not a group header
            var dep = e.OriginalSource as DependencyObject;
            while (dep != null && !(dep is GroupItem) && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            // ignore if double click came from group item
            if (dep is GroupItem)
            {
                return;
            }

            // only close if they actually selected an item
            if (ProcessDataGrid.SelectedItem != null)
            {
                Close();
            }
        }
    }
}
