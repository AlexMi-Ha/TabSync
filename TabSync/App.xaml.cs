using System.Threading.Tasks;
using System.Windows;
using TabSync.src.Data;
using TabSync.src.UI;

namespace TabSync {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        void Application_Startup(object sender, StartupEventArgs e) {
            MainWindow wnd = new MainWindow();
            wnd.Show();
            if (e.Args.Length > 0) {
                FileSystem fsys = new FileSystem(e.Args[0]);
                LoadProject(fsys, wnd);
            }
        }

        async void LoadProject(FileSystem fsys, MainWindow wnd) {
            await Task.Delay(1000);  // Let AlphaTab player load for a second THEN load the project
            // Load time prevents a bug which causes the player to freeze
            wnd.InitFileSystem(fsys);
        }
    }
}
