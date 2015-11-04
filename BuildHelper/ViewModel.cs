using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BuildHelper
{
    public enum VCS { TFS, GIT }

    class ViewModel
    {
        public bool bBuildsLaunched { get; set; }
        public CfgMan config { get; set; }
        public List<CheckBox> checkboxes { get; set; }
        public Queue<Process> BuildQueue { get; set; }
        public object locker { get; set; }
        public DispatcherTimer timer { get; set; }
        public DispatcherTimer ScheduleTimer { get; set; }
        public DateTime startTime { get; set; }
        public DateTime projstarttime { get; set; }
        public ProgressDialogController controller { get; set; }

        public ViewModel()
        {
            bBuildsLaunched = false;
            config = new CfgMan();
            checkboxes = new List<CheckBox>();
            BuildQueue = new Queue<Process>();
            locker = new object();
            timer = new DispatcherTimer();
            ScheduleTimer = new DispatcherTimer();
        }
    }
}
