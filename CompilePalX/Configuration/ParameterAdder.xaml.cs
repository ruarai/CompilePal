using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CompilePalX
{
    /// <summary>
    /// Interaction logic for ParameterAdder.xaml
    /// </summary>
    public partial class ParameterAdder
    {
        public ConfigItem ChosenItem;
        public ParameterAdder(ObservableCollection<ConfigItem> configItems)
        {
            InitializeComponent();
            var paramView = CollectionViewSource.GetDefaultView(configItems);
            using (paramView.DeferRefresh())
            {
                paramView.GroupDescriptions.Clear();
                paramView.GroupDescriptions.Add(new IsCompatiblePropertyGroup());
            }
            ConfigDataGrid.ItemsSource = paramView;
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

            ChosenItem = (ConfigItem)ConfigDataGrid.SelectedItem;

            Close();
        }
    }
}
