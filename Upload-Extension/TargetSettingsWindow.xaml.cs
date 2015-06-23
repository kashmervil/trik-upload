using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;

namespace UploadExtension
{
    /// <summary>
    ///     Interaction logic for TargetSettingsWindow.xaml
    /// </summary>
    public partial class TargetSettingsWindow
    {
        public TargetSettingsWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(TargetIp.Text, out ip))
            {
                MessageBox.Show(this, "Incorrect IP address");
                return;
            }
            var login = TargetLogin.Text;
            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show(this, "Incorrect Login");
                return;
            }
            var pass = TargetPass.SecurePassword;

            var targetsWindow = (TargetsWindow) DataContext;
            var targetProfiles = targetsWindow.DataContext as Dictionary<string, TargetProfile>;
            if (targetProfiles == null) return;

            if (targetProfiles.ContainsKey(ip.ToString())) //:TODO replacement notification routine
            {
                MessageBox.Show(this, "Profile exists");
                return;
            }
            targetProfiles[ip.ToString()] = new TargetProfile(ip, login, pass);
            var keys = (ObservableCollection<string>) targetsWindow.ListBoxTargets.ItemsSource;
            keys.Add(ip.ToString());
            Close();
        }

        private void TargetPass_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            PassBlock.Visibility = (TargetPass.SecurePassword.Length == 0) ? Visibility.Visible : Visibility.Hidden;
        }
    }
}