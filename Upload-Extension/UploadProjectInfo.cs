using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Trik.Upload_Extension
{
    public class UploadProjectInfo
    {
        public UploadProjectInfo(string projectFilePath)
        {
            ProjectName = Path.GetFileNameWithoutExtension(projectFilePath);
            if (ProjectName == null)
                throw new InvalidOperationException("Unable to find a project file: " + projectFilePath);
            ProjectLocalBuildPath = Path.GetDirectoryName(projectFilePath) + @"\bin\Release\";
            if (!Directory.Exists(ProjectLocalBuildPath))
                Directory.CreateDirectory(ProjectLocalBuildPath);
            var executables =  Directory.GetFiles(ProjectLocalBuildPath).Where(x => x.EndsWith(".exe")).ToArray();
            if (executables.Length != 1) 
                throw new InvalidOperationException("Release folder must contain only one *.exe file");
            ProjectFilePath = projectFilePath;
            ExecutableFileName = Path.GetFileName(executables[0]);
            //Invalid characters replacing routine e.g "project with spaces" ==> "project\ with\ spaces" 
            //which is a valid representation for path/name with spaces in linux 
            var properName = ProjectName.Aggregate("", (acc,x) => (x == ' ')?acc + @"\ ":acc + x.ToString(CultureInfo.InvariantCulture));
            RemoteScriptName = @"/home/root/trik/scripts/trik-sharp/" + properName;
            FilesUploadPath = @"/home/root/trik-sharp/uploads/" + properName + "/";
            UploadedFiles = new Dictionary<string, DateTime>();
        }

        public string ProjectName { get; private set; }
        public string ProjectLocalBuildPath { get; private set; }
        public string ProjectFilePath { get; private set; }
        public string RemoteScriptName { get; private set; }
        public string ExecutableFileName { get; private set; }
        public string FilesUploadPath { get; private set; }
        public Dictionary<string, DateTime> UploadedFiles { get; private set; }
        public string Script
        {
            get
            {
                var text = 
                    "echo \"#!/bin/sh\n"
                    + "#killall trikGui\n"
                    + "mono " + FilesUploadPath + ExecutableFileName + " $* \n"
                    + "#cd ~/trik/\n"
                    + "#./trikGui -qws &> /dev/null &"
                    + "#some other commands\""
                    + " > " + RemoteScriptName
                    + "; chmod +x " + RemoteScriptName;
                return text;
            }
        }
    }
}