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
        ViewModel ContextViewModel
        {
            get { return (ViewModel)DataContext; }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ContextViewModel.config.LoadConfig();

            foreach (var item in ContextViewModel.config.Prjcfg)
                ProjectListBox.Items.Add(item);
            //initialize field from config
            pw_passwordbox.Password = ContextViewModel.config.Tfscfg.PassWord;

            //Tag radioButtons with resolve confict option
            noautroresolve_rbx.Tag = GetOptions.NoAutoResolve;
            none_rbx.Tag = GetOptions.None;
            overwrite_rbx.Tag = GetOptions.Overwrite;

            ContextViewModel.timer.Interval = new TimeSpan(0, 0, 1);
            ContextViewModel.timer.Tick += dispatcherTimer_Tick;
            ContextViewModel.ScheduleTimer.Tick += Scheduletimer_Tick;
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            timer_label.Content = (DateTime.Now - ContextViewModel.startTime).ToString(@"hh':'mm':'ss");
        }

        private void LaunchButton_OnClick(object sender, EventArgs e)
        {
            if (ContextViewModel.config.Prjcfg.Count == 0)
            {
                System.Windows.MessageBox.Show("Config is empty");
                return;
            }
            status_progressRing.IsActive = !status_progressRing.IsActive;

            if (ContextViewModel.bBuildsLaunched)
            {
                StopBuild();
                return;
            }
            ContextViewModel.startTime = DateTime.Now;
            if (ContextViewModel.ScheduleTimer.IsEnabled == false)
                output_listbox.Items.Clear();
            Launch.Content = "Cancel builds";
            ContextViewModel.timer.Start();
            CreateBuildQueue();
            StartBuild();
        }

        private void CreateBuildQueue()
        {
            var processes = ProjectListBox.Items.Cast<Project>().
                SelectMany(proj => proj.RebuildInfoList, (proj, RebuildInfoItem) => new { proj.ProjectPath, RebuildInfoItem }).
                Select(item => new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "C:/Program Files (x86)/Microsoft Visual Studio 12.0/Common7/IDE/devenv.com",
                        Arguments = "\"" + item.ProjectPath + "\"" + @" /REBUILD " + item.RebuildInfoItem,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                });

            foreach (var process in processes)
            {
                process.Exited += ProcExited;
                ContextViewModel.BuildQueue.Enqueue(process);
            }
        }

        private void StartBuild()
        {
            ContextViewModel.bBuildsLaunched = true;

            output_listbox.Items.Add("BUILDS LAUNCHED!");

            try
            {
                ContextViewModel.BuildQueue.Peek().Start();
                ContextViewModel.projstarttime = DateTime.Now;
                Task.Run(() => ReadOutput(ContextViewModel.BuildQueue.Peek()));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to launch build:" + ex.Message);
            }
        }

        private void StopBuild()
        {
            ContextViewModel.bBuildsLaunched = false;
            if (ContextViewModel.BuildQueue.Count > 0)
                KillProcessAndChildren(ContextViewModel.BuildQueue.Peek().Id);
            ContextViewModel.BuildQueue.Clear();
            ContextViewModel.timer.Stop();
            ContextViewModel.ScheduleTimer.Stop();
            timer_label.Content = "";
            output_listbox.Items.Add("BUILDS CANCELLED!");
            Launch.Content = "Launch builds!";

        }

        private static void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        private void ReadOutput(Process proc)
        {
            string result;
            while ((result = proc.StandardOutput.ReadLine()) != null)
            {
                output_listbox.Dispatcher.Invoke(
                    delegate
                    {
                        ListViewItem li = new ListViewItem();
                        li.Content = result;
                        if (result.Contains("warning"))
                            li.Foreground = Brushes.Orange;
                        if (result.Contains("error") || result.Contains("failed") || result.Contains("unresolved") || result.Contains("not found"))
                            li.Foreground = Brushes.Red;
                        if (result.Contains("succeeded"))
                            li.Foreground = Brushes.Green;
                        output_listbox.Items.Add(li);
                        output_listbox.ScrollIntoView(li);
                    });
            }
        }

        private void ProcExited(object sender, EventArgs e)
        {
            string projName = (sender as Process).StartInfo.Arguments;
            TimeSpan buildtime = DateTime.Now - ContextViewModel.projstarttime;
            Task.Run(() => AddBuildTime(projName, buildtime.Ticks));
            if (ContextViewModel.bBuildsLaunched)
                this.Dispatcher.Invoke(() =>
                {
                    output_listbox.Items.Add(projName + ": " + buildtime.ToString(@"hh':'mm':'ss"));
                    //make sure no children processes alive
                    KillProcessAndChildren(ContextViewModel.BuildQueue.Peek().Id);
                    ContextViewModel.BuildQueue.Dequeue();
                    if (ContextViewModel.BuildQueue.Count == 0)
                    {
                        ContextViewModel.timer.Stop();
                        Launch.Content = "Launch builds!";
                        status_progressRing.IsActive = !status_progressRing.IsActive;
                        ContextViewModel.bBuildsLaunched = false;
                        Launch.IsEnabled = true;
                    }
                    else
                        StartBuild();
                });
        }

        private void AddBuildTime(string arg, long time)
        {
            var proj = ContextViewModel.config.Prjcfg.FirstOrDefault(item => arg.Contains(item.ProjectPath));
            if (proj != null)
            {
                proj.buildTimes.Add(time);
                ContextViewModel.config.SaveConfig();
            }
        }

        private GetOptions GetFetchOptions()
        {
            var rbx = Src_grid.Children.OfType<RadioButton>().FirstOrDefault(r => (bool)r.IsChecked);
            return (GetOptions)rbx.Tag;
        }

        private void FetchCode(string userName, string userPass, string tfsPath, string tfsWorkSpace, string requestPath, GetOptions opts)
        {
            GetStatus getStat = null;
            try
            {
                ICredentials myCred = new NetworkCredential(userName, userPass);
                TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(tfsPath), myCred);
                tfs.EnsureAuthenticated();
                VersionControlServer vcs = tfs.GetService<VersionControlServer>();
                Workspace myWorkspace = vcs.GetWorkspace(tfsWorkSpace, vcs.AuthorizedUser);
                vcs.Getting += OnGettingEvent;
                GetRequest request = new GetRequest(new ItemSpec(requestPath, RecursionType.Full), VersionSpec.Latest);

                getStat = myWorkspace.Get(request, opts);
            }
            catch (Exception ex)
            {
                output_listbox.Dispatcher.Invoke((Action)(() =>
                {
                    output_listbox.Items.Add("Fetching code failed with exception: " + ex.Message);
                }));
                return;
            }
            if (getStat == null || getStat.NumFailures > 0 || getStat.NumWarnings > 0)
            {
                output_listbox.Dispatcher.Invoke((Action)(() =>
                {
                    output_listbox.Items.Add("Errors while getting latest have occurred");
                }));
                return;
            }

            if (getStat.NumOperations == 0)
            {
                output_listbox.Dispatcher.Invoke((Action)(() =>
                {
                    output_listbox.Items.Add("All files are up to date");
                }));
            }
            else
                output_listbox.Dispatcher.Invoke((Action)(() =>
                {
                    output_listbox.Items.Add("Successfully downloaded code");
                }));
        }

        private void x64R_checkbox_CheckedChange(object sender, RoutedEventArgs e)
        {
            SetConfig((CheckBox)sender, ProjectTypeEnum.x64R);
        }

        private void x64D_checkbox_CheckedChange(object sender, RoutedEventArgs e)
        {
            SetConfig((CheckBox)sender, ProjectTypeEnum.x64D);
        }

        private void x86R_checkbox_CheckedChange(object sender, RoutedEventArgs e)
        {
            SetConfig((CheckBox)sender, ProjectTypeEnum.x86R);
        }

        private void x86D_checkbox_CheckedChange(object sender, RoutedEventArgs e)
        {
            SetConfig((CheckBox)sender, ProjectTypeEnum.x86D);
        }

        private void SetConfig(CheckBox checkBox, ProjectTypeEnum projectType)
        {
            if (ProjectListBox.SelectedIndex < 0)
                return;

            if (checkBox.IsChecked.Value)
                ContextViewModel.config.Prjcfg[ProjectListBox.SelectedIndex].ProjectType |= projectType;
            ContextViewModel.config.SaveConfig();
        }

        private void ProjectListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProjectListBox.SelectedIndex < 0)
                return;
            x64D_checkbox1.IsChecked = (ProjectListBox.Items[ProjectListBox.SelectedIndex] as Project).ProjectType.HasFlag(ProjectTypeEnum.x64D);
            x64R_checkbox1.IsChecked = (ProjectListBox.Items[ProjectListBox.SelectedIndex] as Project).ProjectType.HasFlag(ProjectTypeEnum.x64R);
            x86R_checkbox1.IsChecked = (ProjectListBox.Items[ProjectListBox.SelectedIndex] as Project).ProjectType.HasFlag(ProjectTypeEnum.x86R);
            x86D_checkbox1.IsChecked = (ProjectListBox.Items[ProjectListBox.SelectedIndex] as Project).ProjectType.HasFlag(ProjectTypeEnum.x86D);
        }

        private void createProject_button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(Projectname_textbox.Text))
            {
                System.Windows.MessageBox.Show("Project name is empty");
                return;
            }
            if (!File.Exists(Projectpath_textbox.Text))
            {
                System.Windows.MessageBox.Show("Project path not found");
                return;
            }

            var proj = new Project();
            proj.ProjectName = Projectname_textbox.Text;
            proj.ProjectPath = Projectpath_textbox.Text;
            ProjectListBox.Items.Add(proj);
            ContextViewModel.config.Prjcfg.Add(proj);
            ContextViewModel.config.SaveConfig();
            Projectname_textbox.Clear();
            Projectpath_textbox.Clear();
        }

        private void removeproject_button_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectListBox.SelectedIndex < 0)
                return;
            ContextViewModel.config.Prjcfg.RemoveAt(ProjectListBox.SelectedIndex);
            ProjectListBox.Items.Clear();
            foreach (var item in ContextViewModel.config.Prjcfg)
                ProjectListBox.Items.Add(item);
            ContextViewModel.config.SaveConfig();
        }

        private async void FetchButton_OnClick(object sender, RoutedEventArgs e)
        {
            Launch.IsEnabled = false;
            string userName = ContextViewModel.config.Tfscfg.UserName;
            string userPass = ContextViewModel.config.Tfscfg.PassWord;
            string tfsPath = ContextViewModel.config.Tfscfg.TfsPath;
            string tfsWorkSpace = ContextViewModel.config.Tfscfg.TfsWorkspace;
            string requestPath = ContextViewModel.config.Tfscfg.RequestPath;

            ContextViewModel.controller = await this.ShowProgressAsync("Please wait", "Downloading...", false);
            GetOptions opts = GetFetchOptions();
            await Task.Run(() => FetchCode(userName, userPass, tfsPath, tfsWorkSpace, requestPath, opts));
            Launch.IsEnabled = true;
            await ContextViewModel.controller.CloseAsync();

        }

        private void OnGettingEvent(object sender, GettingEventArgs e)
        {
            var status = (GettingEventArgs)e;
            if (e.Total == 0)
                return;
            int current = (int)status.GetType().GetProperty("Current").GetValue(status, null);
            int progress = (current / e.Total);
            using (StreamWriter w = File.AppendText("OnGetting.txt"))
            {
                w.WriteLine(progress.ToString());
            }
            this.Dispatcher.Invoke(() =>
            {
                ContextViewModel.controller.SetProgress(progress);
                ContextViewModel.controller.SetMessage((progress * 100).ToString());
            });
        }

        private void FetchCheckBox_Click(object sender, RoutedEventArgs e)
        {

        }

        private void rememberTFScfg_click(object sender, RoutedEventArgs e)
        {
            ContextViewModel.config.Tfscfg.PassWord = pw_passwordbox.Password;
            ContextViewModel.config.SaveConfig();
        }


        private void On_moveup(object sender, RoutedEventArgs e)
        {
            MoveItem(-1);
        }

        private void On_movedown(object sender, RoutedEventArgs e)
        {
            MoveItem(1);
        }

        public void MoveItem(int direction)
        {
            // Checking selected item
            if (ProjectListBox.SelectedItem == null || ProjectListBox.SelectedIndex < 0)
                return; // No selected item - nothing to do

            // Calculate new index using move direction
            int newIndex = ProjectListBox.SelectedIndex + direction;

            // Checking bounds of the range
            if (newIndex < 0 || newIndex >= ProjectListBox.Items.Count)
                return; // Index out of range - nothing to do

            object selected = ProjectListBox.SelectedItem;

            // Removing removable element
            ProjectListBox.Items.Remove(selected);
            // Insert it in new position
            ProjectListBox.Items.Insert(newIndex, selected);
            // Restore selection
            ProjectListBox.SelectedIndex = newIndex;

            ContextViewModel.config.Prjcfg.Clear();
            foreach (var item in ProjectListBox.Items)
                ContextViewModel.config.Prjcfg.Add(item as Project);
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

            ContextViewModel.ScheduleTimer.Interval = GetTriggerTimeSpan();
            ContextViewModel.ScheduleTimer.Start();
        }

        private async void Scheduletimer_Tick(object sender, EventArgs e)
        {
            ContextViewModel.ScheduleTimer.Stop();
            ContextViewModel.ScheduleTimer.Interval = GetTriggerTimeSpan(); //set next tick timespan
            ContextViewModel.ScheduleTimer.Start();

            //fetch code option checked
            if (FetchOnLaunch_checkbox.IsChecked == true)
            {
                Launch.IsEnabled = false;
                string userName = ContextViewModel.config.Tfscfg.UserName;
                string userPass = ContextViewModel.config.Tfscfg.PassWord;
                string tfsPath = ContextViewModel.config.Tfscfg.TfsPath;
                string tfsWorkSpace = ContextViewModel.config.Tfscfg.TfsWorkspace;
                string requestPath = ContextViewModel.config.Tfscfg.RequestPath;

                ContextViewModel.controller = await this.ShowProgressAsync("Please wait", "Downloading...", true);
                GetOptions opts = GetFetchOptions();
                await Task.Run(() => FetchCode(userName, userPass, tfsPath, tfsWorkSpace, requestPath, opts));
                Launch.IsEnabled = true;
                await ContextViewModel.controller.CloseAsync();
            }
            //launch builds
            LaunchButton_OnClick(sender, new RoutedEventArgs());
        }

        TimeSpan GetTriggerTimeSpan()
        {
            var sched_time = (DateTime)m_timepicker.Value;
            var now = DateTime.Now;

            if (now > sched_time)
                sched_time = sched_time.AddDays(1.0);

            var timespan = sched_time - now;
            return timespan;
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
                stats.Calculate((ProjectListBox.Items[ProjectListBox.SelectedIndex] as Project).buildTimes);
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
