using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Renci.SshNet;
using System.IO;
using System.Timers;

namespace Trik.Upload_Extension
{
    public class Uploader : IDisposable
    {
        readonly ScpClient _scpClient;
        readonly SshClient _sshClient;
        ShellStream _shellStream;
        private StreamWriter _shellWriterStream;
        readonly Timer _timer = new Timer(5000.0);
        private readonly string _libconwrapPath;
        
        public Uploader(string ip)
        {
            _scpClient = new ScpClient(ip, "root", "");
            _scpClient.Connect();
            _scpClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);

            _sshClient = new SshClient(ip, "root", "");
            _sshClient.Connect();
            _sshClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);
            _shellStream = _sshClient.CreateShellStream("TRIK-SHELL", 80, 24, 800, 600, 1024);
            _shellWriterStream = new StreamWriter(_shellStream) { AutoFlush = true };
            
            _timer.Start();
            _timer.Elapsed += KeepAlive;
            _sshClient.RunCommand("mkdir /home/root/trik-sharp /home/root/trik-sharp/uploads /home/root/trik/scripts/trik-sharp");

            var resources = Path.GetDirectoryName(typeof(Uploader).Assembly.Location);
            _libconwrapPath = resources + @"\Resources\libconWrap.so.1.0.0";
        }

        public SolutionManager SolutionManager { get; set; }

        private void KeepAlive(object sender, ElapsedEventArgs e)
        {
 	        _scpClient.SendKeepAlive();
            _sshClient.SendKeepAlive();
        }
        public void Reconnect()
        {
            _timer.Elapsed -= KeepAlive;
            _scpClient.Disconnect();
            _scpClient.Connect();
            _sshClient.Disconnect();
            _sshClient.Connect();
            _shellStream = _sshClient.CreateShellStream("TRIK-SHELL", 80, 24, 800, 600, 1024);
            _shellWriterStream = new StreamWriter(_shellStream) { AutoFlush = true };
            _timer.Elapsed += KeepAlive;
        }
        public void UploadActiveProject() 
        {
            if (SolutionManager.ActiveProject == null)
                throw new InvalidOperationException("Calling UploadActiveProject before setting ActiveProject property");

            var newFiles = Directory.GetFiles(SolutionManager.ActiveProject.ProjectLocalBuildPath);
            var uploadedFiles = SolutionManager.ActiveProject.UploadedFiles;
            foreach (var file in newFiles)
            {
                if (!uploadedFiles.ContainsKey(file))
                {
                        uploadedFiles.Add(file, DateTime.MinValue);
                }
                var info = new FileInfo(file);
                if (info.LastWriteTime <= uploadedFiles[file]) continue;
                _scpClient.Upload(info, SolutionManager.ActiveProject.FilesUploadPath + Path.GetFileName(file));
                uploadedFiles[file] = info.LastWriteTime;
            }
        }

        public ShellStream RunProgram()
        {
            _shellWriterStream.WriteLine("." + SolutionManager.ActiveProject.RemoteScriptName);    
            return _shellStream;
        }

        public void StopProgram()
        {
            _sshClient.RunCommand("killall mono");
        }

        public string ActiveProject
        {
            get { return SolutionManager.ActiveProject.ProjectFilePath; }
            set
            {
                var project = SolutionManager.Projects.Find(x => x.ProjectFilePath == value);
                if (project != null)
                {
                    SolutionManager.ActiveProject = project;
                    return;
                }

                var newProject = new UploadProjectInfo(value);
                _sshClient.RunCommand("mkdir " + newProject.FilesUploadPath + "; " + newProject.Script);

                _scpClient.Upload(new FileInfo(_libconwrapPath), newProject.FilesUploadPath + Path.GetFileName(_libconwrapPath));
                SolutionManager.Projects.Add(newProject);
                SolutionManager.ActiveProject = newProject;
            }
        }

        public void Dispose()
        {
            _sshClient.Dispose();
            _scpClient.Dispose();
            _timer.Dispose();
        }
    }

    public class SolutionManager
    {
        public SolutionManager()
        {
            Projects = new List<UploadProjectInfo>();
        }
        public SolutionManager(IList<string> projects)
        {
            Projects = new List<UploadProjectInfo>();
            foreach (var project in projects)
            {
                Projects.Add(new UploadProjectInfo(project));
            }
        }
        public List<UploadProjectInfo> Projects { get; private set; }

        public UploadProjectInfo ActiveProject { get; set; }
    }

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
