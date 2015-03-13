using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace Trik.Upload_Extension
{
    /// <summary>
    ///     Interaction logic for NewTargetWindow.xaml
    /// </summary>
    public partial class NewTargetWindow
    {
        public NewTargetWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(TargetIP.Text, out ip))
            {
                MessageBox.Show(this, "Incorrect IP address");
                return;
            }
            var ips = DataContext as ObservableCollection<string>;
            if (ips == null) return;
            if (!ips.Contains(ip.ToString())) ips.Add(ip.ToString());
            Close();
        }

        private void TargetIP_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}