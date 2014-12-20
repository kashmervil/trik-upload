﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private EventHandler<Renci.SshNet.Common.ShellDataEventArgs> _shellStreamHandler;

        public Uploader(string ip)
        {
            Ip = ip;
            _scpClient = new ScpClient(Ip, "root", "");
            _sshClient = new SshClient(Ip, "root", "");
            var resources = Path.GetDirectoryName(typeof(Uploader).Assembly.Location);
            _libconwrapPath = resources + @"\Resources\libconWrap.so.1.0.0";
        }

        public async Task ConnectAsync()
        {
            await Task.Run(() =>_scpClient.Connect());
            _scpClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);

            await Task.Run(() => _sshClient.Connect());
            _sshClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);
            _shellStream = _sshClient.CreateShellStream("TRIK-SHELL", 80, 24, 800, 600, 1024);
            _shellWriterStream = new StreamWriter(_shellStream) { AutoFlush = true };

            _timer.Start();
            _timer.Elapsed += KeepAlive;
            await Task.Run( () => _sshClient.RunCommand("mkdir /home/root/trik-sharp /home/root/trik-sharp/uploads /home/root/trik/scripts/trik-sharp"));
        }

        public SolutionManager SolutionManager { get; set; }

        private void KeepAlive(object sender, ElapsedEventArgs e)
        {
 	        _scpClient.SendKeepAlive();
            _sshClient.SendKeepAlive();
        }
        public async Task<bool> ReconnectAsync()
        {
            return await Task.Run(() =>
            {
                _timer.Elapsed -= KeepAlive;
                _scpClient.Disconnect();
                _scpClient.Connect();
                _sshClient.Disconnect();
                _sshClient.Connect();
                _shellStream = _sshClient.CreateShellStream("TRIK-SHELL", 80, 24, 800, 600, 1024);
                _shellWriterStream = new StreamWriter(_shellStream) { AutoFlush = true };
                _timer.Elapsed += KeepAlive;
                return true;
            });
        }
        public async Task<string> UploadActiveProjectAsync()
        {
            return await Task.Run(() => //TODO: Remove this ***
            {
                var error = "";
                var project = SolutionManager.ActiveProject;
                if (project == null)
                    throw new InvalidOperationException(
                        "Calling UploadActiveProjectAsync before setting ActiveProject property");
                try
                {
                    if (project.UploadedFiles.Count == 0)
                    {
                        _sshClient.RunCommand("mkdir " + project.FilesUploadPath + "; " + project.Script);
                        _scpClient.Upload(new FileInfo(_libconwrapPath),
                            project.FilesUploadPath + Path.GetFileName(_libconwrapPath));
                    }

                    var newFiles = Directory.GetFiles(project.ProjectLocalBuildPath);
                    var uploadedFiles = project.UploadedFiles;
                    foreach (var file in newFiles)
                    {
                        if (!uploadedFiles.ContainsKey(file))
                        {
                            uploadedFiles.Add(file, DateTime.MinValue);
                        }
                        var info = new FileInfo(file);
                        if (info.LastWriteTime <= uploadedFiles[file]) continue;
                        _scpClient.Upload(info, project.FilesUploadPath + Path.GetFileName(file));
                        uploadedFiles[file] = info.LastWriteTime;
                    }
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                }
                return error;
            });
        }

        public void RunProgram()
        {
            _shellStream.DataReceived -= _shellStreamHandler;
            _shellWriterStream.WriteLine("sh " + SolutionManager.ActiveProject.RemoteScriptName);
            _shellStream.DataReceived += _shellStreamHandler;
        }

        public Action<string> OutputAction
        {
            set
            {
                if (_shellStream != null) _shellStream.DataReceived -= _shellStreamHandler;

                _shellStreamHandler = (sender, args) => value(Encoding.UTF8.GetString(args.Data));
            }
        }

        public void StopProgram()
        {
            _sshClient.RunCommand("killall mono");
        }

        public string Ip { get; private set; }
        public void Dispose()
        {
            _sshClient.Dispose();
            _scpClient.Dispose();
            _timer.Dispose();
        }
    }

    public class SolutionManager
    {
        public SolutionManager(string name)
        {
            FullName = name;
            Projects = new List<UploadProjectInfo>();
        }
        public SolutionManager(string name, IList<string> projects)
        {
            FullName = name;
            Projects = new List<UploadProjectInfo>();
            foreach (var project in projects)
            {
                Projects.Add(new UploadProjectInfo(project));
            }
        }

        public void UpdateProjects(IList<string> newProjects)
        {
            var oldProjects = Projects.Select(x => x.ProjectFilePath).ToList();
            foreach (var i in newProjects.Except(oldProjects))
            {
                Projects.Add(new UploadProjectInfo(i));
            }
            foreach (var i in oldProjects.Except(newProjects))
            {
                Projects.RemoveAll(x => x.ProjectFilePath == i);
            }
        }
        public List<UploadProjectInfo> Projects { get; private set; }
        public string FullName { get; private set; }
        public UploadProjectInfo ActiveProject { get; set; }
    }
}
