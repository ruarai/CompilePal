using System;
using System.ComponentModel;
using System.Windows;

namespace CompilePalX;

/// <summary>
/// Interaction logic for GameConfigurationWindow.xaml
/// </summary>
public partial class GameConfigurationWindow
{
    private static GameConfigurationWindow? instance;
    public static GameConfigurationWindow Instance => instance ??= new GameConfigurationWindow();
    private int? Index;

    private GameConfigurationWindow(GameConfiguration? gc = null)
    {
        InitializeComponent();
        gc ??= new GameConfiguration();
        this.DataContext = gc;
    }

    public void Open(GameConfiguration? gc = null, int? index = null)
    {
        gc ??= new GameConfiguration();
        this.DataContext = gc;
        this.Index = index;
        Show();
        Focus();
    }


    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        // TODO: validate
        // if index is not null, this is an edit
        if (this.Index != null)
            GameConfigurationManager.GameConfigurations[(int)this.Index] = (GameConfiguration)this.DataContext;
        else
            GameConfigurationManager.GameConfigurations.Add((GameConfiguration)this.DataContext);

        GameConfigurationManager.SaveGameConfigurations();
        LaunchWindow.Instance.RefreshGameConfigurationList();
        Close();
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        instance = null;
        base.OnClosing(e);
    }
}