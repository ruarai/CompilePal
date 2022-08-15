using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// Interaction logic for ParameterAdder.xaml
    /// </summary>
    public partial class ParameterAdder
    {
        public ConfigItem ChosenItem;
        public ParameterAdder(ObservableCollection<ConfigItem> configItems )
        {
            InitializeComponent();
            ICollectionView paramView = CollectionViewSource.GetDefaultView(configItems);
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
            DependencyObject? dep = e.OriginalSource as DependencyObject;
            while ((dep != null) && !(dep is GroupItem) && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            // ignore if double click came from group item
            if (dep is GroupItem)
                return;

            ChosenItem = (ConfigItem) ConfigDataGrid.SelectedItem;

            Close();
        }
    }
}
