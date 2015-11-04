using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BuildHelper
{
    [XmlInclude(typeof(TFSAccount))]
    public class TFSAccount : Notifier
    {
        string _UserName = String.Empty;
        public string UserName
        {
            get { return _UserName; }
            set { SetField(ref _UserName, value); } 
        }

        string _TfsPath = String.Empty;
        public string TfsPath
        {
            get { return _TfsPath; }
            set { SetField(ref _TfsPath, value); }
        }

        string _TfsWorkspace = String.Empty;
        public string TfsWorkspace
        {
            get { return _TfsWorkspace; }
            set { SetField(ref _TfsWorkspace, value); }
        }

        string _RequestPath = String.Empty;
        public string RequestPath
        {
            get { return _RequestPath; }
            set { SetField(ref _RequestPath, value); }
        }

        string _PassWord = String.Empty;
        public string PassWord
        {
            get { return _PassWord; }
            set { SetField(ref _PassWord, value); } 
        }
    }
}
