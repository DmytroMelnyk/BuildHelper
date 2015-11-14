using GalaSoft.MvvmLight.Command;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace BuildHelper
{
    class ApplicationViewModel : Notifier
    {
        private TimeSpan _MuTime;
        public TimeSpan MuTime
        {
            get { return _MuTime; }
            set { SetField(ref _MuTime, value); }
        }

        private TimeSpan _SigmaTime;
        public TimeSpan SigmaTime
        {
            get { return _SigmaTime; }
            set { SetField(ref _SigmaTime, value); }
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
            set 
            {
                SetField(ref _ScheduleTime, value);
                ((RelayCommand)RunScheduleCommand).RaiseCanExecuteChanged();
            }
        }

        TimeSpan? _TimeElapsed;
        public TimeSpan? TimeElapsed
        {
            get { return _TimeElapsed; }
            set { SetField(ref _TimeElapsed, value); }
        }

        CfgMan _config = new CfgMan();
        public CfgMan config
        {
            get { return _config; }
            set { SetField(ref _config, value); }
        }

        bool _BuildsLaunched = false;
        public bool BuildsLaunched
        {
            get { return _BuildsLaunched; }
            set { SetField(ref _BuildsLaunched, value); }
        }

        ObservableCollection<string> _ProcessOutput = new ObservableCollection<string>();
        public ObservableCollection<string> ProcessOutput
        {
            get { return _ProcessOutput; }
            set { SetField(ref _ProcessOutput, value); }
        }

        int _SelectedIndex = -1;
        public int SelectedIndex
        {
            get { return _SelectedIndex; }
            set
            {
                SetField(ref _SelectedIndex, value);
                ((RelayCommand)RemoveProjectCommand).RaiseCanExecuteChanged();
                ((RelayCommand)MoveItemUpCommand).RaiseCanExecuteChanged();
                ((RelayCommand)MoveItemDownCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CalculateStatsCommand).RaiseCanExecuteChanged();
                ((RelayCommand)SetProjectPathCommand).RaiseCanExecuteChanged();
            }
        }

        ICommand _StartStopBuildCommand;
        public ICommand StartStopBuildCommand
        {
            get
            {
                return _StartStopBuildCommand ??
                    (_StartStopBuildCommand = new RelayCommand(
                        () => StartStopBuild(),
                        () => config.Prjcfg.Count != 0
                    ));
            }
        }

        ICommand _AddProjectCommand;
        public ICommand AddProjectCommand
        {
            get
            {
                return _AddProjectCommand ??
                    (_AddProjectCommand = new RelayCommand(
                        () => AddProject()
                    ));
            }
        }

        ICommand _RemoveProjectCommand;
        public ICommand RemoveProjectCommand
        {
            get
            {
                return _RemoveProjectCommand ??
                    (_RemoveProjectCommand = new RelayCommand(
                        () => RemoveProject(),
                        () => SelectedIndex != -1
                    ));
            }
        }

        ICommand _MoveItemUpCommand;
        public ICommand MoveItemUpCommand
        {
            get
            {
                return _MoveItemUpCommand ??
                    (_MoveItemUpCommand = new RelayCommand(
                        () => MoveItem(-1),
                        () => SelectedIndex > 0
                    ));
            }
        }

        ICommand _MoveItemDownCommand;
        public ICommand MoveItemDownCommand
        {
            get
            {
                return _MoveItemDownCommand ??
                    (_MoveItemDownCommand = new RelayCommand(
                        () => MoveItem(1),
                        () => SelectedIndex >= 0 && SelectedIndex + 1 < config.Prjcfg.Count
                    ));
            }
        }

        ICommand _RunScheduleCommand;
        public ICommand RunScheduleCommand
        {
            get
            {
                return _RunScheduleCommand ??
                    (_RunScheduleCommand = new RelayCommand(
                        () => RunSchedule(),
                        () => ScheduleTime != null && ScheduleTime.Value != null
                    ));
            }
        }

        ICommand _CalculateStatsCommand;
        public ICommand CalculateStatsCommand
        {
            get
            {
                return _CalculateStatsCommand ??
                    (_CalculateStatsCommand = new RelayCommand(
                        () => CalculateStats(),
                        () => SelectedIndex != -1
                    ));
            }
        }

        ICommand _FetchCommand;
        public ICommand FetchCommand
        {
            get
            {
                return _FetchCommand ??
                    (_FetchCommand = new RelayCommand(
                        async () => await FetchAsync()
                    ));
            }
        }

        ICommand _SetProjectPathCommand;
        public ICommand SetProjectPathCommand
        {
            get
            {
                return _SetProjectPathCommand ??
                    (_SetProjectPathCommand = new RelayCommand(
                        () => config.Prjcfg[SelectedIndex].ProjectPath = DialogService.Instance.OpenFileDialog(".sln", "Solution Files |*.sln"),
                        () => SelectedIndex != -1
                    ));
            }
        }

        #region private methods
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

        GetStatus Fetch()
        {
            ICredentials myCred = new NetworkCredential(config.Tfscfg.UserName, config.Tfscfg.PassWord);
            TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(new Uri(config.Tfscfg.TfsPath), myCred);
            tfs.EnsureAuthenticated();
            VersionControlServer vcs = tfs.GetService<VersionControlServer>();
            vcs.Getting += OnGettingEvent;
            Workspace myWorkspace = vcs.GetWorkspace(config.Tfscfg.TfsWorkspace, vcs.AuthorizedUser);
            GetRequest request = new GetRequest(new ItemSpec(config.Tfscfg.RequestPath, RecursionType.Full), VersionSpec.Latest);
            return myWorkspace.Get(request, config.Tfscfg.FetchOptions);
        }

        void OnGettingEvent(object sender, GettingEventArgs status)
        {
            if (status.Total == 0)
                return;

            double current = status.GetCurrent();
            double progress = (current / status.Total);
            DialogService.Instance.UpdateProgressWindow(progress);
        }

        async Task FetchAsync()
        {
            GetStatus getStat = null;
            try
            {
                await DialogService.Instance.ShowProgressWindowAsync("Please wait", "Downloading...", false);
                getStat = await Task.Run(() => Fetch());
            }
            catch (Exception ex)
            {
                DialogService.Instance.ShowMessageBox(ex.Message, "Exception", System.Windows.MessageBoxButton.OK);
            }
            finally
            {
                DialogService.Instance.CloseProgressWindow();
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
        }

        void ProcExited(object sender, EventArgs e)
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
        
        void StartStopBuild()
        {
            if (BuildsLaunched)
                StopBuild();
            else
                StartBuild();
        }

        async void StartBuild()
        {
            try
            {
                Process proc = BuildQueue.Peek();
                startTime = DateTime.Now;
                timer.Start();
                proc.Start();
                projstarttime = DateTime.Now;
                BuildsLaunched = true;
                string result;
                while ((result = await proc.StandardOutput.ReadLineAsync()) != null)
                    ProcessOutput.Add(result);
            }
            catch (Exception ex)
            {
                DialogService.Instance.ShowMessageBox(ex.Message, "Exception", System.Windows.MessageBoxButton.OK);
            }
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
        
        void AddProject()
        {
            var project = new Project();
            bool dialogResult = DialogService.Instance.ShowDialog(project).Value;
            if (dialogResult)
            {
                config.Prjcfg.Add(project);
                SelectedIndex = config.Prjcfg.Count - 1;
                ((RelayCommand)StartStopBuildCommand).RaiseCanExecuteChanged();
            }
        }
        
        void RemoveProject()
        {
            config.Prjcfg.RemoveAt(SelectedIndex);
            ((RelayCommand)StartStopBuildCommand).RaiseCanExecuteChanged();
        }
                
        void MoveItem(int direction)
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

        static void KillProcessAndChildren(int pid)
        {
            ManagementObjectCollection moc = 
                new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid).Get();

            foreach (ManagementObject mo in moc)
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));

            try
            {
                Process.GetProcessById(pid).Kill();
            }
            catch (Exception ex)
            {
                DialogService.Instance.ShowMessageBox(ex.Message, "Exception", System.Windows.MessageBoxButton.OK);
            }
        }

        DispatcherTimer timer { get; set; }
        DispatcherTimer ScheduleTimer { get; set; }
        DateTime startTime { get; set; }
        DateTime projstarttime { get; set; }

        public ApplicationViewModel()
        {
            config.LoadConfig();
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += (o, e) => TimeElapsed = (DateTime.Now - startTime);
            ScheduleTimer = new DispatcherTimer();
            ScheduleTimer.Tick += Scheduletimer_Tick;
        }

        async void Scheduletimer_Tick(object sender, EventArgs e)
        {
            ScheduleTimer.Stop();
            ScheduleTimer.Interval = GetTriggerTimeSpan(); //set next tick timespan
            ScheduleTimer.Start();

            //fetch code option checked
            if (FetchOnLaunch == true)
                await FetchAsync();
            
            //launch builds
            StartStopBuild();
        }

        TimeSpan GetTriggerTimeSpan()
        {
            var sched_time = ScheduleTime.Value;
            var now = DateTime.Now;

            if (now > sched_time)
                sched_time = sched_time.AddDays(1.0);

            return sched_time - now;
        }

        void RunDaily()
        {
            ScheduleTimer.Interval = GetTriggerTimeSpan();
            ScheduleTimer.Start();
        }

        void RunSchedule()
        {
            if (ScheduleTimer.IsEnabled)
            {
                ScheduleTimer.Stop();
            }
            else
            {
                RunDaily();
            }
        }
        
        void CalculateStats()
        {
            Stats stats = new Stats();
            stats.Calculate(config.Prjcfg[SelectedIndex].BuildTimes);
            MuTime = new TimeSpan((long)stats.Mu);
            SigmaTime = new TimeSpan((long)stats.Sigma);
        }
        #endregion
    }
}
