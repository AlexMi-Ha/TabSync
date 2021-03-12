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
            if(e.Args.Length > 0) {
                FileSystem fsys = new FileSystem(e.Args[0]);
                wnd.InitFileSystem(fsys);
            }
            wnd.Show();
        }
    }
}
