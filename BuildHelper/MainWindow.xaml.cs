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
        public ProgressDialogController controller { get; set; }

        ApplicationViewModel ContextViewModel
        {
            get { return (ApplicationViewModel)DataContext; }
        }

        void ContextViewModel_Fetching(object sender, FetchEventArgs e)
        {
            controller.SetProgress(e.Progress);
            controller.SetMessage((e.Progress * 100).ToString());
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ApplicationViewModel();
            pw_passwordbox.Password = ContextViewModel.config.Tfscfg.PassWord;
            ContextViewModel.FetchBegin += async (s, e) => controller = await this.ShowProgressAsync("Please wait", "Downloading...", false);
            ContextViewModel.Fetching += ContextViewModel_Fetching;
            ContextViewModel.FetchCompleted += async (s, e) => await controller.CloseAsync();
        }

        private void LaunchButton_OnClick(object sender, EventArgs e)
        {
            ContextViewModel.SwitchBuild();
        }

        private void createProject_button_Click(object sender, RoutedEventArgs e)
        {
            ContextViewModel.AddProject();
        }

        private void removeproject_button_Click(object sender, RoutedEventArgs e)
        {
            ContextViewModel.RemoveProject();
        }

        private void rememberTFScfg_click(object sender, RoutedEventArgs e)
        {
            ContextViewModel.config.Tfscfg.PassWord = pw_passwordbox.Password;
        }

        private void On_moveup(object sender, RoutedEventArgs e)
        {
            ContextViewModel.MoveItem(-1);
        }

        private void On_movedown(object sender, RoutedEventArgs e)
        {
            ContextViewModel.MoveItem(1);
        }

        private void filedialog_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = ".sln",
                Filter = "Solution Files |*.sln"
            };

            bool? result = dlg.ShowDialog();
            if (result == true)
                Projectpath_textbox.Text = dlg.FileName;
        }

        private void runschedule_btn_Click(object sender, RoutedEventArgs e)
        {
            ContextViewModel.RunSchedule();
        }

        private void calculatestats_btn_Click(object sender, RoutedEventArgs e)
        {
            ContextViewModel.CalculateStats();
        }

        private void pw_passwordbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ContextViewModel.config.Tfscfg.PassWord = ((PasswordBox)e.Source).Password;
        }

        private async void fetchcode_button_Click(object sender, RoutedEventArgs e)
        {
            await ContextViewModel.FetchAsync();
        }
    }
}
