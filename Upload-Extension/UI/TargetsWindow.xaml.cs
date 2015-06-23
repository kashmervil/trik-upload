using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace UploadExtension
{
    /// <summary>
    ///     Interaction logic for TargetsWindow.xaml
    /// </summary>
    public partial class TargetsWindow
    {
        public TargetsWindow()
        {
            InitializeComponent();
        }

        private void AddTargetButton_OnClick(object sender, RoutedEventArgs e)
        {
            var addTarget = new TargetSettingsWindow {DataContext = this};
            addTarget.ShowDialog();
        }

        private void DeleteTargetButton_OnClick(object sender, RoutedEventArgs e)
        {
            var targets = DataContext as Dictionary<string, TargetProfile>;
            if (targets == null) return;
            var itemToRemove = ListBoxTargets.SelectedItem as string;
            if (itemToRemove != null)
            {
                targets.Remove(itemToRemove);
                ((ObservableCollection<string>) ListBoxTargets.ItemsSource).Remove(itemToRemove);
            }
        }
    }
}