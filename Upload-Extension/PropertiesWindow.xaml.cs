using System.Linq;
using System.Windows;

namespace UploadExtension
{
    /// <summary>
    ///     Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow
    {
        public PropertiesWindow()
        {
            InitializeComponent();
        }

        private void ButtonClose_OnClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonOkay_Click(object sender, RoutedEventArgs e)
        {
            var solutionManager = DataContext as SolutionManager;
            if (solutionManager != null)
                solutionManager.ActiveProject =
                    solutionManager.Projects.First(x => x.ProjectName == (string) ComboBox.SelectedItem);
            Close();
        }
    }
}