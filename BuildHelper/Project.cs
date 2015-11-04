using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace BuildHelper
{
    [Flags]
    public enum ProjectTypeEnum
    {
        None = 0,
        x86D = 1,
        x86R = 2, 
        x64D = 4, 
        x64R = 8
    }

    [XmlInclude(typeof(Project))]
    public class Project
    {
        //public VCS projectVCS = VCS.TFS; //TODO

        public string ProjectName = String.Empty;
        public string ProjectPath = String.Empty;
        public ProjectTypeEnum ProjectType = ProjectTypeEnum.None;

        [XmlElement(ElementName = "BuildTimes")]
        public List<long> buildTimes = new List<long>();

        public override string ToString()
        {
            return ProjectName;
        }

        public List<string> RebuildInfoList
        {
            get
            {
                List<string> ret = new List<string>();

                if (ProjectType.HasFlag(ProjectTypeEnum.x64D))
                    ret.Add(@"Debug|x64");
                if (ProjectType.HasFlag(ProjectTypeEnum.x64R))
                    ret.Add(@"Release|x64");
                if (ProjectType.HasFlag(ProjectTypeEnum.x86D))
                    ret.Add("Debug|" + ProjectTypeString); // C++ is win32, C# is x86
                if (ProjectType.HasFlag(ProjectTypeEnum.x86R))
                    ret.Add("Release|" + ProjectTypeString);
                return ret;
            }
        }

        string ProjectTypeString
        {
            get
            {
                bool isCSharpProj = File.ReadLines(ProjectPath).Any(line => line.Contains("csproj"));
                return isCSharpProj ? "x86" : "Win32";
            }
        }
    }
}
