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
    public class Project : Notifier
    {
        string _ProjectName = String.Empty;
        public string ProjectName
        {
            get { return _ProjectName; }
            set { SetField(ref _ProjectName, value); }
        }

        string _ProjectPath = String.Empty;
        public string ProjectPath
        {
            get { return _ProjectPath; }
            set { SetField(ref _ProjectPath, value); }
        }

        ProjectTypeEnum _ProjectType = ProjectTypeEnum.None;
        public bool IsX64D
        {
            get { return _ProjectType.HasFlag(ProjectTypeEnum.x64D); }
            set { SetField(ref _ProjectType, value, ProjectTypeEnum.x64D); }
        }

        public bool IsX64R
        {
            get { return _ProjectType.HasFlag(ProjectTypeEnum.x64R); }
            set { SetField(ref _ProjectType, value, ProjectTypeEnum.x64R); }
        }

        public bool IsX86D
        {
            get { return _ProjectType.HasFlag(ProjectTypeEnum.x86D); }
            set { SetField(ref _ProjectType, value, ProjectTypeEnum.x86D); }
        }

        public bool IsX86R
        {
            get { return _ProjectType.HasFlag(ProjectTypeEnum.x86R); }
            set { SetField(ref _ProjectType, value, ProjectTypeEnum.x86R); }
        }

        List<long> _BuildTimes = new List<long>();
        public List<long> BuildTimes
        {
            get { return _BuildTimes; }
            set { SetField(ref _BuildTimes, value); }
        }

        public IEnumerable<string> RebuildInfoList
        {
            get
            {
                if (_ProjectType.HasFlag(ProjectTypeEnum.x64D))
                    yield return @"Debug|x64";
                if (_ProjectType.HasFlag(ProjectTypeEnum.x64R))
                    yield return @"Release|x64";
                if (_ProjectType.HasFlag(ProjectTypeEnum.x86D))
                    yield return "Debug|" + ProjectTypeString;
                if (_ProjectType.HasFlag(ProjectTypeEnum.x86R))
                    yield return "Release|" + ProjectTypeString;
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

        public override string ToString()
        {
            return ProjectName;
        }
    }
}
