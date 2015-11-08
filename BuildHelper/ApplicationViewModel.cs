using MahApps.Metro.Controls.Dialogs;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BuildHelper
{
    class ApplicationViewModel : Notifier
    {
        CfgMan _config = new CfgMan();
        public CfgMan config
        {
            get { return _config; }
            set { SetField(ref _config, value); }
        }

        public bool _BuildsLaunched = false;
        public bool BuildsLaunched
        {
            get { return _BuildsLaunched; }
            set { SetField(ref _BuildsLaunched, value); }
        }

        Queue<Process> _BuildQueue = new Queue<Process>();
        Queue<Process> BuildQueue
        {
            get
            {
                if (_BuildQueue.Count != 0)
                    return _BuildQueue;

                var processes = config.Prjcfg.
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

                _BuildQueue.Clear();
                foreach (var process in processes)
                {
                    process.Exited += ProcExited;
                    _BuildQueue.Enqueue(process);
                }
                return _BuildQueue;
            }
        }

        private void ProcExited(object sender, EventArgs e)
        {
            string projName = (sender as Process).StartInfo.Arguments;
            TimeSpan buildtime = DateTime.Now - projstarttime;
            AddBuildTime(projName, buildtime.Ticks);
            if (BuildsLaunched)
            {
                //make sure no children processes alive
                KillProcessAndChildren(BuildQueue.Peek().Id);
                BuildQueue.Dequeue();
                if (BuildQueue.Count == 0)
                {
                    timer.Stop();
                    BuildsLaunched = false;
                }
                else
                    StartBuild();
            }
        }

        public void SwitchBuild()
        {
            if (config.Prjcfg.Count == 0)
                return;

            if (BuildsLaunched)
                StopBuild();
            else
                StartBuild();
        }

        async void StartBuild()
        {
            startTime = DateTime.Now;
            timer.Start();
            BuildsLaunched = true;
            try
            {
                Process proc = BuildQueue.Peek();
                proc.Start();
                projstarttime = DateTime.Now;
                string result;
                while ((result = await proc.StandardOutput.ReadLineAsync()) != null)
                    ProcessOutput.Add(result);
            }
            catch { }
        }

        ObservableCollection<string> _ProcessOutput = new ObservableCollection<string>();
        public ObservableCollection<string> ProcessOutput
        {
            get { return _ProcessOutput; }
            set { SetField(ref _ProcessOutput, value); }
        }

        void StopBuild()
        {
            BuildsLaunched = false;
            if (BuildQueue.Count > 0)
                KillProcessAndChildren(BuildQueue.Peek().Id);
            BuildQueue.Clear();
            timer.Stop();
            ScheduleTimer.Stop();
        }

        int _SelectedIndex = -1;
        public int SelectedIndex
        {
            get { return _SelectedIndex; }
            set { SetField(ref _SelectedIndex, value); }
        }

        public void MoveItem(int direction)
        {
            // Checking selected item
            if (SelectedIndex < 0)
                return; // No selected item - nothing to do

            // Calculate new index using move direction
            int newIndex = SelectedIndex + direction;

            // Checking bounds of the range
            if (newIndex < 0 || newIndex >= config.Prjcfg.Count)
                return; // Index out of range - nothing to do

            Project selected = config.Prjcfg[SelectedIndex];
            // Removing removable element
            config.Prjcfg.Remove(selected);
            // Insert it in new position
            config.Prjcfg.Insert(newIndex, selected);
            // Restore selection
            SelectedIndex = newIndex;
        }

        void AddBuildTime(string arg, long time)
        {
            var proj = config.Prjcfg.FirstOrDefault(item => arg.Contains(item.ProjectPath));
            if (proj != null)
            {
                proj.BuildTimes.Add(time);
                config.SaveConfig();
            }
        }

        private static void KillProcessAndChildren(int pid)
        {
            ManagementObjectCollection moc = 
                new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid).Get();

            foreach (ManagementObject mo in moc)
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));

            try
            {
                Process.GetProcessById(pid).Kill();
            }
            catch { }
        }

        public DispatcherTimer timer { get; set; }
        public DispatcherTimer ScheduleTimer { get; set; }
        public DateTime startTime { get; set; }
        public DateTime projstarttime { get; set; }
        public ProgressDialogController controller { get; set; }

        public ApplicationViewModel()
        {
            config.LoadConfig();
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += (o, e) => TimeElapsed = (DateTime.Now - startTime);
            ScheduleTimer = new DispatcherTimer();
            ScheduleTimer.Tick += Scheduletimer_Tick;
        }

        TimeSpan? _TimeElapsed;
        public TimeSpan? TimeElapsed
        {
            get { return _TimeElapsed; }
            set { SetField(ref _TimeElapsed, value); }
        }

        private async void Scheduletimer_Tick(object sender, EventArgs e)
        {
            ScheduleTimer.Stop();
            ScheduleTimer.Interval = GetTriggerTimeSpan(); //set next tick timespan
            ScheduleTimer.Start();

            //fetch code option checked
            if (FetchOnLaunch == true)
            {
                string userName = config.Tfscfg.UserName;
                string userPass = config.Tfscfg.PassWord;
                string tfsPath = config.Tfscfg.TfsPath;
                string tfsWorkSpace = config.Tfscfg.TfsWorkspace;
                string requestPath = config.Tfscfg.RequestPath;

                //controller = await this.ShowProgressAsync("Please wait", "Downloading...", true);
                //GetOptions opts = GetFetchOptions();
                //await Task.Run(() => FetchCode(userName, userPass, tfsPath, tfsWorkSpace, requestPath, opts));
                await controller.CloseAsync();
            }
            //launch builds
            SwitchBuild();
        }

        bool? _FetchOnLaunch = false;
        public bool? FetchOnLaunch 
        {
            get { return _FetchOnLaunch; }
            set { SetField(ref _FetchOnLaunch, value); }
        }

        DateTime? _ScheduleTime;
        public DateTime? ScheduleTime
        {
            get { return _ScheduleTime; }
            set { SetField(ref _ScheduleTime, value); }
        }

        TimeSpan GetTriggerTimeSpan()
        {
            var sched_time = ScheduleTime.Value;
            var now = DateTime.Now;

            if (now > sched_time)
                sched_time = sched_time.AddDays(1.0);

            return sched_time - now;
        }

        private async Task FetchCode()
        {
            string userName = config.Tfscfg.UserName;
            string userPass = config.Tfscfg.PassWord;
            string tfsPath = config.Tfscfg.TfsPath;
            string tfsWorkSpace = config.Tfscfg.TfsWorkspace;
            string requestPath = config.Tfscfg.RequestPath;
            GetOptions opts = config.Tfscfg.FetchOptions;

            //controller = await this.ShowProgressAsync("Please wait", "Downloading...", false);
            
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
                ProcessOutput.Add("Fetching code failed with exception: " + ex.Message);
                return;
            }
            if (getStat == null || getStat.NumFailures > 0 || getStat.NumWarnings > 0)
            {
                ProcessOutput.Add("Errors while getting latest have occurred");
                return;
            }

            if (getStat.NumOperations == 0)
                ProcessOutput.Add("All files are up to date");
            else
                ProcessOutput.Add("Successfully downloaded code");

            await controller.CloseAsync();
        }

        private void OnGettingEvent(object sender, GettingEventArgs status)
        {
            if (status.Total == 0)
                return;
            int current = (int)status.GetType().GetProperty("Current").GetValue(status, null);
            int progress = (current / status.Total);
            //using (StreamWriter w = File.AppendText("OnGetting.txt"))
            //{
            //    w.WriteLine(progress.ToString());
            //}
            //this.Dispatcher.Invoke(() =>
            //{
            //    ContextViewModel.controller.SetProgress(progress);
            //    ContextViewModel.controller.SetMessage((progress * 100).ToString());
            //});
        }
    }
}
