using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Net;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Management;
using Microsoft.Win32;

namespace BuildHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        ApplicationViewModel ContextViewModel
        {
            get { return (ApplicationViewModel)DataContext; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void pw_passwordbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ContextViewModel.config.Tfscfg.PassWord = ((PasswordBox)e.Source).Password;
        }

        private void pw_passwordbox_Initialized(object sender, EventArgs e)
        {
            pw_passwordbox.Password = ContextViewModel.config.Tfscfg.PassWord;
        }
    }
}
