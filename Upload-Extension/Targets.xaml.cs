using System.Collections.ObjectModel;
using System.Windows;

namespace Trik.Upload_Extension
{
    /// <summary>
    ///     Interaction logic for Targets.xaml
    /// </summary>
    public partial class Targets
    {
        public Targets()
        {
            InitializeComponent();
        }

        private void AddTargetButton_OnClick(object sender, RoutedEventArgs e)
        {
            var addTarget = new NewTargetWindow {DataContext = ListBoxTargets.ItemsSource};
            addTarget.ShowDialog();
            Close();
        }

        private void DeleteTargetButton_OnClick(object sender, RoutedEventArgs e)
        {
            var targets = ListBoxTargets.ItemsSource as ObservableCollection<string>;
            if (targets != null) targets.Remove(ListBoxTargets.SelectedItem as string);
        }
    }
}