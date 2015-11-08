using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildHelper
{
    class FetchEventArgs : EventArgs
    {
        public double Progress { get; private set; }
        public FetchEventArgs(double progress)
        {
            Progress = progress;
        }
    }
}
