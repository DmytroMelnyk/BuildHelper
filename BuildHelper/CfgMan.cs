using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace BuildHelper
{
    public class CfgMan : Notifier
    {
        static string currentDir = Directory.GetCurrentDirectory();

        ObservableCollection<Project> _Prjcfg;
        public ObservableCollection<Project> Prjcfg
        {
            get { return _Prjcfg; }
            set { SetField(ref _Prjcfg, value); }
        }

        TFSAccount _Tfscfg;
        public TFSAccount Tfscfg
        {
            get { return _Tfscfg; }
            set { SetField(ref _Tfscfg, value); }
        }

        public void SaveConfig()
        {
            Serialize(Prjcfg, "config.xml");
            Serialize(Tfscfg, "tfsconfig.xml");
        }

        public void LoadConfig()
        {
            Prjcfg = Deserialize<ObservableCollection<Project>>("config.xml");
            Tfscfg = Deserialize<TFSAccount>("tfsconfig.xml");
        }

        void Serialize<T>(T cfg, string path)
        {
            try
            {
                using (FileStream fs = new FileStream(currentDir + @"\" + path, FileMode.Create))
                {
                    XmlWriter writer = XmlWriter.Create(fs, new XmlWriterSettings() { Indent = true });
                    new XmlSerializer(typeof(T)).Serialize(writer, cfg);
                }
            }
            catch { }
        }

        T Deserialize<T>(string path) where T : class, new()
        {
            if (File.Exists(currentDir + @"\" + path))
            {
                try
                {
                    using (FileStream fs = new FileStream(currentDir + @"\" + path, FileMode.Open))
                    {
                        XmlReader reader = XmlReader.Create(fs);
                        return new XmlSerializer(typeof(T)).Deserialize(reader) as T;
                    }
                }
                catch { }
            }
            return new T();
        }
    }
}
