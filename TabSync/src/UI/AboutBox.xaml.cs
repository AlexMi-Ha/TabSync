using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace TabSync.src.UI {
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutBox : Window
    {
        public AboutBox()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            using (Process p = new Process()) {
                p.StartInfo.FileName = e.Uri.AbsoluteUri;
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }
            e.Handled = true;
        }
    }
}
