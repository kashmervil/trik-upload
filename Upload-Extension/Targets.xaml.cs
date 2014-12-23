using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Trik.Upload_Extension
{
    /// <summary>
    /// Interaction logic for Targets.xaml
    /// </summary>
    public partial class Targets : Window
    {
        public Targets()
        {
            InitializeComponent();
        }

        private void AddTargetButton_OnClick(object sender, RoutedEventArgs e)
        {
            var addTarget = new NewTargetWindow {DataContext = ListBoxTargets.ItemsSource};
            addTarget.ShowDialog();

        }

        private void DeleteTargetButton_OnClick(object sender, RoutedEventArgs e)
        {
            var targets = ListBoxTargets.ItemsSource as ObservableCollection<string>;
            if (targets != null) targets.Remove(ListBoxTargets.SelectedItem as string);
        }
    }
}
