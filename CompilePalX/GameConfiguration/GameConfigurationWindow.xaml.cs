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

namespace CompilePalX;

/// <summary>
/// Interaction logic for GameConfigurationWindow.xaml
/// </summary>
public partial class GameConfigurationWindow
{
    public GameConfigurationWindow(GameConfiguration? gc = null)
    {
        InitializeComponent();
        gc ??= new GameConfiguration();
        this.DataContext = gc;
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        // TODO: validate
        GameConfigurationManager.GameConfigurations.Add((GameConfiguration)this.DataContext);
        GameConfigurationManager.SaveGameConfigurations();
        LaunchWindow.Instance.RefreshGameConfigurationList();
        Close();
    }
}