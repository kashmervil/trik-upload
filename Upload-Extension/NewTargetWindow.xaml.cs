using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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

namespace Trik.Upload_Extension
{
    /// <summary>
    /// Interaction logic for NewTargetWindow.xaml
    /// </summary>
    public partial class NewTargetWindow : Window
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TargetIP_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
