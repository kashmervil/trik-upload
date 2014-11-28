using System;
using System.Collections.Generic;
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
}
