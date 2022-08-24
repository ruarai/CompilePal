using System.ComponentModel;
using System.Windows;

namespace CompilePalX
{

    /// <summary>
    /// Interaction logic for GameConfigurationWindow.xaml
    /// </summary>
    public partial class GameConfigurationWindow
    {
        private static GameConfigurationWindow? instance;
        private int? index;

        private GameConfigurationWindow(GameConfiguration? gc = null)
        {
            InitializeComponent();
            gc ??= new GameConfiguration();
            DataContext = gc;
        }
        public static GameConfigurationWindow Instance => instance ??= new GameConfigurationWindow();

        public void Open(GameConfiguration? gc = null, int? index = null)
        {
            gc ??= new GameConfiguration();
            DataContext = gc;
            this.index = index;
            Show();
            Focus();
        }


        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            // TODO: validate
            // if index is not null, this is an edit
            if (index != null)
            {
                GameConfigurationManager.GameConfigurations[(int)index] = (GameConfiguration)DataContext;
            }
            else
            {
                GameConfigurationManager.GameConfigurations.Add((GameConfiguration)DataContext);
            }

            GameConfigurationManager.SaveGameConfigurations();
            LaunchWindow.Instance?.RefreshGameConfigurationList();
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            instance = null;
            base.OnClosing(e);
        }
    }
}
