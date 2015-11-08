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

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            pw_passwordbox.Password = ContextViewModel.config.Tfscfg.PassWord;
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ApplicationViewModel();
        }

        private void LaunchButton_OnClick(object sender, EventArgs e)
        {
            ContextViewModel.SwitchBuild();
        }

        private void createProject_button_Click(object sender, RoutedEventArgs e)
        {
            var temp = new Project();
            ContextViewModel.config.Prjcfg.Add(temp);
            ProjectListBox.SelectedItem = temp;
            ContextViewModel.config.SaveConfig();
        }

        private void removeproject_button_Click(object sender, RoutedEventArgs e)
        {
            ContextViewModel.config.Prjcfg.Remove((Project)ProjectListBox.SelectedItem);
            ContextViewModel.config.SaveConfig();
        }

        private async void FetchButton_OnClick(object sender, RoutedEventArgs e)
        {
            //await FetchCode();
        }

        private void rememberTFScfg_click(object sender, RoutedEventArgs e)
        {
            ContextViewModel.config.Tfscfg.PassWord = pw_passwordbox.Password;
            ContextViewModel.config.SaveConfig();
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
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".sln";
            dlg.Filter = "Solution Files |*.sln";

            bool? result = dlg.ShowDialog();
            if (result == true)
                Projectpath_textbox.Text = dlg.FileName;
        }

        private void RunDaily()
        {
            if (m_timepicker.Value == null)
                return;

            //ContextViewModel.ScheduleTimer.Interval = GetTriggerTimeSpan();
            //ContextViewModel.ScheduleTimer.Start();
        }

        private void runschedule_btn_Click(object sender, RoutedEventArgs e)
        {
            if (m_timepicker.Value == null)
            {
                System.Windows.MessageBox.Show("Pick scheduled time");
                return;
            }
            if (ContextViewModel.ScheduleTimer.IsEnabled)
            {
                ContextViewModel.ScheduleTimer.Stop();
                runschedule_btn.Content = "schedule";
                Launch.IsEnabled = true;
            }
            else
            {
                RunDaily();
                runschedule_btn.Content = "cancel";
                Launch.IsEnabled = false;
            }
        }

        private void calculatestats_btn_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectListBox.SelectedIndex >= 0)
            {
                Stats stats = new Stats();
                stats.Calculate((ProjectListBox.Items[ProjectListBox.SelectedIndex] as Project).BuildTimes);
                TimeSpan mutime = new TimeSpan((long)stats.Mu);
                TimeSpan sigmatime = new TimeSpan((long)stats.Sigma);
                mu_tbx.Text = mutime.ToString(@"hh':'mm':'ss");
                sigma_tbx.Text = sigmatime.ToString(@"hh':'mm':'ss");
            }
        }

        private void pw_passwordbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ContextViewModel.config.Tfscfg.PassWord = ((PasswordBox)e.Source).Password;
        }
    }
}
