﻿using System;
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
using ASCIIEncoding = Renci.SshNet.Common.ASCIIEncoding;

namespace Trik.Upload_Extension
{
    /// <summary>
    /// Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window
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
                    solutionManager.Projects.First(x => x.ProjectName == (string)ComboBox.SelectedItem);
            Close();

        }
    }
}
