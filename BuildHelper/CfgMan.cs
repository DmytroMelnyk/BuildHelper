using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace BuildHelper
{
    public class CfgMan
    {
        public List<Project> Prjcfg { get; set; }
        public TFSAccount Tfscfg { get; set; }
        private static string currentDir = Directory.GetCurrentDirectory();

        public CfgMan()
        {
            Prjcfg = new List<Project>();
            Tfscfg = new TFSAccount();
        }

        public void SaveConfig()
        {
            Serialize(Prjcfg, "config.xml");
            Serialize(Tfscfg, "tfsconfig.xml");
        }

        public void LoadConfig()
        {
            Prjcfg = Deserialize<List<Project>>("config.xml");
            Tfscfg = Deserialize<TFSAccount>("tfsconfig.xml");
        }

        private void Serialize<T>(T cfg, string path)
        {
            try
            {
                using (FileStream fs = new FileStream(currentDir + @"\" + path, FileMode.Create))
                {
                    XmlWriter writer = XmlWriter.Create(fs, new XmlWriterSettings() { Indent = true });
                    new XmlSerializer(typeof(T)).Serialize(writer, cfg);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error during saving config: " + ex.Message);
            }
        }
        private T Deserialize<T>(string path) where T: class, new()
        {
            if (File.Exists(currentDir + @"\" + path))
            {
                try
                {
                    using (FileStream fs = new FileStream(currentDir + @"\" + path, FileMode.OpenOrCreate))
                    {
                        XmlReader reader = XmlReader.Create(fs);
                        return new XmlSerializer(typeof(T)).Deserialize(reader) as T;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error during loading config: " + ex.Message);
                }
            }
            return new T();
        }
    }
}
